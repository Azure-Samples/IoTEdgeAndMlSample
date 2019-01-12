using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DeviceHarness
{
    public class TrainingFileManager
    {
        private readonly string dataFileMask = "train_{0}.txt";
        private readonly string dataFileFullPath;

        private int maxDeviceId = 0;
        private int totalMessages = 0;

        public TrainingFileManager(string fileIdentifier)
        {
            string fileName = String.Format(dataFileMask, fileIdentifier);
            string workDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            dataFileFullPath = Path.Combine(workDir, fileName);
        }

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