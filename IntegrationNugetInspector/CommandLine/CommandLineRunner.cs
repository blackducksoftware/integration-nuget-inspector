using Mono.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    public class RunOptions
    {
        [CommandLineArg(CommandLineArgKeys.AppSettingsFile, "The file path for the application settings that overrides all settings.")]
        public string AppSettingsFile = "";

        [AppConfigArg(AppConfigKeys.TargetPath)]
        [CommandLineArg(CommandLineArgKeys.TargetPath, "The path to the solution or project file to find dependencies")]
        public string TargetPath = "";

        [AppConfigArg(AppConfigKeys.OutputDirectory)]
        [CommandLineArg(CommandLineArgKeys.OutputDirectory, "The directory path to output the dependency node files.")]
        public string OutputDirectory = "";

        [AppConfigArg(AppConfigKeys.ExcludedModules)]
        [CommandLineArg(CommandLineArgKeys.ExcludedModules, "The names of the projects in a solution to exclude from dependency node generation.")]
        public string ExcludedModules = "";

        [AppConfigArg(AppConfigKeys.IgnoreFailures)]
        [CommandLineArg(CommandLineArgKeys.IgnoreFailures, "If true log the error but do not throw an exception.")]
        public string IgnoreFailures = "";

        [AppConfigArg(AppConfigKeys.PackagesRepoUrl)]
        [CommandLineArg(CommandLineArgKeys.PackagesRepoUrl, "The URL of the NuGet repository to get the packages.")]
        public string PackagesRepoUrl = "";

        public bool ShowHelp;
        public bool Verbose;

        public void Override(RunOptions overide)
        {
            AppSettingsFile = String.IsNullOrEmpty(overide.AppSettingsFile) ? this.AppSettingsFile : overide.AppSettingsFile;
            TargetPath = String.IsNullOrEmpty(overide.TargetPath) ? this.TargetPath : overide.TargetPath;
            OutputDirectory = String.IsNullOrEmpty(overide.OutputDirectory) ? this.OutputDirectory : overide.OutputDirectory;
            ExcludedModules = String.IsNullOrEmpty(overide.ExcludedModules) ? this.ExcludedModules : overide.ExcludedModules;
            IgnoreFailures = String.IsNullOrEmpty(overide.IgnoreFailures) ? this.IgnoreFailures : overide.IgnoreFailures;
            PackagesRepoUrl = String.IsNullOrEmpty(overide.PackagesRepoUrl) ? this.PackagesRepoUrl : overide.PackagesRepoUrl;
        }
    }

    class CommandLineRunner
    {

        private InspectorDispatch Dispatch;

        public CommandLineRunner(InspectorDispatch dispatch)
        {
            Dispatch = dispatch;
        }

        private RunOptions ParseArguments(string[] args)
        {
            RunOptions result = new RunOptions();
            OptionSet commandOptions = new OptionSet();

            foreach (var field in typeof(RunOptions).GetFields())
            {
                var attr = GetAttr<CommandLineArgAttribute>(field);
                if (attr != null)
                {
                    commandOptions.Add($"{attr.Key}=", attr.Description, (value) => { field.SetValue(result, value); });
                }
            }

            commandOptions.Add("?|h|help", "Display the information on how to use this executable.", value => result.ShowHelp = value != null);
            commandOptions.Add("v|verbose", "Display more messages when the executable runs.", value => result.Verbose = value != null);

            try
            {
                commandOptions.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelpMessage("Error processing command line, usage is: IntegrationNugetInspector.exe [OPTIONS]", commandOptions);
                return null;
            }

            if (result.ShowHelp)
            {
                LogOptions(result);
                ShowHelpMessage("Usage is IntegrationNugetInspector.exe [OPTIONS]", commandOptions);
                return null;
            }

            return result;
        } 

        public RunOptions LoadAppSettings(string path)
        {

            RunOptions result = new RunOptions();
            
            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = result.AppSettingsFile;
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            foreach (KeyValueConfigurationElement element in config.AppSettings.Settings)
            {
                foreach (var field in typeof(RunOptions).GetFields())
                {
                    var attr = GetAttr<AppConfigArgAttribute>(field);
                    if (attr != null && element.Key == attr.Key)
                    {
                         field.SetValue(result, element.Value);
                    }
                }
            }

            return result;
        }

        //Execute and if succesfull, return a property map of command options.
        public List<InspectionResult> Execute(string[] args)
        {
            RunOptions options = ParseArguments(args);

            if (options == null) return null;

            if (!string.IsNullOrWhiteSpace(options.AppSettingsFile))
            {
                RunOptions appOptions = LoadAppSettings(options.AppSettingsFile);
                options.Override(appOptions);
            }

            if (string.IsNullOrWhiteSpace(options.TargetPath))
            {
                options.TargetPath = Directory.GetCurrentDirectory();
            }

            InspectionOptions opts = new InspectionOptions()
            {
                ExcludedModules = options.ExcludedModules,
                IgnoreFailure = options.IgnoreFailures == "true",
                OutputDirectory = options.OutputDirectory,
                PackagesRepoUrl = options.PackagesRepoUrl,
                TargetPath = options.TargetPath,
                Verbose = options.Verbose
            };

            var inspectionResults = Dispatch.Inspect(opts);

            if (inspectionResults != null)
            {
                foreach (var result in inspectionResults)
                {
                    var writer = new InspectionResultJsonWriter(result);
                    writer.Write();
                    Console.WriteLine("Info file created at {0}", writer.FilePath());
                }
            }

            return inspectionResults;
        }

        private T GetAttr<T>(FieldInfo field) where T : class
        {
            var attrs = field.GetCustomAttributes(typeof(T), false);
            if (attrs.Length > 0)
            {
                return attrs[0] as T;
            }
            return null;
        }

        private void ShowHelpMessage(string message, OptionSet optionSet)
        {
            Console.Error.WriteLine(message);
            optionSet.WriteOptionDescriptions(Console.Error);
        }

        private void LogOptions(RunOptions options)
        {

            Console.WriteLine("Configuration Properties: ");
            Console.WriteLine("Property {0} = {1}", CommandLineArgKeys.AppSettingsFile, options.AppSettingsFile);
            Console.WriteLine("Property {0} = {1}", CommandLineArgKeys.TargetPath, options.TargetPath);
            Console.WriteLine("Property {0} = {1}", CommandLineArgKeys.OutputDirectory, options.OutputDirectory);
            Console.WriteLine("Property {0} = {1}", CommandLineArgKeys.ExcludedModules, options.ExcludedModules);
            Console.WriteLine("Property {0} = {1}", CommandLineArgKeys.IgnoreFailures, options.IgnoreFailures);
            Console.WriteLine("Property {0} = {1}", CommandLineArgKeys.PackagesRepoUrl, options.PackagesRepoUrl);
        }


    }
}
