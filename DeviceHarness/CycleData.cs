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
using System.Text;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;

namespace DeviceHarness
{
    /// <summary>
    /// Class to parse and model a single row of device data found in the
    /// datasets.
    /// </summary>
    public class CycleData
    {
        private readonly Dictionary<string, object> Columns = new Dictionary<string, object>();

        /// <summary>
        ///  Serializes the CycleData into a message in JSON format to be sent to IoT Hub
        /// </summary>
        /// <returns>JSON serialized message to send to IoT Hub</returns>
        public string Message => JsonConvert.SerializeObject(Columns);

        /// <summary>
        /// Public constructor takes an array of string representing a row of 
        /// data from a data set file
        /// </summary>
        /// <param name="cycleDataRow">An array of string representing a row of data from a dataset</param>
        public CycleData(string[] cycleDataRow)
        {
            if (cycleDataRow.Length != 26)
            {
                throw new ArgumentOutOfRangeException("cycleDataRow", $"Expected 26 columns, but got {cycleDataRow.Length}");
            }

            if (!int.TryParse(cycleDataRow[1], out int cycle))
            {
                throw new ApplicationException("The cycle time is invalid");
            }

            Columns.Add("CycleTime", cycle);

            for (int i = 2; i < cycleDataRow.Length; i++)
            {
                float.TryParse(cycleDataRow[i], out float columnValue);
                if (i <= 4) //columns 3-5 are operational settings
                {
                    Columns.Add($"OperationalSetting{i - 1}", columnValue);
                }
                else //remaining columns are sensor readings
                {
                    Columns.Add($"Sensor{i - 4}", columnValue);
                }
            }
        }
    }
}