using Mono.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    public class CommandLineOptions
    {
        [ParamKey(ParamKeys.AppSettingsFile, "The file path for the application settings that overrides all settings.")]
        public string AppSettingsFile = "";

        [ParamKey(ParamKeys.TargetPath, "The path to the solution or project file to find dependencies")]
        public string TargetPath = "";

        [ParamKey(ParamKeys.OutputDirectory, "The directory path to output the dependency node files.")]
        public string OutputDirectory = "";

        [ParamKey(ParamKeys.ExcludedModules, "The names of the projects in a solution to exclude from dependency node generation.")]
        public string ExcludedModules = "";

        [ParamKey(ParamKeys.IgnoreFailures, "If true log the error but do not throw an exception.")]
        public string IgnoreFailures = "";

        [ParamKey(ParamKeys.PackagesRepoUrl, "The URL of the NuGet repository to get the packages.")]
        public string PackagesRepoUrl = "";

        public bool ShowHelp;
        public bool Verbose;
    }

    class CommandLineRunner
    {

        private InspectorDispatch Dispatch;

        public CommandLineRunner(InspectorDispatch dispatch)
        {
            Dispatch = dispatch;
        }

        //Execute and if succesfull, return a property map of command options.
        public InspectionResult Execute(string[] args)
        {
            CommandLineOptions result = new CommandLineOptions();
            OptionSet commandOptions = new OptionSet();

            foreach (var field in typeof(CommandLineOptions).GetFields())
            {
                var attr = GetParamKeyAttr(field);
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

            if (!string.IsNullOrWhiteSpace(result.AppSettingsFile))
            {
                ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
                configFileMap.ExeConfigFilename = result.AppSettingsFile;
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
                foreach (KeyValueConfigurationElement element in config.AppSettings.Settings)
                {
                    foreach (var field in typeof(CommandLineOptions).GetFields())
                    {
                        var attr = GetParamKeyAttr(field);
                        if (attr != null && element.Key == attr.Key)
                        {
                            if (String.IsNullOrWhiteSpace(field.GetValue(result) as String))
                            {
                                field.SetValue(result, element.Value);
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(result.TargetPath))
            {
                result.TargetPath = Directory.GetCurrentDirectory();
            }

            if (result.ShowHelp)
            {
                LogOptions(result);
                ShowHelpMessage("Usage is IntegrationNugetInspector.exe [OPTIONS]", commandOptions);
            }
            else
            {
                InspectionOptions opts = new InspectionOptions()
                {
                    ExcludedModules = result.ExcludedModules,
                    IgnoreFailure = result.IgnoreFailures == "true",
                    OutputDirectory = result.OutputDirectory,
                    PackagesRepoUrl = result.PackagesRepoUrl,
                    TargetPath = result.TargetPath,
                    Verbose = result.Verbose
                };

                var inspectionResult = Dispatch.Inspect(opts);

                if (inspectionResult != null)
                {
                    var writer = new InspectionResultWriter(inspectionResult);
                    writer.Write();
                    Console.WriteLine("Info file created at {0}", writer.FilePath());
                }
            }

            return null;
        }

        private ParamKeyAttribute GetParamKeyAttr(FieldInfo field)
        {
            var attrs = field.GetCustomAttributes(typeof(ParamKeyAttribute), false);
            if (attrs.Length > 0)
            {
                return attrs[0] as ParamKeyAttribute;
            }
            return null;
        }

        private void ShowHelpMessage(string message, OptionSet optionSet)
        {
            Console.Error.WriteLine(message);
            optionSet.WriteOptionDescriptions(Console.Error);
        }

        private void LogOptions(CommandLineOptions options)
        {

            Console.WriteLine("Configuration Properties: ");
            Console.WriteLine("Property {0} = {1}", ParamKeys.AppSettingsFile, options.AppSettingsFile);
            Console.WriteLine("Property {0} = {1}", ParamKeys.TargetPath, options.TargetPath);
            Console.WriteLine("Property {0} = {1}", ParamKeys.OutputDirectory, options.OutputDirectory);
            Console.WriteLine("Property {0} = {1}", ParamKeys.ExcludedModules, options.ExcludedModules);
            Console.WriteLine("Property {0} = {1}", ParamKeys.IgnoreFailures, options.IgnoreFailures);
            Console.WriteLine("Property {0} = {1}", ParamKeys.PackagesRepoUrl, options.PackagesRepoUrl);
        }


    }
}
