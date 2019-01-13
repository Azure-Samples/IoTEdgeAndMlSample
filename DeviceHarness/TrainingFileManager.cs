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

namespace DeviceHarness
{
    /// <summary>
    /// Class for reading and managing the dataset file used in a run
    /// </summary>
    public class TrainingFileManager
    {
        private readonly string dataFileMask = "train_{0}.txt";
        private readonly string dataFileFullPath;
        private int maxDeviceId = 0;
        private int totalMessages = 0;

        /// <summary>
        /// Public constructor takes a dataset identifier e.g. "FD003", derives
        /// the data set name and returns a TrainingFileManager for working with the data 
        /// </summary>
        /// <param name="dataSetIdentifier">Unique portion of the dataset to be loaded must be one of 
        /// "FD001", "FD002", "FD003", "FD004"</param>
        public TrainingFileManager(string dataSetIdentifier)
        {
            string fileName = String.Format(dataFileMask, dataSetIdentifier);
            string workDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            dataFileFullPath = Path.Combine(workDir, fileName);
        }

        /// <summary>
        /// Total number of rows in the dataset which will be sent to IoT Hub
        /// </summary>
        /// <value>Integer representing the total lines of data in a dataset</value>
        public int TotalMessages
        {
            get
            {
                if (totalMessages == 0)
                {
                    totalMessages = File.ReadLines(dataFileFullPath).Count();
                }
                return totalMessages;
            }
        }

        /// <summary>
        /// The highest numbered data series in the dataset, which represents the
        /// largest device id for the set
        /// </summary>
        /// <value>Integer representing the largest device id in the dataset</value>
        public int MaxDeviceId
        {
            get
            {
                if (maxDeviceId == 0)
                {
                    string lastRow = File.ReadLines(dataFileFullPath).Last();
                    maxDeviceId = int.Parse(lastRow.Substring(0, lastRow.IndexOf(' ')));
                }
                return maxDeviceId;
            }
        }

        /// <summary>
        /// Read all the data in the dataset for the given device and return it as a 
        /// List of type CycleData.
        /// </summary>
        /// <param name="deviceId">Integer representing the device in the data series</param>
        /// <returns>List<CycleData> representing all of the data for the given device in the dataset</returns>
        public List<CycleData> ReadDeviceData(int deviceId)
        {
            string devicePrefix = $"{deviceId} ";

            return File.ReadLines(dataFileFullPath)
                .Where(line => line.StartsWith(devicePrefix))
                .Select(row =>
                    {
                        string[] columns = row.TrimEnd().Split(' ');
                        return new CycleData(columns);
                    })
                .ToList();
        }
    }
}