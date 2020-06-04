using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Rca.OneWireLib.Slaves;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config.OneWire
{
    /// <summary>
    /// 1-wire sensor definition
    /// </summary>
    public class Sensor
    {
        /// <summary>
        /// Sensortype, hardware device type
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SupportedSensors SensorType { get; set; }

        /// <summary>
        /// User-defined name, is displayed in the sensor overview (API)
        /// </summary>
        public string CustomName { get; set; }

        /// <summary>
        /// User-defined ID, must be unique and >10
        /// </summary>
        public int CustomUniqueId { get; set; }

        /// <summary>
        /// Unique 1-wire address string, bytes can be seperatet by '-' or ':' char
        /// </summary>
        public string OneWireAddressString { get; set; }

        public int MasterChannel { get; set; }

        public string Description
        {
            get
            {
                return SensorType.GetDescription();
            }
        }
    }
}
