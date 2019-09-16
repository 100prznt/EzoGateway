using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzoGateway.Helpers
{
    public static class PathHelper
    {
        public static void AppendSegment(ref string path, string segment)
        {
            var tmpPath = path.TrimEnd('\\');

            path = tmpPath + "\\" + segment.Trim('\\');
        }

        public static string ToExternalPath(this string path, string externalRoot)
        {
            var cleanPath = path;//.Replace('\\', '#');

            if (cleanPath.StartsWith("c:", StringComparison.OrdinalIgnoreCase))
                return externalRoot + cleanPath.Substring(2);

            else
                return cleanPath;
        }
    }
}
