using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Measurement
{
    /// <summary>
    /// Class with meta data of a sensor.
    /// </summary>
    public class SensorInfo
    {
        /// <summary>
        /// Sensor name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Generell description of the sensor.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Interface via which the sensor is connected to the system.
        /// </summary>
        public string Interface { get; set; }

        /// <summary>
        /// Address of the connected sensor.
        /// </summary>
        public int Address { get; set; }

        /// <summary>
        /// Unique serial number.
        /// </summary>
        public string Serial { get; set; }

        /// <summary>
        /// Firmware version of the sensor.
        /// </summary>
        public string FirmwareVersion { get; set; }

        /// <summary>
        /// Supply voltage of the connected sensor. NaN if not available.
        /// </summary>
        public double SupplyVoltage { get; set; }

        /// <summary>
        /// Library name and version.
        /// </summary>
        public string Package { get; set; }
    }
}
