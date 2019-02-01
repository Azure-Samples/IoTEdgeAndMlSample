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
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Azure.IoT.Samples
{
    public class Program
    {
        private const int cycleTimeSeconds = 1;

        private static string connectionString;
        private static CommandLineApplication app = new CommandLineApplication();
        private static CommandOption connectionStringOption;
        private static CommandOption certificateOption;

        private static void Main(string[] args)
        {
            InitializeApp();

            app.OnExecute(() =>
            {
                GetConnectionString();
                InstallCertificate();
                SensorArray sensorArray = new SensorArray();
                DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

                foreach (var cycle in sensorArray.Cycles)
                {
                    SendEvent(deviceClient, cycle.Message).Wait();
                }
                return 0;
            });

            app.Execute(args);
        }

        private static void InstallCertificate()
        {
            string certificatePath;
            if (!certificateOption.HasValue())
            {
                certificatePath = Environment.GetEnvironmentVariable("CA_CERTIFICATE_PATH");
            }
            else
            {
                certificatePath = certificateOption.Value();
            }

            if (!String.IsNullOrWhiteSpace(certificatePath))
            {
                CertificateManager.InstallCACert(certificatePath);
            }
        }

        private static void InitializeApp()
        {
            app.Name = "LeafDevice";
            app.Description = "Demo leaf device";

            app.HelpOption("-?|-h|--help");

            connectionStringOption = app.Option(
                "-x|--connection",
                "Device connection string e.g. HostName=yourHub.azure-devices.net;DeviceId=yourDevice;SharedAccessKey=XXXYYYZZZ=;GatewayHostName=mygateway.contoso.com",
                CommandOptionType.SingleValue);

            certificateOption = app.Option(
                "-c|--certificate",
                "Certificate with root CA in PEM format",
                CommandOptionType.SingleValue);
        }

        private static void GetConnectionString()
        {
            if (!connectionStringOption.HasValue())
            {
                connectionString = Environment.GetEnvironmentVariable("DEVICE_CONNECTION_STRING");
                app.ShowHint();
            }
            else
            {
                connectionString = connectionStringOption.Value();
            }

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                app.ShowHint();
                throw new ArgumentException();
            }

            Console.WriteLine($"Using connection string: {connectionString}");
        }

        private static async Task SendEvent(DeviceClient deviceClient, string message)
        {
            Console.WriteLine($"{DateTime.Now} > Sending message: {message}");
            Message eventMessage = new Message(Encoding.ASCII.GetBytes(message));
            await deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(cycleTimeSeconds)).ConfigureAwait(false);
        }
    }
}