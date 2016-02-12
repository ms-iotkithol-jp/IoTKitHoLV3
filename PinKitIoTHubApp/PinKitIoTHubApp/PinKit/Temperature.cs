using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace PinKit
{
    public class Temperature
    {
        static Cpu.AnalogChannel aiChannel = (Cpu.AnalogChannel)7;   // P1_15, A5
        AnalogInput aiThermistor;

        public Temperature()
        {
            aiThermistor=new AnalogInput(aiChannel);
            VR1 = 5000f;
            Bc = 3435f;
        }
        /// <summary>
        /// Adjust VR1 - Default 5000ƒ¶
        /// </summary>
        public double VR1 { get; set; }
        /// <summary>
        /// B constant - Default 3435ƒ¶
        /// </summary>
        public double Bc { get; set; }

        public double TakeMeasurement()
        {
            var raw = aiThermistor.ReadRaw();

            double tk = 273f;
            double t25 = tk + 25f;
            double r25 = 10000f;
            double t = 1 / (System.Math.Log(VR1 * raw / (4096 - raw) / r25) / Bc + 1 / t25) - tk;
            return t;
        }
    }
}
