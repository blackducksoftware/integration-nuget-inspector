using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{


    class ProjectInspectionOptions : InspectionOptions
    {
        public ProjectInspectionOptions() { }

        public ProjectInspectionOptions(InspectionOptions old)
        {
            this.TargetPath = old.TargetPath;
            this.Verbose = old.Verbose;
            this.PackagesRepoUrl = old.PackagesRepoUrl;
            this.OutputDirectory = old.OutputDirectory;
            this.ExcludedModules = old.ExcludedModules;
            this.IgnoreFailure = old.IgnoreFailure;
        }


        public string ProjectName { get; set; }
        public string ProjectDirectory { get; set; }
        public string VersionName { get; set; }
        public string PackagesConfigPath { get; set; }
        
    }
}
