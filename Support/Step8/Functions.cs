using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Microsoft.AspNet.SignalR.Client;

namespace DeviceMonitorWebJob
{
    public class Functions
    {
        static HubConnection hub = new HubConnection("http://[user-web-site].azurewebsites.net/");
        static IHubProxy proxy = hub.CreateHubProxy("DeviceStatusHub");

        public static async Task SBQueueListener(
      [ServiceBusTrigger("monitor")] BrokeredMessage msg, TextWriter logger)
        {
            var contentType = msg.ContentType;
            logger.WriteLine("Enqued Sequence Number" + msg.EnqueuedSequenceNumber);
            try
            {
                if (hub.State == ConnectionState.Disconnected)
                {
                    await hub.Start();
                }
                if (stepHub.State== ConnectionState.Disconnected)
                {
                    await stepHub.Start();
                }

                using (var stream = msg.GetBody<Stream>())
                {
                    var reader = new StreamReader(stream);
                    var body = reader.ReadToEnd();
                    body = body.Substring(body.IndexOf("\u0001") + 1);
                    body = body.Substring(0, body.Length - 1);
                    var rcvdDevStatus = JsonConvert.DeserializeObject<DeviceStatus>(body);
                    var deviceStatus = new DeviceStatus()
                    {
                        DeviceId = rcvdDevStatus.DeviceId,
                        tempavg = rcvdDevStatus.tempavg,
                        accelxavg = rcvdDevStatus.accelxavg,
                        accelyavg = rcvdDevStatus.accelyavg,
                        accelzavg = rcvdDevStatus.accelzavg,
                        Status = "normal",
                        time = rcvdDevStatus.time
                    };
                    int currentStatus = 0;
                    if (rcvdDevStatus.tempavg > 32)
                    {
                        currentStatus = 1;
                        deviceStatus.Status = "Too Hot!";
                    }
                    else
                    {
                        currentStatus = 2;
                    }
                    if (LastStatus != currentStatus)
                    {
                        await proxy.Invoke("Update", new[] { deviceStatus });
                        LastStatus = currentStatus;

                        await stepProxy.Invoke("Notify", new[] { new IoTKitHolTrackingPacket() {
                         DeviceId=deviceStatus.DeviceId,
                         HolVersion="V2R3",
                         HolStep="Step9",
                         Message="Working"  } });
                    }

                    logger.WriteLine(body);
                }
                //   msg.
                msg.Complete();
            }
            catch (Exception ex)
            {
                logger.WriteLine("Failed - " + ex.Message);
            }
        }
        static int LastStatus = 0;
        static HubConnection stepHub = new HubConnection("http://egholservice.azurewebsites.net/");
        static IHubProxy stepProxy = stepHub.CreateHubProxy("HoLTrackHub");
    }

    public class DeviceStatus
    {
        public string DeviceId { get; set; }
        public double tempavg { get; set; }
        public double accelxavg { get; set; }
        public double accelyavg { get; set; }
        public double accelzavg { get; set; }
        public string Status { get; set; }
        public DateTime time { get; set; }
    }

    public class IoTKitHolTrackingPacket
    {
        public string DeviceId { get; set; }
        public string HolVersion { get; set; }
        public string HolStep { get; set; }
        public string Message { get; set; }
    }

}
