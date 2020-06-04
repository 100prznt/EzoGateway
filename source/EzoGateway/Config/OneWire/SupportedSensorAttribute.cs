using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config.OneWire
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SupportedSensorAttribute : Attribute
    {
        public Type SensorType { get; set; }

        public SensorUnits Unit { get; set; }

        public string Description { get; set; }

        public SupportedSensorAttribute(Type sensorType, SensorUnits unit, string description = "")
        {
            SensorType = sensorType;
            Unit = unit;
            Description = description;
        }
    }
}
