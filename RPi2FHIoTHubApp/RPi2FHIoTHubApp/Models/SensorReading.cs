using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPi2FHIoTHubApp.Models
{
    public class SensorReading
    {
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
        public DateTime time { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
