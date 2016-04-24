using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace IoTWeb.Hubs
{
    [HubName("DeviceStatusHub")]
    public class DeviceStatusHub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }

        public void Update(Models.DeviceStatus status)
        {
            Clients.Others.Update(status);
        }

        public void Test(string msg)
        {
            Clients.Others.Test(msg);
        }
    }

}