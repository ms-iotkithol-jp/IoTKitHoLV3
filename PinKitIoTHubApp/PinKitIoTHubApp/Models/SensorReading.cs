using System;
using Microsoft.SPOT;

namespace PinKitIoTApp.Models
{
    public class SensorReading
    {
        /// <summary>
        /// Device Id
        /// </summary>
        public string deviceId { get; set; }
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
        public string msgId { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
