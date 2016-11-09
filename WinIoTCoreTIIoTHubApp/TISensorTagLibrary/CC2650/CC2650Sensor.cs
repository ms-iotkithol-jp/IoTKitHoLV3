using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace TISensorTagLibrary.CC2650
{
    public class CC2650Sensor : BLETISensor
    {
        private Accelerometer accelerometer;
        private HumiditySensor humidity;
        private PressureSensor pressure;
        private IRTemperatureSensor temperature;

        public CC2650Sensor()
            : base()
        {
            accelerometer = new Accelerometer();
            humidity = new HumiditySensor();
            pressure = new PressureSensor();
            temperature = new IRTemperatureSensor();
            accelerometer.SensorValueChanged += CC2650Sensor_SensorValueChanged;
            humidity.SensorValueChanged += CC2650Sensor_SensorValueChanged;
            pressure.SensorValueChanged += CC2650Sensor_SensorValueChanged;
            temperature.SensorValueChanged += CC2650Sensor_SensorValueChanged;
        }

        public override async void Initialize()
        {
            await accelerometer.Initialize(accelerometer.SensorServiceUuid);
            await accelerometer.EnableSensor();
            await accelerometer.EnableNotifications();

            await humidity.Initialize(humidity.SensorServiceUuid);
            await humidity.EnableSensor();
            await humidity.EnableNotifications();

            await pressure.Initialize(pressure.SensorServiceUuid);
            await pressure.EnableSensor();
            await pressure.EnableNotifications();

            await temperature.Initialize(temperature.SensorServiceUuid);
            await temperature.EnableSensor();
            await temperature.EnableNotifications();
        }

        private  void CC2650Sensor_SensorValueChanged(object sender, SensorValueChangedEventArgs e)
        {
            switch (e.Origin)
            {
                case SensorName.Accelerometer:
                    double[] accValues = Accelerometer.CalculateCoordinates(e.RawData, 1 / 64.0);
                    lock (lastSensorReading)
                    {
                        lastSensorReading.AccelX = accValues[0];
                        lastSensorReading.AccelY = accValues[1];
                        lastSensorReading.AccelZ = accValues[2];
                    }
                    break;
                case SensorName.HumiditySensor:
                    double rh = HumiditySensor.CalculateHumidityInPercent(e.RawData);
                    lock (lastSensorReading)
                    {
                        lastSensorReading.Humidity = rh;
                    }
                    break;
                case SensorName.PressureSensor:
                    double hp = (PressureSensor.CalculatePressure(e.RawData));
                    lock (lastSensorReading)
                    {
                        lastSensorReading.Pressure = hp;
                    }
                    break;
                case SensorName.SimpleKeyService:
                    bool leftKey = false;
                    if (SimpleKeyService.LeftKeyHit(e.RawData))
                    {
                        leftKey = true;
                    }
                    bool rightKey = false;
                    if (SimpleKeyService.RightKeyHit(e.RawData))
                    {
                        rightKey = true;
                    }
                    lock (lastSensorReading)
                    {
                        lastSensorReading.LeftKey = leftKey;
                        lastSensorReading.RightKey = rightKey;
                    }
                    break;
                case SensorName.TemperatureSensor:
                    double ambient = IRTemperatureSensor.CalculateAmbientTemperature(e.RawData, TemperatureScale.Celsius);
                    double target = IRTemperatureSensor.CalculateTargetTemperature(e.RawData, ambient, TemperatureScale.Celsius);
                    lock (lastSensorReading)
                    {
                        lastSensorReading.ATemperature = ambient;
                        lastSensorReading.OTemperature = target;
                    }
                    break;
            }
        }
    }

    public class Accelerometer: SensorBase
    {
        public Accelerometer()
            : base(SensorName.Accelerometer, SensorTagUuid.UUID_ACC_SERV, SensorTagUuid.UUID_ACC_CONF, SensorTagUuid.UUID_ACC_DATA)
        {

        }
        public static double[] CalculateCoordinates(byte[] sensorData, double scale)
        {
            if (scale == 0)
                throw new ArgumentOutOfRangeException("scale", "scale cannot be 0");
            return new double[] { sensorData[0] * scale, sensorData[1] * scale, sensorData[2] * scale };
        }

        public async Task SetReadPeriod(byte time)
        {
            if (time < 10)
                throw new ArgumentOutOfRangeException("time", "Period can't be lower than 100ms");

            GattCharacteristic dataCharacteristic = deviceService.GetCharacteristics(new Guid(SensorTagUuid.UUID_ACC_PERI))[0];

            byte[] data = new byte[] { time };
            GattCommunicationStatus status = await dataCharacteristic.WriteValueAsync(data.AsBuffer());
            if (status == GattCommunicationStatus.Unreachable)
            {
                throw new ArgumentOutOfRangeException();
            }
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
            var raw= BitConverter.ToUInt32(rawData, 0);
            return ((double)raw) / 100.0;
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

    public class SimpleKeyService : SensorBase
    {
        public SimpleKeyService()
            : base(SensorName.SimpleKeyService, SensorTagUuid.UUID_KEY_SERV, null, SensorTagUuid.UUID_KEY_DATA)
        {

        }

        public static bool LeftKeyHit(byte[] sensorData)
        {
            return (new BitArray(sensorData))[1];
        }

        public static bool RightKeyHit(byte[] sensorData)
        {   return (new BitArray(sensorData))[0];
        }

        public static bool SideKeyHit(byte[] sensorData)
        {
            return (new BitArray(sensorData))[2];
        }

#pragma warning disable

        public  override async Task EnableSensor()
        {
            // not possible in this case
        }

        public async override Task DisableSensor()
        {
            // not possible in this case
        }

#pragma warning restore
    }

}
