using Rca.OneWireLib.Slaves;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config
{
    /// <summary>
    /// 1-wire sensor definition
    /// </summary>
    public class OneWireSensor
    {
        /// <summary>
        /// Sensortype, hardware device type
        /// </summary>
        public IOneWireSlave SensorType { get; set; }

        /// <summary>
        /// User-defined name, is displayed in the sensor overview (API)
        /// </summary>
        public string CustomName { get; set; }

        /// <summary>
        /// User-defined ID, must be unique
        /// </summary>
        public int CustomUniqueId { get; set; }

        /// <summary>
        /// Unique 1-wire address string, bytes can be seperatet by '-' or ':' char
        /// </summary>
        public string OneWireAddressString { get; set; }

        public int MasterChannel { get; set; }
    }
}
