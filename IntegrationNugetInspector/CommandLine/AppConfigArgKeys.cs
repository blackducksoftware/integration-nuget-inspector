using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    public static class AppConfigKeys
    {
        public const string TargetPath = "target_path";
        public const string PackagesRepoUrl = "packages_repo_url"; 
        public const string NugetConfigPath = "nuget_config_path"; 
        public const string OutputDirectory = "output_directory";
        public const string ExcludedModules = "excluded_modules";
        public const string IncludedModules = "included_modules";
        public const string IgnoreFailures = "ignore_failure";
    }
}
