using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace TISensorTagLibrary
{
    public abstract class BLETISensor
    {
        protected BLETISensor()
        {
            lastSensorReading = new SensorReading();
        }

        private static BLETISensor sensor = null;
        public AccelRange AccelRange { get; set; }
        public TemperatureScale TemparetureScale { get; set; }
        public string ManifactureName { get; set; }
        public string FirmwareRevision { get; set; }
        public string ConnectionStatus { get; set; }

        public GattReadResult deviceNameValue;
        public GattReadResult systemIdValue;
        public bool InitializedStatus;

        public const string devicesContainerId = "System.Devices.ContainerId";

        public static async Task<BLETISensor> Sensor(SensorTag sensorType)
        {
            string selector = null;
            switch (sensorType)
            {
                case SensorTag.CC2541:
                    sensor = new CC2541.CC2541Sensor();
                    sensor.tagService = await Initialize(CC2541.SensorTagUuid.UUID_INF_SERV);
                    if (sensor.tagService == null)
                    {
                        sensor.ConnectionStatus = "Need Pairing";
                        throw new Exception("Need to make CC2541 Pairing before start!");
                    }
                    sensor.ManifactureName = await sensor.ReadCharacteristicStringAsync(CC2541.SensorTagUuid.UUID_INF_MANUF_NR);
                    sensor.FirmwareRevision = await sensor.ReadCharacteristicStringAsync(CC2541.SensorTagUuid.UUID_INF_FW_NR);
                    selector = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(CC2541.SensorTagUuid.UUID_INF_SERV));
                    Debug.WriteLine("CC2541:" + sensor.ManifactureName + "," + sensor.FirmwareRevision);
                    break;

                case SensorTag.CC2650:
                    sensor = new CC2650.CC2650Sensor();
                    sensor.tagService = await Initialize(CC2650.SensorTagUuid.UUID_INF_SERV);
                    if (sensor.tagService == null)
                    {
                        sensor.ConnectionStatus = "Need Pairing";
                        throw new Exception("Need to make CC2650 Pairing before start!");
                    }
                    sensor.ManifactureName = await sensor.ReadCharacteristicStringAsync(CC2650.SensorTagUuid.UUID_INF_MANUF_NR);
                    sensor.FirmwareRevision = await sensor.ReadCharacteristicStringAsync(CC2650.SensorTagUuid.UUID_INF_FW_NR);
                    selector = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(CC2650.SensorTagUuid.UUID_INF_SERV));
                    Debug.WriteLine("CC2650:" + sensor.ManifactureName + "," + sensor.FirmwareRevision);
                    break;
            }
            sensor.ConnectionStatus = "Waiting to connect..."; 
            Debug.WriteLine(sensor.ConnectionStatus);

            sensor.deviceNameValue = await ReadDeviceNameAsync(selector);
            sensor.systemIdValue = await ReadDeviceIdAsync(selector);
            return sensor;
        }

        private static async Task<GattReadResult> ReadDeviceNameAsync(string selector)
        {
            var devices = await DeviceInformation.FindAllAsync(selector, new string[] { devicesContainerId });
            DeviceInformation di = devices.FirstOrDefault();
            var containerId = di.Properties[devicesContainerId].ToString();

            // Access to Generic Attribute Profile service
            var genericSlector = GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.GenericAccess);
            var selectorWithContainer = String.Format("{0} AND System.Devices.ContainerId:=\"{{{1}}}\"", genericSlector, containerId);
            var serviceInformations = await DeviceInformation.FindAllAsync(selectorWithContainer);
            var gapService = await GattDeviceService.FromIdAsync(serviceInformations.Single().Id);
            var deviceName = gapService.GetCharacteristics(GattDeviceService.ConvertShortIdToUuid(0x2a00)).First();

            return await deviceName.ReadValueAsync(BluetoothCacheMode.Uncached);
        }
        private static async Task<GattReadResult> ReadDeviceIdAsync(string selector)
        {
            var devices = await DeviceInformation.FindAllAsync(selector, new string[] { devicesContainerId });
            DeviceInformation di = devices.FirstOrDefault();
            var containerId = di.Properties[devicesContainerId].ToString();

            var selectorWithContainer = String.Format("{0} AND System.Devices.ContainerId:=\"{{{1}}}\"", selector, containerId);
            var serviceInformations = await DeviceInformation.FindAllAsync(selectorWithContainer);
            var deviceInformationService = await GattDeviceService.FromIdAsync(serviceInformations.Single().Id);
            var systemId = deviceInformationService.GetCharacteristics(GattDeviceService.ConvertShortIdToUuid(0x2a23)).First(); // System ID

            return await systemId.ReadValueAsync(BluetoothCacheMode.Uncached);
        }

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
        private static async Task<GattDeviceService> Initialize(string serviceUuid)
        {
            Debug.WriteLine("*Initialize: enter=" + serviceUuid);
            GattDeviceService deviceService = null;
            string selector = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(serviceUuid));
            var devices = await DeviceInformation.FindAllAsync(selector, new string[] { "System.Devices.ContainerId" });
            //devicesTask.Wait();
            if (devices != null && devices.Count > 0)
            {
                var deviceInfo = devices[0];
                if (deviceInfo != null)
                {
                    deviceService = await GattDeviceService.FromIdAsync(deviceInfo.Id);
                    Debug.WriteLine("*Initialize: exit with ok=" + serviceUuid);
                }
            }
            if (deviceService == null)
            {
                Debug.WriteLine("*Initialize: exit with error=" + serviceUuid);
            }
            return deviceService;
        }

        public abstract  void Initialize();

        public abstract void WriteValue(byte[] data);

#if false
        public void WriteData(byte[] data)
        {
            WriteDataAsync(data);
        }
        public async Task WriteDataAsync(byte[] data)
        {
            //const string UUID_IO_SERV = "f000aa64-0451-4000-b000-000000000000"; //only for CC2650
            const string UUID_IO_DATA = "f000aa65-0451-4000-b000-000000000000"; //only for CC2650
            const string UUID_IO_CONF = "f000aa66-0451-4000-b000-000000000000"; //only for CC2650

            if (sensor.tagService == null)
            {
                Debug.WriteLine("*WriteData: deviceService == null return!");
            }
            Debug.WriteLine("*WriteData: enter");

            //GattCharacteristic dataCharacteristic = sensor.tagService.GetCharacteristics(new Guid(UUID_IO_DATA))[0];
            //GattCharacteristic dataCharacteristic = sensor.tagService.GetCharacteristics(new Guid(UUID_IO_DATA));
            var characteristics = sensor.tagService.GetCharacteristics(new Guid(UUID_IO_DATA));
            var dataCharacteristic = characteristics.FirstOrDefault();
            if (dataCharacteristic == null)
            {
                throw new Exception("dataCharacteristic " + UUID_IO_DATA + " does not exist");
            }
            var status = await dataCharacteristic.WriteValueAsync(data.AsBuffer());
            if (status == GattCommunicationStatus.Unreachable)
            {
                //throw new ArgumentOutOfRangeException();
                Debug.WriteLine("*EnableSensor: Status.Unreachable!");
            }
            Debug.WriteLine("*WriteData: ok");
        }
#endif

        public SensorReading ReadSensorValue()
        {
            var data = new SensorReading();
            lock (lastSensorReading)
            {
                data.AccelX = lastSensorReading.AccelX;
                data.AccelY = lastSensorReading.AccelY;
                data.AccelZ = lastSensorReading.AccelZ;
                data.GyroX = lastSensorReading.GyroX;
                data.GyroY = lastSensorReading.GyroY;
                data.GyroZ = lastSensorReading.GyroZ;
                data.MagX = lastSensorReading.MagX;
                data.MagY = lastSensorReading.MagY;
                data.MagZ = lastSensorReading.MagZ;
                data.ATemperature = lastSensorReading.ATemperature;
                data.OTemperature = lastSensorReading.OTemperature;
                data.Pressure = lastSensorReading.Pressure;
                data.Humidity = lastSensorReading.Humidity;
                data.BatteryLevel = lastSensorReading.BatteryLevel;
                data.Lightness = lastSensorReading.Lightness;
                data.LeftKey = lastSensorReading.LeftKey;
                data.RightKey = lastSensorReading.RightKey;
            }
            return data;
        }

        protected SensorReading lastSensorReading;

        public class SensorReading
        {
            public float AccelX { get; set; }
            public float AccelY { get; set; }
            public float AccelZ { get; set; }
            public float GyroX { get; set; }
            public float GyroY { get; set; }
            public float GyroZ { get; set; }
            public float MagX { get; set; }
            public float MagY { get; set; }
            public float MagZ { get; set; }
            public double ATemperature { get; set; }
            public double OTemperature { get; set; }
            public double HTemperature { get; set; }
            public double PTemperature { get; set; }
            public double Pressure { get; set; }
            public double Humidity { get; set; }
            public double Lightness { get; set; }
            public double BatteryLevel { get; set; }
            public bool LeftKey { get; set; }
            public bool RightKey { get; set; }
        }
    }
}
