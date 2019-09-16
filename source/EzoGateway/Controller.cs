using EzoGateway.Measurement;
using Rca.EzoDeviceLib;
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
        public Dictionary<int, SensorInfo> SensorInfos { get; set; }

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

        public Controller()
        {
            var t = Task.Run(() => InitHardware());
            t.Wait();
        }

        /// <summary>
        /// Hardware initialization
        /// </summary>
        /// <returns>true: success; false: failed</returns>
        public bool InitHardware()
        {
            try
            {
                RedoxSensor = new EzoOrp(); //init with default i2c address (0x62)
                PhSensor = new EzoPh(); //init with default i2c address (0x63)
                //TempSensor = new EzoRtd(); //init with default i2c address (0x66)

                SensorInfos = new Dictionary<int, SensorInfo>();
                SensorInfos.Add(1, GetSensorInfo(PhSensor, "Atlas Scientific EZO pH Circuit")); //id for pH: 1
                SensorInfos.Add(2, GetSensorInfo(RedoxSensor, "Atlas Scientific EZO ORP circuit")); //id for Redox: 2
                //SensorInfos.Add(3, GetSensorInfo(TempSensor)); //id for Temperature: 3

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

        #region Internal services

        SensorInfo GetSensorInfo(EzoBase ezoSensor, string description = "N/A")
        {
            var devInfo = ezoSensor.GetDeviceInfo();
            var devStatus = ezoSensor.GetDeviceStatus();

            var info = new SensorInfo()
            {
                Name = devInfo.DeviceType,
                Description = description,
                Interface = $"I2C ({ezoSensor.BusSpeed})",
                FirmwareVersion = devInfo.FirmwareVersion,
                SupplyVoltage = devStatus.VccVoltage,
                Address = ezoSensor.I2CAddress,
                Serial = "N/A",
                Package = typeof(EzoBase).AssemblyQualifiedName
            };

            return info;
        }

        #endregion Internal services
    }
}
