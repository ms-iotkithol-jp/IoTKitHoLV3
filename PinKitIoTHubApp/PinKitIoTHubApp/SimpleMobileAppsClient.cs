using System;
using Microsoft.SPOT;
using System.Collections;
using System.Net;
using System.IO;

namespace EGIoTKit.Utility
{
    public class SimpleMobileAppsClient
    {
        public string MobileAppsEndpoint { get; set; }

        public SimpleMobileAppsClient(string url)
        {
            MobileAppsEndpoint = url;
            if (MobileAppsEndpoint.IndexOf("http://") < 0)
            {
                MobileAppsEndpoint = "http://" + MobileAppsEndpoint;
            }
            if (MobileAppsEndpoint.LastIndexOf("/") != MobileAppsEndpoint.Length - 1)
            {
                MobileAppsEndpoint += "/";
            }
        }

        public ArrayList Query(string tableName, string filter="")
        {
            ArrayList results = new ArrayList();
            var endpoint = MobileAppsEndpoint + "tables/" + tableName;
            if (filter != "")
            {
                endpoint += "?$filter=" + filter;
            }
            var msRequest = HttpWebRequest.Create(endpoint);
            msRequest.Method = "GET";
            msRequest.Headers.Add(ZumoApiVersionKey, ZumoApiVersionValue);
            try {
                using (var response = msRequest.GetResponse() as HttpWebResponse)
                {
                    using (var stream = response.GetResponseStream())
                    {
                        var reader = new StreamReader(stream);
                        var queried = reader.ReadToEnd();
                        results = Json.NETMF.JsonSerializer.DeserializeString(queried) as ArrayList;
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.Print("SimpleMobileAppClient query failed.");
                Debug.Print(ex.Message);
            }
            return results;
        }

        public ArrayList Insert(string tableName, object item)
        {
            ArrayList results = new ArrayList();
            string endpoint = MobileAppsEndpoint + "tables/" + tableName;
            var msRequest = HttpWebRequest.Create(endpoint) as HttpWebRequest;
            msRequest.Headers.Add(ZumoApiVersionKey, ZumoApiVersionValue);
            msRequest.Method = "POST";
            msRequest.ContentType = "application/json";
            var itemJson = Json.NETMF.JsonSerializer.SerializeObject(item);
            var content = System.Text.UTF8Encoding.UTF8.GetBytes(itemJson);
            msRequest.ContentLength = content.Length;
            try
            {
                using (var reqStream = msRequest.GetRequestStream())
                {
                    reqStream.Write(content, 0, content.Length);
                }
                using (var response = msRequest.GetResponse() as HttpWebResponse)
                {
                    using (var resStream = response.GetResponseStream())
                    {
                        var reader = new StreamReader(resStream);
                        var result = reader.ReadToEnd();
                        results = Json.NETMF.JsonSerializer.DeserializeString(result) as ArrayList;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print("SimpleMobileAppClient Insert failed.");
                Debug.Print(ex.Message);
            }
            return results;
        }

        private string ZumoApiVersionKey = "ZUMO-API-VERSION";
        private string ZumoApiVersionValue = "2.0.0";
    }
}
