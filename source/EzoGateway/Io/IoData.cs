using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Calibration
{
    /// <summary>
    /// Class for transferring i/o data via the REST API.
    /// </summary>
    public class IoData
    {
        /// <summary>
        /// on/off state
        /// </summary>
        public bool State { get; set; }

        public IoData(bool state)
        {
            State = state;
        }
    }
}
