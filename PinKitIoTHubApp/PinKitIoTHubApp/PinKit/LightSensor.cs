using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace PinKit
{
    /// <summary>
    /// This library use GHI Electronics's LightSense sensor - https://www.ghielectronics.com/catalog/product/336
    /// Please connect the sensor to Socket 1
    /// When you want to use this sensor please define 'USE_LIGHTSENSE' macro in Build property of this project
    /// </summary>
    public class LightSensor
    {
        static Cpu.AnalogChannel aiChannel = (Cpu.AnalogChannel)0;   // P1_8, A0
        AnalogInput aiLightSense;

        public LightSensor()
        {
            aiLightSense = new AnalogInput(aiChannel);
        }

        /// <summary>
        /// Most dark - 0.0
        /// Most bright - 1.0
        /// </summary>
        /// <returns></returns>
        public double TakeMeasurement()
        {
            var raw = aiLightSense.ReadRaw();

            return (double)raw/4096;
        }

    }
}

