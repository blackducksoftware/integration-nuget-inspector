using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget
{
    public class NugetFramework
    {
        public int Major;
        public int Minor;

        public NugetFramework(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }
    }
}
