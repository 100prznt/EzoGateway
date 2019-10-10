using EzoGateway.Measurement;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Rca.EzoDeviceLib;
using Rca.EzoDeviceLib.Specific.Rtd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config
{
    public class GeneralSettings
    {
        #region Properties
        [JsonIgnore]
        public string LocalPath { get; set; }

        /// <summary>
        /// Hardware settings for the EZO pH device.
        /// </summary>
        public EzoConfig PhSensor { get; set; }

        /// <summary>
        /// Hardware settings for the EZO ORP (Redox) device.
        /// </summary>
        public EzoConfig RedoxSensor { get; set; }

        /// <summary>
        /// Hardware settings for the EZO RTD (Temperature) device.
        /// </summary>
        public EzoConfig TemperatureSensor { get; set; }

        /// <summary>
        /// Activation of temperature compensation on the EZO pH device.
        /// </summary>
        public bool EnablePhTemperatureCompensation { get; set; }

        /// <summary>
        /// Temperature unit, valid for the whole app.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public TemperatureScales TemperatureUnit { get; set; }

        public PlcConfig LogoConnection { get; set; }

        /// <summary>
        /// Default settings
        /// </summary>
        public static GeneralSettings Default =>
            new GeneralSettings()
            {
                PhSensor = new EzoConfig(EzoPh.DEFAULT_ADDRESS),
                RedoxSensor = new EzoConfig(EzoOrp.DEFAULT_ADDRESS),
                TemperatureSensor = new EzoConfig(EzoRtd.DEFAULT_ADDRESS),
                EnablePhTemperatureCompensation = true,
                TemperatureUnit = TemperatureScales.Celsius,
                LogoConnection = new PlcConfig("192.168.0.195", 106, 302, 304, 306, 300)
            };

        #endregion Properties

        #region Serialization

        public static GeneralSettings FromJson(string json)
        {
            var config = JsonConvert.DeserializeObject<GeneralSettings>(json,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            return config;
        }

        public static GeneralSettings FromJsonFile(string path)
        {
            using (var sr = File.OpenText(path))
            {
                var reader = new JsonTextReader(sr);

                JsonSerializer serializer = new JsonSerializer()
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };

                var conf = serializer.Deserialize<GeneralSettings>(reader);
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
        #endregion Serialization
    }
}
