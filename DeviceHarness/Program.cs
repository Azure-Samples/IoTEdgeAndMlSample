﻿//*********************************************************
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
        private static List<Task> deviceTasks = new List<Task>();
        private static CommandLineApplication app = new CommandLineApplication();
        private static CommandOption connectionStringOption;
        private static CommandOption trainingSetOption;

        /// <summary>
        /// Main entry point for the device harness. If args are not passed default data sett FD003
        /// is used and program prompts for IoT Hub connection string
        /// </summary>
        /// <param name="args">-x "<your connection string>" -t "FDOO3"</param>
        static void Main(string[] args)
        {
            InitializeApp();
            app.OnExecute(() =>
            {
                string iotHubConnectionString = GetConnectionString();
                string trainingSet = GetTrainingSet();

                var fileManager = new TrainingFileManager(trainingSet);
                int maxDevice = fileManager.MaxDeviceId;

                for (int i = 1; i <= maxDevice; i++)
                {
                    var device = new TurbofanDevice(i, iotHubConnectionString, fileManager);
                    devices.Add(device);
                    deviceTasks.Add(device.RunDeviceAsync());
                }

                Task.WhenAll(deviceTasks).Wait();

                int totalMessages = 0;
                foreach (var dev in devices)
                {
                    totalMessages += dev.MessagesSent;
                }

                if (fileManager.TotalMessages != totalMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Sent a total of {totalMessages} to IotHub, but should have sent {fileManager.TotalMessages}");
                }

                return 0;
            });

            app.Execute(args);
        }

        /// <summary>
        /// Checks for the presence of a trainingSetOption. If not found, the method returns the default
        /// training set name ("FD003"). If trainingSetOption is passed, the method validates the value
        /// </summary>
        /// <returns>String representing the name of the training set to be used</returns>
        private static string GetTrainingSet()
        {
            var correctSet = new List<string> { "FD001", "FD002", "FD003", "FD004" };
            string setName = String.IsNullOrWhiteSpace(trainingSetOption.Value())
                ? "FD003" //default to FD003
                : trainingSetOption.Value();

            if (!correctSet.Contains(setName))
            {
                app.ShowHelp();
                throw new ArgumentOutOfRangeException("Invalid value for training set");
            }

            return setName;
        }

        /// <summary>
        /// Retrieves the value of the connection string from the connectionStringOption. 
        /// If the connection string wasn't passed method prompts for the connection string.
        /// </summary>
        /// <returns></returns>
        private static string GetConnectionString()
        {
            string connectionString = connectionStringOption.Value();
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
                "IoT Hub Connection String e.g HostName=hubname.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=xxxxxx;",
                CommandOptionType.SingleValue);

            trainingSetOption = app.Option(
                "-t|--trainingSet",
                "The name of the data set to send to the IoT Hub. Must be in the set [FD001, FD002, FD003, FD004]",
                CommandOptionType.SingleValue);
        }


    }
}
