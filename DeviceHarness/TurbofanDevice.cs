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

using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Message = Microsoft.Azure.Devices.Client.Message;

namespace DeviceHarness
{
    /// <summary>
    /// Simulates the physical device connected to the IoT Hub.
    /// </summary>
    public class TurbofanDevice
    {
        private static readonly TimeSpan cycleTime = TimeSpan.FromMilliseconds(1);
        private readonly int deviceUnitNumber;
        private readonly string deviceConnectionString;
        private readonly TrainingFileManager trainingFileManager;
        private int messagesSent = 0;

        /// <summary>
        /// The total number of messages sent by this device to IoT Hub
        /// </summary>
        public int MessagesSent => messagesSent;

        /// <summary>
        /// Public constructor for TurbofanDevice class.
        /// </summary>
        /// <param name="deviceNumber">Integer number of the data series device to send</param>
        /// <param name="iotHubConnectionString">Connection string for the IoT Hub in which to create the device</param>
        /// <param name="fileManager">TrainingFileManager for the dataset to be read and sent</param>
        public TurbofanDevice(int deviceNumber, string iotHubConnectionString, TrainingFileManager fileManager, string gatewayFqdn = null)
        {
            deviceUnitNumber = deviceNumber;
            string deviceId = $"Client_{deviceNumber:000}";
            deviceConnectionString = GetIotHubDevice(iotHubConnectionString, deviceId, gatewayFqdn).Result;
            trainingFileManager = fileManager;
        }

        /// <summary>
        /// Creates an asynchronous task for sending all data for the device.
        /// </summary>
        /// <returns>Task for asynchronous device operation</returns>
        public async Task RunDeviceAsync()
        {
            List<CycleData> cycleData = LoadDeviceData();
            await SendDataToHub(cycleData).ConfigureAwait(false);
        }

        /// <summary>
        /// Takes a set of CycleData for a device in a dataset and sends the
        /// data to the message with a configurable delay between each message
        /// </summary>
        /// <param name="cycleData">The set of data to send as messages to the IoT Hub</param>
        /// <returns></returns>
        private async Task SendDataToHub(List<CycleData> cycleData)
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString);
            foreach (CycleData c in cycleData)
            {
                await SendEvent(deviceClient, c.Message).ConfigureAwait(false);
                await Task.Delay(cycleTime).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Uses the IoT Hub connection string to try to retrieve the given device ID. If the
        /// device does not exist in the IoT Hub the method creates the device in the IoT Hub
        /// </summary>
        /// <param name="iotHubConnectionString">Connection string for the IoT Hub</param>
        /// <param name="deviceId">Name of the device in the IoT Hub e.g Device_001</param>
        private async Task<string> GetIotHubDevice(string iotHubConnectionString, string deviceId, string gatewayFqdn = null)
        {
            RegistryManager regManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            string hostname = Microsoft.Azure.Devices.IotHubConnectionStringBuilder.Create(iotHubConnectionString).HostName;

            Device device = await regManager.GetDeviceAsync(deviceId).ConfigureAwait(false);
            if (device == null)
            {
                Console.WriteLine($"Creating new IoT device: {deviceId}");
                device = await regManager.AddDeviceAsync(new Device(deviceId)).ConfigureAwait(false);
            }
            Console.WriteLine($"Found existing device: {device.Id}");

            string connectionString = $"HostName={hostname};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";

            if (!String.IsNullOrWhiteSpace(gatewayFqdn))
            {
                connectionString = $"{connectionString};GatewayHostName={gatewayFqdn.ToLower()}";
            }
            Console.WriteLine($"Using device connection string: {connectionString}");
            
            return connectionString;
        }

        /// <summary>
        ///  Using the trainingFileManager for this instance read the device data for this device
        /// </summary>
        /// <returns>List representing all cycle data for this device</returns>
        private List<CycleData> LoadDeviceData()
        {
            return trainingFileManager.ReadDeviceData(deviceUnitNumber);
        }
        
        /// <summary>
        /// Uses the DeviceClient to send a message to the IoT Hub
        /// </summary>
        /// <param name="deviceClient">Azure Devices client for connecting to and send data to IoT Hub</param>
        /// <param name="message">JSON string representing serialized device data</param>
        /// <returns>Task for async execution</returns>
        private async Task SendEvent(DeviceClient deviceClient, string message)
        {
            using (Message eventMessage = new Message(Encoding.UTF8.GetBytes(message)))
            {
                // Set the content type and encoding so the IoT Hub knows to treat the message body as JSON
                eventMessage.ContentEncoding = "utf-8";
                eventMessage.ContentType = "application/json";

                await deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);

                // keep track of messages sent and update progress periodically
                int currCount = Interlocked.Increment(ref messagesSent);
                if (currCount % 50 == 0)
                {
                    Console.WriteLine($"Device: {deviceUnitNumber} Message count: {currCount}");
                }
            }
        }
    }
}