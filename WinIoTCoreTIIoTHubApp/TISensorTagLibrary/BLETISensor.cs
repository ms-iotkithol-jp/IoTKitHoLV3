using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace TISensorTagLibrary
{
    public abstract class BLETISensor
    {
        protected BLETISensor()
        {
            lastSensorReading = new SensorReading();
        }

        private static BLETISensor sensor = null;

        public static async Task<BLETISensor> Sensor(SensorTag sensorType)
        {
            switch (sensorType)
            {
                case SensorTag.CC2541:
                    sensor = new CC2541.CC2541Sensor();
                    sensor.tagService =  await Initialize(CC2541.SensorTagUuid.UUID_INF_SERV);
                    sensor.manifactureName = await sensor.ReadCharacteristicStringAsync(CC2541.SensorTagUuid.UUID_INF_MANUF_NR);
                    sensor.firmwareRevision = await sensor.ReadCharacteristicStringAsync(CC2541.SensorTagUuid.UUID_INF_FW_NR);
                    Debug.Write("CC2541:" + sensor.manifactureName + "," + sensor.firmwareRevision);
                    break;
                case SensorTag.CC2650:
                    sensor = new CC2650.CC2650Sensor();
                    sensor.tagService = await Initialize(CC2650.SensorTagUuid.UUID_INF_SERV);
                    sensor.manifactureName = await sensor.ReadCharacteristicStringAsync(CC2650.SensorTagUuid.UUID_INF_MANUF_NR);
                    sensor.firmwareRevision = await sensor.ReadCharacteristicStringAsync(CC2650.SensorTagUuid.UUID_INF_FW_NR);
                    Debug.Write("CC2650:" + sensor.manifactureName + "," + sensor.firmwareRevision);
                    break;
            }
            return sensor;
        }

        private string manifactureName;
        private string firmwareRevision;
        private async Task<string> ReadCharacteristicStringAsync(string uuid)
        {
            var characteristics = tagService.GetCharacteristics(Guid.Parse(uuid));

            var defaultCharacteristic = characteristics.FirstOrDefault();

            if (defaultCharacteristic == null)
            {
                throw new Exception("characteristic " + uuid + " does not exist");
            }

            var result = await defaultCharacteristic.ReadValueAsync();
            return (result.Status == GattCommunicationStatus.Success)
                   ? ReadString(result.Value)
                   : string.Empty;
        }
        public static string ReadString(IBuffer buffer)
        {
            var reader = DataReader.FromBuffer(buffer);

            byte[] bytes = new byte[buffer.Length];

            reader.ReadBytes(bytes);

            return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        GattDeviceService tagService;
        private static async Task< GattDeviceService> Initialize(string serviceUuid)
        {
            GattDeviceService deviceService = null;
            string selector = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(serviceUuid));
            var devices = await DeviceInformation.FindAllAsync(selector, new string[] { "System.Devices.ContainerId" });
//            devicesTask.Wait();
            var deviceInfo = devices[0];
            if (deviceInfo != null)
            {
                deviceService = await GattDeviceService.FromIdAsync(deviceInfo.Id);
            }
            return deviceService;
        }

        public abstract  void Initialize();
        public   SensorReading ReadSensorValue()
        {
            var data = new SensorReading();
            lock (lastSensorReading)
            {
                data.AccelX = lastSensorReading.AccelX;
                data.AccelY = lastSensorReading.AccelY;
                data.AccelZ = lastSensorReading.AccelZ;
                data.ATemperature = lastSensorReading.ATemperature;
                data.OTemperature = lastSensorReading.OTemperature;
                data.Pressure = lastSensorReading.Pressure;
                data.Humidity = lastSensorReading.Humidity;
                data.LeftKey = lastSensorReading.LeftKey;
                data.RightKey = lastSensorReading.RightKey;
            }
            return data;
        }

        protected SensorReading lastSensorReading;

        public class SensorReading
        {
            public double AccelX { get; set; }
            public double AccelY { get; set; }
            public double AccelZ { get; set; }
            public double ATemperature { get; set; }
            public double OTemperature { get; set; }
            public double Pressure { get; set; }
            public double Humidity { get; set; }

            public bool LeftKey { get; set; }
            public bool RightKey { get; set; }
        }
    }
}
