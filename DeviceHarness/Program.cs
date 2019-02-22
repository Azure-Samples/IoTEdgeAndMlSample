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

using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeviceHarness
{
    class Program
    {
        private static List<TurbofanDevice> devices = new List<TurbofanDevice>();
        private static CommandLineApplication app = new CommandLineApplication();
        private static CommandOption connectionStringOption;
        private static CommandOption dataSetOption;
        private static CommandOption certificateOption;
        private static CommandOption maxDevicesOption;
        private static CommandOption gatewayHostNameOption;


        /// <summary>
        /// Main entry point for the device harness. If args are not passed default data set FD003
        /// is used and program prompts for IoT Hub connection string
        /// </summary>
        /// <param name="args">-x "hub connection string" -d "FDOO3" -c "certificate path" -m "maximum # devices"</param>
        static void Main(string[] args)
        {
            InitializeApp();
            app.OnExecute(() =>
            {
                InstallCertificate();

                string trainingSet = GetDataSet();
                var fileManager = new TrainingFileManager(trainingSet);

                List<Task> deviceRunTasks = SetupDeviceRunTasks(fileManager);
                Task.WhenAll(deviceRunTasks).Wait();

                return 0;
            });

            app.Execute(args);
        }

        /// <summary>
        /// Looks for a certificate either passed as a parameter or in the CA_CERTIFICATE_PATH
        /// environment variable and, if present, attempts to install the certificate
        /// </summary>
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

        /// <summary>
        /// Checks for the presence of a dataSetOption. If not found, the method returns the default
        /// data set name ("FD003"). If dataSetOption is passed, the method validates the value
        /// </summary>
        /// <returns>String representing the name of the data set to be used</returns>
        private static string GetDataSet()
        {
            var correctSet = new List<string> { "FD001", "FD002", "FD003", "FD004" };
            string setName = String.IsNullOrWhiteSpace(dataSetOption.Value())
                ? "FD003" //default to FD003
                : dataSetOption.Value();

            if (!correctSet.Contains(setName))
            {
                app.ShowHelp();
                throw new ArgumentOutOfRangeException("Invalid value for training set");
            }

            return setName;
        }

        /// <summary>
        /// Creates the set of tasks that will send data to the IoT hub.
        /// </summary>
        /// <param name="fileManager">TrainingFileManager for the data set for devices to send.</param>
        /// <returns></returns>
        private static List<Task> SetupDeviceRunTasks(TrainingFileManager fileManager)
        {
            List<Task> deviceTasks = new List<Task>();

            string iotHubConnectionString = GetConnectionString();
            int maxDevice = GetMaxDevice(fileManager);
            string gatewayHost = gatewayHostNameOption.HasValue() ? gatewayHostNameOption.Value() : null;

            for (int i = 1; i <= maxDevice; i++)
            {
                var device = new TurbofanDevice(i, iotHubConnectionString, fileManager, gatewayHost);
                devices.Add(device);
                deviceTasks.Add(device.RunDeviceAsync());
            }

            return deviceTasks;
        }

        /// <summary>
        /// Return the number of devices for which to send, which will be the lower
        /// of the number of devices in the data set and the value of the passed in max devices
        /// </summary>
        /// <param name="filemanager">TrainingFileManager for the data set for devices to send.</param>
        /// <returns></returns>
        private static int GetMaxDevice(TrainingFileManager filemanager)
        {
            int maxDevices = filemanager.MaxDeviceId;

            if (!maxDevicesOption.HasValue())
            {
                return maxDevices;
            }

            int.TryParse(maxDevicesOption.Value(), out int maxRequested);

            if (maxRequested == 0)
            {
                return maxDevices;
            }

            return Math.Min(maxRequested, maxDevices);
        }

        /// <summary>
        /// Retrieves the value of the connection string from the connectionStringOption. 
        /// If the connection string wasn't passed method prompts for the connection string.
        /// </summary>
        /// <returns></returns>
        private static string GetConnectionString()
        {
            string connectionString;

            if (!connectionStringOption.HasValue())
            {
                connectionString = Environment.GetEnvironmentVariable("DEVICE_CONNECTION_STRING");
                app.ShowHint();
            }
            else
            {
                connectionString = connectionStringOption.Value();
            }

            while (String.IsNullOrWhiteSpace(connectionString))
            {
                Console.Write("IoT Hub Connection String:");
                connectionString = Console.ReadLine();
            }

            Console.WriteLine($"Using connection string: {connectionString}");
            return connectionString;
        }

        /// <summary>
        /// Initializes the instance of the CommandLineApplication
        /// </summary>
        private static void InitializeApp()
        {
            app.Name = "DeviceHarness";
            app.Description = "Device data generation";

            app.HelpOption("-?|-h|--help");

            connectionStringOption = app.Option(
                "-x|--connection",
                @"IoT Hub Connection String e.g HostName=hubname.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=xxxxxx;",
                CommandOptionType.SingleValue);

            dataSetOption = app.Option(
                "-d|--data-set",
                "The name of the data set to send to the IoT Hub. Must be in the set [FD001, FD002, FD003, FD004]",
                CommandOptionType.SingleValue);

            certificateOption = app.Option(
                "-c|--certificate",
                "Certificate with root CA in PEM format",
                 CommandOptionType.SingleValue);

            maxDevicesOption = app.Option(
               "-m|--max-devices",
               "Maximum number of devices to simulate. If value exceeds number of devices in the data set it will be ignored.",
                CommandOptionType.SingleValue);

            gatewayHostNameOption = app.Option(
               "-g|--gateway-host-name",
               "Fully qualified domain name of the edge device acting as a gateway. e.g. iotedge-xxx.westus2.cloudapp.azure.com ",
                CommandOptionType.SingleValue);
        }


    }
}
