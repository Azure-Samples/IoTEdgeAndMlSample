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
    public class TurbofanDevice
    {
        private static readonly TimeSpan CycleTime = TimeSpan.FromSeconds(1);
        
        private readonly int deviceUnitNumber;
        private readonly string deviceConnectionString;
        private readonly  TrainingFileManager trainingFileManager;
        
        private int messagesSent = 0;

        public int MessagesSent => messagesSent;

        public TurbofanDevice(int deviceNumber, string iotHubConnectionString, TrainingFileManager fileManager)
        {
            deviceUnitNumber = deviceNumber;
            string deviceId = $"Client_{deviceNumber:000}";
            deviceConnectionString = CreateIotHubDevice(iotHubConnectionString, deviceId).Result;
            trainingFileManager = fileManager;
        }

        public async Task RunDeviceAsync()
        {
            List<CycleData> cycleData = LoadDeviceData();
            await SendDataToHub(cycleData).ConfigureAwait(false);
        }

        private async Task SendDataToHub(List<CycleData> cycleData)
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString);
            foreach (CycleData c in cycleData)
            {
                await SendEvent(deviceClient, c.Message).ConfigureAwait(false);
                await Task.Delay(CycleTime).ConfigureAwait(false);
            }
        }

        private async Task<string> CreateIotHubDevice(string iotHubConnectionString, string deviceId)
        {
            RegistryManager regManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            string hostname = Microsoft.Azure.Devices.IotHubConnectionStringBuilder.Create(iotHubConnectionString).HostName;

            Device device = await regManager.GetDeviceAsync(deviceId).ConfigureAwait(false);
            if (device == null)
            {
                Console.WriteLine($"Creating new IoT device: {deviceId}");
                Device newDevice = await regManager.AddDeviceAsync(new Device(deviceId)).ConfigureAwait(false);
                return $"HostName={hostname};DeviceId={newDevice.Id};SharedAccessKey={newDevice.Authentication.SymmetricKey.PrimaryKey}";
            }
            Console.WriteLine($"Found existing device: {device.Id}");
            return $"HostName={hostname};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";
        }

        private List<CycleData> LoadDeviceData()
        {
            return trainingFileManager.ReadDeviceData(deviceUnitNumber);
        }

        private async Task SendEvent(DeviceClient deviceClient, string message)
        {
            using (Message eventMessage = new Message(Encoding.UTF8.GetBytes(message)))
            {
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