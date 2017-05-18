/*******************************************************************************
 * Copyright (C) 2017 Black Duck Software, Inc.
 * http://www.blackducksoftware.com/
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements. See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership. The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied. See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *******************************************************************************/
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class Application
    {
        public const string PARAM_KEY_APP_SETTINGS_FILE = "app_settings_file";
        public const string PARAM_KEY_TARGET = "target_path";
        public const string PARAM_KEY_PACKAGE_REPO_URL = "packages_repo_url";
        public const string PARAM_KEY_OUTPUT_DIRECTORY = "output_directory";
        public const string PARAM_KEY_EXCLUDED_MODULES = "excluded_modules";
        public const string PARAM_KEY_IGNORE_FAILURE = "ignore_failure";

        private string[] Args;
        private Dictionary<string, string> PropertyMap;
        private Dictionary<string, string> CommandLinePropertyMap;
        private Dictionary<string, string> AppSettingsMap;

        private Inspector Inspector;

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
                Console.Error.WriteLine(ex.ToString());
            }
        }

        public void Execute()
        {
            try
            {
                Configure();
                if (!ShowHelp)
                {
                    string fileOutputPath = Inspector.Execute();
                    Console.WriteLine("Info file created at {0}", fileOutputPath);
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error occurred executing command: {0}", ex.ToString());
                Environment.Exit(-1);
            }
        }

        private void Configure()
        {
            PopulateParameterMap();
            OptionSet commandOptions = CreateOptionSet();
            ParseCommandLine(commandOptions);
            string usageMessage = "Usage is IntegrationNugetInspector.exe [OPTIONS]";

            if (ShowHelp)
            {
                LogProperties();
                ShowHelpMessage(usageMessage, commandOptions);
            }

            ConfigureInspector(commandOptions);
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
            AddMenuOption(optionSet, PARAM_KEY_TARGET, "The path to the solution or project file to find dependencies");
            AddMenuOption(optionSet, PARAM_KEY_OUTPUT_DIRECTORY, "The directory path to output the dependency node files.");
            AddMenuOption(optionSet, PARAM_KEY_EXCLUDED_MODULES, "The names of the projects in a solution to exclude from dependency node generation.");
            AddMenuOption(optionSet, PARAM_KEY_IGNORE_FAILURE, "If true log the error but do not throw an exception.");
            AddMenuOption(optionSet, PARAM_KEY_PACKAGE_REPO_URL, "The URL of the NuGet repository to get the packages.");

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
                ShowHelpMessage("Error processing command line, usage is: IntegrationNugetInspector.exe [OPTIONS]", commandOptions);
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

        private void ConfigureInspector(OptionSet commandOptions)
        {
            ResolveProperties();

            Inspector = CreateInspector();

            if (Inspector == null)
            {
                ShowHelpMessage("Couldn't find a solution or project. Usage IntegrationNugetInspector.exe [OPTIONS]", commandOptions);
            }


            if (PropertyMap.ContainsKey(PARAM_KEY_OUTPUT_DIRECTORY))
            {
                if (String.IsNullOrWhiteSpace(PropertyMap[PARAM_KEY_OUTPUT_DIRECTORY]))
                {
                    string currentDirectory = Directory.GetCurrentDirectory();
                    string defaultOutputDirectory = $"{currentDirectory}{Path.DirectorySeparatorChar}{InspectorUtil.DEFAULT_OUTPUT_DIRECTORY}";
                    PropertyMap[PARAM_KEY_OUTPUT_DIRECTORY] = defaultOutputDirectory;
                }
            }
            LogProperties();
        }


        private Inspector CreateInspector()
        {
            Inspector inspector = null;
            string targetPath = GetPropertyValue(PARAM_KEY_TARGET);

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                targetPath = Directory.GetCurrentDirectory();
            }


            if (Directory.Exists(targetPath))
            {
                Console.WriteLine("Searching for a solution file to process...");
                // search for solution
                string[] solutionPaths = Directory.GetFiles(targetPath, "*.sln");

                if (solutionPaths != null && solutionPaths.Length >= 1)
                {
                    inspector = new SolutionInspector();
                    inspector.TargetPath = solutionPaths[0];
                }
                else
                {
                    Console.WriteLine("No Solution file found.  Searching for a project file...");
                    string[] projectPaths = Directory.GetFiles(targetPath, "*.*proj");
                    if (projectPaths != null && projectPaths.Length > 0)
                    {
                        string projectPath = projectPaths[0];
                        Console.WriteLine("Found project {0}", projectPath);
                        inspector = new ProjectInspector();
                        inspector.TargetPath = projectPath;
                    }
                }
            }
            else if (File.Exists(targetPath))
            {
                if (targetPath.Contains(".sln"))
                {
                    inspector = new SolutionInspector();
                    inspector.TargetPath = targetPath;
                }
                else
                {
                    inspector = new ProjectInspector();
                    inspector.TargetPath = targetPath;
                }
            }


            if (inspector != null)
            {
                inspector.Verbose = Verbose;
                inspector.PackagesRepoUrl = GetPropertyValue(PARAM_KEY_PACKAGE_REPO_URL);
                inspector.OutputDirectory = GetPropertyValue(PARAM_KEY_OUTPUT_DIRECTORY);
                inspector.ExcludedModules = GetPropertyValue(PARAM_KEY_EXCLUDED_MODULES);
                inspector.IgnoreFailure = Convert.ToBoolean(GetPropertyValue(PARAM_KEY_IGNORE_FAILURE, "false"));

            }

            return inspector;
        }


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
