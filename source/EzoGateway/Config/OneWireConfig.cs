using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config
{
    /// <summary>
    /// Configuration of the 1-wire feature on the EzoGateway hardware/PCB
    /// </summary>
    public class OneWireConfig
    {
        /// <summary>
        /// List of defined 1-wire sensors
        /// </summary>
        public List<OneWireSensor> SensorList { get; set; }

        /// <summary>
        /// Sensor which delivers the value for the temperature compensation
        /// </summary>
        public int? SensorIdForTemperatureCompensation { get; set; }
    }
}
