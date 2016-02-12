//#define ACCESS_MOBILE_SERVICE
//#define ACCESS_IOT_HUB
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace WpfEmuIoTHubApp
{
#if (ACCESS_IOT_HUB)
    using Microsoft.Azure.Devices.Client;
#endif
#if (ACCESS_MOBILE_SERVICE)
    using Microsoft.WindowsAzure.MobileServices;
    using Newtonsoft.Json;
#endif
    public partial class MainWindow
    {
        // Device Entry Configuration
        string DeviceEntryEndPoint = "http://[mobile service name].azurewebsites.net";

        // Identifier of this board. this value will be set by this app.
        Guid deviceId = new Guid(0x1a8216fe, 0xf180, 0x48f4, 0x82, 0xb3, 0xab, 0xff, 0x8a, 0x7c, 0xb8, 0xac);
  //      Guid deviceId = new Guid(/* Your Guid */);
        double Latitude = 35.62661;
        double Longitude = 139.740987;

        // IoT Hub Configuration
        string IoTHubEndpoint = "";
        string DeviceKey = "";
        bool IoTServiceAvailabled = false;

        bool TryConnect()
        {
            bool result = false;
            var request = HttpWebRequest.Create("http://egholservice.azurewebsites.net/api/DeviceConnect") as HttpWebRequest;
            request.Headers.Add("device-id", deviceId.ToString());
            request.Headers.Add("device-message", "Hello from Wpf Emulator");
            using (var response = request.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (var stream = response.GetResponseStream())
                    {
                        var reader = new StreamReader(stream);
                        var content = reader.ReadToEnd();
                        Debug.WriteLine("Recieved - " + content);
                        Debug.WriteLine("TryConnect - Succeeded");
                        result = true;
                    }
                }
                else
                {
                    Debug.WriteLine("TryConnect Failed - " + response.StatusCode);
                }
            }
            return result;
        }

        async void InitializeUpload()
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


        private DispatcherTimer uploadTimer;
        private long uploadIntervalMSec = 1000;

        private void UploadTimer_Tick(object sender, object e)
        {
            uploadTimer.Stop();
            Upload();
            uploadTimer.Start();
        }
        int sendCount = 0;
        async void Upload()
        {
#if (ACCESS_IOT_HUB)
            if (deviceClient != null)
            {
                var now = DateTime.Now;
                var sensorReading = new Models.SensorReading()
                {
                    msgId = deviceId.ToString() + now.ToString("yyyyMMddhhmmssfff")
                };
                lock (this)
                {
                    sensorReading.deviceId = deviceId.ToString();
                    sensorReading.accelx = lastAccelX;
                    sensorReading.accely = lastAccelY;
                    sensorReading.accelz = lastAccelZ;
                    sensorReading.time = now;
                    sensorReading.Latitude = Latitude;
                    sensorReading.Longitude = Longitude;
                }
                var payload = JsonConvert.SerializeObject(sensorReading);
                var message = new Message(System.Text.UTF8Encoding.UTF8.GetBytes(payload));
                try {
                    await deviceClient.SendEventAsync(message);
                    Debug.WriteLine("Send[" + sendCount++ + "] - Completed");
                }
                catch (Exception ex)
                {
                    Debug.Write(ex.Message);
                }
            }
#endif
        }

#if (ACCESS_IOT_HUB)
        DeviceClient deviceClient;
        string iotHubConnectionString = "";

#endif
        void SetupIoTHub()
        {
#if (ACCESS_IOT_HUB)
            iotHubConnectionString = "HostName=" + IoTHubEndpoint + ";DeviceId=" + deviceId + ";SharedAccessKey=" + DeviceKey;
            try {
                deviceClient = DeviceClient.CreateFromConnectionString(iotHubConnectionString, Microsoft.Azure.Devices.Client.TransportType.Amqp);
                Debug.Write("IoT Hub Connected.");
            }
            catch(Exception ex)
            {
                Debug.Write(ex.Message);
            }
#endif
        }

#if (ACCESS_MOBILE_SERVICE)
        MobileServiceClient mobileService;
#endif
        private async Task EntryDevice()
        {
            try
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
                            txtIoTHubEndpoint.Text = IoTHubEndpoint;
                            txtDeviceKey.Text = DeviceKey;
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
                        ServiceAvailable = IoTServiceAvailabled,
                        IoTHubEndpoint = IoTHubEndpoint,
                        DeviceKey = DeviceKey
                    };
                    await table.InsertAsync(entry);
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
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
                        txtReceivedCommand.Text = messageData
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
