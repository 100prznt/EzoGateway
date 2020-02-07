using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config
{
    /// <summary>
    /// Appearance settings for build-in GUI and web-interface
    /// </summary>
    public class UiConfig
    {
        /// <summary>
        /// User specified device name, showed in GUI (e.g. "My own pool")
        /// </summary>
        public string DeviceName { get; set; }
    }
}
