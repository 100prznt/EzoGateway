using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config
{
    /// <summary>
    /// Hardware configuration of an EZO device.
    /// </summary>
    public class EzoConfig
    {
        /// <summary>
        /// EZO device is installed and in use.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// I2C Address of the EZO device.
        /// </summary>
        public byte I2CAddress { get; set; }



        /// <summary>
        /// New configuration, EZO device deactivated.
        /// </summary>
        public EzoConfig()
        {
            Enabled = false;
            I2CAddress = 0x00;
        }

        /// <summary>
        /// New configuration, EZO device activated.
        /// </summary>
        /// <param name="address">I2C address of the EZO device.</param>
        public EzoConfig(byte address)
        {
            Enabled = true;
            I2CAddress = address;
        }
    }
}
