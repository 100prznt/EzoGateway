using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config
{
    public class PlcConfig
    {
        /// <summary>
        /// PLC interface enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// IP address of connected PLC.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// VM address of the trigger-output, to performe a new measurement.
        /// Direction: PLC --> EzoGateway
        /// </summary>
        public int TriggerVmAddress { get; set; }

        /// <summary>
        /// Bit position inside the trigger VM address
        /// </summary>
        public int TriggerVmAddressBit { get; set; }

        /// <summary>
        /// VM address of the analog input for pH value.
        /// Scaling factor: 1000 (pH 7.2 --> 7200)
        /// Direction: EzoGateway --> PLC
        /// Valid value range: pH 0 .. 14.00
        /// </summary>
        public int PhVmAddress { get; set; }

        /// <summary>
        /// VM address of the analog input for pH value.
        /// Scaling factor: 100 (960 mV --> 9600)
        /// Direction: EzoGateway --> PLC
        /// Valid value range: 0 .. 3276.7 mV
        /// </summary>
        public int RedoxVmAddress { get; set; }

        /// <summary>
        /// VM address of the analog input for temperature value.
        /// Scaling factor: 100 (20.5 °C --> 2050)
        /// Direction: EzoGateway --> PLC
        /// Valid value range: -273.15 .. 54.52 °C
        /// </summary>
        public int TemperatureVmAddress { get; set; }

        /// <summary>
        /// VM address of the analog input for the secure counter.
        /// Direction: EzoGateway --> PLC
        /// </summary>
        public int SecureCounterVmAddress { get; set; }

        /// <summary>
        /// New configuration, PLC activated, TriggerVmAddressBit = 0
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="vmTrigger"></param>
        /// <param name="vmPh"></param>
        /// <param name="vmRedox"></param>
        /// <param name="vmTemperature"></param>
        /// <param name="vmCounter"></param>
        public PlcConfig(string ip, int vmTrigger, int vmPh, int vmRedox, int vmTemperature, int vmCounter)
        {
            Enabled = true;
            IpAddress = ip;
            TriggerVmAddress = vmTrigger;
            TriggerVmAddressBit = 0;
            PhVmAddress = vmPh;
            RedoxVmAddress = vmRedox;
            TemperatureVmAddress = vmTemperature;
            SecureCounterVmAddress = vmCounter;
        }

        public int GetVmAddressByName(string name)
        {
            switch (name.Trim().ToUpper())
            {
                case "PH VALUE":
                    return PhVmAddress;
                case "REDOX POTENTIAL":
                    return RedoxVmAddress;
                case "TEMPERATURE":
                    return TemperatureVmAddress;
                default:
                    throw new ArgumentException("GetVmAddressByName(name = " + name + ") Name is invalid.");
            }
        }
    }
}
