using System;
using Microsoft.SPOT;

namespace PinKitIoTHubApp
{
    public static class IoTHoLConfig
    {
        public static string DeviceEntryEndPoint = "[MobileAppName].azurewebsites.net";
        // IoT Hub Configuration
        public static string IoTHubEndpoint = "[IoTHubName].azure-devices.net";
        public static string DeviceKey = "";

        // Location 
        public static double Latitude = 35.62661;
        public static double Longitude = 139.740987;
    }
}
