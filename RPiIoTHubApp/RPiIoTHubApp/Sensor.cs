using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace IoTDevice
{
    public class IoTKitHoLSensor
    {
        public enum TemperatureSensor {
            BME280,
            TMP102
        }

        private static IoTKitHoLSensor currentSensor;

        public static IoTKitHoLSensor GetCurrent(TemperatureSensor sensorKind)
        {
            if(currentSensor== null)
            {
                currentSensor = new IoTKitHoLSensor(sensorKind);
                currentSensor.Initialize();
            }
            return currentSensor;
        }

        private TemperatureSensor CurrentSensorKind;
        private IoTKitHoLSensor(TemperatureSensor sensorKind)
        {
            CurrentSensorKind = sensorKind;
        }

        private void Initialize()
        {
            switch (CurrentSensorKind)
            {
                case TemperatureSensor.BME280:
                    InitBME280();
                    break;
                case TemperatureSensor.TMP102:
                    InitI2CTemp102();
                    break;
            }
            InitI2CAccel();
        }

        
        async void InitBME280()
        {
            string
aqs = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);
            var dis = await DeviceInformation.FindAllAsync(aqs);

            var settings = new I2cConnectionSettings(BMA280_I2C_ADDR);
            settings.BusSpeed = I2cBusSpeed.FastMode;
            I2CBMA280Sensor = await I2cDevice.FromIdAsync(dis[0].Id, settings);
            if (I2CBMA280Sensor != null)
            {
                byte[] writeBuf_CTRL_MEAS = { I2C_BMA280_REG_CTRL_MEAS_ADDR, 0x27 };
                I2CBMA280Sensor.Write(writeBuf_CTRL_MEAS);

                byte[] writeBuf_CTRL_HUM = { I2C_BMA280_REG_ADDR_CTRL_HUM, 0x01 };
                I2CBMA280Sensor.Write(writeBuf_CTRL_HUM);

                I2CBMA280Sensor.WriteRead(new byte[] { I2C_BMA280_REG_ADDR_CARIB_00 }, carib_00_26);
                I2CBMA280Sensor.WriteRead(new byte[] { I2C_BMA280_REG_ADDR_CARIB_27 }, carib_27_41);

                dig_T1 = (carib_00_26[1] << 8) | carib_00_26[0];
                dig_T2 = (carib_00_26[3] << 8) | carib_00_26[2];
                dig_T3 = (carib_00_26[5] << 8) | carib_00_26[4];
                dig_P1 = (carib_00_26[7] << 8) | carib_00_26[6];
                dig_P2 = (carib_00_26[9] << 8) | carib_00_26[8];
                dig_P3 = (carib_00_26[11] << 8) | carib_00_26[10];
                dig_P4 = (carib_00_26[13] << 8) | carib_00_26[12];
                dig_P5 = (carib_00_26[15] << 8) | carib_00_26[14];
                dig_P6 = (carib_00_26[17] << 8) | carib_00_26[16];
                dig_P7 = (carib_00_26[19] << 8) | carib_00_26[18];
                dig_P8 = (carib_00_26[21] << 8) | carib_00_26[20];
                dig_P9 = (carib_00_26[23] << 8) | carib_00_26[22];
                dig_H1 = carib_00_26[24];
                dig_H2 = (carib_27_41[1] << 8) | carib_27_41[0];
                dig_H3 = carib_27_41[2];
                dig_H4 = (carib_27_41[3] << 4) | (0x0f & carib_27_41[4]);
                dig_H5 = (carib_27_41[5] << 4) | ((carib_27_41[4] >> 4) & 0xf);
                dig_H6 = carib_27_41[6];

            }
        }

        bool IsBME280Measuring()
        {
            byte[] writeBuf = { I2C_BMA280_REG_ADDR_STATUS };
            byte[] readBuf = new byte[1];
            I2CBMA280Sensor.WriteRead(writeBuf, readBuf);
            return (readBuf[0] & 0x08) != 0;
        }

        public ATReading TakeMeasurement()
        {
            var reading = new ATReading();
            switch (CurrentSensorKind)
            {
                case TemperatureSensor.BME280:
                    Int32 temp;
                    UInt32 hum, press;
                    BME280ReadPressureTmeperatureHumidity(out press, out temp, out hum);
                    reading.Temperature = (double)temp / 100.0;
                    double pressure = (double)press / 100;
                    double humidity = (double)hum / 1024;
                    break;
                case TemperatureSensor.TMP102:
                    reading.Temperature = TakeMeasurementTemp102();
                    break;
            }
            var accelReading= ReadI2CAccel();
            reading.AccelX = accelReading.X;
            reading.AccelY = accelReading.Y;
            reading.AccelZ = accelReading.Z;

            return reading;
        }

        // I2C parameters
        private const string I2C_CONTROLLER_NAME = "I2C1";

        // BME280 parameters
        private I2cDevice I2CBMA280Sensor;

        private const byte BMA280_I2C_ADDR = 0x76;
        private const byte I2C_BMA280_REG_CTRL_MEAS_ADDR = 0xF4;
        private const byte I2C_BMA280_REG_ADDR_CONFIG = 0xF5;
        private const byte I2C_BMA280_REG_ADDR_STATUS = 0xF3;
        private const byte I2C_BMA280_REG_ADDR_CTRL_HUM = 0xF2;
        private const byte I2C_BMA280_REG_ADDR_HUM_LSB = 0xFE;
        private const byte I2C_BMA280_REG_ADDR_HUM_MSB = 0xFD;
        private const byte I2C_BMA280_REG_ADDR_TEMP_XLSB = 0xFC;
        private const byte I2C_BMA280_REG_ADDR_TEMP_LSB = 0xFB;
        private const byte I2C_BMA280_REG_ADDR_TEMP_MSB = 0xFA;
        private const byte I2C_BMA280_REG_ADDR_PRESS_XLSB = 0xF9;
        private const byte I2C_BMA280_REG_ADDR_PRESS_LSB = 0xF8;
        private const byte I2C_BMA280_REG_ADDR_PRESS_MSB = 0xF7;
        private const byte I2C_BMA280_REG_ADDR_CARIB_00 = 0x88;
        private const byte I2C_BMA280_REG_ADDR_CARIB_27 = 0xe1;

        private byte[] carib_27_41 = new byte[16];
        private byte[] carib_00_26 = new byte[26];
        int dig_T1, dig_T2, dig_T3;
        int dig_P1, dig_P2, dig_P3, dig_P4, dig_P5, dig_P6, dig_P7, dig_P8, dig_P9;
        int dig_H1, dig_H2, dig_H3, dig_H4, dig_H5, dig_H6;

        private void BME280ReadUncompPressureTemperatureHumidity(out Int32 unCompPressure, out Int32 unCompTemperature, out Int32 unCompHumidity)
        {
            byte[] writeBuf = { I2C_BMA280_REG_ADDR_PRESS_MSB };
            byte[] readBuf = new byte[8];
            I2CBMA280Sensor.WriteRead(writeBuf, readBuf);

            unCompPressure=(Int32)((
                ((UInt32)(readBuf[0]))<<12)|
                (((UInt32)(readBuf[1])) << 4) |
                ((UInt32)readBuf[2]) >> 4);
            unCompTemperature = (Int32)((
                ((Int32)(readBuf[3])) << 12) |
                (((Int32)(readBuf[4])) << 4) |
                ((Int32)readBuf[5] >> 4));
            unCompHumidity = (Int32)((
                ((Int32)(readBuf[6])) << 8) |
                ((Int32)(readBuf[7])));
        }
        Int32 t_Fine;

        Int32 BME280CompensateTemperature(Int32 uncompTemperature)
        {
            Int32 x1U32r = 0;
            Int32 x2U32r = 0;
            Int32 temperature = 0;

            x1U32r =
                ((((uncompTemperature >> 3) - ((Int32)dig_T1 << 1)))
                * ((Int32)dig_T2)) >> 11;
            x2U32r = (((((uncompTemperature >> 4) - ((Int32)dig_T1))
                * ((uncompTemperature >> 4) - ((Int32)dig_T1))) >> 12) *
                ((Int32)dig_T3)) >> 14;
            t_Fine = x1U32r + x2U32r;
            temperature = (t_Fine * 5 + 128) >> 8;
            return temperature;
        }
        UInt32 BME280CompensatePressure(Int32 uncompPressure)
        {
            Int32 x1U32 = 0;
            Int32 x2U32 = 0;
            UInt32 pressure = 0;
            x1U32 = (((Int32)t_Fine) >> 1) - (Int32)64000;
            x2U32 = (((x1U32 >> 2) * (x1U32 >> 2)) >> 11) * ((Int32)dig_P6);
            x2U32 = x2U32 + ((x1U32 * ((Int32)dig_P5)) << 1);
            x2U32 = (x2U32 >> 2) + (((Int32)dig_P4) << 16);
            x1U32 = (((dig_P3 * (((x1U32 >> 2) * (x1U32 >> 2)) >> 13))
                >> 3) + ((((Int32)dig_P2) * x1U32) >> 1)) >> 18;
            x1U32 = ((((32768)) * ((Int32)dig_P1)) >> 15);
            pressure = (((UInt32)(((Int32)1048576) - uncompPressure)
                - ((UInt32)x2U32 >> 12))) * 3125;

            if (pressure > 0)
            {
                if (x1U32 != 0)
                {
                    pressure = (pressure << 1) / ((UInt32)x1U32);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                if (x1U32 != 0)
                {
                    pressure = (pressure / (UInt32)x1U32) * 2;
                }
                else
                {
                    return 0;
                }
            }
            x1U32 = (((Int32)dig_P9) *
                ((Int32)(((pressure >> 3)
                * (pressure >> 3))
                >> 13)))
                >> 12;
            x2U32 = (((Int32)(pressure >> 2)) *
                ((Int32)dig_P8)) >> 13;
            pressure = (UInt32)((Int32)pressure +
                ((x1U32 + x2U32 + dig_P7) >> 4));
            return pressure;
        }
        UInt32 BME280CompensateHumidity(Int32 uncompHumidity)
        {
            int x1U32 = 0;

            x1U32 = (t_Fine - ((Int32)76800));
            x1U32 = (((((uncompHumidity << 14) -
                (((Int32)dig_H4) << 20) -
                (((Int32)dig_H5) * x1U32)) +
                ((Int32)16384)) >> 15)
                * (((((((x1U32 * ((Int32)dig_H6)) >> 10) *
                (((x1U32 * ((Int32)dig_H3)) >> 11) + ((Int32)32768)))
                >> 10) + ((Int32)2097152)) *
                ((Int32)dig_H2) + 8192) >> 14));
            x1U32 = (x1U32 - (((((x1U32 >> 15) *
                (x1U32 >> 15)) >> 7) *
                ((Int32)dig_H1)) >> 4));
            x1U32 = (x1U32 < 0 ? 0 : x1U32);
            x1U32 = (x1U32 > 419430400 ? 419430400 : x1U32);
            return (UInt32)(x1U32 >> 12);
        }

        // Refered - https://github.com/BoschSensortec/BME280_driver/blob/master/bme280.c
        private void BME280ReadPressureTmeperatureHumidity(out UInt32 pressure, out Int32 temperature, out UInt32 humidity)
        {
            Int32 unCompPressure = 0;
            Int32 unCompTemperature = 0;
            Int32 unCompHumidity = 0;

            BME280ReadUncompPressureTemperatureHumidity(out unCompPressure, out unCompTemperature, out unCompHumidity);
            temperature = BME280CompensateTemperature(unCompTemperature);
            pressure = BME280CompensatePressure(unCompPressure);
            humidity = BME280CompensateHumidity(unCompHumidity);
        }

        private const byte TMP102_I2C_ADDR = 0x48;
        private I2cDevice I2CTMP102;

        private const byte I2CTMP102_REG_READ_TEMP = 0x00;
        private const byte I2CTMP102_REG_CONFIG = 0x01;
        private const byte I2CTMP102_REG_READ_MIN_TEMP = 0x02;
        private const byte I2CTMP102_REG_READ_MAX_TEMP = 0x03;

        private async void InitI2CTemp102()
        {
            try
            {
                string
                    aqs = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);
                var dis = await DeviceInformation.FindAllAsync(aqs);

                var settings = new I2cConnectionSettings(TMP102_I2C_ADDR);
                settings.BusSpeed = I2cBusSpeed.FastMode;
                I2CTMP102 = await I2cDevice.FromIdAsync(dis[0].Id, settings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        private double TakeMeasurementTemp102()
        {
            byte[] writeBuf = new byte[] { I2CTMP102_REG_READ_TEMP };
            byte[] readBuf = new byte[2];
            I2CTMP102.WriteRead(writeBuf, readBuf);
            int valh = ((int)readBuf[0]) << 4;
            int vall = ((int)readBuf[1] >> 4);
            int val = valh + vall;
            double unit = 0.0625;
            if ((val & 0x800) != 0)
            {
                val = 0x1000 - val;
                unit *= -1;
            }
            double reading = val * unit;
            return reading;
        }

        private const byte ACCEL_I2C_ADDR = 0x53;
        // PinKit ADXL345
        //  private const byte ACCEL_I2C_ADDR = 0x1d;
        private I2cDevice I2CAccel;

        private const byte ACCEL_REG_POWER_CONTROL = 0x2D;
        private const byte ACCEL_REG_DATA_FORMAT = 0x31;
        private const byte ACCEL_REG_X = 0x32;
        private async void InitI2CAccel()
        {
            try
            {
                string
                    aqs = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);
                var dis = await DeviceInformation.FindAllAsync(aqs);

                var settings = new I2cConnectionSettings(ACCEL_I2C_ADDR);
                settings.BusSpeed = I2cBusSpeed.FastMode;
                //    var aqs = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);
                //     var dis = await DeviceInformation.FindAllAsync(aqs);
                I2CAccel = await I2cDevice.FromIdAsync(dis[0].Id, settings);

                byte[] WriteBuf_DataFormat = new byte[] { ACCEL_REG_DATA_FORMAT, 0x01 };
                byte[] WriteBuf_PowerControl = new byte[] { ACCEL_REG_POWER_CONTROL, 0x08 };

                I2CAccel.Write(WriteBuf_DataFormat);
                I2CAccel.Write(WriteBuf_PowerControl);

            }
            catch (Exception ex)
            {

            }
        }

        private Acceleration ReadI2CAccel()
        {
            const int ACCEL_RES = 1024;
            const int ACCEL_DYN_RANGE_G = 8;
            const int UNITS_PER_G = ACCEL_RES / ACCEL_DYN_RANGE_G;

            byte[] RegAddrBuf = new byte[] { ACCEL_REG_X };
            byte[] ReadBuf = new byte[6];

            I2CAccel.WriteRead(RegAddrBuf, ReadBuf);

            short rx = BitConverter.ToInt16(ReadBuf, 0);
            short ry = BitConverter.ToInt16(ReadBuf, 2);
            short rz = BitConverter.ToInt16(ReadBuf, 4);

            Acceleration accel = new Acceleration();
            accel.X = (double)rx / UNITS_PER_G;
            accel.Y = (double)ry / UNITS_PER_G;
            accel.Z = (double)rz / UNITS_PER_G;
            return accel;
        }

        private struct Acceleration
        {
            public
                 double X
            { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }


    }

    public class ATReading
    {
        public double AccelX { get; set; }
        public double AccelY { get; set; }
        public double AccelZ { get; set; }
        public double Temperature { get; set; }
    }
}
