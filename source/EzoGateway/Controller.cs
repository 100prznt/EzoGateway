using EzoGateway.Calibration;
using EzoGateway.Config;
using EzoGateway.Helpers;
using EzoGateway.Measurement;
using EzoGateway.Plc;
using Rca.EzoDeviceLib;
using Rca.EzoDeviceLib.Specific.Ph;
using Rca.EzoGateway.Plc.Sharp7;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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

        #region Members
        private Int16 m_SecureCounter;
        private PlcWorker m_PlcWorker;

        #endregion Members

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
                await DeleteConfigFile();

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

        public async Task DeleteConfigFile()
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

                //InitPlc();
                if (Configuration.LogoConnection != null && Configuration.LogoConnection.Enabled)
                {
                    m_PlcWorker = new PlcWorker(Configuration.LogoConnection.IpAddress);
                    m_PlcWorker.SetUpTrigger(Configuration.LogoConnection.TriggerVmAddress, Configuration.LogoConnection.TriggerVmAddressBit);
                    m_PlcWorker.TriggerEvent += M_PlcWorker_TriggerEvent;
                    m_PlcWorker.Start();
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

        #region Measurement
        public async Task<bool> SingleMeasurementAsync()
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

                Debug.WriteLine("SingleMeasurementAsync() successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SingleMeasurementAsync() failed. Inner exception: " + ex);
                return false;
            }
        }

        private void AddMeasDataInfo(int id, double? measValue, MeasDataInfo info)
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

                if (Configuration.LogoConnection != null && Configuration.LogoConnection.Enabled)
                {
                    int factor = 100;
                    if (string.Equals(info.Name, "ph", StringComparison.OrdinalIgnoreCase))
                    {
                        factor = 1000;
                    }

                    var scaledValue = value * factor;
                    if (scaledValue < Int16.MinValue || scaledValue > Int16.MaxValue)
                        Debug.WriteLine("Value to large for DWORD");
                    else
                        SendValueToPlc(Configuration.LogoConnection.GetVmAddressByName(info.Name), Convert.ToInt16(scaledValue));
                }
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

        public bool CalOrpAddPoint(CalData data, out string errorMessage)
        {
            errorMessage = "";
            if (!data.EzoDevice.Equals("ORP", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"Calibration data not for EZO ORP Circuit, calibration aborted.";
                return false;
            }

            if (RedoxSensor != null)
            {
                RedoxSensor.SetCalibrationPoint((int)data.Value);
                return true;
            }
            else
            {
                errorMessage = $"EZO ORP Circuit not initialized, calibration aborted.";
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

        #region Plc
        private void M_PlcWorker_TriggerEvent()
        {
            Debug.WriteLine("PLC trigger detected.");

            if (LatestMeasData == null || LatestMeasData.First().Value.Timestamp < DateTime.Now - TimeSpan.FromSeconds(15))
            {
                Debug.WriteLine("PLC trigger has initiated a single measurement!");
                SingleMeasurementAsync();
            }
        }

        //public void InitPlc()
        //{
        //    if (Configuration.LogoConnection != null && Configuration.LogoConnection.Enabled)
        //    {
        //        m_Plc = new S7Client();

        //        lock (m_Plc)
        //        {
        //            m_Plc.SetConnectionParams(Configuration.LogoConnection.IpAddress, 0x0300, 0x0200);
        //            if (m_Plc.Connect() != 0)
        //            {
        //                Debug.WriteLine("Failed to open PLC connection.");
        //            }
        //            else
        //            {
        //                // Create an AutoResetEvent to signal the timeout threshold in the timer callback has been reached.
        //                var autoEvent = new AutoResetEvent(false);

        //                var statusChecker = new PlcChecker(ref m_Plc,
        //                    Configuration.LogoConnection.TriggerVmAddress,
        //                    Configuration.LogoConnection.TriggerVmAddressBit);
        //                statusChecker.PlcTriggerEvent += StatusChecker_PlcTriggerEvent;
        //                m_PlcTimer = new Timer(statusChecker.CheckStatus, autoEvent, 10000, 250);
        //                m_BlockPlcTrigger = false;

        //                Debug.WriteLine("PLC successfully initialized.");
        //            }
        //        }
        //    }
        //}


        //private class PlcChecker
        //{
        //    private int m_Address;
        //    private int m_Bit;
        //    private S7Client m_Plc;
        //    private int m_ErrorCount;

        //    public PlcChecker(ref S7Client plc, int vmTriggerAddress, int vmTriggerBit)
        //    {
        //        Debug.WriteLine("Init PLC status checker.");
        //        m_Plc = plc;
        //        m_Address = vmTriggerAddress;
        //        m_Bit = vmTriggerBit;
        //    }

        //    // This method is called by the timer delegate.
        //    public void CheckStatus(Object stateInfo)
        //    {
        //        AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

        //        lock (m_Plc)
        //        {
        //            var buffer = new byte[1];
        //            var result = m_Plc.ReadArea(0x84, 1, m_Address, 1, S7Consts.S7WLByte, buffer);
        //            if (result != 0)
        //                Debug.WriteLine("Error during read trigger signal from PLC.");

        //            if ((buffer[0] & (1 << m_Bit)) != 0)
        //                PlcTriggerEvent?.Invoke();
        //        }

        //        autoEvent.Set();
        //    }

        //    public event Action PlcTriggerEvent;
        //}

        /// <summary>
        /// Send a value to the connected PLC
        /// </summary>
        /// <param name="vmAddress">VM address</param>
        /// <param name="value">value</param>
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
    }
}
