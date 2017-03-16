using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

using System.Diagnostics;

namespace TISensorTagLibrary
{
    public abstract class SensorBase : IDisposable
    {
        public event SensorValueChangedEventHandler SensorValueChanged;
        public delegate void SensorValueChangedEventHandler(object sender, SensorValueChangedEventArgs e);

        protected GattDeviceService deviceService;

        private string sensorServiceUuid;
        private string sensorConfigUuid;
        private string sensorDataUuid;

        private GattCharacteristic configCharacteristic;
        private GattCharacteristic dataCharacteristic;

        private SensorName sensorName;

        private bool disposed;

        public SensorBase(SensorName sensorName, string sensorServiceUuid, string sensorConfigUuid, string sensorDataUuid)
        {
            this.sensorServiceUuid = sensorServiceUuid;
            this.sensorConfigUuid = sensorConfigUuid;
            this.sensorDataUuid = sensorDataUuid;
            this.sensorName = sensorName;
        }

        public string SensorServiceUuid
        {
            get { return sensorServiceUuid; }
        }

        protected string SensorConfigUuid
        {
            get { return sensorConfigUuid; }
        }

        protected string SensorDataUuid
        {
            get { return sensorDataUuid; }
        }

        public async Task<bool> Initialize()
        {
            if (this.deviceService != null)
            {
                Clean();
            }
            this.deviceService = await getDeviceService();
            if (this.deviceService == null)
                return false;
            return true;
        }

        public async Task< bool> Initialize(string serviceUuid)
        {
            string selector = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(serviceUuid));

            Debug.WriteLine("*SensorBase::Initialize: enter, uuid=" + serviceUuid);

            var devices = await DeviceInformation.FindAllAsync(selector, new string[] { "System.Devices.ContainerId" });

            Debug.WriteLine("*SensorBase::Initialize: devices.Count=" + devices.Count);

            if (devices.Count == 0)
            {
                Debug.WriteLine("*SensorBase::Initialize: Count 0 return");
                return false;
            }

            var deviceInfo = devices[0];
            if (deviceInfo != null)
            {
                if (this.deviceService != null)
                {
                    Clean();
                }

                this.deviceService = await GattDeviceService.FromIdAsync(deviceInfo.Id);
                if (this.deviceService == null)
                {
                    Debug.WriteLine("*SensorBase::Initialize: error");
                    return false;
                }
                else
                {
                    Debug.WriteLine("*SensorBase::Initialize: ok");
                }
                return true;
            }
            return false;
        }

        public virtual async Task EnableSensor()
        {
            await EnableSensor(new byte[] { 1 });
        }

        /// <summary>
        /// Disables the sensor by writing a 0 to the config characteristic.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="DeviceNotInitializedException">Thrown if sensor has not been initialized successfully.</exception>
        /// <exception cref="DeviceUnreachableException">Thrown if it wasn't possible to communicate with the device.</exception>
        public virtual async Task DisableSensor()
        {

            GattCharacteristic configCharacteristic = deviceService.GetCharacteristics(new Guid(sensorConfigUuid))[0];

            GattCommunicationStatus status = await configCharacteristic.WriteValueAsync((new byte[] { 0 }).AsBuffer());
            if (status == GattCommunicationStatus.Unreachable)
            {
                //throw new ArgumentOutOfRangeException();
                Debug.WriteLine("*DisableSensor: Status.Unreachable!");
            }
        }

        public virtual async Task EnableNotifications()
        {
            dataCharacteristic = deviceService.GetCharacteristics(new Guid(sensorDataUuid))[0];

            dataCharacteristic.ValueChanged -= dataCharacteristic_ValueChanged;
            dataCharacteristic.ValueChanged += dataCharacteristic_ValueChanged;

            var status = await
                     dataCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status == GattCommunicationStatus.Unreachable)
            {
                //throw new ArgumentOutOfRangeException();
                Debug.WriteLine("*EnableNotifications: Status.Unreachable!");
            }
        }

        public virtual async Task DisableNotifications()
        {
            dataCharacteristic = deviceService.GetCharacteristics(new Guid(sensorDataUuid))[0];

            dataCharacteristic.ValueChanged -= dataCharacteristic_ValueChanged;

            GattCommunicationStatus status =
                await dataCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.None);

            if (status == GattCommunicationStatus.Unreachable)
            {
                //throw new ArgumentOutOfRangeException();
                Debug.WriteLine("*DisableNotifications: Status.Unreachable!");
            }
        }

        public async Task<byte[]> ReadValue()
        {
            if (dataCharacteristic == null)
                dataCharacteristic = deviceService.GetCharacteristics(new Guid(sensorDataUuid))[0];

            GattReadResult readResult = await dataCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
            if (readResult.Status == GattCommunicationStatus.Unreachable)
            {
                //throw new ArgumentOutOfRangeException();
                Debug.WriteLine("*ReadValue: Status.Unreachable!");
            }
            var sensorData = new byte[readResult.Value.Length];

            DataReader.FromBuffer(readResult.Value).ReadBytes(sensorData);

            return sensorData;
        }

        public async Task WriteValue(byte[] data)
        {
            if (dataCharacteristic == null)
                configCharacteristic = deviceService.GetCharacteristics(new Guid(SensorConfigUuid))[0];

            var status = await configCharacteristic.WriteValueAsync((new byte[] { 1 }).AsBuffer());
            if (status == GattCommunicationStatus.Unreachable)
            {
                //throw new ArgumentOutOfRangeException();
                Debug.WriteLine("*ReadValue: Status.Unreachable!");
            }

            if (dataCharacteristic == null)
                dataCharacteristic = deviceService.GetCharacteristics(new Guid(sensorDataUuid))[0];

            Debug.WriteLine("*WriteValue: enter data=" + data[0].ToString() + "," + sensorDataUuid);
            status = await dataCharacteristic.WriteValueAsync(data.AsBuffer());
            if (status == GattCommunicationStatus.Unreachable)
            {
                //throw new ArgumentOutOfRangeException();
                Debug.WriteLine("*ReadValue: Status.Unreachable!");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected async Task EnableSensor(byte[] sensorEnableData)
        {
            configCharacteristic = deviceService.GetCharacteristics(new Guid(sensorConfigUuid))[0];

            Debug.WriteLine("*EnableSensor: enter=" + sensorConfigUuid);
            var status = await configCharacteristic.WriteValueAsync(sensorEnableData.AsBuffer());
            if (status == GattCommunicationStatus.Unreachable)
            {
                //throw new ArgumentOutOfRangeException();
                Debug.WriteLine("*EnableSensor: Status.Unreachable!");
                //await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            Debug.WriteLine("*EnableSensor: ok");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Clean();
                }
            }

            disposed = true;
        }

        private async Task<GattDeviceService> getDeviceService()
        {
            Debug.WriteLine("*getDeviceService: enter=" + sensorServiceUuid);

            string selector = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(sensorServiceUuid));
            var devices = await DeviceInformation.FindAllAsync(selector);
            DeviceInformation di = devices.FirstOrDefault();

            if (di == null)
                throw new ArgumentOutOfRangeException();

            return await GattDeviceService.FromIdAsync(di.Id);
        }

        private void dataCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];

            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            OnSensorValueChanged(data, args.Timestamp);
        }

        private void OnSensorValueChanged(byte[] rawData, DateTimeOffset timeStamp)
        {
            if (SensorValueChanged != null)
            {
                SensorValueChanged(this, new SensorValueChangedEventArgs(rawData, timeStamp, sensorName));
            }
        }

        private void Clean()
        {
            if (deviceService != null)
                deviceService.Dispose();
            deviceService = null;
            if (dataCharacteristic != null)
                dataCharacteristic.ValueChanged -= dataCharacteristic_ValueChanged;
        }
    }
}
