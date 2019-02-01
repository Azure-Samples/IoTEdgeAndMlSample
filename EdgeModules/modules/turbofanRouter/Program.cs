//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace turbofanRouter
{
    //TODO: make the log level settable via the hub

    internal class EndpointNames
    {
        public const string ToClassifierModule = "classOutput";
        public const string ToIotHub = "hubOutput";
        public const string ToDeadMessage = "deadMessages";
        public const string ToAvroWriter = "avroOutput";
        public const string FromClassifier = "rulInput";
        public const string FromLeafDevice = "deviceInput";
    }

    public class Program
    {
        static int deviceMessageCounter;
        static int classifierCallbackCounter;
        static ReaderWriterLockSlim readWriteLock = new ReaderWriterLockSlim();

        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        internal static async Task Init()
        {
            Logger.LoggingLevel = LogSeverity.Verbose;

            AmqpTransportSettings amqpSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { amqpSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient
                .CreateFromEnvironmentAsync(settings)
                .ConfigureAwait(false);
            await ioTHubModuleClient.OpenAsync().ConfigureAwait(false);
            Console.WriteLine("IoT Hub module client initialized.");

            // Register callbacks for messages to the module
            await ioTHubModuleClient
                .SetInputMessageHandlerAsync(EndpointNames.FromLeafDevice, LeafDeviceInputMessageHandler, ioTHubModuleClient)
                .ConfigureAwait(false);
            await ioTHubModuleClient
                .SetInputMessageHandlerAsync(EndpointNames.FromClassifier, ClassifierCallbackMessageHandler, ioTHubModuleClient)
                .ConfigureAwait(false);

            // Register a callback for updates to the module twin's desired properties.
            await ioTHubModuleClient
                .SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null)
                .ConfigureAwait(false);
        }

        internal static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Logger.Log("Desired property change:");
                Logger.Log(JsonConvert.SerializeObject(desiredProperties));

                LogSeverity newSeverity = Logger.LoggingLevel;

                if (desiredProperties["LoggingLevel"] != null
                    && Enum.TryParse<LogSeverity>(desiredProperties["LoggingLevel"], out newSeverity))
                {
                    Logger.LoggingLevel = newSeverity;
                }
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Logger.Log($"Error when receiving desired property: {exception}", LogSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error when receiving desired property: {ex.Message}", LogSeverity.Error);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles messages coming back from the classifier corresponds with route
        /// "FROM /messages/modules/classifier/outputs/amloutput INTO BrokeredEndpoint(\"/modules/turbofanRouter/inputs/rulInput\")"
        /// </summary>
        internal static async Task<MessageResponse> ClassifierCallbackMessageHandler(Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref classifierCallbackCounter);
            Logger.Log($"Received message on {EndpointNames.FromClassifier}; Count: {counterValue}");

            var moduleClient = GetClientFromContext(userContext);
            var fanMessage = new TurbofanMessage(message);
            if (!fanMessage.HasRemainingRul)
            {
                Logger.Log($"Endpoint {EndpointNames.FromClassifier}: not classified, stop processing", LogSeverity.Error);
                return await HandleBadMessage(message, moduleClient).ConfigureAwait(false);
            }

            await SendRulMessageToIotHub(moduleClient, fanMessage).ConfigureAwait(false);

            await SendMessageToAvroWriter(moduleClient, fanMessage).ConfigureAwait(false);

            return MessageResponse.Completed;
        }

        /// <summary>
        /// Method to handle messages coming from a leaf device corresponds with route
        /// "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO BrokeredEndpoint(\"/modules/turbofanRouter/inputs/deviceInput\")"
        /// </summary>
        internal static async Task<MessageResponse> LeafDeviceInputMessageHandler(Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref deviceMessageCounter);
            Logger.Log($"Received message on {EndpointNames.FromLeafDevice}; Count: {counterValue}");

            try
            {
                var fanMessage = new TurbofanMessage(message);
                var moduleClient = GetClientFromContext(userContext);

                if (fanMessage.HasRemainingRul)
                {
                    Logger.Log($"Endpoint {EndpointNames.FromLeafDevice}: already classified, stop processing.", LogSeverity.Warning);
                    return await HandleBadMessage(message, moduleClient).ConfigureAwait(false);
                }

                await SendMessageToClassifier(moduleClient, fanMessage).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"LeafDeviceInputMessageHandler got exception {ex.Message}", LogSeverity.Error);
            }

            return MessageResponse.Completed;
        }

        private static async Task SendMessageToClassifier(ModuleClient moduleClient, TurbofanMessage fanMessage)
        {
            Message classifierMessage = fanMessage.CreateClassifierMessage();
            await moduleClient
                .SendEventAsync(EndpointNames.ToClassifierModule, classifierMessage)
                .ConfigureAwait(false);
            Logger.Log($"Sent message to classifier on {EndpointNames.ToClassifierModule} for DeviceId: {fanMessage.DeviceId} CycleTime: {fanMessage.Message["CycleTime"]}");
        }

        private static async Task SendRulMessageToIotHub(ModuleClient moduleClient, TurbofanMessage fanMessage)
        {
            Message rulMessage = fanMessage.CreateRemainingLifeMessage();
            await moduleClient
                .SendEventAsync(EndpointNames.ToIotHub, rulMessage)
                .ConfigureAwait(false);
            Logger.Log($"Sent rul message to {EndpointNames.ToIotHub} for DeviceId: {fanMessage.DeviceId} CycleTime: {fanMessage.Message["CycleTime"]}");
        }

        private static async Task SendMessageToAvroWriter(ModuleClient moduleClient, TurbofanMessage fanMessage)
        {
            Message avroMessage = fanMessage.CreateAvroWriterMessage();
            await moduleClient
                .SendEventAsync(EndpointNames.ToAvroWriter, avroMessage)
                .ConfigureAwait(false);
            Logger.Log($"Sent avro message to {EndpointNames.ToAvroWriter} for DeviceId: {fanMessage.DeviceId} CycleTime: {fanMessage.Message["CycleTime"]}");
        }

        static ModuleClient GetClientFromContext(object userContext)
        {
            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new ArgumentException($"Could not cast userContext. Expected {typeof(ModuleClient)} but got: {userContext.GetType()}");
            }
            return moduleClient;
        }

        private static async Task<MessageResponse> HandleBadMessage(Message message, ModuleClient moduleClient)
        {
            message.Properties.Add("DeadLetter", "true");
            await moduleClient
                .SendEventAsync(EndpointNames.ToDeadMessage, message)
                .ConfigureAwait(false);
                
            return MessageResponse.Completed;
        }
    }
}
