using System;
using System.Collections.Generic;

namespace WinIoTCoreTIIoTHubApp.Models
{
    public class SensorValue
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class SensorReading
    {
#if false
        public List<SensorValue> SensorValues { get; set; }
#else
        /// <summary>
        /// Temperature
        /// </summary>
        public double temp { get; set; }
        /// <summary>
        /// Acceleration X
        /// </summary>
        public double accelx { get; set; }
        /// <summary>
        /// Acceleration Y
        /// </summary>
        public double accely { get; set; }
        /// <summary>
        /// Acceleration Z
        /// </summary>
        public double accelz { get; set; }
        /// <summary>
        /// Measured Time
        /// </summary>
#endif
        /// <summary>
        /// Measured Time
        /// </summary>
        public DateTime time { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

#if (USE_LIGHTSENSE)
        public double Brightness { get; set; }
#endif
    }
}
