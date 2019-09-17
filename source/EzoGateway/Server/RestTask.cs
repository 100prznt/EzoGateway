using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Server
{
    public class RestTask
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        public string Name { get; set; }

        [JsonProperty("href")]
        public string Reference { get; set; }

        public RestTask(int id, string name, string href = "")
        {
            Id = id;
            Name = name;
            Reference = href;
        }
    }
}
