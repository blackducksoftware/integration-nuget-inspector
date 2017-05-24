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
        public const string TargetPath = "target_path";
        public const string PackagesRepoUrl = "packages_repo_url";
        public const string OutputDirectory = "output_directory";
        public const string ExcludedModules = "excluded_modules";
        public const string IgnoreFailures = "ignore_failure";
    }
}
