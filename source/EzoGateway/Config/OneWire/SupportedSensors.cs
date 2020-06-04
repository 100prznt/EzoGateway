using Rca.OneWireLib.Slaves;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config.OneWire
{
    public enum SupportedSensors
    {
        [SupportedSensor(typeof(DS18B20), SensorUnits.Celsius)]
        DS18B20,
    }
}
