using EzoGateway.Calibration;
using EzoGateway.Config;
using EzoGateway.Helpers;
using EzoGateway.Measurement;
using Rca.EzoDeviceLib;
using Rca.EzoDeviceLib.Specific.Ph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Networking.Connectivity;
using Windows.Storage;

namespace EzoGateway
{
    /// <summary>
    /// Main controller for EZO Gateway functionality
    /// </summary>
    public class Controller
    {
        #region Properties
        public GeneralSettings Configuration { get; set; }

        public Dictionary<int, SensorInfo> SensorInfos { get; set; }

        public Dictionary<int, MeasData> LatestMeasData { get; set; }

        /// <summary>
        /// Hardware is initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// pH probe on EZO device
        /// </summary>
        public EzoPh PhSensor { get; set; }

        /// <summary>
        /// O.R.P. (Redox) probe on EZO device
        /// </summary>
        public EzoOrp RedoxSensor { get; set; }

        /// <summary>
        /// Temperature probe on EZO device
        /// </summary>
        public EzoRtd TempSensor { get; set; }

        #endregion Properties

        #region Constructor
        public Controller()
        {
            ConfigIsLoadedEvent += Controller_ConfigIsLoadedEvent;
            ConfigIsSavedEvent += Controller_ConfigIsSavedEvent;
            ConfigIsDeletedEvent += Controller_ConfigIsDeletedEvent;

            LoadConfig();

            LatestMeasData = new Dictionary<int, MeasData>();

#if DEBUG //generating some measdata for testing
            LatestMeasData.Add(1, new MeasData() { Value = 18.86, Timestamp = DateTime.Now });
            LatestMeasData.Add(2, new MeasData() { Value = 7.03 });
            LatestMeasData.Add(3, new MeasData() { Value = 662 });
#endif
        }

        #endregion Constructor

        #region Configuration
        /// <summary>
        /// Load config from local config file
        /// </summary>
        /// <returns>true: successful; false: can not load config, generate default config</returns>
        public async Task<bool> LoadConfig()
        {
            var localFolder = ApplicationData.Current.LocalFolder;

            var item = await localFolder.TryGetItemAsync("ezogateway.config.json");
            if (item != null)
            {
                try
                {
                    Configuration = GeneralSettings.FromJsonFile(item.Path);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                ConfigIsLoadedEvent?.Invoke();
                return true;
            }
            else //Generate default
            {
                Configuration = GeneralSettings.Default;
                SaveConfig();
                ConfigIsLoadedEvent?.Invoke();
                return false;
            }
        }

        public void UpdateConfig(GeneralSettings settings)
        {
            Configuration = settings;
            SaveConfig();
        }

        public async void SaveConfig()
        {
            try
            {
                DeleteConfigFile();

                var localFolder = ApplicationData.Current.LocalFolder;

                var file = await localFolder.CreateFileAsync("ezogateway.config.json", CreationCollisionOption.ReplaceExisting);

                if (Configuration == null)
                    Configuration = GeneralSettings.Default;

                await FileIO.WriteTextAsync(file, Configuration.ToJson());

                ConfigIsSavedEvent?.Invoke();
            }
            catch (Exception ex)
            {
                throw new ArgumentException("SaveConfig", ex);
            }
        }

        public async void DeleteConfigFile()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;

                var item = await localFolder.TryGetItemAsync("ezogateway.config.json");
                if (item != null)
                {
                    await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    ConfigIsDeletedEvent?.Invoke();
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("DeleteConfigFile", ex);
            }
        }

        private void Controller_ConfigIsDeletedEvent()
        {
            Debug.WriteLine("Controller_ConfigIsDeletedEvent");
        }

        private void Controller_ConfigIsSavedEvent()
        {
            Debug.WriteLine("Controller_ConfigIsSavedEvent");
        }

        private void Controller_ConfigIsLoadedEvent()
        {
            Debug.WriteLine("Controller_ConfigIsLoadedEvent");

            var t = Task.Run(() => InitHardware()); //Initialization in the current task fails. So, outsourcing to own task...
            t.Wait();
        }

        #region Events
        public event Action ConfigIsSavedEvent;
        public event Action ConfigIsLoadedEvent;
        public event Action ConfigIsDeletedEvent;

        #endregion Events


        #endregion Configuration

        #region Hardware Init
        /// <summary>
        /// Hardware initialization
        /// </summary>
        /// <returns>true: success; false: failed</returns>
        public async Task<bool> InitHardware()
        {
            try
            {
                SensorInfos = new Dictionary<int, SensorInfo>();

                if (Configuration.PhSensor.Enabled)
                {
                    PhSensor = new EzoPh(Configuration.PhSensor.I2CAddress);
                    await PhSensor.InitSensorAsync();
                    SensorInfos.Add(1, GetSensorInfo(PhSensor, "Atlas Scientific EZO pH Circuit")); //id for pH: 1
                }

                if (Configuration.RedoxSensor.Enabled)
                {
                    RedoxSensor = new EzoOrp(Configuration.RedoxSensor.I2CAddress);
                    await RedoxSensor.InitSensorAsync();
                    SensorInfos.Add(2, GetSensorInfo(RedoxSensor, "Atlas Scientific EZO ORP circuit")); //id for Redox: 2
                }

                if (Configuration.TemperatureSensor.Enabled)
                {
                    TempSensor = new EzoRtd(Configuration.TemperatureSensor.I2CAddress);
                    await TempSensor.InitSensorAsync();
                    SensorInfos.Add(3, GetSensorInfo(TempSensor, "Atlas Scientific EZO RTD circuit")); //id for Temperature: 3
                }

                Debug.WriteLine("Hardware successfully initialized.");
                IsInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Hardware initialization failed. Exception: " + ex);
                return false;
            }
        }

        SensorInfo GetSensorInfo(EzoBase ezoSensor, string description = "N/A")
        {
            var devInfo = ezoSensor.GetDeviceInfo();
            var devStatus = ezoSensor.GetDeviceStatus();

            var info = new SensorInfo()
            {
                Name = devInfo.DeviceType,
                Description = description,
                Interface = $"I2C ({ezoSensor.Settings.BusSpeed})",
                FirmwareVersion = devInfo.FirmwareVersion,
                SupplyVoltage = devStatus.VccVoltage,
                Address = ezoSensor.I2CAddress,
                Serial = "N/A",
                Package = typeof(EzoBase).AssemblyQualifiedName
            };

            return info;
        }

        #endregion Hardware Init

        #region Measurement
        public async Task<bool> SingleMeasurement()
        {
            if (!IsInitialized)
                throw new Exception("Hardware is not ininitialized.");

            try
            {
                double? ph = null;
                var temp = await TempSensor?.GetMeasValue(); //Test async
                if (Configuration.EnablePhTemperatureCompensation && temp is double tempValue)
                    ph = PhSensor?.GetMeasValue(tempValue);
                else
                    ph = PhSensor?.GetMeasValue();
                var redox = RedoxSensor?.GetMeasValue();
                AddMeasDataInfo(1, temp, TempSensor?.ValueInfo);
                AddMeasDataInfo(2, ph, PhSensor?.ValueInfo);
                AddMeasDataInfo(3, redox, RedoxSensor?.ValueInfo);

                Debug.WriteLine("Single measurement successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Single measurement failed. Exception: " + ex);
                return false;
            }
        }

        void AddMeasDataInfo(int id, double? measValue, MeasDataInfo info)
        {
            if (measValue is double value && info != null)
            {
                var data = new MeasData()
                {
                    Name = info.Name,
                    Timestamp = DateTime.Now,
                    Value = value,
                    Unit = info.Unit,
                    Symbol = info.Symbol
                };

                LatestMeasData[id] = data;
            }
        }

        #endregion Measurement

        #region Calibration
        public bool CalPhAddPoint(CalData data, out string errorMessage)
        {
            errorMessage = "";
            if (!data.EzoDevice.Equals("PH", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"Calibration data not for EZO pH Circuit, calibration aborted.";
                return false;
            }

            if (PhSensor != null)
            {
                if (Enum.TryParse(data.CalibPointName, out CalPoint pnt))
                {
                    PhSensor.SetCalibrationPoint(pnt, data.Value);
                    return true;
                }
                else
                {
                    errorMessage = $"Invalid name ({data.CalibPointName}) for the calibration range, calibration aborted.";
                    return false;
                }
            }
            else
            {
                errorMessage = $"EZO pH Circuit not initialized, calibration aborted.";
                return false;
            }
        }

        public bool CalRtdAddPoint(CalData data, out string errorMessage)
        {
            errorMessage = "";
            if (!data.EzoDevice.Equals("RTD", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"Calibration data not for EZO RTD Circuit, calibration aborted.";
                return false;
            }

            if (TempSensor != null)
            {
                TempSensor.SetCalibrationPoint(data.Value);
                return true;
            }
            else
            {
                errorMessage = $"EZO pH Circuit not initialized, calibration aborted.";
                return false;
            }
        }

        #endregion Calibration

        #region System
        public string GetLocalIp()
        {
            List<string> IpAddress = new List<string>();
            var Hosts = NetworkInformation.GetHostNames().ToList();
            foreach (var Host in Hosts)
            {
                string IP = Host.DisplayName;
                IpAddress.Add(IP);
            }
            return IpAddress.Last(); //TODO: Ist es wirklich immer die letzte IP???
        }

        public Dictionary<string, string> GetLocalPaths()
        {
            var paths = new Dictionary<string, string>();
            var installedLocation = Package.Current.InstalledLocation;
            var localFolder = ApplicationData.Current.LocalFolder;
            paths.Add("InstalledLocation", Helpers.PathHelper.ToExternalPath(installedLocation.Path, @"\\192.168.0.191\c$")); //TODO: IP?
            paths.Add("LocalFolder", Helpers.PathHelper.ToExternalPath(localFolder.Path, @"\\192.168.0.191\c$"));

            return paths;
        }

        #endregion System
    }
}
