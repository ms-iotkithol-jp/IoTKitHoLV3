using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPiIoTHubApp.Models
{
    public class DeviceEntry
    {
        public string id { get; set; }
        public string DeviceId { get; set; }
        public bool ServiceAvailable { get; set; }
        public string IoTHubEndpoint { get; set; }
        public string DeviceKey { get; set; }
    }
}
