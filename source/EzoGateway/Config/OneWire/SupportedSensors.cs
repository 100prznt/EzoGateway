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
        [SupportedSensor(typeof(DS18B20), SensorUnits.Celsius, "The DS18B20 digital thermometer provides 9-bit to 12-bit celsius temperature measurements, in a range from -55 °C to +125 °C (-67 °F to +257 °F) with a ±0.5 °C accuracy from -10°C to +85 °C.")]
        DS18B20,
    }
}
