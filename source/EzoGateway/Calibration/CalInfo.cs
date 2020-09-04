using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Calibration
{
    /// <summary>
    /// Class for store calibration info in the local storage.
    /// </summary>
    public class CalInfo
    {
        /// <summary>
        /// Last calibration date for specified sensors
        /// </summary>
        public Dictionary<string, DateTime> LastCalDate { get; set; }


        public CalInfo()
        {
            LastCalDate = new Dictionary<string, DateTime>();
        }

        #region Serialization

        public static CalInfo FromJson(string json)
        {
            var config = JsonConvert.DeserializeObject<CalInfo>(json,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            return config;
        }

        public static CalInfo FromJsonFile(string path)
        {
            using (var sr = File.OpenText(path))
            {
                var reader = new JsonTextReader(sr);

                JsonSerializer serializer = new JsonSerializer()
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };

                var conf = serializer.Deserialize<CalInfo>(reader);

                return conf;
            }
        }

        public string ToJson()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            return json;
        }
        #endregion Serialization

    }
}
