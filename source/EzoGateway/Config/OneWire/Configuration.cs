using Newtonsoft.Json;
using Rca.OneWireLib.Slaves;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config.OneWire
{
    /// <summary>
    /// Configuration of the 1-wire feature on the EzoGateway hardware/PCB
    /// </summary>
    public class Configuration
    {
        #region Properties
        [JsonIgnore]
        public string LocalPath { get; set; }

        /// <summary>
        /// List of defined 1-wire sensors
        /// </summary>
        public List<Sensor> SensorList { get; set; }

        /// <summary>
        /// Sensor which delivers the value for the temperature compensation
        /// </summary>
        public int? SensorIdForTemperatureCompensation { get; set; }

        #endregion Properties

        #region Serivices
        public static Configuration FromJsonFile(string path)
        {
            using (var sr = File.OpenText(path))
            {
                var reader = new JsonTextReader(sr);

                JsonSerializer serializer = new JsonSerializer()
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };

                var conf = serializer.Deserialize<Configuration>(reader);
                conf.LocalPath = path;

                return conf;
            }
        }

        public string ToJson()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            return json;
        }

        /// <summary>
        /// Default settings
        /// </summary>
        public static Configuration Default =>
            new Configuration()
            {
                SensorIdForTemperatureCompensation = null,
                SensorList = new List<Sensor>()
                {
                    new Sensor()
                    {
                        SensorType = SupportedSensors.DS18B20,
                        CustomName = "DummySensor",
                        CustomUniqueId = 100,
                        MasterChannel = 0,
                        OneWireAddressString = "28-AA-72-D7-4F-14-01-03"
                    }
                }
            };

        #endregion Serivices
    }
}
