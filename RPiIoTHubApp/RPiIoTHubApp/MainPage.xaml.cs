//#define ACCESS_MOBILE_SERVICE
//#define ACCESS_IOT_HUB
using IoTDevice;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 を参照してください

namespace RPiIoTHubApp
{
#if (ACCESS_MOBILE_SERVICE)
    using Microsoft.WindowsAzure.MobileServices;
#endif
#if (ACCESS_IOT_HUB)
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
#endif
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Device Entry Configuration
        string DeviceEntryEndPoint = "http://[MobileAppName].azurewebsites.net";

        // Identifier of this board. this value will be set by this app.
        Guid deviceId = new Guid(/* Your Guid */);
        double Latitude = 35.62661;
        double Longitude = 139.740987;

        // IoT Hub Configuration
        string IoTHubEndpoint = "[IoTHubName].azure-devices.net";
        string DeviceKey = "";
        bool IoTServiceAvailabled = true;

        int measureIntervalMSec = 1000;
        DispatcherTimer measureTimer;
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        IoTKitHoLSensor sensor;
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var result = await TryConnect();
            sensor = IoTKitHoLSensor.GetCurrent(IoTKitHoLSensor.TemperatureSensor.BME280);
            measureTimer = new DispatcherTimer();
            measureTimer.Interval = TimeSpan.FromMilliseconds(measureIntervalMSec);
            measureTimer.Tick += MeasureTimer_Tick;
            measureTimer.Start();

            InitializeUpload();
        }

        //      private string DeviceId = "";
        async Task<bool> TryConnect()
        {
            bool result = false;
            var client = new Windows.Web.Http.HttpClient();
            client.DefaultRequestHeaders.Add("device-id", deviceId.ToString());
            client.DefaultRequestHeaders.Add("device-message", "Hello from RPi2");
            var response = client.GetAsync(new Uri("http://egholservice.azurewebsites.net/api/DeviceConnect"), HttpCompletionOption.ResponseContentRead);
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
        void InitializeUpload()
        {
            EntryDevice();
            if (IoTServiceAvailabled)
            {
                SetupIoTHub();
                uploadTimer = new DispatcherTimer();
                uploadTimer.Interval = TimeSpan.FromMilliseconds(uploadIntervalMSec);
                uploadTimer.Tick += UploadTimer_Tick;
                uploadTimer.Start();
            }
        }

        private void MeasureTimer_Tick(object sender, object e)
        {
            var sensorReading = sensor.TakeMeasurement();
            lock (this)
            {
                lastTemperature = sensorReading.Temperature;
                lastAccelX = sensorReading.AccelX;
                lastAccelY = sensorReading.AccelY;
                lastAccelZ = sensorReading.AccelZ;
            }
            Debug.WriteLine("Measured - accelx=" + sensorReading.AccelX + ",accely=" + sensorReading.AccelY + ",accelz=" + sensorReading.AccelZ + ",temperature=" + sensorReading.Temperature);
        }

        private DispatcherTimer uploadTimer;
        private long uploadIntervalMSec = 1000;

        private void UploadTimer_Tick(object sender, object e)
        {
            uploadTimer.Stop();
            Upload();
            uploadTimer.Start();
        }

        int counter = 0;
        async void Upload()
        {
#if (ACCESS_IOT_HUB)
            var now = DateTime.Now;
            var sensorReading = new Models.SensorReading()
            {
                msgId = deviceId.ToString() + now.ToString("yyyyMMddhhmmssfff")
            };
            lock (this)
            {
                sensorReading.deviceId = deviceId.ToString();
                sensorReading.temp = lastTemperature;
                sensorReading.accelx = lastAccelX;
                sensorReading.accely = lastAccelY;
                sensorReading.accelz = lastAccelZ;
                sensorReading.time = now;
                sensorReading.Latitude = Latitude;
                sensorReading.Longitude = Longitude;
            }
            var payload = JsonConvert.SerializeObject(sensorReading);
            var message = new Message(System.Text.UTF8Encoding.UTF8.GetBytes(payload));
            try
            {
                await deviceClient.SendEventAsync(message);
                Debug.WriteLine("Send[" + counter++ + "]@" + now.Ticks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Event Hub Send Failed:" + ex.Message);
            }
#endif
        }


#if (ACCESS_MOBILE_SERVICE)
        MobileServiceClient mobileService;
#endif
        private async void EntryDevice()
        {
#if (ACCESS_MOBILE_SERVICE)
            if (mobileService == null)
            {
                mobileService = new MobileServiceClient(DeviceEntryEndPoint);
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
                        IoTHubEndpoint = re.IoTHubEndpoint;
                        DeviceKey = re.DeviceKey;
                        Debug.WriteLine("IoT Hub Service Avaliabled");
                    }
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
                    IoTHubEndpoint = IoTHubEndpoint,
                    DeviceKey = DeviceKey
                };
                await table.InsertAsync(entry);
            }
#else
            IoTServiceAvailabled = true;
#endif
        }
#if (ACCESS_IOT_HUB)
        DeviceClient deviceClient;
        string iotHubConnectionString = "";
#endif
        private void SetupIoTHub()
        {
#if (ACCESS_IOT_HUB)
            iotHubConnectionString = "HostName=" + IoTHubEndpoint + ";DeviceId=" + deviceId + ";SharedAccessKey=" + DeviceKey;
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(iotHubConnectionString, Microsoft.Azure.Devices.Client.TransportType.Amqp);
                Debug.Write("IoT Hub Connected.");
                ReceiveCommands();
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }
#endif
        }
#if (ACCESS_IOT_HUB)
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
                        await deviceClient.CompleteAsync(receivedMessage);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("IoT Hub Receive Failed.");
                    Debug.WriteLine(ex.Message);
                }
                await Task.Delay(TimeSpan.FromSeconds(10));
            }

        }
#endif

        double lastTemperature;
        double lastAccelX;
        double lastAccelY;
        double lastAccelZ;
    }
}
