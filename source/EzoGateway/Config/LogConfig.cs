using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config
{
    /// <summary>
    /// Configuration of the logger
    /// </summary>
    public class LogConfig
    {
        /// <summary>
        /// Subsystems which are excluded from logging
        /// </summary>
        public SubSystem ExcludedSubSystems { get; set; }

        /// <summary>
        /// Log depth from which log messages are saved 
        /// </summary>
        public LoggerLevel MinimumLogLevel { get; set; }
    }
}
