using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IoTWeb.Models
{
    public class SASSensorTable : TableEntity
    {
        public string deviceId { get; set; }
        public double accelx { get; set; }
        public double accely { get; set; }
        public double accelz { get; set; }
        public double temp { get; set; }
        public DateTime time { get; set; }
    }

    public class DeviceStatus
    {
        public string DeviceId { get; set; }
        public double tempavg { get; set; }
        public double accelxavg { get; set; }
        public double accelyavg { get; set; }
        public double accelzavg { get; set; }
        public string Status { get; set; }
        public DateTime time { get; set; }
    }

    public class SensorReadingUnit
    {
        public string SensorType { get; set; }
        public double SensorValue { get; set; }
    }

    public class SensorStatisticsUnit
    {
        public string SensorType { get; set; }
        public int Count { get; set; }
        public double SensorValueAvg { get; set; }
        public double SensorValueMin { get; set; }
        public double SensorValueMax { get; set; }
        public double SensorValueStd { get; set; }
    }

    public class SensorStatisticsPacket
    {
        public string DeviceId { get; set; }
        public DateTime StartTimestamp { get; set; }
        public DateTime EndTimestamp { get; set; }
        public List<SensorStatisticsUnit> SensorStatistics { get; set; }
    }


}