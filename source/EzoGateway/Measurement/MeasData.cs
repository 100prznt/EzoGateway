using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Measurement
{
    [DebuggerDisplay("{Name, nq} = {Value} {Symbol,nq}")]
    public class MeasData
    {
        public string Name { get; set; }

        public DateTime Timestamp { get; set; }

        public double Value { get; set; }

        public string Unit { get; set; }

        public string Symbol { get; set; }

        public string ToString(int decimals)
        {
            var value = Value.ToString($"F{decimals}");

            return $"{value} {Symbol}";
        }
    }
}
