//#define ACCESS_MOBILE_SERVICE
//#define ACCESS_IOT_HUB
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using GIS = GHIElectronics.UWP.Shields;

namespace RPi2FHIoTHubApp
{
#if (ACCESS_MOBILE_SERVICE)
    using Microsoft.WindowsAzure.MobileServices;
#endif
#if(ACCESS_IOT_HUB)
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
#endif
    public partial class MainPage
    {
        // Device Entry Configuration
        //　string DeviceEntryEndPoint = "http://[mobile service name].azurewebsites.net";

        bool IoTServiceAvailabled = false;

        async Task<bool> TryConnect()
        {
            bool result = false;
            var client = new Windows.Web.Http.HttpClient();
            client.DefaultRequestHeaders.Add("device-id", deviceId.ToString());
            client.DefaultRequestHeaders.Add("device-message", "Hello from RPi2");
            var response = client.GetAsync(new Uri("http://egholservice.azurewebsites.net/api/DeviceConnect"), Windows.Web.Http.HttpCompletionOption.ResponseContentRead);
            response.AsTask().Wait();
            var responseResult = response.GetResults();
            if (responseResult.StatusCode == Windows.Web.Http.HttpStatusCode.Ok)
            {
                result = true;
                var received = await responseResult.Content.ReadAsStringAsync();
                Debug.WriteLine("Recieved - " + received);
            }
            else
            {
                Debug.WriteLine("TryConnect Failed - " + responseResult.StatusCode);
            }
            return result;
        }

#if (ACCESS_MOBILE_SERVICE)
        MobileServiceClient mobileService;
#endif
        private async Task EntryDevice()
        {
#if (ACCESS_MOBILE_SERVICE)
            if (mobileService == null)
            {
                mobileService = new MobileServiceClient(MSIoTKiTHoLJP.IoTHoLConfig.DeviceEntryEndPoint);
            }
            var table = mobileService.GetTable<Models.DeviceEntry>();
            var registered = await table.Where((de) => de.DeviceId == deviceId.ToString()).ToListAsync();

            bool registed = false;
            if (registered != null && registered.Count > 0)
            {
                foreach (var re in registered)
                {
                    if (re.ServiceAvailable)
                    {
                        MSIoTKiTHoLJP.IoTHoLConfig.IoTHubEndpoint = re.IoTHubEndpoint;
                        MSIoTKiTHoLJP.IoTHoLConfig.DeviceKey = re.DeviceKey;
                        Debug.WriteLine("IoT Hub Service Avaliabled");
                    }
                    IoTServiceAvailabled = re.ServiceAvailable;
                    registed = true;
                    break;
                }
            }
            if (!registed)
            {
                var entry = new Models.DeviceEntry()
                {
                    DeviceId = deviceId.ToString(),
                    ServiceAvailable = false,
                    IoTHubEndpoint = MSIoTKiTHoLJP.IoTHoLConfig.IoTHubEndpoint,
                    DeviceKey = MSIoTKiTHoLJP.IoTHoLConfig.DeviceKey
                };
                await table.InsertAsync(entry);
            }
#else
            if (!string.IsNullOrEmpty(MSIoTKiTHoLJP.IoTHoLConfig.DeviceKey))
            {
                IoTServiceAvailabled = true;
            }
#endif
        }

        DispatcherTimer uploadTimer;
        int uploadIntervalMSec = 120000;
        int sendCount = 0;

        private async Task InitializeUpload()
        {
            await EntryDevice();
            if (IoTServiceAvailabled)
            {
                SetupIoTHub();
                uploadTimer = new DispatcherTimer();
                uploadTimer.Interval = TimeSpan.FromMilliseconds(uploadIntervalMSec);
                uploadTimer.Tick += UploadTimer_Tick;
                uploadTimer.Start();
            }

        }

        string deviceId = "";
        private async void UploadTimer_Tick(object sender, object e)
        {
            uploadTimer.Stop();
            await SendEvent();
            uploadTimer.Start();
        }

#if (ACCESS_IOT_HUB)
        DeviceClient deviceClient;
        string iotHubConnectionString = "";
#endif
        private void SetupIoTHub()
        {
#if (ACCESS_IOT_HUB)
            iotHubConnectionString = "HostName=" + MSIoTKiTHoLJP.IoTHoLConfig.IoTHubEndpoint + ";DeviceId=" + deviceId + ";SharedAccessKey=" + MSIoTKiTHoLJP.IoTHoLConfig.DeviceKey;
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(iotHubConnectionString, Microsoft.Azure.Devices.Client.TransportType.Http1);
                Debug.Write("IoT Hub Connected.");
                ReceiveCommands();
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }
#endif
        }

#if(ACCESS_IOT_HUB)
        async Task ReceiveCommands()
        {
            Debug.WriteLine("\nDevice waiting for commands from IoTHub...\n");
            Message receivedMessage;
            string messageData;

            while (true)
            {
                try
                {
                    receivedMessage = await deviceClient.ReceiveAsync();

                    if (receivedMessage != null)
                    {
                        messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                        Debug.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);
                        tbReceiveStats.Text = "Received - " + messageData + " - @" + DateTime.Now.ToString();
                        var command = messageData.ToLower();
                        if (command.StartsWith("fezhat:"))
                        {
                            var units = command.Split(new char[] { ':' });
                            var unit = units[1].Split(new char[] { ',' });
                            foreach (var order in unit)
                            {
                                GIS.FEZHAT.RgbLed targetLed = null;
                                var frags = order.Split(new char[] { '=' });
                                switch (frags[0].ToUpper())
                                {
                                    case "D2":
                                        targetLed = fezHat.D2;
                                        break;
                                    case "D3":
                                        targetLed = fezHat.D3;
                                        break;
                                }
                                var orderedColor = GIS.FEZHAT.Color.Black;
                                switch (frags[1].ToLower())
                                {
                                    case "black":
                                        orderedColor = GIS.FEZHAT.Color.Black;
                                        break;
                                    case "red":
                                        orderedColor = GIS.FEZHAT.Color.Red;
                                        break;
                                    case "green":
                                        orderedColor = GIS.FEZHAT.Color.Green;
                                        break;
                                    case "yellow":
                                        orderedColor = GIS.FEZHAT.Color.Yellow;
                                        break;
                                    case "blue":
                                        orderedColor = GIS.FEZHAT.Color.Blue;
                                        break;
                                    case "magenta":
                                        orderedColor = GIS.FEZHAT.Color.Magneta;
                                        break;
                                    case "cyan":
                                        orderedColor = GIS.FEZHAT.Color.Cyan;
                                        break;
                                    case "white":
                                        orderedColor = GIS.FEZHAT.Color.White;
                                        break;
                                }
                                targetLed.Color = orderedColor;
                            }
                        }
                        await deviceClient.CompleteAsync(receivedMessage);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    IndicateDebug(GIS.FEZHAT.Color.Red, 3600);
                }
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
#endif
        async Task SendEvent()
        {
#if (ACCESS_IOT_HUB)
            List<SensorReadingBuffer> currentReadings = new List<SensorReadingBuffer>();
            lock (this)
            {
                foreach (var r in lastSensorReading)
                {
                    currentReadings.Add(new SensorReadingBuffer()
                    {
                        AccelX = r.AccelX,
                        AccelY = r.AccelY,
                        AccelZ = r.AccelZ,
                        Temperature = r.Temperature,
                        Brightness = r.Brightness,
                        Timestamp = r.Timestamp
                    });
                }
                lastSensorReading.Clear();
            }
            Debug.WriteLine("Device sending {0} messages to IoTHub...\n", currentReadings.Count);

            try
            {
                List<Models.SensorReading> sendingBuffers = new List<Models.SensorReading>();
                for (int count = 0; count < currentReadings.Count; count++)
                {
                    var sensorReading = new Models.SensorReading()
                    {
                        accelx = currentReadings[count].AccelX,
                        accely = currentReadings[count].AccelY,
                        accelz = currentReadings[count].AccelZ,
                        temp = currentReadings[count].Temperature,
                        time = currentReadings[count].Timestamp,
                        Longitude = MSIoTKiTHoLJP.IoTHoLConfig.Longitude,
                        Latitude = MSIoTKiTHoLJP.IoTHoLConfig.Latitude
                    };
                    sendingBuffers.Add(sensorReading);
                }
                var payload = JsonConvert.SerializeObject(sendingBuffers);
                Message eventMessage = new Message(Encoding.UTF8.GetBytes(payload));
                Debug.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), currentReadings.Count, payload);

                await deviceClient.SendEventAsync(eventMessage);
                tbSendStatus.Text = "Send[" + sendCount++ + "]@" + DateTime.Now.ToString();
                IndicateDebug(GIS.FEZHAT.Color.Blue, 10);
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
                IndicateDebug(GIS.FEZHAT.Color.Yellow, 3600);
            }
#endif
        }



        DispatcherTimer debugTimer;
        int debugCount = 0;
        int debugRound = 0;
        GIS.FEZHAT.Color debugColor;
        void IndicateDebug(GIS.FEZHAT.Color color, int round)
        {
            if (debugTimer == null)
            {
                debugTimer = new DispatcherTimer();
                debugTimer.Interval = TimeSpan.FromMilliseconds(500);
                debugTimer.Tick += DebugTimer_Tick;
            }
            debugCount = 0;
            debugRound = round;
            debugColor = color;
            debugTimer.Start();
        }

        private void DebugTimer_Tick(object sender, object e)
        {
            if ((debugCount % 2) == 0)
            {
                fezHat.D3.Color = debugColor;
            }
            else
            {
                fezHat.D3.TurnOff();
            }
            debugCount++;
            if (debugCount > debugRound)
            {
                fezHat.D3.TurnOff();
                debugTimer.Stop();
            }
        }

    }
}
