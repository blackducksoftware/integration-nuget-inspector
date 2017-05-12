using Mono.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector.HubNugetInspector
{
    class Application
    {
        public const string PARAM_KEY_APP_SETTINGS_FILE = "app_settings_file";
        public const string PARAM_KEY_SOLUTION = "solution_path";
        public const string PARAM_KEY_OUTPUT_DIRECTORY = "output_directory";
        public const string PARAM_KEY_EXCLUDED_MODULES = "excluded_modules";
        public const string PARAM_KEY_IGNORE_FAILURE = "ignore_failure";

        //private ProjectGenerator ProjectGenerator;
        private string[] Args;
        private Dictionary<string, string> PropertyMap;
        private Dictionary<string, string> CommandLinePropertyMap;
        private Dictionary<string, string> AppSettingsMap;

        private bool ShowHelp = false;
        private bool Verbose = false;

        public Application(string[] args)
        {
            this.Args = args;
            PropertyMap = new Dictionary<string, string>();
            CommandLinePropertyMap = new Dictionary<string, string>();
            AppSettingsMap = new Dictionary<string, string>();
        }

        public static void Main(string[] args)
        {
            try
            {
                Application app = new Application(args);
                app.Execute();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        public bool Execute()
        {
            try
            {
                Configure();
                if (!ShowHelp)
                {
                    return true;//ProjectGenerator.Execute();
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error occurred executing command: {0}", ex.Message);
                Environment.Exit(-1);
                return false;
            }
        }

        private void Configure()
        {
            PopulateParameterMap();
            OptionSet commandOptions = CreateOptionSet();
            ParseCommandLine(commandOptions);
            string usageMessage = "Usage is BuildBom.exe [OPTIONS]";

            if (ShowHelp)
            {
                LogProperties();
                ShowHelpMessage(usageMessage, commandOptions);
            }

            ConfigureGenerator(commandOptions);
        }

        private void LogProperties()
        {
            if (Verbose)
            {
                Console.WriteLine("Configuration Properties: ");
                foreach (string key in PropertyMap.Keys)
                {
                    string property_value = PropertyMap[key];
                    if (key.Contains("password"))
                    {
                        Console.WriteLine("Property {0} = **********", key);
                    }
                    else
                    {
                        Console.WriteLine("Property {0} = {1}", key, PropertyMap[key]);
                    }
                }
            }
        }

        private void PopulateParameterMap()
        {
            NameValueCollection applicationProperties = ConfigurationManager.AppSettings;

            foreach (string key in applicationProperties.AllKeys)
            {
                PropertyMap[key] = applicationProperties[key];
            }
        }

        private OptionSet CreateOptionSet()
        {
            OptionSet optionSet = new OptionSet();
            AddAppSettingsFileMenuOption(optionSet, PARAM_KEY_APP_SETTINGS_FILE, "The file path for the application settings that overrides all settings.");
            AddMenuOption(optionSet, PARAM_KEY_SOLUTION, "The path to the solution file to find dependencies");
            AddMenuOption(optionSet, PARAM_KEY_OUTPUT_DIRECTORY, "The directory path to output the BDIO files.");
            AddMenuOption(optionSet, PARAM_KEY_EXCLUDED_MODULES, "The names of the projects in a solution to exclude from BDIO generation.");
            AddMenuOption(optionSet, PARAM_KEY_IGNORE_FAILURE, "If true log the error but do not throw an exception.");
         
            optionSet.Add("?|h|help", "Display the information on how to use this executable.", value => ShowHelp = value != null);
            optionSet.Add("v|verbose", "Display more messages when the executable runs.", value => Verbose = value != null);

            // add help otion
            return optionSet;
        }

        private void AddAppSettingsFileMenuOption(OptionSet optionSet, string name, string description)
        {
            optionSet.Add($"{name}=", description, (value) =>
            {
                string appSettingsFile = value;
                if (!string.IsNullOrWhiteSpace(appSettingsFile))
                {
                    PopulatePropertyMapByExternalFile(appSettingsFile);
                }
            });
        }

        private void AddMenuOption(OptionSet optionSet, string name, string description)
        {
            optionSet.Add($"{name}=", description, (value) =>
            {
                CommandLinePropertyMap[name] = value;
            });
        }

        private void ParseCommandLine(OptionSet commandOptions)
        {
            try
            {
                commandOptions.Parse(this.Args);
            }
            catch (OptionException)
            {
                ShowHelpMessage("Error processing command line, usage is: buildBom.exe [OPTIONS]", commandOptions);
            }
        }

        private void PopulatePropertyMapByExternalFile(string path)
        {
            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = path;
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            foreach (KeyValueConfigurationElement element in config.AppSettings.Settings)
            {
                AppSettingsMap[element.Key] = element.Value;
            }
        }

        private void ResolveProperties()
        {
            foreach (string key in AppSettingsMap.Keys)
            {
                PropertyMap[key] = AppSettingsMap[key];
            }

            foreach (string key in CommandLinePropertyMap.Keys)
            {
                PropertyMap[key] = CommandLinePropertyMap[key];
            }
        }

        private void ConfigureGenerator(OptionSet commandOptions)
        {
            ResolveProperties();
            /**
            ProjectGenerator = CreateGenerator();

            if (ProjectGenerator == null)
            {
                ShowHelpMessage("Couldn't find a solution or project. Usage buildBom.exe [OPTIONS]", commandOptions);
            }
            **/

            if (PropertyMap.ContainsKey(PARAM_KEY_OUTPUT_DIRECTORY))
            {
                if (String.IsNullOrWhiteSpace(PropertyMap[PARAM_KEY_OUTPUT_DIRECTORY]))
                {
                    string currentDirectory = Directory.GetCurrentDirectory();
                    string defaultOutputDirectory = $"{currentDirectory}{Path.DirectorySeparatorChar}"; // {ProjectGenerator.DEFAULT_OUTPUT_DIRECTORY}";
                    PropertyMap[PARAM_KEY_OUTPUT_DIRECTORY] = defaultOutputDirectory;
                }
            }

            LogProperties();

          //  ProjectGenerator.Verbose = Verbose;
          //  ProjectGenerator.PackagesRepoUrl = GetPropertyValue(PARAM_KEY_PACKAGE_REPO_URL);
          // ProjectGenerator.OutputDirectory = GetPropertyValue(PARAM_KEY_OUTPUT_DIRECTORY);
          //  ProjectGenerator.ExcludedModules = GetPropertyValue(PARAM_KEY_EXCLUDED_MODULES);
          //  ProjectGenerator.IgnoreFailure = Convert.ToBoolean(GetPropertyValue(PARAM_KEY_IGNORE_FAILURE, "false"));
           
        }

        /**
        private ProjectGenerator CreateGenerator()
        {
            string solutionPath = GetPropertyValue(PARAM_KEY_SOLUTION);
            if (string.IsNullOrWhiteSpace(solutionPath))
            {
                Console.WriteLine("Searching for a solution file to process...");
                // search for solution
                string currentDirectory = Directory.GetCurrentDirectory();
                string[] solutionPaths = Directory.GetFiles(currentDirectory, "*.sln");

                if (solutionPaths != null && solutionPaths.Length >= 1)
                {
                    SolutionGenerator solutionGenerator = new SolutionGenerator();
                    solutionGenerator.SolutionPath = solutionPaths[0];
                    solutionGenerator.GenerateMergedBdio = Convert.ToBoolean(GetPropertyValue(PARAM_KEY_HUB_CREATE_MERGED_BDIO, "false"));
                    PropertyMap[PARAM_KEY_SOLUTION] = solutionPaths[0];
                    return solutionGenerator;
                }
                else
                {
                    Console.WriteLine("No Solution file found.  Searching for a project file...");
                    string[] projectPaths = Directory.GetFiles(currentDirectory, "*.csproj");
                    if (projectPaths != null && projectPaths.Length > 0)
                    {
                        string projectPath = projectPaths[0];
                        Console.WriteLine("Found project {0}", projectPath);
                        ProjectGenerator projectGenerator = new ProjectGenerator();
                        projectGenerator.ProjectPath = projectPath;
                        return projectGenerator;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                if (solutionPath.Contains(".sln"))
                {
                    SolutionGenerator solutionGenerator = new SolutionGenerator();
                    solutionGenerator.SolutionPath = solutionPath;

                    return solutionGenerator;
                }
                else
                {
                    ProjectGenerator projectGenerator = new ProjectGenerator();
                    projectGenerator.ProjectPath = solutionPath;
                    return projectGenerator;
                }
            }
        }
    **/

        private string GetPropertyValue(string key, string defaultValue = "")
        {
            if (PropertyMap.ContainsKey(key))
            {
                return PropertyMap[key];
            }
            else
            {
                return defaultValue;
            }
        }

        private void ShowHelpMessage(string message, OptionSet optionSet)
        {
            Console.Error.WriteLine(message);
            optionSet.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
        }
    }
}
