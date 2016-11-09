using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSIoTKiTHoLJP
{
    public static class IoTHoLConfig
    {
        // Device Entry Configuration
        public static string DeviceEntryEndPoint = "http://[mobile service name].azurewebsites.net";

        // Identifier of this board. If you've set the name of your Raspberry Pi2 board, then you don't need to set this value.
        public static Guid DeviceID = new Guid(/* Your Guid */);

        // Your location
        public static double Latitude = 35.62661;
        public static double Longitude = 139.740987;

        // IoT Hub Configuration
        public static string IoTHubEndpoint = "";
        public static string DeviceKey = "";
    }
}
