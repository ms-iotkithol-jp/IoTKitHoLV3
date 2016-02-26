using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure;

namespace DeviceMonitorWebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            var cs = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var config = new JobHostConfiguration();
            config.UseServiceBus();
            var host = new JobHost(config);
            // The following code ensures that the WebJob will be running continuously

            host.RunAndBlock();
        }


    }

}
