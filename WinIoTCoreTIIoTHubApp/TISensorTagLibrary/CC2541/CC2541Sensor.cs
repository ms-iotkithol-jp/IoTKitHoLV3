using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace TISensorTagLibrary.CC2541
{
    public class CC2541Sensor : BLETISensor
    {
        private Accelerometer accelerometer;
        private HumiditySensor humidity;
        private PressureSensor pressure;
        private IRTemperatureSensor temperature;
        private SimpleKeyService key;
        public CC2541Sensor()
            : base()
        {
            accelerometer = new Accelerometer();
            humidity = new HumiditySensor();
            pressure = new PressureSensor();
            temperature = new IRTemperatureSensor();
            key = new SimpleKeyService();
            accelerometer.SensorValueChanged += CC2541Sensor_SensorValueChanged;
            humidity.SensorValueChanged += CC2541Sensor_SensorValueChanged;
            pressure.SensorValueChanged += CC2541Sensor_SensorValueChanged;
            temperature.SensorValueChanged += CC2541Sensor_SensorValueChanged;
            key.SensorValueChanged += CC2541Sensor_SensorValueChanged;
        }

        public override async void Initialize()
        {
            Debug.WriteLine("*CC2541Sensor::Initialize: enter");
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
                    Debug.WriteLine("accelerometer doesn't exists");
                }

                if (await humidity.Initialize(humidity.SensorServiceUuid))
                {
                    await humidity.EnableSensor();
                    await humidity.EnableNotifications();
                    Debug.WriteLine("Humidity enabled");
                }
                else
                {
                    Debug.WriteLine("humidity doesn't exists");
                }

                if (await pressure.Initialize(pressure.SensorServiceUuid))
                {
                    await pressure.EnableSensor();
                    await pressure.EnableNotifications();
                    Debug.WriteLine("Pressure enabled");
                }
                else
                {
                    Debug.WriteLine("pressure doesn't exists");
                }

                if (await temperature.Initialize(temperature.SensorServiceUuid))
                {
                    await temperature.EnableSensor();
                    await temperature.EnableNotifications();
                    Debug.WriteLine("Temperature enabled");
                }
                else
                {
                    Debug.WriteLine("temperature doesn't exists");
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            Debug.WriteLine("*CC2541Sensor::Initialize: exit");
        }

        public override void WriteValue(byte[] data) { }

        private void CC2541Sensor_SensorValueChanged(object sender, SensorValueChangedEventArgs e)
        {
            switch (e.Origin)
            {
                case SensorName.Accelerometer:
                    double[] accValues = Accelerometer.CalculateCoordinates(e.RawData, 1 / 64.0);
                    lock (lastSensorReading)
                    {
                        lastSensorReading.AccelX = (float) accValues[0];
                        lastSensorReading.AccelY = (float) accValues[1];
                        lastSensorReading.AccelZ = (float) accValues[2];
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
                    double hp = (PressureSensor.CalculatePressure(e.RawData, pressure.CalibrationData) / 100);
                    lock (lastSensorReading)
                    {
                        lastSensorReading.Pressure = hp;
                    }
                    break;
                case SensorName.SimpleKeyService:
                    bool leftKey = false;
                    if (SimpleKeyService.LeftKeyHit(e.RawData))
                    {
                        Debug.WriteLine("leftKey");
                        leftKey = true;
                    }
                    bool rightKey = false;
                    if (SimpleKeyService.RightKeyHit(e.RawData))
                    {
                        Debug.WriteLine("rightKey");
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
            // more info http://www.sensirion.com/nc/en/products/humidity-temperature/download-center/?cid=880&did=102&sechash=c204b9cc
            int hum = BitConverter.ToUInt16(sensorData, 2);

            //cut first two statusbits
            hum = hum - (hum % 4);

            // calculate in percent
            return (-6f) + 125f * (hum / 65535f);
        }
    }

    public class PressureSensor : SensorBase
    {
        private int[] calibrationData = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

        /// <summary>
        /// Returns the calibration data read from the sensor after EnableSensor() was called. 
        /// </summary>
        public int[] CalibrationData
        {
            get { return calibrationData; }
        }

        public PressureSensor()
            : base(SensorName.PressureSensor, SensorTagUuid.UUID_BAR_SERV, SensorTagUuid.UUID_BAR_CONF, SensorTagUuid.UUID_BAR_DATA)
        {

        }

        public static double CalculatePressure(byte[] sensorData, int[] calibrationData)
        {
            //more info about the calculation:
            //http://www.epcos.com/web/generator/Web/Sections/ProductCatalog/Sensors/PressureSensors/T5400-ApplicationNote,property=Data__en.pdf;/T5400_ApplicationNote.pdf
            int t_r, p_r;	// Temperature raw value, Pressure raw value from sensor
            double t_a, S, O; 	// Temperature actual value in unit centi degrees celsius, interim values in calculation

            t_r = BitConverter.ToInt16(sensorData, 0);
            p_r = BitConverter.ToUInt16(sensorData, 2);

            t_a = (100 * (calibrationData[0] * t_r / Math.Pow(2, 8) + calibrationData[1] * Math.Pow(2, 6))) / Math.Pow(2, 16);
            S = calibrationData[2] + calibrationData[3] * t_r / Math.Pow(2, 17) + ((calibrationData[4] * t_r / Math.Pow(2, 15)) * t_r) / Math.Pow(2, 19);
            O = calibrationData[5] * Math.Pow(2, 14) + calibrationData[6] * t_r / Math.Pow(2, 3) + ((calibrationData[7] * t_r / Math.Pow(2, 15)) * t_r) / Math.Pow(2, 4);
            return (S * p_r + O) / Math.Pow(2, 14);
        }

        public override async Task EnableSensor()
        {
            await StoreAndReadCalibrationValues();
            await base.EnableSensor();
        }

        private async Task StoreAndReadCalibrationValues()
        {
            GattCharacteristic tempConfig = deviceService.GetCharacteristics(new Guid(SensorConfigUuid))[0];

            byte[] confdata = new byte[] { 2 };
            var status = await tempConfig.WriteValueAsync(confdata.AsBuffer());
            if (status == GattCommunicationStatus.Unreachable)
            {
                //throw new ArgumentOutOfRangeException();
                Debug.WriteLine("*StoreAndReadCalibrationValues: tempConfig.WriteValueAsync error");
                return;
            }
            //GattCharacteristic calibrationCharacteristic = deviceService.GetCharacteristics(new Guid(SensorTagUuid.UUID_BAR_CALI))[0];
            var calibrationCharacteristicList = deviceService.GetCharacteristics(new Guid(SensorTagUuid.UUID_BAR_CALI));
            if (calibrationCharacteristicList != null && calibrationCharacteristicList.Count > 0)
            {
                GattCharacteristic calibrationCharacteristic = calibrationCharacteristicList[0];

                var res = await calibrationCharacteristic.ReadValueAsync(Windows.Devices.Bluetooth.BluetoothCacheMode.Uncached);
                if (res.Status == GattCommunicationStatus.Unreachable)
                    throw new ArgumentOutOfRangeException();

                var sdata = new byte[res.Value.Length];

                DataReader.FromBuffer(res.Value).ReadBytes(sdata);

                calibrationData[0] = BitConverter.ToUInt16(sdata, 0);
                calibrationData[1] = BitConverter.ToUInt16(sdata, 2);
                calibrationData[2] = BitConverter.ToUInt16(sdata, 4);
                calibrationData[3] = BitConverter.ToUInt16(sdata, 6);
                calibrationData[4] = BitConverter.ToInt16(sdata, 8);
                calibrationData[5] = BitConverter.ToInt16(sdata, 10);
                calibrationData[6] = BitConverter.ToInt16(sdata, 12);
                calibrationData[7] = BitConverter.ToInt16(sdata, 14);
            }
            else
            {
                Debug.WriteLine("*StoreAndReadCalibrationValues: GetCharacteristics error");
            }
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
            if (scale == TemperatureScale.Celsius)
                return BitConverter.ToUInt16(sensorData, 2) / 128.0;
            else
                return (BitConverter.ToUInt16(sensorData, 2) / 128.0) * 1.8 + 32;
        }

        public static double CalculateTargetTemperature(byte[] sensorData, TemperatureScale scale)
        {
            if (scale == TemperatureScale.Celsius)
                return CalculateTargetTemperature(sensorData, (BitConverter.ToUInt16(sensorData, 2) / 128.0));
            else
                return CalculateTargetTemperature(sensorData, (BitConverter.ToUInt16(sensorData, 2) / 128.0)) * 1.8 + 32;
        }

        public static double CalculateTargetTemperature(byte[] sensorData, double ambientTemperature, TemperatureScale scale)
        {
            if (scale == TemperatureScale.Celsius)
                return CalculateTargetTemperature(sensorData, ambientTemperature);
            else
                return CalculateTargetTemperature(sensorData, ambientTemperature) * 1.8 + 32;
        }

        private static double CalculateTargetTemperature(byte[] sensorData, double ambientTemperature)
        {
            double Vobj2 = BitConverter.ToInt16(sensorData, 0);
            Vobj2 *= 0.00000015625;

            double Tdie = ambientTemperature + 273.15;

            double S0 = 5.593E-14;
            double a1 = 1.75E-3;
            double a2 = -1.678E-5;
            double b0 = -2.94E-5;
            double b1 = -5.7E-7;
            double b2 = 4.63E-9;
            double c2 = 13.4;
            double Tref = 298.15;
            double S = S0 * (1 + a1 * (Tdie - Tref) + a2 * Math.Pow((Tdie - Tref), 2));
            double Vos = b0 + b1 * (Tdie - Tref) + b2 * Math.Pow((Tdie - Tref), 2);
            double fObj = (Vobj2 - Vos) + c2 * Math.Pow((Vobj2 - Vos), 2);
            double tObj = Math.Pow(Math.Pow(Tdie, 4) + (fObj / S), .25);

            return tObj - 273.15;
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
