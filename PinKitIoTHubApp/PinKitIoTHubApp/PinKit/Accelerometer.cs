using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace PinKit
{
    public class Accelerometer
    {
        const byte ADXL345_ADDR = 0x1d;
        // ADXL345 registers
        const byte R_DEVID = 0x00;
        const byte R_THRESH_TAP = 0x1D;
        const byte R_OFSX = 0x1E;
        const byte R_OFSY = 0x1F;
        const byte R_OFSZ = 0x20;
        const byte R_DUR = 0x21;
        const byte R_Latent = 0x22;
        const byte R_Window = 0x23;
        const byte R_THRESH_INACT = 0x25;
        const byte R_TIME_INACT = 0x26;
        const byte R_ACT_INACT_CTL = 0x27;
        const byte R_THRESH_FF = 0x28;
        const byte R_TIME_FF = 0x29;
        const byte R_TAP_AXES = 0x2A;
        const byte R_ACT_TAP_STATUS = 0x2B;
        const byte R_BW_RATE = 0x2C;
        const byte R_POWER_CTL = 0x2D;
        const byte R_INT_ENABLE = 0x2E;
        const byte R_INT_MAP = 0x2F;
        const byte R_INT_SOURCE = 0x30;
        const byte R_DATA_FORMAT = 0x31;
        const byte R_DATAX0 = 0x32;
        const byte R_DATAX1 = 0x33;
        const byte R_DATAY0 = 0x34;
        const byte R_DATAY1 = 0x35;
        const byte R_DATAZ0 = 0x36;
        const byte R_DATAZ1 = 0x37;
        const byte R_FIFO_CTL = 0x38;
        const byte R_FIFO_STATUS = 0x39;

        private I2CDevice i2c;
        private int timeout = 1000;
        private byte[] adata = new byte[1];
        private byte[] rdata = new byte[1];
        private byte[] wdata = new byte[2];
        private I2CDevice.I2CTransaction[] trRegRead;
        private I2CDevice.I2CTransaction[] trRegWrite;
        public byte[] xyz = new byte[6];

        public byte RegRead(byte reg)
        {
            adata[0] = reg;
            trRegRead = new I2CDevice.I2CTransaction[] { 
                I2CDevice.CreateWriteTransaction(adata),
                I2CDevice.CreateReadTransaction(rdata) };
            int result = i2c.Execute(trRegRead, timeout);
#if DEBUG_ADXL345
            Debug.Print("R[0x" + reg.ToString("X2") + "]=0x" + rdata[0].ToString("X2"));
#endif
            return rdata[0];
        }

        public void RegReads(byte reg, ref byte[] data)
        {
            adata[0] = reg;
            trRegRead = new I2CDevice.I2CTransaction[] { 
                I2CDevice.CreateWriteTransaction(adata),
                I2CDevice.CreateReadTransaction(data) };
            int result = i2c.Execute(trRegRead, timeout);
#if DEBUG_ADXL345
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Print("R[0x" + (reg + i).ToString("X2") + "]=0x" + data[i].ToString("X2"));
            }
#endif
        }

        public void RegWrite(byte reg, byte val)
        {
            wdata[0] = reg;
            wdata[1] = val;
            trRegWrite = new I2CDevice.I2CTransaction[] { I2CDevice.CreateWriteTransaction(wdata) };
            int result = i2c.Execute(trRegWrite, timeout);
        }

        public void RegWriteMask(byte reg, byte val, byte mask)
        {
            byte tmp = RegRead(reg);
            wdata[0] = reg;
            wdata[1] = (byte)(((int)tmp & ~(int)mask) | ((int)val & (int)mask));
            trRegWrite = new I2CDevice.I2CTransaction[] { I2CDevice.CreateWriteTransaction(wdata) };
            int result = i2c.Execute(trRegWrite, timeout);
        }

        public byte DeviceID()
        {
            return RegRead(R_DEVID);
        }

        public void ToSleep()
        {
            RegWriteMask(R_POWER_CTL, 0x04, 0x04);
        }

        public void ToWakeup()
        {
            RegWriteMask(R_POWER_CTL, 0x00, 0x04);
        }

        // Normal Power
        // Code     Output  BandWidth
        // 0b1111   3200Hz  (1600Hz)
        // 0b1110   1600Hz  (800Hz)
        // 0b1101   800Hz   (40Hz)
        // 0b1100   400Hz   (200Hz)
        // 0b1011   200Hz   (100Hz)
        // 0b1010   100Hz   (50Hz)
        // 0b1001   50Hz    (25H)
        // 0b1000   25Hz    (12.5Hz)
        // 0b0111   12.5Hz  (6.25Hz)
        // 0b0110   6.25Hz  (3.125Hz)
        // Low Power
        // 0b1100   400Hz   (200Hz)
        // 0b1011   200Hz   (100Hz)
        // 0b1010   100Hz   (50Hz)
        // 0b1001   50Hz    (25H)
        // 0b1000   25Hz    (12.5Hz)
        // 0b0111   12.5Hz  (6.25Hz)
        public void SetBW_RATE(byte n)
        {
            RegWrite(R_BW_RATE, n);
        }

        // D7: SELF_TEST
        // D6: SPI
        // D5: INT_INVERT
        // D4: 0
        // D3: FULL_RES
        // D2: Justfy
        // D1-D0: Range
        // 0 - 0: +-2g
        // 0 - 1: +-4g
        // 1 - 0: +-8g
        // 1 - 1: +-16g
        public void SetDataFormat(byte n)
        {
            RegWrite(R_DATA_FORMAT, n);
        }

        public void SetFullResolution()
        {
            RegWriteMask(R_DATA_FORMAT, 0x08, 0x08);
        }

        public void Measure()
        {
            RegWriteMask(R_POWER_CTL, 0x08, 0x08);
        }

        private void ReadXYZ(out Int16 x, out Int16 y, out Int16 z)
        {
            RegReads(R_DATAX0, ref xyz);
            x = (Int16)(((UInt16)xyz[1] << 8) + (UInt16)xyz[0]);
            y = (Int16)(((UInt16)xyz[3] << 8) + (UInt16)xyz[2]);
            z = (Int16)(((UInt16)xyz[5] << 8) + (UInt16)xyz[4]);
        }

        private bool measuring = false;
        public Accelerometer(int clockRate=100, int timeout=1000)
        {
            this.timeout = timeout;
            i2c = new I2CDevice(new I2CDevice.Configuration((ushort)ADXL345_ADDR, clockRate));
            ToWakeup();
            System.Threading.Thread.Sleep(10);

            SetDataFormat(0x01);    // +-2g 10bit
            SetBW_RATE(0x19);       // sampling rate 50Hz
     
        }

        public SensorReading TakeMeasurements()
        {
            if (!measuring)
            {
                Measure();
            }
            short x, y, z;
            ReadXYZ(out x, out y, out z);

            return new SensorReading()
            {
                X = ((double)x) / 127f,
                Y = ((double)y) / 127f,
                Z = ((double)z) / 127f
            };
        }
        public class SensorReading
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }
    }
    
}
