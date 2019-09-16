using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Server
{
    [AttributeUsage(AttributeTargets.Field)]
    public class IsBinaryAttribute : Attribute
    {
        public bool IsBinary { get; set; }

        public IsBinaryAttribute(bool isBinary = true)
        {
            IsBinary = isBinary;
        }
    }
}
