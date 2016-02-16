using Microsoft.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace IoTWeb.Controllers
{
    public class SASSensorController : ApiController
    {
        public IEnumerable<Models.SASSensorTable> Get()
        {
            var storeCS = CloudConfigurationManager.GetSetting("StorageConnectionString");
            var storageAccount = CloudStorageAccount.Parse(storeCS);
            var tableClient = storageAccount.CreateCloudTableClient();
            var sensorReadingTable = tableClient.GetTableReference("SASSensor");
            var query = new TableQuery<Models.SASSensorTable>().Where(
                TableQuery.GenerateFilterConditionForDate("Timestamp",
 QueryComparisons.GreaterThanOrEqual, DateTimeOffset.Now.AddMonths(-1))
                );
            var results = sensorReadingTable.ExecuteQuery(query).
Select((ent => (Models.SASSensorTable)ent)).ToList();
            return results;

        }
    }

    public class SensorStatisticsController : ApiController
    {

        public Models.SensorStatisticsPacket Get([FromUri]int duringDay)
        {
            var storeCS = CloudConfigurationManager.GetSetting("StorageConnectionString");
            var storageAccount = CloudStorageAccount.Parse(storeCS);
            var tableClient = storageAccount.CreateCloudTableClient();
            var sassTable = tableClient.GetTableReference("SASSensor");
            var srQquery = new TableQuery<Models.SASSensorTable>().Where(
                TableQuery.GenerateFilterConditionForDate("Timestamp",
 QueryComparisons.GreaterThanOrEqual, DateTimeOffset.Now.AddDays(-duringDay))
                );
            string[] sensorTypes = { "accelx", "accely", "accelz", "temp" };
            Dictionary<string, StatUnit> ssUnits = new Dictionary<string, StatUnit>();
            foreach (var st in sensorTypes)
            {
                ssUnits.Add(st, new StatUnit());
                ssUnits[st].Max = Double.MinValue;
                ssUnits[st].Min = Double.MaxValue;
            }

            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now.AddDays(-duringDay);
            var packet = new Models.SensorStatisticsPacket();
            packet.SensorStatistics = new List<Models.SensorStatisticsUnit>();
            foreach (var sass in sassTable.ExecuteQuery(srQquery))
            {
                packet.DeviceId = sass.deviceId;
                if (startTime > sass.time) startTime = sass.time;
                if (endTime < sass.time) endTime = sass.time;
                ssUnits["accelx"].Add(sass.accelx);
                ssUnits["accely"].Add(sass.accely);
                ssUnits["accelz"].Add(sass.accelz);
                ssUnits["temp"].Add(sass.temp);
            }
            packet.EndTimestamp = endTime;
            packet.StartTimestamp = startTime;
            foreach (var st in sensorTypes)
            {
                var unit = new Models.SensorStatisticsUnit()
                {
                    Count = ssUnits[st].Count,
                    SensorType = st,
                    SensorValueMax = ssUnits[st].Max,
                    SensorValueMin = ssUnits[st].Min
                };
                if (unit.Count > 0)
                {
                    unit.SensorValueAvg = ssUnits[st].Avg;
                    unit.SensorValueStd = ssUnits[st].Std;
                }
                packet.SensorStatistics.Add(unit);
            }
            return packet;
        }

        class StatUnit
        {
            public double Total { get; set; }
            public int Count { get; set; } = 0;
            public double Max { get; set; }
            public double Min { get; set; }
            public void Add(double value)
            {
                Count++;
                Total += value;
                if (Max < value) Max = value;
                if (Min > value) Min = value;
                values.Add(value);
            }
            public double Avg { get { return Total / Count; } }
            public double Std
            {
                get
                {
                    double std = 0;
                    double avg = Avg;
                    foreach (var val in values)
                    {
                        std += (val - avg) * (val - avg);
                    }
                    return Math.Sqrt(std / values.Count);
                }
            }
            private List<double> values = new List<double>();
        }
    }
}
