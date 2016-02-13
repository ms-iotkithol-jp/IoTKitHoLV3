//#define ACCESS_MOBILE_SERVICE
//#define ACCESS_IOT_HUB

using System;
using Microsoft.SPOT;
using System.IO;
using System.Net;
using System.Collections;
using System.Threading;
using System.Text;

#if(ACCESS_IOT_HUB)
using Microsoft.Azure.Devices.Client;
#endif

namespace PinKitIoTHubApp
{
    public partial class Program
    {
#if (ACCESS_MOBILE_SERVICE)
        // Device Entry Configuration
        string DeviceEntryEndPoint = "http://[MobileAppName].azurewebsites.net";

#endif

#if(ACCESS_IOT_HUB) 
        // IoT Hub Configuration
        string ioTHubEndpoint = "[IoTHubName].azure-devices.net";
        string deviceKey = "";
#endif

        // Identifier of this board. this value will be set by this app.
        String deviceId = "";
        double Latitude = 35.62661;
        double Longitude = 139.740987;

        bool IoTServiceAvailabled = false;

        private void TryConnect()
        {
            using (var request = HttpWebRequest.Create("http://egholservice.azurewebsites.net/api/DeviceConnect") as HttpWebRequest)
            {
                if (proxyHost != "")
                {
                    request.Proxy = new WebProxy(proxyHost, proxyPort);
                }

                if (IoTDeviceId == "" && pinkit.DeviceName != "")
                {
                    IoTDeviceId = pinkit.DeviceName;
                }
                if (IoTDeviceId != "")
                {
                    request.Headers.Add("device-id", IoTDeviceId.ToString());
                    request.Headers.Add("device-message", "hello from pinkit");
#if false
                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var reader = new StreamReader(response.GetResponseStream());
                            string message = reader.ReadToEnd();
                            Debug.Print(message);
                        }
                    }
#endif
                    deviceId = IoTDeviceId;
                }
            }
        }

        void InitializeUpload()
        {
            EntryDevice();
            if (IoTServiceAvailabled)
            {
                SetupIoTHub();
            }
        }

        int counter = 0;
        void Upload()
        {
            Debug.Print("Sending messages of " + sensorReadings.Count + " to IoT Hub..." + DateTime.Now.Ticks);
#if (ACCESS_IOT_HUB)

            string content = "";
            lock (this)
            {
                content = Json.NETMF.JsonSerializer.SerializeObject(sensorReadings);
            }
            try
            {
                var message = new Message(System.Text.UTF8Encoding.UTF8.GetBytes(content));
                deviceClient.SendEvent(message);
                Debug.Print("Send[" + counter++ + "] - " + DateTime.Now.Ticks);
                BlinkPinKitLED(PinKit.BoardFullColorLED.Colors.Blue, 1000, 500, 5);
            }
            catch (Exception ex)
            {
                Debug.Print("Send Error - " + ex.Message);
                BlinkPinKitLED(PinKit.BoardFullColorLED.Colors.Red, 1000, 500, 10);
            }
#endif
        }

        DispatcherTimer measureTimer;
        DispatcherTimer uploadTimer;
        long measureIntervalMSec = 1000; // measure interval 1000 msec
        long uploadIntervalMSec = 120000;  // upload interval 1000 msec
        
          private void Initialize()
        {
            InitializeUpload();

            measureTimer = new DispatcherTimer();
            measureTimer.Interval = TimeSpan.FromTicks(measureIntervalMSec * TimeSpan.TicksPerMillisecond);
            measureTimer.Tick += MeasureTimer_Tick;
            measureTimer.Start();

            uploadTimer = new DispatcherTimer();
            uploadTimer.Interval = TimeSpan.FromTicks(uploadIntervalMSec * TimeSpan.TicksPerMillisecond);
            uploadTimer.Tick += UploadTimer_Tick;
            uploadTimer.Start();
        }

        private void UploadTimer_Tick(object sender, EventArgs e)
        {
            uploadTimer.Stop();
            lock (this)
            {
                Upload();
            }
            uploadTimer.Start();
        }

        private void MeasureTimer_Tick(object sender, EventArgs e)
        {
            measureTimer.Stop();

            lock (this)
            {
                    var now = DateTime.Now;
                    var accel = pinkit.Accelerometer.TakeMeasurements();
                    var sensorReading = new PinKitIoTApp.Models.SensorReading()
                    {
                        temp = pinkit.Temperature.TakeMeasurement(),
                        accelx = accel.X,
                        accely = accel.Y,
                        accelz = accel.Z,
                        deviceId = this.deviceId,
                        msgId = deviceId.ToString() + now.ToString("yyyyMMddhhmmssfff"),
                        time = now,
                        Latitude = Latitude,
                        Longitude = Longitude
                    };
                    Debug.Print("Accelerometer:X=" + accel.X.ToString() + ",Y=" + accel.Y.ToString() + ",Z=" + accel.Z.ToString());
                    Debug.Print("Temperature:" + sensorReading.temp.ToString());
                    sensorReadings.Add(sensorReading);
            }
            measureTimer.Start();
        }

#if (ACCESS_MOBILE_SERVICE)
        EGIoTKit.Utility.SimpleMobileAppsClient mobileService;
        string DeviceEntryTableName = "DeviceEntry";
#endif
        private void EntryDevice()
        {
#if (ACCESS_MOBILE_SERVICE)
            if (mobileService == null)
            {
                mobileService = new EGIoTKit.Utility.SimpleMobileAppsClient(DeviceEntryEndPoint);
            }
            var registered = mobileService.Query(DeviceEntryTableName);
            bool registed = false;
            if (registered != null && registered.Count > 0)
            {
                foreach (var re in registered)
                {
                    var registedEntry = re as Hashtable;
                    if ((string)(registedEntry["deviceId"]) == deviceId)
                    {
                        if (registedEntry["serviceAvailable"] != null)
                        {
                            IoTServiceAvailabled = (bool)registedEntry["serviceAvailable"];
                            if (IoTServiceAvailabled)
                            {
                                ioTHubEndpoint = (string)registedEntry["iotHubEndpoint"];
                                deviceKey = (string)registedEntry["deviceKey"];
                                Debug.Print("IoT Hub Service Availabled - IoTHubEndpoint=" + ioTHubEndpoint + ",deviceKey=" + deviceKey);
                            }
                        }
                        registed = true;
                        break;
                    }
                }
            }
            if (!registed)
            {
                var entry = new Models.DeviceEntry()
                {
                    DeviceId = deviceId.ToString(),
                    ServiceAvailable = false,
                    DeviceKey = "",
                    IoTHubEndpoint = ""
                };
                var insertedItem = mobileService.Insert(DeviceEntryTableName, entry);
            }
#else
#if (ACCESS_IOT_HUB)
            IoTServiceAvailabled = true;
#endif
#endif
            if (IoTServiceAvailabled)
            {
                sensorReadings = new ArrayList();
            }
        }

#if (ACCESS_IOT_HUB)
        DeviceClient deviceClient;
#endif
        string iotHubConnectionString = "";
        ArrayList sensorReadings;

        private void SetupIoTHub()
        {
#if (ACCESS_IOT_HUB)
            iotHubConnectionString = "HostName=" + ioTHubEndpoint + ";DeviceId=" + deviceId + ";SharedAccessKey=" + deviceKey;
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(iotHubConnectionString, TransportType.Http1);
                BlinkPinKitLED(PinKit.BoardFullColorLED.Colors.Green, 1000, 500, 3);
                ReceiveCommands();
            }
            catch (Exception ex)
            {
                Debug.Print("IoT Hub Connection Failed. - " + ex.Message);
                uploadTimer.Stop();
            }
#endif
        }
#if (ACCESS_IOT_HUB)
        long receiveTimeoutSec = 60;

        private void ReceiveCommands()
        {
            new Thread(() =>
            {
                Hashtable commandColors = new Hashtable();
                commandColors.Add("black", PinKit.BoardFullColorLED.Colors.Black);
                commandColors.Add("red", PinKit.BoardFullColorLED.Colors.Red);
                commandColors.Add("green", PinKit.BoardFullColorLED.Colors.Green);
                commandColors.Add("yellow", PinKit.BoardFullColorLED.Colors.Yellow);
                commandColors.Add("blue", PinKit.BoardFullColorLED.Colors.Blue);
                commandColors.Add("magenta", PinKit.BoardFullColorLED.Colors.Magenta);
                commandColors.Add("cyan", PinKit.BoardFullColorLED.Colors.Cyan);
                commandColors.Add("white", PinKit.BoardFullColorLED.Colors.White);

                Debug.Print("Start to receive command...");
                while (true)
                {
                    try
                    {
                        var receivedMessage = deviceClient.Receive();
                        if (receivedMessage != null)
                        {
                            var buf = receivedMessage.GetBytes();
                            if (buf != null && buf.Length > 0)
                            {
                                var content = new string(System.Text.UTF8Encoding.UTF8.GetChars(buf));                                
                                Debug.Print(DateTime.Now.ToLocalTime() + "> Received Message:" + content);

                                deviceClient.Complete(receivedMessage);

                                string lContent = content.ToLower();
                                foreach(var ccKey in commandColors.Keys)
                                {
                                    if (lContent.IndexOf((string)ccKey) > 0)
                                    {
                                        pinkit.LED.SetColor((PinKit.BoardFullColorLED.Colors)commandColors[ccKey]);
                                    }
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.Print("Receive Error - " + ex.Message);
                        BlinkPinKitLED(PinKit.BoardFullColorLED.Colors.Yellow, 1000, 500, 5);
                    }
                }
            }).Start();
        }
#endif

        private void BlinkPinKitLED(PinKit.BoardFullColorLED.Colors color, int onmsec,int offmsec, int blinkCount)
        {
            new Thread(() =>
            {
                lock (this.pinkit)
                {
                    while (blinkCount-- > 0)
                    {
                        pinkit.LED.SetColor(color);
                        Thread.Sleep(onmsec);
                        pinkit.LED.SetColor(PinKit.BoardFullColorLED.Colors.Black);
                        Thread.Sleep(offmsec);
                    }
                }
            }).Start();
        }

    }
}
