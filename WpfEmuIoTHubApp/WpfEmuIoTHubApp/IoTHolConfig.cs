using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfEmuIoTHubApp
{
    public static class IoTHoLConfig
    {
        public static string DeviceEntryEndPoint = "[MobileAppName].azurewebsites.net";
        // IoT Hub Configuration
        public static string IoTHubEndpoint = "[IoTHubName].azure-devices.net";

        // Identifier of this board. this value will be set by this app.
        public static Guid deviceId = new Guid(/* Your Guid */);

        // Location 
        public static double? Latitude = 35.62661;
        public static double? Longitude = 139.740987;
    }
}
