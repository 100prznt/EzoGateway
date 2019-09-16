using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Server
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FileExtensionAttribute : Attribute
    {
        public string[] FileExtensions { get; set; }

        public FileExtensionAttribute(params string[] extensions)
        {
            FileExtensions = extensions;
        }
    }
}
