using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

using System.Diagnostics;

namespace TISensorTagLibrary.CC2650
{
    public class CC2650Sensor : BLETISensor
    {
        private Accelerometer accelerometer;
        private HumiditySensor humidity;
        private LightSensor light;
        private PressureSensor pressure;
        private IRTemperatureSensor temperature;
        private BatteryLevel battery;
        private SimpleKeyService key;
        private IOService io;

        public CC2650Sensor()
            : base()
        {
            accelerometer = new Accelerometer();
            humidity = new HumiditySensor();
            light = new LightSensor();
            pressure = new PressureSensor();
            temperature = new IRTemperatureSensor();
            battery = new BatteryLevel();

            key = new SimpleKeyService();
            io = new IOService();

            accelerometer.SensorValueChanged += CC2650Sensor_SensorValueChanged;
            humidity.SensorValueChanged += CC2650Sensor_SensorValueChanged;
            light.SensorValueChanged += CC2650Sensor_SensorValueChanged;
            pressure.SensorValueChanged += CC2650Sensor_SensorValueChanged;
            temperature.SensorValueChanged += CC2650Sensor_SensorValueChanged;
            battery.SensorValueChanged += CC2650Sensor_SensorValueChanged;
            key.SensorValueChanged += CC2650Sensor_SensorValueChanged;
        }

        public override async void Initialize()
        {
            Debug.WriteLine("*CC2650Sensor::Initialize: enter");
            try
            {
                if (await accelerometer.Initialize(accelerometer.SensorServiceUuid))
                {
                    await accelerometer.EnableSensor();
                    await accelerometer.EnableNotifications();
                    Debug.WriteLine("Accelerometer enabled");
                }
                else
                {
                    Debug.WriteLine("Accelerometer doesn't exists");
                }

                if (await humidity.Initialize(humidity.SensorServiceUuid))
                {
                    await humidity.EnableSensor();
                    await humidity.EnableNotifications();
                    Debug.WriteLine("Humidity enabled");
                }
                else
                {
                    Debug.WriteLine("Humidity doesn't exists");
                }

                if (await light.Initialize(light.SensorServiceUuid))
                {
                    await light.EnableSensor();
                    await light.EnableNotifications();
                    Debug.WriteLine("Lightness enabled");
                }
                else
                {
                    Debug.WriteLine("Lightness doesn't exists");
                }

                if (await pressure.Initialize(pressure.SensorServiceUuid))
                {
                    await pressure.EnableSensor();
                    await pressure.EnableNotifications();
                    Debug.WriteLine("Pressure enabled");
                }
                else
                {
                    Debug.WriteLine("Pressure doesn't exists");
                }

                if (await temperature.Initialize(temperature.SensorServiceUuid))
                {
                    await temperature.EnableSensor();
                    await temperature.EnableNotifications();
                    Debug.WriteLine("Temperature enabled");
                }
                else
                {
                    Debug.WriteLine("Temperature doesn't exists");
                }

                if (await key.Initialize(key.SensorServiceUuid))
                {
                    await key.EnableSensor();
                    await key.EnableNotifications();
                    Debug.WriteLine("Simple Key Service enabled");
                }
                else
                {
                    Debug.WriteLine("Simple Key doesn't exists");
                }

                if (await battery.Initialize(battery.SensorServiceUuid))
                {
                    battery.Setup();
                    byte[] bytes = await BatteryLevel.GetLevel();
                    double bl = BatteryLevel.CalculateBatteryInPercent(bytes);
                    Debug.WriteLine("Battery Level Service enabled=" + (int) bl);
                }
                else
                {
                    Debug.WriteLine("Battery Level doesn't exists");
                }

                if (await io.Initialize(io.SensorServiceUuid))
                {
                    await io.EnableSensor();
                    Debug.WriteLine("IO Service enabled");
                }
                else
                {
                    Debug.WriteLine("IO Service doesn't exists");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            Debug.WriteLine("*CC2650Sensor::Initialize: exit");
            InitializedStatus = true;
        }

        public override void WriteValue(byte [] data)
        {
            Task.Run(() => io.WriteValue(data));
        }

        private void CC2650Sensor_SensorValueChanged(object sender, SensorValueChangedEventArgs e)
        {
            //Debug.WriteLine("*CC2650Sensor_SensorValueChanged:" + e.Origin);
            switch (e.Origin)
            {
                case SensorName.Accelerometer:
                    float[] accValues = Accelerometer.CalculateCoordinates(e.RawData, AccelRange);
                    lock (lastSensorReading)
                    {
                        lastSensorReading.AccelX = accValues[0];
                        lastSensorReading.AccelY = accValues[1];
                        lastSensorReading.AccelZ = accValues[2];
                        lastSensorReading.GyroX = accValues[3];
                        lastSensorReading.GyroY = accValues[4];
                        lastSensorReading.GyroZ = accValues[5];
                        lastSensorReading.MagX = accValues[6];
                        lastSensorReading.MagY = accValues[7];
                        lastSensorReading.MagZ = accValues[8];
                    }
                    break;
                case SensorName.HumiditySensor:
                    double rh = HumiditySensor.CalculateHumidityInPercent(e.RawData);
                    double rt = HumiditySensor.CalculateHumidityTempareture(e.RawData, TemparetureScale);
                    lock (lastSensorReading)
                    {
                        lastSensorReading.Humidity = rh;
                        lastSensorReading.HTemperature = rt;
                    }
                    break;
                case SensorName.LightSensor:
                    double rl = LightSensor.CalculateLightnessInLux(e.RawData);
                    lock (lastSensorReading)
                    {
                        lastSensorReading.Lightness = rl;
                    }

                    //Special service for Battery here!
                    Task<byte[]> task = Task.Run(() => BatteryLevel.GetLevel());
                    byte[] bytes = task.Result;
                    if (bytes != null)
                    {
                        double bl = BatteryLevel.CalculateBatteryInPercent(bytes);
                        //Debug.WriteLine("Battery Level:" + (int) bl);
                        lock (lastSensorReading)
                        {
                            lastSensorReading.BatteryLevel = bl;
                        }
                    }
                    break;
                case SensorName.PressureSensor:
                    double hp = (PressureSensor.CalculatePressure(e.RawData));
                    double ht = (PressureSensor.CalculatePressureTempareture(e.RawData, TemparetureScale));
                    lock (lastSensorReading)
                    {
                        lastSensorReading.Pressure = hp;
                        lastSensorReading.PTemperature = ht;
                    }
                    break;
                case SensorName.BatteryLevel:
                    Debug.WriteLine("Battery Level Service is not here.");
                    break;
                case SensorName.SimpleKeyService:
                    bool leftKey = false;
                    if (SimpleKeyService.LeftKeyHit(e.RawData))
                    {
                        leftKey = true;
                        //Debug.WriteLine("leftKey");
                    }
                    bool rightKey = false;
                    if (SimpleKeyService.RightKeyHit(e.RawData))
                    {
                        rightKey = true;
                        //Debug.WriteLine("rightKey");
                    }
                    lock (lastSensorReading)
                    {
                        lastSensorReading.LeftKey = leftKey;
                        lastSensorReading.RightKey = rightKey;
                    }
                    break;
                case SensorName.TemperatureSensor:
                    double ambient = IRTemperatureSensor.CalculateAmbientTemperature(e.RawData, TemparetureScale);
                    double target = IRTemperatureSensor.CalculateTargetTemperature(e.RawData, ambient, TemparetureScale);
                    lock (lastSensorReading)
                    {
                        lastSensorReading.ATemperature = ambient;
                        lastSensorReading.OTemperature = target;
                    }
                    break;
            }
        }
    }

    public class Accelerometer : SensorBase
    {
        private static AccelRange accelRange;
        public Accelerometer()
            // CC2650 does support UUID_MOV
            : base(SensorName.Accelerometer, SensorTagUuid.UUID_MOV_SERV, SensorTagUuid.UUID_MOV_CONF, SensorTagUuid.UUID_MOV_DATA)
        {

        }
        //public static double[] CalculateCoordinates(byte[] sensorData, double scale)
        public static float[] CalculateCoordinates(byte[] rawdata, AccelRange range)
        {
            if (!Enum.IsDefined(typeof(AccelRange), range))
            {
                throw new ArgumentOutOfRangeException("AccelRange", "Invalid range=" + range);
            }
            accelRange = range;
            return new float[] {
            sensorMpu9250GyroConvert(rawdata[0]),
            sensorMpu9250GyroConvert(rawdata[1]),
            sensorMpu9250GyroConvert(rawdata[2]),
            sensorMpu9250AccConvert(rawdata[3]),
            sensorMpu9250AccConvert(rawdata[4]),
            sensorMpu9250AccConvert(rawdata[5]),
            sensorMpu9250MagConvert(rawdata[6]),
            sensorMpu9250MagConvert(rawdata[7]),
            sensorMpu9250MagConvert(rawdata[8])
        };
        }

        private static float sensorMpu9250AccConvert(short rawData)
        {
            double v = 0.0;

            switch (accelRange)
            {
                case AccelRange._2G:
                    //-- calculate acceleration, unit G, range -2, +2
                    v = (rawData * 1.0) / (32768 / 2);
                    break;

                case AccelRange._4G:
                    //-- calculate acceleration, unit G, range -4, +4
                    v = (rawData * 1.0) / (32768 / 4);
                    break;

                case AccelRange._8G:
                    //-- calculate acceleration, unit G, range -8, +8
                    v = (rawData * 1.0) / (32768 / 8);
                    break;

                case AccelRange._16G:
                    //-- calculate acceleration, unit G, range -16, +16
                    v = (rawData * 1.0) / (32768 / 16);
                    break;
            }

            return (float)v;
        }

        private static float sensorMpu9250GyroConvert(short data)
        {
            //-- calculate rotation, unit deg/s, range -250, +250
            return (float)(data * 1.0) / (65536 / 500);
        }

        private static float sensorMpu9250MagConvert(short data)
        {
            //-- calculate magnetism, unit uT, range +-4900
            return ((float)data);
        }

        public async Task SetReadPeriod(byte time)
        {
            if (time < 10)
                throw new ArgumentOutOfRangeException("time", "Period can't be lower than 100ms");

            GattCharacteristic dataCharacteristic = deviceService.GetCharacteristics(new Guid(SensorTagUuid.UUID_MOV_PERI))[0];

            byte[] data = new byte[] { time };
            GattCommunicationStatus status = await dataCharacteristic.WriteValueAsync(data.AsBuffer());
            if (status == GattCommunicationStatus.Unreachable)
            {
                throw new ArgumentOutOfRangeException();
            }
        }
        public async override Task EnableSensor()
        {
            //http://processors.wiki.ti.com/index.php/CC2650_SensorTag_User's_Guide#Movement_Sensor
            //
            byte[] movementConfigration = new byte[2];
            movementConfigration[0] = 0xFF; // allsensor enable
            movementConfigration[1] = (byte)accelRange; // Accelerometer range (0=2G, 1=4G, 2=8G, 3=16G) 
            await EnableSensor(movementConfigration);
        }
    }

    public class HumiditySensor : SensorBase
    {
        public HumiditySensor()
            : base(SensorName.HumiditySensor, SensorTagUuid.UUID_HUM_SERV, SensorTagUuid.UUID_HUM_CONF, SensorTagUuid.UUID_HUM_DATA)
        {

        }

        /// <summary>
        /// Calculates the humidity in percent.
        /// </summary>
        /// <param name="sensorData">Complete array of data retrieved from the sensor</param>
        /// <returns></returns>
        public static double CalculateHumidityInPercent(byte[] sensorData)
        {
            int hum = BitConverter.ToUInt16(sensorData, 2);

            //cut first two statusbits

            // calculate in percent
            return (((double)hum) / 65536.0) * 100.0;
        }
        public static double CalculateHumidityTempareture(byte[] sensorData, TemperatureScale scale)
        {
            int temp = BitConverter.ToUInt16(sensorData, 0);
            double htemp = ((double)temp / 65536) * 165 - 40;
            if (scale == TemperatureScale.Celsius)
                return htemp;
            else
                return htemp * 1.8 + 32;
        }
    }

    public class LightSensor : SensorBase
    {
        public LightSensor()
            : base(SensorName.LightSensor, SensorTagUuid.UUID_OPT_SERV, SensorTagUuid.UUID_OPT_CONF, SensorTagUuid.UUID_OPT_DATA)
        {

        }

        /// <summary>
        /// Calculates the Lightness in lux.
        /// </summary>
        /// <param name="sensorData">Complete array of data retrieved from the sensor</param>
        /// <returns></returns>
        public static double CalculateLightnessInLux(byte[] sensorData)
        {
            // from http://processors.wiki.ti.com/index.php/CC2650_SensorTag_User's_Guide#Optical_Sensor
            uint rawData = BitConverter.ToUInt16(sensorData, 0);
            uint e, m;

            m = rawData & 0x0FFF;
            e = (rawData & 0xF000) >> 12;

            return m * (0.01 * Math.Pow(2.0, e));
        }
    }

    public class PressureSensor : SensorBase
    {
        public PressureSensor()
            : base(SensorName.PressureSensor, SensorTagUuid.UUID_BAR_SERV, SensorTagUuid.UUID_BAR_CONF, SensorTagUuid.UUID_BAR_DATA)
        {

        }

        public override async Task EnableSensor()
        {
            await base.EnableSensor();
        }

        public static double CalculatePressure(byte[] sensorData)
        {
            byte[] rawData = new byte[4];
            rawData[0] = sensorData[3];
            rawData[1] = sensorData[4];
            rawData[2] = sensorData[5];
            rawData[3] = 0;
            var raw = BitConverter.ToUInt32(rawData, 0);
            return ((double)raw) / 100.0;
        }

        public static double CalculatePressureTempareture(byte[] sensorData, TemperatureScale scale)
        {
            byte[] rawData = new byte[4];
            rawData[0] = sensorData[0];
            rawData[1] = sensorData[1];
            rawData[2] = sensorData[2];
            rawData[3] = 0;
            var raw = BitConverter.ToUInt32(rawData, 0);

            if (scale == TemperatureScale.Celsius)
                return raw / 100.0;
            else
                return ((double)raw) / 100.0 * 1.8 + 32;
        }
    }

    public class IRTemperatureSensor : SensorBase
    {
        public IRTemperatureSensor()
            : base(SensorName.TemperatureSensor, SensorTagUuid.UUID_IRT_SERV, SensorTagUuid.UUID_IRT_CONF, SensorTagUuid.UUID_IRT_DATA)
        {

        }

        public static double CalculateAmbientTemperature(byte[] sensorData, TemperatureScale scale)
        {
            const double SCALE_LSB = 0.03125;
            double ambient = ((double)(BitConverter.ToUInt16(sensorData, 0) >> 2)) * SCALE_LSB;
            if (scale == TemperatureScale.Celsius)
                return ambient;
            else
                return ambient * 1.8 + 32;
        }

        public static double CalculateTargetTemperature(byte[] sensorData, double ambientTemperature, TemperatureScale scale)
        {
            const double SCALE_LSB = 0.03125;
            double otemprate = ((double)(BitConverter.ToUInt16(sensorData, 2) >> 2)) * SCALE_LSB;
            if (scale == TemperatureScale.Celsius)
                return otemprate;
            else
                return otemprate * 1.8 + 32;
        }
    }

    public class BatteryLevel : SensorBase
    {
        private static GattCharacteristic BatteryLevelCharacteristic = null;
        public BatteryLevel()
            : base(SensorName.BatteryLevel, SensorTagUuid.UUID_BAT_SERV, "", SensorTagUuid.UUID_BAT_LEVL)
        {

        }

        public void Setup()
        {
            var CharacteristicList = deviceService.GetCharacteristics(new Guid(SensorDataUuid));
            BatteryLevelCharacteristic = null;
            if (CharacteristicList != null)
            {
                if (CharacteristicList.Count() > 0)
                {
                    BatteryLevelCharacteristic = CharacteristicList[0];
                    Debug.WriteLine("BatterySetup: List[0].Uuid=" + BatteryLevelCharacteristic.Uuid);
                }
                Debug.WriteLine("BatterySetup: List.Count=" + CharacteristicList.Count());
            }
            else
            {
                Debug.WriteLine("BatterySetup: CharacteristicList is null");
            }
        }
        public static async Task<byte[]> GetLevel()
        {
            byte[] bytes = null;
            GattReadResult result;
            GattCharacteristicProperties flag = GattCharacteristicProperties.Read;

            if (BatteryLevelCharacteristic != null)
            {
                if (BatteryLevelCharacteristic.CharacteristicProperties.HasFlag(flag))
                {
                    result = await BatteryLevelCharacteristic.ReadValueAsync(Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached);
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        bytes = new byte[result.Value.Length];
                        Windows.Storage.Streams.DataReader.FromBuffer(result.Value).ReadBytes(bytes);
                    }
                }
            }
            if (bytes != null)
            {
                ; // Debug.WriteLine("Battery Level: {0}", bytes[0]);
            }
            return bytes;
        }

        public static double CalculateBatteryInPercent(byte[] sensorData)
        {
            uint rawData = 0;
            if (sensorData != null)
            {
                if (sensorData.Count() > 0)
                {
                    rawData = sensorData[0];
                }
            }
            return rawData;
        }
    }

    public class SimpleKeyService : SensorBase
    {
        public SimpleKeyService()
            : base(SensorName.SimpleKeyService, SensorTagUuid.UUID_KEY_SERV, null, SensorTagUuid.UUID_KEY_DATA)
        {

        }

        public static bool LeftKeyHit(byte[] sensorData)
        {
            return (new BitArray(sensorData))[0];
        }

        public static bool RightKeyHit(byte[] sensorData)
        {
            return (new BitArray(sensorData))[1];
        }

        public static bool SideKeyHit(byte[] sensorData)
        {
            return (new BitArray(sensorData))[2];
        }

#pragma warning disable

        public override async Task EnableSensor()
        {
            // not possible in this case
        }

        public async override Task DisableSensor()
        {
            // not possible in this case
        }
#pragma warning restore
    }
    public class IOService : SensorBase
    {
        public IOService()
            : base(SensorName.IOSerivice, SensorTagUuid.UUID_IO_SERV, SensorTagUuid.UUID_IO_CONF, SensorTagUuid.UUID_IO_DATA)
        {

        }

#if true
        //protected new void EnableSensor(byte[] sensorEnableData)
        public async override Task EnableSensor()
        {
            GattCharacteristic configCharacteristic;
            //GattCharacteristic dataCharacteristic;

            configCharacteristic = deviceService.GetCharacteristics(new Guid(SensorConfigUuid))[0];

            Debug.WriteLine("*IOService::EnableSensor: enter=" + SensorConfigUuid);
            var status = await configCharacteristic.WriteValueAsync(new byte[] { 0 }.AsBuffer());
            if (status == GattCommunicationStatus.Unreachable)
            {
                //throw new ArgumentOutOfRangeException();
                Debug.WriteLine("*IOService::EnableSensor: Status.Unreachable!");
                //await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
            Debug.WriteLine("*IOService::EnableSensor: ok");
        }
#endif
    }
}