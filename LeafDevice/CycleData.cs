using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.IoT;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.IoT.Samples
{
    internal class CycleData
    {
        private Dictionary<string, object> Columns = new Dictionary<string, object>();
        
        public string Message => JsonConvert.SerializeObject(Columns);

        public CycleData(string[] cycleDataRow)
        {
            if (cycleDataRow.Length != 26)
            {
                throw new ArgumentOutOfRangeException(nameof(cycleDataRow), $"Expected 26 columns, but got {cycleDataRow.Length}");
            }

            if (!int.TryParse(cycleDataRow[0], out int device))
            {
                throw new ApplicationException("The device id is invalid");
            }
            Columns.Add("DeviceId", device);

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