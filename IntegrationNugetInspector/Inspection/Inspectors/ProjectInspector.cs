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
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Com.Blackducksoftware.Integration.Nuget.DependencyResolvers;
using Com.Blackducksoftware.Integration.Nuget;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class ProjectInspector : IInspector
    {
        public ProjectInspectionOptions Options;
        public NugetSearchService NugetService;

        public ProjectInspector(ProjectInspectionOptions options, NugetSearchService nugetService)
        {
            Options = options;
            NugetService = nugetService;
            if (Options == null)
            {
                throw new Exception("Must provide a valid options object.");
            }

            if (String.IsNullOrWhiteSpace(Options.ProjectDirectory))
            {
                Options.ProjectDirectory = Directory.GetParent(Options.TargetPath).FullName;
            }

            if (String.IsNullOrWhiteSpace(Options.PackagesConfigPath))
            {
                Options.PackagesConfigPath = CreateProjectPackageConfigPath(Options.ProjectDirectory);
            }

            if (String.IsNullOrWhiteSpace(Options.ProjectJsonPath))
            {
                Options.ProjectJsonPath = CreateProjectJsonPath(Options.ProjectDirectory);
            }

            if (String.IsNullOrWhiteSpace(Options.ProjectJsonLockPath))
            {
                Options.ProjectJsonLockPath = CreateProjectJsonLockPath(Options.ProjectDirectory);
            }

            if (String.IsNullOrWhiteSpace(Options.ProjectName))
            {
                Options.ProjectName = Path.GetFileNameWithoutExtension(Options.TargetPath);
            }

            if (String.IsNullOrWhiteSpace(Options.VersionName))
            {
                Options.VersionName = InspectorUtil.GetProjectAssemblyVersion(Options.ProjectDirectory);
            }
        }

        public InspectionResult Inspect()
        {

            try
            {
                return new InspectionResult()
                {
                    Status = InspectionResult.ResultStatus.Success,
                    ResultName = Options.ProjectName,
                    OutputDirectory = Options.OutputDirectory,
                    Node = GetNode()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}", ex.ToString());
                if (Options.IgnoreFailure)
                {
                    Console.WriteLine("Error collecting dependencyinformation on project {0}, cause: {1}", Options.ProjectName, ex);
                    return new InspectionResult()
                    {
                        Status = InspectionResult.ResultStatus.Success,
                        ResultName = Options.ProjectName,
                        OutputDirectory = Options.OutputDirectory
                    };
                }
                else
                {
                    return new InspectionResult()
                    {
                        Status = InspectionResult.ResultStatus.Error,
                        Exception = ex
                    };
                }
            }

        }

        public DependencyNode GetNode()
        {
            if (IsExcluded())
            {
                Console.WriteLine("Project {0} excluded from task", Options.ProjectName);
                return null;
            }
            else
            {
                Console.WriteLine("Processing Project: {0}", Options.ProjectName);
                DependencyNode projectNode = new DependencyNode();
                projectNode.Artifact = Options.ProjectName;
                projectNode.Version = Options.VersionName;
                projectNode.SourcePath = Options.TargetPath;
                projectNode.Type = "Project";

                //Try to parse all output paths for all configurations. 
                projectNode.OutputPaths = FindOutputPaths();
                bool packagesConfigExists = !String.IsNullOrWhiteSpace(Options.PackagesConfigPath) && File.Exists(Options.PackagesConfigPath);
                bool projectJsonExists = !String.IsNullOrWhiteSpace(Options.ProjectJsonPath) && File.Exists(Options.ProjectJsonPath);
                bool projectJsonLockExists = !String.IsNullOrWhiteSpace(Options.ProjectJsonLockPath) && File.Exists(Options.ProjectJsonLockPath);

                if (packagesConfigExists)
                {
                    var packagesConfigResolver = new PackagesConfigResolver(Options.PackagesConfigPath, Options.PackagesRepoUrl, NugetService);
                    var packagesConfigResult = packagesConfigResolver.Process();
                    projectNode.Children = packagesConfigResult.Nodes;
                }
                else if (projectJsonLockExists)
                {
                    var projectJsonLockResolver = new ProjectLockJsonResolver(Options.ProjectJsonLockPath);
                    var projectJsonLockResult = projectJsonLockResolver.Process();
                    projectNode.Children = projectJsonLockResult.Nodes;
                }
                else if (projectJsonExists)
                {
                    var projectJsonResolver = new ProjectJsonResolver(Options.ProjectName, Options.ProjectJsonPath);
                    var projectJsonResult = projectJsonResolver.Process();
                    projectNode.Children = projectJsonResult.Nodes;
                }
                else
                {
                    var referenceResolver = new ProjectReferenceResolver(Options.TargetPath);
                    var projectReferencesResult = referenceResolver.Process();
                    if (projectReferencesResult.Success)
                    {
                        projectNode.Children = projectReferencesResult.Nodes;
                    }
                    else
                    {
                        var xmlResolver = new ProjectXmlResolver(Options.TargetPath);
                        var xmlResult = xmlResolver.Process();
                        projectNode.Version = xmlResult.ProjectVersion;
                        projectNode.Children = xmlResult.Nodes;
                    }
                }

                Console.WriteLine("Finished processing project {0}", Options.ProjectName);
                return projectNode;
            }
        }
        
        
        public List<String> FindOutputPaths()
        {
            try
            {
                Project proj = new Project(Options.TargetPath);
                List<string> outputPaths = new List<string>();
                List<string> configurations;
                proj.ConditionedProperties.TryGetValue("Configuration", out configurations);
                if (configurations == null) configurations = new List<string>();
                foreach (var config in configurations)
                {
                    proj.SetProperty("Configuration", config);
                    proj.ReevaluateIfNecessary();
                    var path = proj.GetPropertyValue("OutputPath");
                    var fullPath = Path.GetFullPath(Path.Combine(proj.DirectoryPath, path));
                    outputPaths.Add(fullPath);
                }
                ProjectCollection.GlobalProjectCollection.UnloadProject(proj);
                return outputPaths;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to load configuration output paths for project {0}", Options.ProjectName);
                return new List<string>();
            }
        }

        public bool IsExcluded()
        {

            if (String.IsNullOrWhiteSpace(Options.ExcludedModules))
            {
                return false;
            }
            else
            {
                ISet<string> excludedSet = new HashSet<string>();
                string[] projectNameArray = Options.ExcludedModules.Split(new char[] { ',' });
                foreach (string projectName in projectNameArray)
                {
                    excludedSet.Add(projectName.Trim());
                }
                return excludedSet.Contains(Options.ProjectName.Trim());
            }
        }

        private string CreateRelativePathToFile(string projectDirectory, string filename)
        {
            List<string> pathSegments = new List<string>();
            pathSegments.Add(projectDirectory);
            pathSegments.Add(filename);
            return InspectorUtil.CreatePath(pathSegments);
        }

        private string CreateProjectPackageConfigPath(string projectDirectory)
        {
            return CreateRelativePathToFile(projectDirectory, "packages.config");
        }

        private string CreateProjectJsonPath(string projectDirectory)
        {
            return CreateRelativePathToFile(projectDirectory, "project.json");
        }

        private string CreateProjectJsonLockPath(string projectDirectory)
        {
            return CreateRelativePathToFile(projectDirectory, "project.lock.json");
        }

    }

}

