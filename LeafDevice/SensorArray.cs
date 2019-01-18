using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.IoT.Samples
{
    internal class SensorArray
    {
        const string FileNameTemplate = "test_FD00{0}.txt";

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
            return File.ReadLines(dataSetFileName)
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
            Random rnd = new Random();
            int deviceId = rnd.Next(1, 101);
            return deviceId.ToString();
        }

        private string PickRandomDataFile()
        {
            string dataFilePath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Random rnd = new Random();
            int dataSet = rnd.Next(1, 4);
            string fileName = String.Format(FileNameTemplate, dataSet);
            return Path.Join(dataFilePath, fileName);
        }
    }
}