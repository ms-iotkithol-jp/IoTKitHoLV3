using System;
using Microsoft.SPOT;

namespace PinKitIoTHubApp.Models
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
