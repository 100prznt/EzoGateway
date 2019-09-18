using EzoGateway.Helpers;
using Rca.EzoDeviceLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;

namespace EzoGateway.Server
{
    public class HttpServer
    {
        #region Member
        private Controller m_Controller;
        private StreamSocketListener m_Listener;
        private Uri m_ServerUri;

        #endregion Member

        #region Properties
        /// <summary>
        /// Current port for the HTTP listener
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Current IP of the server
        /// </summary>
        public string Ip { get => m_Controller.GetLocalIp(); }

        #endregion Properties

        #region Constructor
        /// <summary>
        /// Empty constructor
        /// </summary>
        /// <param name="controller">Reference to the app controller</param>
        /// <param name="port">Port for the HTTP listener</param>
        public HttpServer(ref Controller controller, int port = 80)
        {
            m_Controller = controller;
            Port = (port == 0) ? 80 : port;

            m_ServerUri = new UriBuilder("HTTP", Ip, Port).Uri;
        }

        #endregion Constructor

        #region Services
        /// <summary>
        /// Inizialize the server
        /// </summary>
        public async void ServerInitialize()
        {
            m_Listener = new StreamSocketListener();
            await m_Listener.BindServiceNameAsync(Port.ToString());
            m_Listener.ConnectionReceived += async (sender, args) => HandleRequest(sender, args);

            Debug.WriteLine("HTTP server is successfully initialized and listen for http requests.");
        }

        #endregion Services

        #region Internal services
        /// <summary>
        /// Handle incomming requests
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void HandleRequest(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                Debug.WriteLine("Processing of a new request is started.");

                HttpServerRequest request = null;

                try
                {
                    //read request
                    using (var input = args.Socket.InputStream)
                    {
                        request = await HttpServerRequest.Parse(input, m_ServerUri);
                        Debug.WriteLine("Requested URL: " + request.Uri);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                //write response
                using (IOutputStream output = args.Socket.OutputStream)
                {
                    using (Stream response = output.AsStreamForWrite())
                    {
                        if (request == null)
                        {
                            Debug.WriteLine("Error, \"request\" is null.");
                        }

                        var data = new HttpResource();

                        //API or web resource access?

                        //ROOT
                        if (request.Uri.Segments.Length == 1 && request.Uri.Segments[0].Equals("/"))
                        {
                            try
                            {
                                data.StatusCode = "301 Moved Permanently";
                                data.HeaderFields.Add("location", m_ServerUri.ToString() + "web/");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }
                        }
                        //WEB
                        else if (request.Uri.Segments.Length >= 2 && request.Uri.Segments[1].Trim('/').Equals("WEB", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("PATHS", StringComparison.OrdinalIgnoreCase))
                                {
                                    data = HttpResource.CreateHtmlResource("EzoGateway paths", m_Controller.GetLocalPaths());
                                }
                                else
                                {
                                    //Check if requested resource is available in the file system.
                                    data = await GetWebResource(request.Uri);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                            }
                        }
                        //API
                        else if (request.Uri.Segments.Length >= 2 && request.Uri.Segments[1].Trim('/').Equals("API", StringComparison.OrdinalIgnoreCase))
                        {
                            //Check if requested resource is available via API (Uri) and accessible (Method).
                            data = ApiRequest(request);
                        }
                        //undefined
                        else
                        {
                            Debug.WriteLine("URL not supported!");
                            data = HttpResource.Error400;
                        }

                        var bodyArray = new byte[0];
                        int fsLength = 0;
                        if (!string.IsNullOrWhiteSpace(data.BodyTextContent))
                            bodyArray = Encoding.UTF8.GetBytes(data.BodyTextContent);
                        //else if (data.IsBinary)
                        else if (data.File != null) //bugfix
                        {
                            var fs = await data.File.OpenStreamForReadAsync();
                            if (fs.Length <= int.MaxValue)
                                fsLength = (int)fs.Length;
                            else
                                throw new ArgumentOutOfRangeException("Requested file is to large for current buffer.");
                            bodyArray = new byte[fsLength];
                            fs.Read(bodyArray, 0, fsLength);
                        }
                        var bodyStream = new MemoryStream(bodyArray);

                        string header = data.GetHeader(fsLength);

                        byte[] headerArray = Encoding.UTF8.GetBytes(header);

                        await response.WriteAsync(headerArray, 0, headerArray.Length);
                        await bodyStream.CopyToAsync(response);
                        await response.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task<HttpResource> GetWebResource(Uri uri)
        {
            if (uri.Segments.Length < 2 | !uri.Segments[1].Trim('/').Equals("WEB", StringComparison.OrdinalIgnoreCase))
                return HttpResource.Error404;

            var filePath = Package.Current.InstalledLocation.Path;
            PathHelper.AppendSegment(ref filePath, "WebResources");
            for (int i = 2; i < uri.Segments.Length; i++)
                PathHelper.AppendSegment(ref filePath, uri.Segments[i]);

            var file = await StorageFile.GetFileFromPathAsync(filePath);

            return new HttpResource(file);
        }

        private HttpResource ApiRequest(HttpServerRequest request)
        {
            if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("SENSORS", StringComparison.OrdinalIgnoreCase))
            {
                return HttpResource.CreateJsonResource(m_Controller.SensorInfos);
            }
            else if (request.Uri.Segments.Length > 3 && request.Uri.Segments[2].Trim('/').Equals("SENSOR", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Uri.Segments.Length >= 4)
                {
                    //Select single sensor
                }
            }
            else if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("PATHS", StringComparison.OrdinalIgnoreCase))
            {
                return HttpResource.CreateJsonResource(m_Controller.GetLocalPaths());
            }
            else if (request.Uri.Segments.Length >= 3 && request.Uri.Segments[2].Trim('/').Equals("CONFIG", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Uri.Segments.Length == 3)
                    return HttpResource.CreateJsonResource(m_Controller.Configuration);
                //else if (request.Uri.Segments.Length == 4 && request.Uri.Segments[3].Trim('/').Equals("TIME", StringComparison.OrdinalIgnoreCase))
                //{
                //    var dt = new DateTime(2015, 05, 25, 16, 45, 05);
                //    var o = new TimeSpan(-2, 0, 0);
                //    var dto = new DateTimeOffset(dt, o);

                //    Windows.System.DateTimeSettings.SetSystemDateTime(dto);
                //}
            }
            else if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("ACQ", StringComparison.OrdinalIgnoreCase))
            {
                m_Controller.SingleMeasurement();
                return HttpResource.JsonAccepted202("AcquireMeasdata");
            }
            else if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').StartsWith("FETCH", StringComparison.OrdinalIgnoreCase))
            {
                return HttpResource.CreateJsonResource(m_Controller.LatestMeasData);
            }

            return null;
        }

        #endregion Internal services
    }
}
