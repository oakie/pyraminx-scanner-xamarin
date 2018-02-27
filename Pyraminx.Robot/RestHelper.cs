using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Android.Views.Animations;
using Pyraminx.Common;

namespace Pyraminx.Robot
{
    public static class RestHelper
    {
        public static async Task<string> Get(string url)
        {
            if(!url.StartsWith("http://"))
                url = "http://" + url;
            // Create an HTTP web request using the URL:
            var request = WebRequest.Create(url) as HttpWebRequest;
            if (request == null)
                return null;

            request.ContentType = "application/json";
            request.Method = "GET";

            try
            {
                using (var response = await request.GetResponseAsync())
                {
                    var stream = response?.GetResponseStream();
                    if (stream == null)
                        return null;

                    using (var reader = new StreamReader(stream))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
            catch(Exception e)
            {
                Utils.Log(e.ToString());
                return null;
            }
        }
    }
}