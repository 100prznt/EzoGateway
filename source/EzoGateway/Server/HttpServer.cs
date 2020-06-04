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
using System.Reflection;

namespace EzoGateway.Server
{
    public class HttpServer : IDisposable
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

        /// <summary>
        /// HTTP request counter
        /// </summary>
        public ulong RequestCounter { get; private set; }
        

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

            RequestCounter = 0;
        }

        #endregion Constructor

        #region Services
        /// <summary>
        /// Inizialize the server
        /// </summary>
        public async void ServerInitialize()
        {
            //m_Listener = new StreamSocketListener();
            //await m_Listener.BindServiceNameAsync(Port.ToString());
            //m_Listener.ConnectionReceived += async (sender, args) => HandleRequest(sender, args);

            //New implementation following the example of velsorange
            //Source: https://raspberrypi.stackexchange.com/questions/74937/windows-iot-streamsocketlistener-stops-working
            m_Listener = new StreamSocketListener();
            var currentSetting = m_Listener.Control.QualityOfService;
            m_Listener.Control.QualityOfService = SocketQualityOfService.LowLatency;
            //m_Listener.ConnectionReceived += HandleRequest;
            m_Listener.ConnectionReceived += (s, e) => HandleRequestAsync(e.Socket);
            await m_Listener.BindServiceNameAsync(Port.ToString());

            Logger.Write($"HTTP server is successfully initialized and listen for http requests under: http://{Ip}:{Port}/", SubSystem.HttpServer);

            m_Controller.Io.SetAliveState(true);
        }
        public async void Dispose()
        {
            m_Listener.ConnectionReceived -= (s, e) => HandleRequestAsync(e.Socket);
            await Task.Delay(500);
            await m_Listener.CancelIOAsync();
            m_Listener.Dispose();

            await Task.Delay(500);
        }

        public async Task DisposeAsync()
        {
            m_Listener.ConnectionReceived -= (s, e) => HandleRequestAsync(e.Socket);
            await Task.Delay(500);
            await m_Listener.CancelIOAsync();
            m_Listener.Dispose();

            await Task.Delay(500);
        }

        #endregion Services

        #region Internal services
        /// <summary>
        /// Handle incomming requests
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>

        private async void HandleRequestAsync(StreamSocket socket)
        {
            try
            {
                if (RequestCounter == ulong.MaxValue) //Should "never" occur
                {
                    Logger.Write("Request counter overflow. Counter will be reset.", SubSystem.HttpServer, LoggerLevel.Warning);
                    RequestCounter = 0;
                }

                RequestCounter++;

                if (RequestCounter % 100 == 0)
                    Logger.Write("########### " + RequestCounter + " HTTP requests processed. ###########", SubSystem.HttpServer);
            }
            catch (Exception ex)
            {
                Logger.Write(ex, SubSystem.HttpServer);
            }

            try
            {
                Logger.Write("Processing of a new HTTP request is started.", SubSystem.HttpServer);

                m_Controller.Io.IndicateHttpRequest();

                HttpServerRequest request = null;

                try
                {
                    //read request
                    //if (args.Socket == null)
                    if (socket == null)
                        Logger.Write("StreamSocked is null!", SubSystem.HttpServer, LoggerLevel.Warning);


                    //var input = args.Socket.InputStream;
                    var input = socket.InputStream;
                    request = await HttpServerRequest.Parse(input, m_ServerUri);
                    Logger.Write("Requested URL: " + request.Uri, SubSystem.HttpServer);

                    input.Dispose();
                    
                }
                catch (Exception ex)
                {
                    Logger.Write(ex, SubSystem.HttpServer);
                }

                //write response
                //using (IOutputStream output = args.Socket.OutputStream)
                using (IOutputStream output = socket.OutputStream)
                {
                    using (Stream response = output.AsStreamForWrite())
                    {
                        if (request == null)
                        {
                            Logger.Write("Error, \"request\" is null.", SubSystem.HttpServer, LoggerLevel.Warning);
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
                                Logger.Write(ex.Message, SubSystem.HttpServer);
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
                                    else if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("LOGS", StringComparison.OrdinalIgnoreCase))
                                    {
                                        data = GetWebResourceAsync(await Logger.GetCurrentLogFile());
                                    }
                                    else
                                    {
                                        //Check if requested resource is available in the file system.
                                        data = await GetWebResourceAsync(request.Uri);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Write(ex, SubSystem.HttpServer);
                                }
                            }
                        }
                        //API
                        else if (request.Uri.Segments.Length >= 2 && request.Uri.Segments[1].Trim('/').Equals("API", StringComparison.OrdinalIgnoreCase))
                        {
                            //Check if requested resource is available via API (Uri) and accessible (Method).
                            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete)
                                data = await ApiRequestAsync(request);
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
                            Logger.Write("URL not supported!", SubSystem.HttpServer, LoggerLevel.Warning);
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
                Logger.Write(ex.Message, SubSystem.HttpServer);
            }
        }

        private HttpResource GetWebResourceAsync(StorageFile file)
        {
            try
            {
                if (file == null)
                    return HttpResource.Error404;

                return new HttpResource(file);
            }
            catch (Exception ex)
            {
                Logger.Write(ex, SubSystem.HttpServer);
                return HttpResource.Error404;
            }
        }

        private async Task<HttpResource> GetWebResourceAsync(Uri uri)
        {
            try
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
            catch (Exception ex)
            {
                Logger.Write(ex, SubSystem.HttpServer);
                return HttpResource.Error404;
            }
        }

        /// <summary>
        /// Processing an API call
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <returns>HTTP response</returns>
        private async Task<HttpResource> ApiRequestAsync(HttpServerRequest request)
        {
            try
            {
                if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("SENSORS", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Write("Request sensor info", SubSystem.RestApi);
                    return HttpResource.CreateJsonResource(m_Controller.SensorInfos);
                }
                else if (request.Uri.Segments.Length > 3 && request.Uri.Segments[2].Trim('/').Equals("SENSOR", StringComparison.OrdinalIgnoreCase))
                {
                    if (request.Uri.Segments.Length == 5 && request.Uri.Segments[4].Trim('/').Equals("LIVE", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Write("Request live data from sensor no. " + request.Uri.Segments[3].Trim('/'), SubSystem.RestApi);
                        if (request.Uri.Segments[3].Trim('/').Equals("1", StringComparison.OrdinalIgnoreCase))
                            return HttpResource.CreateJsonResource(new { measValue = m_Controller.PhSensor.GetMeasValue() });
                        else if (request.Uri.Segments[3].Trim('/').Equals("2", StringComparison.OrdinalIgnoreCase))
                            return HttpResource.CreateJsonResource(new { measValue = m_Controller.RedoxSensor.GetMeasValue() });
                        else if (request.Uri.Segments[3].Trim('/').Equals("3", StringComparison.OrdinalIgnoreCase))
                            return HttpResource.CreateJsonResource(new { measValue = await m_Controller.TempSensor.GetMeasValue() });
                    }
                }
                else if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("PATHS", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Write("Request local paths", SubSystem.RestApi);
                    return HttpResource.CreateJsonResource(m_Controller.GetLocalPaths());
                }
                else if (request.Uri.Segments.Length >= 3 && request.Uri.Segments[2].Trim('/').Equals("CONFIG", StringComparison.OrdinalIgnoreCase))
                {
                    if (request.Uri.Segments.Length == 3)
                    {
                        if (request.Method == HttpMethod.Get)
                        {
                            Logger.Write("Request configuration", SubSystem.RestApi);
                            return HttpResource.CreateJsonResource(m_Controller.Configuration);
                        }
                        else if (request.Method == HttpMethod.Put)
                        {
                            //Update config
                            Logger.Write("Update configuration", SubSystem.RestApi);
                            var settings = JsonConvert.DeserializeObject<GeneralSettings>(request.Content);
                            m_Controller.UpdateConfig(settings);
                        }
                    }
                    else if (request.Uri.Segments.Length == 4 && request.Uri.Segments[3].Trim('/').Equals("TIME", StringComparison.OrdinalIgnoreCase))
                    {
                        if (request.Method == HttpMethod.Get) //Request time
                        {
                            Logger.Write("Request time", SubSystem.RestApi);
                            return HttpResource.CreateJsonResource(DateTime.Now);
                        }
                        else if (request.Method == HttpMethod.Put) // Set time
                        {
                            Logger.Write("Set time", SubSystem.RestApi);

                            //TODO: noch zu implementieren!
                            var time = request.Content;

                            m_Controller.SetSystemTime(new DateTime(2020, 01, 01, 12, 0, 0));

                            return HttpResource.Error400;
                        }
                    }
                    else if (request.Uri.Segments.Length == 4 && request.Uri.Segments[3].Trim('/').Equals("ONEWIRE", StringComparison.OrdinalIgnoreCase))
                    {
                        if (request.Method == HttpMethod.Get)
                        {
                            Logger.Write("Request 1-wire configuration", SubSystem.RestApi);
                            return HttpResource.CreateJsonResource(m_Controller.ConfigurationOneWire);
                        }
                        else if (request.Method == HttpMethod.Put)
                        {
                            //Update config
                            Logger.Write("Update 1-wire configuration", SubSystem.RestApi);
                            var settings = JsonConvert.DeserializeObject<Config.OneWire.Configuration>(request.Content);
                            m_Controller.UpdateConfigOneWire(settings);
                        }
                    }
                }
                else if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("ACQ", StringComparison.OrdinalIgnoreCase))
                {
                    if (m_Controller.Configuration.EnableCyclicUpdater)
                    {
                        Logger.Write("The execution of an externally triggered acquisition is not possible, because the automatic cyclic updater is active.", SubSystem.RestApi, LoggerLevel.Warning);
                        return HttpResource.JsonLocked423("The execution of an externally triggered acquisition is not possible, because the automatic cyclic updater is active.");
                    }
                    else
                    {
                        Logger.Write("Request a new data acquesition", SubSystem.RestApi);
                        await m_Controller.SingleMeasurementAsync();
                        await m_Controller.OneWireMeasurementAsync();
                        return HttpResource.JsonAccepted202("AcquireMeasdata");
                    }
                }
                else if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("INIT", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Write("Request a hardware init", SubSystem.RestApi);
                    await m_Controller.InitHardware();
                    return HttpResource.JsonAccepted202("InitHardware");
                }
                else if (request.Uri.Segments.Length == 3 && request.Uri.Segments[2].Trim('/').Equals("FETCH", StringComparison.OrdinalIgnoreCase))
                {
                    if (m_Controller.LatestMeasData == null || m_Controller.LatestMeasData.Count == 0)
                    {
                        Logger.Write("Request latest measdata -> No measurement data acquired.", SubSystem.RestApi, LoggerLevel.Warning);
                        return HttpResource.JsonLocked423("No measurement data acquired.");
                    }
                    else
                    {
                        Logger.Write("Request latest measdata", SubSystem.RestApi);
                        return HttpResource.CreateJsonResource(m_Controller.LatestMeasData);
                    }
                }
                else if (request.Uri.Segments.Length >= 3 && request.Uri.Segments[2].Trim('/').Equals("CAL", StringComparison.OrdinalIgnoreCase))
                {
                    if (request.Uri.Segments.Length == 4)
                    {
                        if (request.Uri.Segments[3].Trim('/').Equals("PH", StringComparison.OrdinalIgnoreCase))
                        {
                            if (request.Method == HttpMethod.Get)
                            {
                                Logger.Write("Request calibration data from PH sensor", SubSystem.RestApi);
                                return HttpResource.CreateJsonResource(new { StoredCalibPoints = m_Controller.PhSensor.GetCalibrationInfo() });
                            }
                            else if (request.Method == HttpMethod.Put)
                            {
                                Logger.Write("Update calibration data for PH sensor", SubSystem.RestApi);
                                //Perform sensor calibration
                                var calibData = JsonConvert.DeserializeObject<CalData>(request.Content);
                                if (m_Controller.CalPhAddPoint(calibData, out string errorMessage))
                                    return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration point successfully added"));
                                else
                                    return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Error, errorMessage));
                            }
                            else if (request.Method == HttpMethod.Delete)
                            {
                                Logger.Write("Delete calibration data from PH sensor", SubSystem.RestApi);
                                //Clear calibration
                                m_Controller.PhSensor.ClearCalibration();
                                return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration data cleared"));
                            }
                        }
                        else if (request.Uri.Segments[3].Trim('/').Equals("ORP", StringComparison.OrdinalIgnoreCase))
                        {
                            if (request.Method == HttpMethod.Get)
                            {
                                Logger.Write("Request calibration data from ORP sensor", SubSystem.RestApi);
                                return HttpResource.CreateJsonResource(new { StoredCalibPoints = m_Controller.RedoxSensor.GetCalibrationInfo() });
                            }
                            else if (request.Method == HttpMethod.Put)
                            {
                                Logger.Write("Update calibration data for ORP sensor", SubSystem.RestApi);
                                //Perform sensor calibration
                                var calibData = JsonConvert.DeserializeObject<CalData>(request.Content);
                                if (m_Controller.CalOrpAddPoint(calibData, out string errorMessage))
                                    return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration point successfully added"));
                                else
                                    return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Error, errorMessage));
                            }
                            else if (request.Method == HttpMethod.Delete)
                            {
                                Logger.Write("Delete calibration data from ORP sensor", SubSystem.RestApi);
                                //Clear calibration
                                m_Controller.RedoxSensor.ClearCalibration();
                                return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration data cleared"));
                            }
                        }
                        else if (request.Uri.Segments[3].Trim('/').Equals("RTD", StringComparison.OrdinalIgnoreCase))
                        {
                            if (request.Method == HttpMethod.Get)
                            {
                                Logger.Write("Request calibration data from RTD sensor", SubSystem.RestApi);
                                return HttpResource.CreateJsonResource(new
                                {
                                    StoredCalibPoints = m_Controller.TempSensor.GetCalibrationInfo(),
                                    UnitScale = m_Controller.Configuration.TemperatureUnit.GetSymbol()
                                });
                            }
                            else if (request.Method == HttpMethod.Put)
                            {
                                Logger.Write("Update calibration data for RTD sensor", SubSystem.RestApi);
                                //Perform sensor calibration
                                var calibData = JsonConvert.DeserializeObject<CalData>(request.Content);
                                if (m_Controller.CalRtdAddPoint(calibData, out string errorMessage))
                                    return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration point successfully added"));
                                else
                                    return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Error, errorMessage));
                            }
                            else if (request.Method == HttpMethod.Delete)
                            {
                                Logger.Write("Delete calibration data from RTD sensor", SubSystem.RestApi);
                                //Clear calibration
                                m_Controller.TempSensor.ClearCalibration();
                                return HttpResource.CreateJsonResource(new RestStatus(OperationStatus.Success, "Calibration data cleared"));
                            }
                        }
                    }
                }
                else if (request.Uri.Segments.Length >= 3 && request.Uri.Segments[2].Trim('/').Equals("INFO", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Write("Request system info", SubSystem.RestApi);

                    var infos = new Dictionary<string, string>
                    {
                        { "Version", typeof(App).GetTypeInfo().Assembly.GetName().Version.ToString() },
                        { "App", typeof(HttpServer).GetType().AssemblyQualifiedName },
                        { "SystemTime", DateTime.Now.ToString() },
                        { "SystemStartTime", m_Controller.SystemStartTime.ToString() },
                        { "SystemRuntime", (DateTime.Now - m_Controller.SystemStartTime).ToString() },
                        { "RequestCounter", RequestCounter.ToString() }
                    };

                    var temperature = m_Controller.GetOnBoardTemperature();
                    if (double.IsNaN(temperature))
                        infos.Add("OnboardTemperature", "Only available on <a href=\"https://github.com/100prznt/EzoGateway/tree/master/hardware\" target=\"_blank\">EzoGateway hardware/PCB</a>");
                    else
                        infos.Add("OnboardTemperature", temperature.ToString("F1") + " °C");

                    return HttpResource.CreateJsonResource(infos);
                }
                else if (request.Uri.Segments.Length >= 3 && request.Uri.Segments[2].Trim('/').Equals("ONEWIRE", StringComparison.OrdinalIgnoreCase))
                {
                    if (request.Uri.Segments.Length == 5)
                    {
                        if (request.Uri.Segments[3].Trim('/').Equals("SCANCHANNEL", StringComparison.OrdinalIgnoreCase)
                            && request.Method == HttpMethod.Get)
                        {
                            if (Int32.TryParse(request.Uri.Segments[4].Trim('/'), out int channel))
                            {
                                if (channel >= 0 && channel <= 1)
                                    return HttpResource.CreateJsonResource(m_Controller.ScanOneWire(channel));
                            }
                        }
                    }
                }

                return HttpResource.Error400;
            }
            catch (Exception ex)
            {
                Logger.Write(ex, SubSystem.HttpServer);
                return HttpResource.Error400;
            }
        }

        #endregion Internal services
    }
}
