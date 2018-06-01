using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    public static class CommandLineArgKeys 
    {
        public const string AppSettingsFile = "app_settings_file";
        public const string TargetPath = AppConfigKeys.TargetPath;
        public const string PackagesRepoUrl = AppConfigKeys.PackagesRepoUrl;
        public const string NugetConfigPath = AppConfigKeys.NugetConfigPath;
        public const string OutputDirectory = AppConfigKeys.OutputDirectory;
        public const string ExcludedModules = AppConfigKeys.ExcludedModules;
        public const string IncludedModules = AppConfigKeys.IncludedModules;
        public const string IgnoreFailures = AppConfigKeys.IgnoreFailures;
    }
}
