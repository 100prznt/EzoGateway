using EzoGateway.Calibration;
using EzoGateway.Config;
using EzoGateway.Helpers;
using Newtonsoft.Json;
using Rca.EzoDeviceLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;
using Rca.EzoDeviceLib.Specific.Rtd;

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
                Debug.WriteLine("Processing of a new HTTP request is started.");

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
                            if (request.Method != HttpMethod.Get)
                            {
                                data = HttpResource.Error405;
                            }
                            else
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
                        }
                        //API
                        else if (request.Uri.Segments.Length >= 2 && request.Uri.Segments[1].Trim('/').Equals("API", StringComparison.OrdinalIgnoreCase))
                        {
                            //Check if requested resource is available via API (Uri) and accessible (Method).
                            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete)
                                data = await ApiRequest(request);
                            else
                                data = HttpResource.Error405;
                        }
                        //test
                        else if (request.Uri.Segments.Length >= 2 && request.Uri.Segments[1].Trim('/').Equals("TEST", StringComparison.OrdinalIgnoreCase))
                        {
                            //some test code...
                            //m_Controller.SendValueToPlc(302, 725);
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

        /// <summary>
        /// Processing an API call
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <returns>HTTP response</returns>
        private async Task<HttpResource> ApiRequest(HttpServerRequest request)
        {
            if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("SENSORS", StringComparison.OrdinalIgnoreCase))
            {
                return HttpResource.CreateJsonResource(m_Controller.SensorInfos);
            }
            else if (request.Uri.Segments.Length > 3 && request.Uri.Segments[2].Trim('/').Equals("SENSOR", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Uri.Segments.Length == 5 && request.Uri.Segments[4].Trim('/').Equals("LIVE", StringComparison.OrdinalIgnoreCase))
                {
                    if(request.Uri.Segments[3].Trim('/').Equals("1", StringComparison.OrdinalIgnoreCase))
                        return HttpResource.CreateJsonResource(new { measValue = m_Controller.PhSensor.GetMeasValue() });
                    else if (request.Uri.Segments[3].Trim('/').Equals("2", StringComparison.OrdinalIgnoreCase))
                        return HttpResource.CreateJsonResource(new { measValue = m_Controller.RedoxSensor.GetMeasValue() });
                    else if (request.Uri.Segments[3].Trim('/').Equals("3", StringComparison.OrdinalIgnoreCase))
                        return HttpResource.CreateJsonResource(new { measValue = await m_Controller.TempSensor.GetMeasValue() });
                }
            }
            else if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("PATHS", StringComparison.OrdinalIgnoreCase))
            {
                return HttpResource.CreateJsonResource(m_Controller.GetLocalPaths());
            }
            else if (request.Uri.Segments.Length >= 3 && request.Uri.Segments[2].Trim('/').Equals("CONFIG", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Uri.Segments.Length == 3)
                    if (request.Method == HttpMethod.Get)
                        return HttpResource.CreateJsonResource(m_Controller.Configuration);
                    else if (request.Method == HttpMethod.Put)
                    {
                        //Update config
                        var settings = JsonConvert.DeserializeObject<GeneralSettings>(request.Content);
                        m_Controller.UpdateConfig(settings);
                    }
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
                if (m_Controller.Configuration.EnableCyclicUpdater)
                {
                    Debug.WriteLine("The execution of an externally triggered acquisition is not possible, because the automatic cyclic updater is active.");
                    return HttpResource.JsonLocked423("The execution of an externally triggered acquisition is not possible, because the automatic cyclic updater is active.");
                }
                else
                {
                    await m_Controller.SingleMeasurementAsync();
                    return HttpResource.JsonAccepted202("AcquireMeasdata");
                }
            }
            else if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("INIT", StringComparison.OrdinalIgnoreCase))
            {
                m_Controller.InitHardware();
                return HttpResource.JsonAccepted202("InitHardware");
            }
            else if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("FETCH", StringComparison.OrdinalIgnoreCase))
            {
                if (m_Controller.LatestMeasData == null || m_Controller.LatestMeasData.Count == 0)
                    return HttpResource.JsonLocked423("No measurement data acquired.");
                else
                    return HttpResource.CreateJsonResource(m_Controller.LatestMeasData);
            }
            else if (request.Uri.Segments.Length >= 3 && request.Uri.Segments[2].Trim('/').Equals("CAL", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Uri.Segments.Length == 4)
                {
                    if (request.Uri.Segments[3].Trim('/').Equals("PH", StringComparison.OrdinalIgnoreCase))
                    {
                        if (request.Method == HttpMethod.Get)
                        {
                            return HttpResource.CreateJsonResource(new { StoredCalibPoints = m_Controller.PhSensor.GetCalibrationInfo() });
                        }
                        else if (request.Method == HttpMethod.Put)
                        {
                            //Perform sensor calibration
                            var calibData = JsonConvert.DeserializeObject<CalData>(request.Content);
                            if (m_Controller.CalPhAddPoint(calibData, out string errorMessage))
                                return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration point successfully added"));
                            else
                                return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Error, errorMessage));
                        }
                        else if (request.Method == HttpMethod.Delete)
                        {
                            //Clear calibration
                            m_Controller.PhSensor.ClearCalibration();
                            return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration cleared added"));
                        }
                    }
                    else if(request.Uri.Segments[3].Trim('/').Equals("ORP", StringComparison.OrdinalIgnoreCase))
                    {
                        if (request.Method == HttpMethod.Get)
                        {
                            return HttpResource.CreateJsonResource(new { StoredCalibPoints = m_Controller.RedoxSensor.GetCalibrationInfo() });
                        }
                        else if (request.Method == HttpMethod.Put)
                        {
                            //Perform sensor calibration
                            var calibData = JsonConvert.DeserializeObject<CalData>(request.Content);
                            if (m_Controller.CalOrpAddPoint(calibData, out string errorMessage))
                                return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration point successfully added"));
                            else
                                return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Error, errorMessage));
                        }
                        else if (request.Method == HttpMethod.Delete)
                        {
                            //Clear calibration
                            m_Controller.RedoxSensor.ClearCalibration();
                            return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration cleared added"));
                        }
                    }
                    else if (request.Uri.Segments[3].Trim('/').Equals("RTD", StringComparison.OrdinalIgnoreCase))
                    {
                        if (request.Method == HttpMethod.Get)
                        {
                            return HttpResource.CreateJsonResource(new
                            {
                                StoredCalibPoints = m_Controller.TempSensor.GetCalibrationInfo(),
                                UnitScale =  m_Controller.Configuration.TemperatureUnit.GetSymbol()
                                });
                        }
                        else if (request.Method == HttpMethod.Put)
                        {
                            //Perform sensor calibration
                            var calibData = JsonConvert.DeserializeObject<CalData>(request.Content);
                            if (m_Controller.CalRtdAddPoint(calibData, out string errorMessage))
                                return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration point successfully added"));
                            else
                                return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Error, errorMessage));
                        }
                        else if (request.Method == HttpMethod.Delete)
                        {
                            //Clear calibration
                            m_Controller.TempSensor.ClearCalibration();
                            return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration cleared added"));
                        }
                    }
                }
            }

            return null; //Unknown request
        }

        #endregion Internal services
    }
}
