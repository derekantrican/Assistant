using System;
using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json.Linq;
using TimeZoneConverter;

namespace Assistant
{
    public static class DateTimeStringParser
    {
        public static DateTime ParseString(string dateTimeString)
        {
            string requestUrl = $"https://api.mailbots.com/api/v1/natural_time/?format={HttpUtility.UrlEncode(dateTimeString)}&timezone={HttpUtility.UrlEncode(TZConvert.WindowsToIana(TimeZoneInfo.Local.Id))}";

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(requestUrl);
            httpRequest.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse serverResponse = (HttpWebResponse)httpRequest.GetResponse())
            using (StreamReader reader = new StreamReader(serverResponse.GetResponseStream()))
            {
                JObject response = JObject.Parse(reader.ReadToEnd());
                return DateTime.Parse(response["time_gmt"].Value<string>()).ToLocalTime();
            }
        }
    }
}
