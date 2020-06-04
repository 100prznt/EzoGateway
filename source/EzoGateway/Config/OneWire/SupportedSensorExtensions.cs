using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Config.OneWire
{
    public static class SupportedSensorExtensions
    {
        public static Type GetSensorType(this SupportedSensors sensor)
        {
            Attribute[] attributes = sensor.GetAttributes();

            SupportedSensorAttribute attr = null;

            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i].GetType() == typeof(SupportedSensorAttribute))
                {
                    attr = (SupportedSensorAttribute)attributes[i];
                    break;
                }
            }

            if (attr == null)
                return null;
            else
                return attr.SensorType;
        }

        public static SensorUnits GetUnit(this SupportedSensors sensor)
        {
            Attribute[] attributes = sensor.GetAttributes();

            SupportedSensorAttribute attr = null;

            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i].GetType() == typeof(SupportedSensorAttribute))
                {
                    attr = (SupportedSensorAttribute)attributes[i];
                    break;
                }
            }

            if (attr == null)
                return SensorUnits.NotSet;
            else
                return attr.Unit;
        }

        public static string GetDescription(this SupportedSensors sensor)
        {
            Attribute[] attributes = sensor.GetAttributes();

            SupportedSensorAttribute attr = null;

            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i].GetType() == typeof(SupportedSensorAttribute))
                {
                    attr = (SupportedSensorAttribute)attributes[i];
                    break;
                }
            }

            if (attr == null)
                return "No description available.";
            else
                return attr.Description;
        }

        public static Attribute[] GetAttributes(this SupportedSensors sensor)
        {
            var fi = sensor.GetType().GetField(sensor.ToString());
            Attribute[] attributes = (Attribute[])fi.GetCustomAttributes(typeof(Attribute), false);

            return attributes;
        }
    }
}
