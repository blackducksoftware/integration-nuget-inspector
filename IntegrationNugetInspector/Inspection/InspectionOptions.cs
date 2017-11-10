using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class InspectionOptions
    {

        public string TargetPath { get; set; } = "";
        public bool Verbose { get; set; } = false;
        public string PackagesRepoUrl { get; set; } = "";
        public string OutputDirectory { get; set; } = "";
        public string ExcludedModules { get; set; } = "";
        public string IncludedModules { get; set; } = "";
        public bool IgnoreFailure { get; set; } = false;

    }
}
