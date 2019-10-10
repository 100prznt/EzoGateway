using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Plc
{
    public class PlcDbData
    {
        public int Address { get; set; }

        public byte[] Data { get; set; }

        public PlcDbData(int address, byte[] data)
        {
            Address = address;
            Data = data;
        }
    }
}
