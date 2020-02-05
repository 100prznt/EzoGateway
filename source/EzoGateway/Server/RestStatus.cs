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
            var level = status == OperationStatus.Error ? LoggerLevel.Error : LoggerLevel.Info;
            Logger.Write(message, SubSystem.RestApi, level);

            Status = status;
            Message = message;
        }
    }
}
