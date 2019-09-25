using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Server
{
    public class RestStatus
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationStatus Status { get; set; }

        public string Message { get; set; }

        public RestStatus(OperationStatus status, string message)
        {
            Status = status;
            Message = message;
        }
    }
}
