using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace EzoGateway.Server
{
    public class HttpServerRequest
    {
        #region Constants
        private const uint BUFFER_SIZE = 8192;

        #endregion Constants

        public HttpMethod Method { get; set; }

        public Uri Uri { get; set; }

        public string Protocol { get; set; }

        public Dictionary<string, string> Details { get; set; }

        public Dictionary<string, string> Parameters { get; set; }

        /// <summary>
        /// Empty constructor
        /// </summary>
        public HttpServerRequest()
        {
            Details = new Dictionary<string, string>();
            Parameters = new Dictionary<string, string>();
        }

        /// <summary>
        /// Parse an input stream
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static async Task<HttpServerRequest> Parse(IInputStream input, Uri baseUri)
        {
            var request = new HttpServerRequest();
            var requestString = new StringBuilder();


            var data = new byte[BUFFER_SIZE];
            var buffer = data.AsBuffer();
            var dataRead = BUFFER_SIZE;

            while (dataRead == BUFFER_SIZE)
            {
                await input.ReadAsync(buffer, BUFFER_SIZE, InputStreamOptions.Partial);
                requestString.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                dataRead = buffer.Length;
            }

            string[] requestFragments = requestString.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None);
            var uriBuilder = new UriBuilder(baseUri);
            try
            {
                var headData = requestFragments[0].Split(' ');
                request.Method = new HttpMethod(headData[0]);
                var uri = headData[1];
                //extract GET params
                //URL Escape Codes see: https://www.w3schools.com/tags/ref_urlencode.asp
                var s = uri.Split(new string[] { "%3F", "?", "%26", "&" }, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length > 1) //parameter available
                {
                    for (int i = 1; i < s.Length; i++)
                    {
                        var para = s[i].Split(new string[] { "%3D", "=" }, StringSplitOptions.RemoveEmptyEntries);
                        if (para.Length == 2)
                            request.Parameters.Add(para[0], para[1]);
                        else
                            Debug.WriteLine($"Invalid GET parameters received. ({s[i]})");
                    }
                }
                uriBuilder.Path = s[0];
                request.Uri = uriBuilder.Uri;
                if (headData.Length > 2)
                    request.Protocol = headData[2];

                if (requestFragments.Length > 1)
                {
                    var rex = new Regex(@"^(?<key>.+?):\ (?<value>.+?)$");
                    for (int i = 1; i < requestFragments.Length; i++)
                    {
                        var m = rex.Match(requestFragments[i]);
                        if (m.Success)
                            request.Details.Add(m.Groups["key"].Value, m.Groups["value"].Value);
                    }
                }

            }
            catch (Exception ex)
            {
                return null;
            }

            return request;
        }
    }
}
