using EzoGateway.Calibration;
using EzoGateway.Config;
using EzoGateway.Config.OneWire;
using EzoGateway.Helpers;
using EzoGateway.Measurement;
using EzoGateway.Plc;
using Rca.EzoDeviceLib;
using Rca.EzoDeviceLib.Specific.Ph;
using Rca.EzoGateway.Plc.Sharp7;
using Rca.OneWireLib;
using Rca.OneWireLib.Masters;
using Rca.OneWireLib.Slaves;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Gpio;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.System.Threading;

namespace EzoGateway
{
    /// <summary>
    /// Main controller for EZO Gateway functionality
    /// </summary>
    public class Controller : INotifyPropertyChanged
    {

        #region Properties
        public GeneralSettings Configuration { get; set; }

        public CalInfo LastCalibration { get; set; }

        /// <summary>
        /// 1-wire configuration for EzoGateway hardware/PCB
        /// </summary>
        public Config.OneWire.Configuration ConfigurationOneWire { get; set; }

        public Dictionary<int, SensorInfo> SensorInfos { get; set; }

        public Dictionary<int, MeasData> LatestMeasData
        {
            get => m_LatestMeasData;
            set
            {
                m_LatestMeasData = value;
                PropChanged();
            }
        }

        private Dictionary<int, MeasData> m_LatestMeasData;

        public DateTime SystemStartTime { get; set; }

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

        public Dictionary<int, IOneWireSlave> OneWireSensors { get; set; }

        public IoDispatcher Io { get; set; }

        #endregion Properties

        #region Members
        private Int16 m_SecureCounter;
        private PlcWorker m_PlcWorker;
        private OneWireController m_OneWireController;
        private DS18B20 m_OnBoardThermometer;

        private static TimeSpan m_CyclicUpdatePeriod = new TimeSpan(0, 0, 5);
        private static event Action CyclicUpdateEvent;
        private bool m_CyclicUpdateEventIsAttached = false;
        private ThreadPoolTimer m_CyclicUpdateTimer;
        //ThreadPoolTimer m_CyclicUpdateTimer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
        //{
        //    CyclicUpdateEvent?.Invoke();

        //}, m_CyclicUpdatePeriod);

        #endregion Members

        #region Constructor
        public Controller()
        {
            //SetSystemTime(new DateTime(2020, 2, 5, 10, 12, 0));

            SystemStartTime = DateTime.Now;

            Logger.Write("Init EzoGateway controller", SubSystem.App);

            Io = new IoDispatcher();

            ConfigIsLoadedEvent += Controller_ConfigIsLoadedEvent;
            ConfigIsSavedEvent += Controller_ConfigIsSavedEvent;
            ConfigIsDeletedEvent += Controller_ConfigIsDeletedEvent;

            OneWireConfigIsLoadedEvent += Controller_OneWireConfigIsLoadedEvent;

            LoadConfig();
            LoadConfigOneWire();

            LoadCalInfo();


            LatestMeasData = new Dictionary<int, MeasData>();

#if DEBUG //generating some measdata for testing
            Logger.Write("Generate dummy meas data for testing. (Temperature = 18.86 °C, pH value = 7.03, Redox potential = 662 mV)", SubSystem.App);
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
            Logger.Write("Start load config", SubSystem.Configuration);

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
                    Logger.Write(ex, SubSystem.Configuration);
                }

                ConfigIsLoadedEvent?.Invoke();
                return true;
            }
            else //Generate default
            {
                Logger.Write("Configuration not found, genarate default config", SubSystem.Configuration, LoggerLevel.Warning);
                Configuration = GeneralSettings.Default;
                SaveConfig();
                ConfigIsLoadedEvent?.Invoke();
                return false;
            }
        }

        public void UpdateConfig(GeneralSettings settings)
        {
            Logger.Write("Start update config", SubSystem.Configuration);
            Configuration = settings;
            SaveConfig();
        }

        public async void SaveConfig()
        {
            Logger.Write("Start save config", SubSystem.Configuration);
            try
            {
                await DeleteConfigFile("ezogateway.config.json");

                var localFolder = ApplicationData.Current.LocalFolder;

                var file = await localFolder.CreateFileAsync("ezogateway.config.json", CreationCollisionOption.ReplaceExisting);

                if (Configuration == null)
                    Configuration = GeneralSettings.Default;

                await FileIO.WriteTextAsync(file, Configuration.ToJson());

                ConfigIsSavedEvent?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Write(ex, SubSystem.Configuration);
                throw new ArgumentException("SaveConfig", ex);
            }
        }

        public async Task DeleteConfigFile(string fileName)
        {
            Logger.Write("Start delete config", SubSystem.Configuration);
            try
            {
                //var localFolder = ApplicationData.Current.LocalFolder;

                var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);
                if (item != null)
                {
                    await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    ConfigIsDeletedEvent?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Logger.Write(ex, SubSystem.Configuration);
                throw new ArgumentException("DeleteConfigFile ->" + fileName, ex);
            }
        }

        private void Controller_ConfigIsDeletedEvent()
        {
            Logger.Write("Controller_ConfigIsDeletedEvent", SubSystem.Configuration);
        }

        private void Controller_ConfigIsSavedEvent()
        {
            Logger.Write("Controller_ConfigIsSavedEvent", SubSystem.Configuration);
            Logger.Write("Configuration JSON (in the following lines)\n" + Configuration.ToJson(), SubSystem.Configuration);

            InitAfterConfigChange();
        }

        private void Controller_ConfigIsLoadedEvent()
        {
            Logger.Write("Controller_ConfigIsLoadedEvent", SubSystem.Configuration);
            Logger.Write("Configuration JSON (in the following lines)\n" + Configuration.ToJson(), SubSystem.Configuration);

            var t = Task.Run(() => InitHardware()); //Initialization in the current task fails. So, outsourcing to own task...
            t.Wait();

            InitAfterConfigChange();
        }

        private void InitAfterConfigChange()
        {

            if (Configuration?.Logger != null)
                Logger.ApplyConfig(Configuration.Logger.Enabled, Configuration.Logger.ExcludedSubSystems, Configuration.Logger.MinimumLogLevel);

            InitCyclicUpdater();
        }

        #region Events
        public event Action ConfigIsSavedEvent;
        public event Action ConfigIsLoadedEvent;
        public event Action ConfigIsDeletedEvent;


        public event Action OneWireConfigIsLoadedEvent;


        public event Action CalInfoIsLoadedEvent;

        #endregion Events


        #endregion Configuration

        #region Hardware Init
        /// <summary>
        /// Hardware initialization
        /// </summary>
        /// <returns>true: success; false: failed</returns>
        public async Task<bool> InitHardware()
        {
            Logger.Write("Start init hardware", SubSystem.LowLevel);
            try
            {
                SensorInfos = new Dictionary<int, SensorInfo>();

                if (Configuration.PhSensor.Enabled)
                {
                    try
                    {
                        Logger.Write("Start initialization of the Atlas Scientific EZO pH Circuit", SubSystem.LowLevel);
                        PhSensor = new EzoPh(Configuration.PhSensor.I2CAddress);
                        await PhSensor.InitSensorAsync();
                        Logger.Write("Atlas Scientific EZO pH Circuit successfully initialized, FW: " + PhSensor.GetDeviceInfo().FirmwareVersion, SubSystem.LowLevel);

                        SensorInfos.Add(1, GetSensorInfo(PhSensor, "Atlas Scientific EZO pH Circuit")); //id for pH: 1
                    }
                    catch (Exception ex)
                    {
                        PhSensor = null;
                        Logger.Write("Error during initialization of the Atlas Scientific EZO pH Circuit, see details below", SubSystem.LowLevel, LoggerLevel.Error);
                        Logger.Write(ex, SubSystem.LowLevel, LoggerLevel.Error);
                    }
                }

                if (Configuration.RedoxSensor.Enabled)
                {
                    try
                    {
                        Logger.Write("Start initialization of the Atlas Scientific EZO ORP circuit", SubSystem.LowLevel);
                        RedoxSensor = new EzoOrp(Configuration.RedoxSensor.I2CAddress);
                        await RedoxSensor.InitSensorAsync();
                        Logger.Write("Atlas Scientific EZO ORP circuit successfully initialized, FW: " + RedoxSensor.GetDeviceInfo().FirmwareVersion, SubSystem.LowLevel);

                        SensorInfos.Add(2, GetSensorInfo(RedoxSensor, "Atlas Scientific EZO ORP circuit")); //id for Redox: 2
                    }
                    catch (Exception ex)
                    {
                        RedoxSensor = null;
                        Logger.Write("Error during initialization of the Atlas Scientific EZO ORP Circuit, see details below", SubSystem.LowLevel, LoggerLevel.Error);
                        Logger.Write(ex, SubSystem.LowLevel, LoggerLevel.Error);
                    }
                }

                if (Configuration.TemperatureSensor.Enabled)
                {
                    try
                    {
                        Logger.Write("Start initialization of the Atlas Scientific EZO RTD circuit", SubSystem.LowLevel);
                        TempSensor = new EzoRtd(Configuration.TemperatureSensor.I2CAddress);
                        await TempSensor.InitSensorAsync();
                        Logger.Write("Atlas Scientific EZO RTD circuit successfully initialized, FW: " + TempSensor.GetDeviceInfo().FirmwareVersion, SubSystem.LowLevel);

                        SensorInfos.Add(3, GetSensorInfo(TempSensor, "Atlas Scientific EZO RTD circuit")); //id for Temperature: 3
                    }
                    catch (Exception ex)
                    {
                        TempSensor = null;
                        Logger.Write("Error during initialization of the Atlas Scientific EZO RTD Circuit, see details below", SubSystem.LowLevel, LoggerLevel.Error);
                        Logger.Write(ex, SubSystem.LowLevel, LoggerLevel.Error);
                    }
                }

#if EZOGW_HW
                try
                {
                    m_OneWireController = new OneWireController();
                    m_OneWireController.InitMaster<DS2482_100>(0x18, "I2C1");
                    m_OneWireController.SearchSlaves();

                    //GetOnBoardTemperature();
                }
                catch (Exception ex)
                {
                    Logger.Write(ex, SubSystem.LowLevel, LoggerLevel.Error);
                }

                InitOneWireSensors();
#endif



                //InitPlc();
                if (Configuration.LogoConnection != null && Configuration.LogoConnection.Enabled)
                {
                    Logger.Write("Init Siemens LOGO! plc", SubSystem.Plc);
                    m_PlcWorker = new PlcWorker(Configuration.LogoConnection.IpAddress);
                    m_PlcWorker.SetUpTrigger(Configuration.LogoConnection.TriggerVmAddress, Configuration.LogoConnection.TriggerVmAddressBit);
                    m_PlcWorker.TriggerEvent += M_PlcWorker_TriggerEvent;
                    m_PlcWorker.Start();
                }

                Logger.Write("Hardware successfully initialized.", SubSystem.LowLevel);
                IsInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Write("Hardware initialization failed with Exception: " + ex, SubSystem.LowLevel, LoggerLevel.Error);
                return false;
            }
        }



        private SensorInfo GetSensorInfo(EzoBase ezoSensor, string description = "N/A")
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

        #region CyclicUpdater
        /// <summary>
        /// Init the cyclic updater, depending on the current configuration
        /// </summary>
        /// <returns>true: success; false: error</returns>
        public async Task<bool> InitCyclicUpdater()
        {
            Logger.Write("Start init cyclic updater", SubSystem.App);

            Io.SetCyclicUpdaterState(Configuration.EnableCyclicUpdater);
            if (Configuration.EnableCyclicUpdater)
            {
                if (m_CyclicUpdateEventIsAttached)
                {
                    Logger.Write("Cyclic updater already enabled -> is restarted", SubSystem.App);
                    CyclicUpdateEvent -= Controller_CyclicUpdateEvent;
                    await Task.Delay(50);
                }

                m_CyclicUpdatePeriod = TimeSpan.FromSeconds(Configuration.UpdateInterval);

                if (m_CyclicUpdateTimer != null)
                    m_CyclicUpdateTimer.Cancel();

                m_CyclicUpdateTimer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
                {
                    CyclicUpdateEvent?.Invoke();

                }, m_CyclicUpdatePeriod);


                Logger.Write("Enable cyclic updater.", SubSystem.App);
                CyclicUpdateEvent += Controller_CyclicUpdateEvent;
                m_CyclicUpdateEventIsAttached = true;

            }
            else
            {
                Logger.Write("Disable cyclic updater.", SubSystem.App);
                CyclicUpdateEvent -= Controller_CyclicUpdateEvent;
                m_CyclicUpdateEventIsAttached = false;
            }

            return true;
        }

        private void Controller_CyclicUpdateEvent()
        {
            SingleMeasurementAsync();
            OneWireMeasurementAsync();
        }



        #endregion CyclicUpdater

        #region Measurement
        /// <summary>
        /// Get current measurement data of the active EZO modules.
        /// </summary>
        /// <returns>true: success; false: error</returns>
        public async Task<bool> SingleMeasurementAsync()
        {
            Logger.Write("Perform single measurement", SubSystem.LowLevel);

            if (!IsInitialized)
                Logger.Write("Hardware is not ininitialized.", SubSystem.LowLevel, LoggerLevel.Error);

            //TODO: Check for valide values and wire cuts

            Io.IndicateMeasurement();

            try
            {
                double? ph = null;
                double? temp = null;
                if (TempSensor != null)
                    temp = await TempSensor?.GetMeasValue(); //Test async
                if (Configuration.EnablePhTemperatureCompensation && temp is double tempValue)
                    ph = PhSensor?.GetMeasValue(tempValue);
                else
                    ph = PhSensor?.GetMeasValue();
                var redox = RedoxSensor?.GetMeasValue();
                AddMeasDataInfo(1, temp, TempSensor?.ValueInfo);
                AddMeasDataInfo(2, ph, PhSensor?.ValueInfo);
                AddMeasDataInfo(3, redox, RedoxSensor?.ValueInfo);

                Logger.Write("Single measurement successfully", SubSystem.LowLevel);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Write("Single measurement failed with exception: " + ex, SubSystem.LowLevel);
                return false;
            }
        }

        public async Task<bool> OneWireMeasurementAsync()
        {
            if (!IoDispatcher.EZO_GATEWAY_HARDWARE_AVAILABLE)
                return false;


            Logger.Write("Perform 1-wire measurement", SubSystem.LowLevel);

            if (!IsInitialized)
                Logger.Write("Hardware is not ininitialized.", SubSystem.LowLevel, LoggerLevel.Error);

            Io.IndicateMeasurement();


            try
            {

                foreach (var sensor in OneWireSensors)
                {
                    int uid = sensor.Key;

                    var tempSensor = sensor.Value as DS18B20;

                    if (tempSensor != null)
                    {
                        var name = ConfigurationOneWire.SensorList.First(x => x.CustomUniqueId == uid).CustomName;

                        var info = new MeasDataInfo(name, "Celsius", "°C");
                        AddMeasDataInfo(uid, tempSensor.GetTemperature(), info);
                    }
                }

                Logger.Write("1-wire measurement successfully", SubSystem.LowLevel);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Write("1-wire measurement failed with exception: " + ex, SubSystem.LowLevel);
                return false;
            }
        }

        private void AddMeasDataInfo(int id, double? measValue, MeasDataInfo info)
        {
            if (measValue is double value && info != null && info != null)
            {
                Logger.Write($"Add {info.Name} measdata to the latest-measdata-collection", SubSystem.App);

                var data = new MeasData()
                {
                    Name = info.Name,
                    Timestamp = DateTime.Now,
                    Value = value,
                    Unit = info.Unit,
                    Symbol = info.Symbol
                };

                LatestMeasData[id] = data;

                PropertyChanged(null, new PropertyChangedEventArgs(info.Name));

                if (Configuration.LogoConnection != null && Configuration.LogoConnection.Enabled)
                {
                    int factor = 100;
                    if (string.Equals(info.Name, "ph", StringComparison.OrdinalIgnoreCase))
                    {
                        factor = 1000;
                    }

                    var scaledValue = value * factor;
                    if (scaledValue < Int16.MinValue || scaledValue > Int16.MaxValue)
                        Logger.Write("Value to large for DWORD (plc interface)", SubSystem.Plc);
                    else
                        SendValueToPlc(Configuration.LogoConnection.GetVmAddressByName(info.Name), Convert.ToInt16(scaledValue));
                }
                return;
            }

            //skip
            var dummy = new MeasData()
            {
                Name = string.Empty,
                Timestamp = DateTime.Now,
                Value = double.NaN,
                Unit = string.Empty,
                Symbol = string.Empty
            };

            LatestMeasData[id] = dummy;
        }

        #endregion Measurement

        #region 1-wire (EzoGateway hardware/PCB)

        public async Task<bool> LoadConfigOneWire()
        {
            Logger.Write("Start load 1-wire config", SubSystem.Configuration);

            var localFolder = ApplicationData.Current.LocalFolder;

            var item = await localFolder.TryGetItemAsync("onewire.config.json");
            if (item != null)
            {
                try
                {
                    ConfigurationOneWire = Config.OneWire.Configuration.FromJsonFile(item.Path);
                }
                catch (Exception ex)
                {
                    Logger.Write(ex, SubSystem.Configuration);
                }

                OneWireConfigIsLoadedEvent?.Invoke();
                return true;
            }
            else //Generate default
            {
                Logger.Write("1-wire configuration not found, genarate default config", SubSystem.Configuration, LoggerLevel.Warning);
                ConfigurationOneWire = Config.OneWire.Configuration.Default;
                SaveConfigOneWire();
                OneWireConfigIsLoadedEvent?.Invoke();
                return false;
            }
        }

        public async void SaveConfigOneWire()
        {
            Logger.Write("Start save 1-wire config", SubSystem.Configuration);
            try
            {
                await DeleteConfigFile("onewire.config.json");

                var localFolder = ApplicationData.Current.LocalFolder;

                var file = await localFolder.CreateFileAsync("onewire.config.json", CreationCollisionOption.ReplaceExisting);

                if (ConfigurationOneWire == null)
                    ConfigurationOneWire = Config.OneWire.Configuration.Default;

                await FileIO.WriteTextAsync(file, ConfigurationOneWire.ToJson());

                //ConfigIsSavedEvent?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Write(ex, SubSystem.Configuration);
                throw new ArgumentException("SaveConfigOneWire", ex);
            }
        }



        public void UpdateConfigOneWire(Config.OneWire.Configuration settings)
        {
            Logger.Write("Start update 1-wire config", SubSystem.Configuration);
            ConfigurationOneWire = settings;
            SaveConfigOneWire();
        }

        public async Task<bool> InitOneWireSensors()
        {
            if (m_OneWireController == null)
            {
                Logger.Write("1-wire master not available!", SubSystem.LowLevel);
                return false;
            }

            foreach (var sensorConf in ConfigurationOneWire.SensorList)
            {
                if (OneWireSensors == null)
                    OneWireSensors = new Dictionary<int, IOneWireSlave>();
                else if (OneWireSensors.ContainsKey(sensorConf.CustomUniqueId))
                    OneWireSensors.Remove(sensorConf.CustomUniqueId);

                var arr = sensorConf.OneWireAddressString.Split('-', ':');
                var address = new byte[arr.Length];
                for (int i = 0; i < arr.Length; i++)
                    address[i] = Convert.ToByte(arr[i], 16);

                switch (sensorConf.SensorType)
                {
                    case SupportedSensors.DS18B20:
                        try
                        {
                            m_OneWireController.SelectMasterChannel(sensorConf.MasterChannel);
                            var sensor = m_OneWireController.GetSlave<DS18B20>(sensorConf.MasterChannel, address);
                            //var sensor = m_OneWireController.GetSlave<DS18B20>(infoDS18B20.MasterChannel, infoDS18B20.Address);
                            OneWireSensors.Add(sensorConf.CustomUniqueId, sensor);

                            var info = new SensorInfo();
                            info.Name = sensorConf.CustomName;
                            info.Serial = sensorConf.OneWireAddressString;
                            info.Address = sensorConf.MasterChannel;
                            info.Description = sensorConf.SensorType.ToString();
                            info.Interface = "1-wire";
                            info.Package = typeof(SlaveBase).AssemblyQualifiedName;

                            SensorInfos.Add(sensorConf.CustomUniqueId, info);
                        }
                        catch (Exception ex)
                        {
                            Logger.Write(ex, SubSystem.LowLevel, LoggerLevel.Error);
                        }
                        break;
                    default:
                        break;
                }

            }


            return false;
        }

        private void Controller_OneWireConfigIsLoadedEvent()
        {
            //InitOneWireSensors();
        }


        public string[] ScanOneWire(int masterChannel)
        {
            if (m_OneWireController == null)
                return null;

            var infos = m_OneWireController.GetSlaveInfos();

            var channelInfos = infos.Where(x => x.MasterChannel == masterChannel);

            return channelInfos.Select(x => BitConverter.ToString(x.Address)).ToArray();
        }


        public double GetOnBoardTemperature()
        {
            if (m_OneWireController == null)
                return double.NaN;

            if (m_OnBoardThermometer == null) //init thermometer
            {
                var infos = m_OneWireController.GetSlaveInfos(); //Get info of all connected slaves
                var infoDS18B20 = infos.FirstOrDefault(x => x.MasterChannel == 7 && x.FamilyCode == FamilyCode.DS18B20); //look for a DS18B20 device on hw ch. 7
                
                if (infoDS18B20 != null) //if DS18B20 available
                {
                    m_OneWireController.SelectMasterChannel(infoDS18B20.MasterChannel); //Select the master channel!
                    m_OnBoardThermometer = m_OneWireController.GetSlave<DS18B20>(infoDS18B20.MasterChannel, infoDS18B20.Address); //Get the slave (DS18B20) device instance
                }
                else
                {
                    Logger.Write("Onboard thermometer (DS18B20) not available!", SubSystem.LowLevel);
                    return double.NaN;
                }
            }

            return m_OnBoardThermometer.GetTemperature();
        }

        #endregion 1-wire (EzoGateway hardware/PCB)

        #region Calibration
        public bool CalPhAddPoint(CalData data, out string errorMessage)
        {
            errorMessage = "";
            if (!data.EzoDevice.Equals("PH", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Calibration data not for EZO pH Circuit, calibration aborted.";
                Logger.Write(errorMessage, SubSystem.Logger, LoggerLevel.Warning);
                return false;
            }

            if (PhSensor != null)
            {
                if (Enum.TryParse(data.CalibPointName, out CalPoint pnt))
                {
                    PhSensor.SetCalibrationPoint(pnt, data.Value);
                    Logger.Write($"Calibration point ({data.CalibPointName}) added successfully for pH sensor.", SubSystem.Logger);
                    UpdateCalInfo("PH");
                    return true;
                }
                else
                {
                    errorMessage = $"Invalid name ({data.CalibPointName}) for the calibration range, calibration aborted.";
                    Logger.Write(errorMessage, SubSystem.Logger, LoggerLevel.Warning);
                    return false;
                }
            }
            else
            {
                errorMessage = "EZO pH Circuit not initialized, calibration aborted.";
                Logger.Write(errorMessage, SubSystem.Logger, LoggerLevel.Warning);
                return false;
            }
        }

        public bool CalOrpAddPoint(CalData data, out string errorMessage)
        {
            errorMessage = "";
            if (!data.EzoDevice.Equals("ORP", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"Calibration data not for EZO ORP Circuit, calibration aborted.";
                Logger.Write(errorMessage, SubSystem.Logger, LoggerLevel.Warning);
                return false;
            }

            if (RedoxSensor != null)
            {
                RedoxSensor.SetCalibrationPoint((int)data.Value);
                Logger.Write($"Calibration point ({data.CalibPointName}) added successfully for ORP sensor.", SubSystem.Logger);
                UpdateCalInfo("ORP");
                return true;
            }
            else
            {
                errorMessage = $"EZO ORP Circuit not initialized, calibration aborted.";
                Logger.Write(errorMessage, SubSystem.Logger, LoggerLevel.Warning);
                return false;
            }
        }

        public bool CalRtdAddPoint(CalData data, out string errorMessage)
        {
            errorMessage = "";
            if (!data.EzoDevice.Equals("RTD", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"Calibration data not for EZO RTD Circuit, calibration aborted.";
                Logger.Write(errorMessage, SubSystem.Logger, LoggerLevel.Warning);
                return false;
            }

            if (TempSensor != null)
            {
                TempSensor.SetCalibrationPoint(data.Value);
                Logger.Write($"Calibration point ({data.CalibPointName}) added successfully for Rtd sensor.", SubSystem.Logger);
                UpdateCalInfo("RTD");
                return true;
            }
            else
            {
                errorMessage = $"EZO pH Circuit not initialized, calibration aborted.";
                Logger.Write(errorMessage, SubSystem.Logger, LoggerLevel.Warning);
                return false;
            }
        }


        public async Task<bool> LoadCalInfo()
        {
            Logger.Write("Start load calibration info", SubSystem.Configuration);

            var localFolder = ApplicationData.Current.LocalFolder;

            var item = await localFolder.TryGetItemAsync("calibration-info.json");
            if (item != null)
            {
                try
                {
                    LastCalibration = CalInfo.FromJsonFile(item.Path);
                }
                catch (Exception ex)
                {
                    Logger.Write(ex, SubSystem.Configuration);
                }

                CalInfoIsLoadedEvent?.Invoke();
                return true;
            }
            else //Generate default
            {
                Logger.Write("Calibration info not found", SubSystem.Configuration, LoggerLevel.Warning);
                LastCalibration = new CalInfo();
                SaveCalInfo();
                CalInfoIsLoadedEvent?.Invoke();
                return false;
            }
        }

        public async void SaveCalInfo()
        {
            Logger.Write("Start save calibration info", SubSystem.Configuration);
            try
            {
                await DeleteConfigFile("onewire.config.json");

                var localFolder = ApplicationData.Current.LocalFolder;

                var file = await localFolder.CreateFileAsync("calibration-info.json", CreationCollisionOption.ReplaceExisting);

                if (LastCalibration == null)
                    LastCalibration = new CalInfo();

                await FileIO.WriteTextAsync(file, LastCalibration.ToJson());

                //ConfigIsSavedEvent?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Write(ex, SubSystem.Configuration);
                throw new ArgumentException("Save calibration info", ex);
            }
        }



        public void UpdateCalInfo(string sensorName)
        {
            Logger.Write("Start update calibration info", SubSystem.Configuration);

            if (LastCalibration == null)
                Logger.Write("Last calibration info not found!", SubSystem.Configuration, LoggerLevel.Warning);
            else
            {
                if (LastCalibration.LastCalDate.ContainsKey(sensorName))
                    LastCalibration.LastCalDate[sensorName] = DateTime.Now;
                else
                    LastCalibration.LastCalDate.Add(sensorName, DateTime.Now);

                SaveCalInfo();
            }
        }

        #endregion Calibration

        #region System

        public void SetSystemTime(DateTime time)
        {
            var localTime = DateTime.SpecifyKind(time, DateTimeKind.Local);
            DateTimeOffset dto = localTime;
            Windows.System.DateTimeSettings.SetSystemDateTime(dto);
        }

        public string GetLocalIp()
        {
            var ipAddresses = new List<string>();
            var hosts = NetworkInformation.GetHostNames().ToList();
            foreach (var host in hosts)
            {
                string ip = host.DisplayName;
                //nur IP v4 Adressen auswählen
                var match = Regex.Match(ip, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
                if (match.Success)
                    ipAddresses.Add(ip);
            }

            return ipAddresses.Last(); //TODO: Ist es wirklich immer die letzte IP???
        }

        /// <summary>
        /// Get the paths used by the application 
        /// </summary>
        /// <returns>Local paths (InstalledLocation and LocalFolder)</returns>
        public Dictionary<string, string> GetLocalPaths()
        {
            var paths = new Dictionary<string, string>();
            var installedLocation = Package.Current.InstalledLocation;
            var localFolder = ApplicationData.Current.LocalFolder;
            paths.Add("InstalledLocation", installedLocation.Path.ToExternalPath($@"\\{GetLocalIp()}\c$"));
            paths.Add("LocalFolder", localFolder.Path.ToExternalPath($@"\\{GetLocalIp()}\c$"));
            paths.Add("LogFolder", localFolder.Path.ToExternalPath($@"\\{GetLocalIp()}\c$") + $@"\{Logger.LOG_FOLDER}");

            return paths;
        }

        #endregion System

        #region Plc
        private void M_PlcWorker_TriggerEvent()
        {
            Logger.Write("PLC trigger detected.", SubSystem.Plc);

            if (Configuration.EnableCyclicUpdater)
            {
                Logger.Write("The execution of an externally triggered acquisition is not possible, because the automatic cyclic updater is active.", SubSystem.Plc, LoggerLevel.Warning);
                return;
            }

            if (LatestMeasData == null || LatestMeasData.First().Value.Timestamp < DateTime.Now - TimeSpan.FromSeconds(15))
            {
                Logger.Write("PLC trigger has initiated a single measurement", SubSystem.Plc);
                SingleMeasurementAsync();
                OneWireMeasurementAsync();
            }
        }

        /// <summary>
        /// Send a value to the connected PLC
        /// </summary>
        /// <param name="vmAddress">VM address</param>
        /// <param name="value">value</param>
        /// 
        public void SendValueToPlc(int vmAddress, Int16 value, bool incrementSecureCounter = true)
        {
            if (Configuration.LogoConnection != null && Configuration.LogoConnection.Enabled)
            {
                if (vmAddress < 0 || vmAddress > 850)
                    throw new Exception("Invalid VM-Address!");

                //if (m_PlcWorker == null || m_PlcWorker.IsRunning)
                //    throw new Exception("PLC worker is not running.");

                var data = BitConverter.GetBytes(value);

                Array.Reverse(data);
                m_PlcWorker.SendBuffer.Enqueue(new PlcDbData(vmAddress, data));

                if (incrementSecureCounter)
                    IncrementSecureCounter();
            }
        }

        private void IncrementSecureCounter()
        {
            if (m_SecureCounter == Int16.MaxValue)
                m_SecureCounter = Int16.MinValue;
            else
                m_SecureCounter++;

            SendValueToPlc(Configuration.LogoConnection.SecureCounterVmAddress, m_SecureCounter, false);
        }

        #endregion Plc

        #region INotifyPropertyChanged

        /// <summary>
        /// Helpmethod, to call the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="propName">Name of changed property</param>
        protected void PropChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        /// <summary>
        /// Updated property values available
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion INotifyPropertyChanged
    }
}
