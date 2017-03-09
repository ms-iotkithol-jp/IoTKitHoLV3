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
        /// <summary>
        /// Temperature
        /// </summary>
        public float temp { get; set; }
        public float atemp { get; set; }
        public float otemp { get; set; }
        public float htemp { get; set; }
        public float ptemp { get; set; }
        public float hum { get; set; }
        public float light { get; set; }
        public float press { get; set; }
        public float accelx { get; set; }
        public float accely { get; set; }
        public float accelz { get; set; }
        public float gyrox { get; set; }
        public float gyroy { get; set; }
        public float gyroz { get; set; }
        public float magx { get; set; }
        public float magy { get; set; }
        public float magz { get; set; }
        public bool lkey { get; set; }
        public bool rkey { get; set; }

        /// <summary>
        /// Measured Time
        /// </summary>
        public DateTime time { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
