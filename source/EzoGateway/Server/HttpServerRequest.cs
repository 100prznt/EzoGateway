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

        public string Content { get; set; }

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
            try
            {
                var requestFragments = new string[0];
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

                requestFragments = requestString.ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None);
                var uriBuilder = new UriBuilder(baseUri);
                int processedRequestLines = 0;


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
                    processedRequestLines++;

                    if (requestFragments.Length > 1)
                    {
                        var rex = new Regex(@"^(?<key>.+?):\ (?<value>.+?)$");
                        for (int i = 1; i < requestFragments.Length; i++)
                        {
                            var m = rex.Match(requestFragments[i]);
                            if (m.Success)
                            {
                                request.Details.Add(m.Groups["key"].Value.ToUpper(), m.Groups["value"].Value);
                                processedRequestLines++;
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Write("Error parsing the HTTP request. " + ex.Message, SubSystem.HttpServer);
                    return null;
                }

                try
                {
                    //Handle HTTP content
                    if (requestFragments.Length > processedRequestLines && request.Details.ContainsKey("CONTENT-LENGTH"))
                    {
                        if (int.TryParse(request.Details["CONTENT-LENGTH"], out int lenght))
                        {
                            if (lenght > 1)
                                //TODO: quick and dirty implemented!
                                request.Content = requestFragments.Last();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write("Error parsing the HTTP request content. " + ex.Message, SubSystem.HttpServer);
                    return null;
                }
                return request;
            }
            catch (Exception ex)
            {
                Logger.Write(ex, SubSystem.HttpServer);
                return null;
            }
        }
    }
}
