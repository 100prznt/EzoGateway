using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Server
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MimeAttribute : Attribute
    {
        public string ContentType { get; set; }

        public MimeAttribute(string contentType)
        {
            ContentType = contentType;
        }
    }
}
