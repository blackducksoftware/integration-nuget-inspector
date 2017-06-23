using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{


    class SolutionInspectionOptions : InspectionOptions
    {

        public SolutionInspectionOptions() { }

        public SolutionInspectionOptions(InspectionOptions old)
        {
            this.TargetPath = old.TargetPath;
            this.Verbose = old.Verbose;
            this.PackagesRepoUrl = old.PackagesRepoUrl;
            this.OutputDirectory = old.OutputDirectory;
            this.ExcludedModules = old.ExcludedModules;
            this.IgnoreFailure = old.IgnoreFailure;
        }

        public string SolutionName { get; set; }
        
    }
}
