using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Calibration
{
    /// <summary>
    /// Class for transferring calibration data via the REST API.
    /// </summary>
    public class CalData
    {
        /// <summary>
        /// Type of EZO device
        /// </summary>
        public string EzoDevice { get; set; }

        /// <summary>
        /// Range for which the calibration point is valid
        /// </summary>
        public string CalibPointName { get; set; }

        /// <summary>
        /// Value of the calibration point
        /// </summary>
        public double Value { get; set; }
    }
}
