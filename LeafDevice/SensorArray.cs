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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.IoT.Samples
{
    internal class SensorArray
    {
        private readonly Random rnd = new Random();
        private const string fileNameTemplate = "test_FD00{0}.txt";

        public List<CycleData> Cycles;

        public SensorArray()
        {
            LoadDeviceData();
        }

        private void LoadDeviceData()
        {
            string dataSetFileName = PickRandomDataFile();
            string deviceId = GetRandomDevice();

            if (File.Exists(dataSetFileName))
            {
                Cycles = ReadDeviceData(dataSetFileName, deviceId);
            }
        }

        private List<CycleData> ReadDeviceData(string dataSetFileName, string deviceId)
        {
            return File
                .ReadLines(dataSetFileName)
                .Where(line => line.StartsWith(deviceId + " "))
                .Select(row =>
                    {
                        string[] columns = row.TrimEnd().Split(' ');
                        return new CycleData(columns);
                    })
                .ToList();
        }

        private string GetRandomDevice()
        {
            int deviceId = rnd.Next(1, 101);
            return deviceId.ToString();
        }

        private string PickRandomDataFile()
        {
            string dataFilePath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            int dataSet = rnd.Next(1, 4);
            string fileName = String.Format(fileNameTemplate, dataSet);
            return Path.Join(dataFilePath, fileName);
        }
    }
}