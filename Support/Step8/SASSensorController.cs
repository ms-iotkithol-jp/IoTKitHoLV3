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

        public List<Models.SensorStatisticsPacket> Get([FromUri]int duringDay)
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
   
            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now.AddDays(-duringDay);
            var dssUnits = new Dictionary<string, Dictionary<string, StatUnit>>();
            var dstatistics = new Dictionary<string, Models.SensorStatisticsPacket>();
            foreach(var sass in sassTable.ExecuteQuery(srQquery))
            {
                if (!dssUnits.ContainsKey(sass.deviceId))
                {
                    Dictionary<string, StatUnit> units = new Dictionary<string, StatUnit>();
                    foreach (var st in sensorTypes)
                    {
                        units.Add(st, new StatUnit());
                        units[st].Max = Double.MinValue;
                        units[st].Min = Double.MaxValue;
                    }
                    dssUnits.Add(sass.deviceId, units);
                    var ssp = new Models.SensorStatisticsPacket();
                    ssp.StartTimestamp = startTime;
                    ssp.EndTimestamp = endTime;
                    ssp.DeviceId = sass.deviceId;
                    ssp.SensorStatistics = new List<Models.SensorStatisticsUnit>();
                    dstatistics.Add(sass.deviceId, ssp);
                }
                if (dstatistics[sass.deviceId].StartTimestamp > sass.time)
                {
                    dstatistics[sass.deviceId].StartTimestamp = sass.time;
                }
                if (dstatistics[sass.deviceId].EndTimestamp < sass.time)
                {
                    dstatistics[sass.deviceId].EndTimestamp = sass.time;
                }
                dssUnits[sass.deviceId]["accelx"].Add(sass.accelx);
                dssUnits[sass.deviceId]["accely"].Add(sass.accely);
                dssUnits[sass.deviceId]["accelz"].Add(sass.accelz);
                dssUnits[sass.deviceId]["temp"].Add(sass.temp);
            }
            var stats = new List<Models.SensorStatisticsPacket>();
            foreach (var devId in dssUnits.Keys)
            {
                foreach (var st in sensorTypes)
                {
                    var unit = new Models.SensorStatisticsUnit()
                    {
                        Count = dssUnits[devId][st].Count,
                        SensorType = st,
                        SensorValueMax = dssUnits[devId][st].Max,
                        SensorValueMin = dssUnits[devId][st].Min
                    };
                    if (unit.Count > 0)
                    {
                        unit.SensorValueAvg = dssUnits[devId][st].Avg;
                        unit.SensorValueStd = dssUnits[devId][st].Std;
                        dstatistics[devId].SensorStatistics.Add(unit);
                    }
                }
                stats.Add(dstatistics[devId]);
            }
            return stats;
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
