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

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class ProjectInspector : IInspector
    {
        public ProjectInspectionOptions Options;

        public ProjectInspector(ProjectInspectionOptions options)
        {
            Options = options;

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
                List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
                providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support
                providers.AddRange(Repository.Provider.GetCoreV2());  // Add v2 API support
                List<PackageMetadataResource> metadataResourceList = CreateMetaDataResourceList(providers);
                Console.WriteLine("Processing Project: {0}", Options.ProjectName);
                DependencyNode projectNode = new DependencyNode();
                projectNode.Artifact = Options.ProjectName;
                projectNode.Version = Options.VersionName;
                HashSet<DependencyNode> children = new HashSet<DependencyNode>();
                if (String.IsNullOrWhiteSpace(Options.PackagesConfigPath) || !File.Exists(Options.PackagesConfigPath))
                {
                    try
                    {
                        //ProjectCollection collection = new ProjectCollection();
                        Project proj = new Project(Options.TargetPath);//,   collection);
                        foreach (ProjectItem reference in proj.GetItems("Reference"))
                        {
                            if (reference.Xml != null && !String.IsNullOrWhiteSpace(reference.Xml.Include) && reference.Xml.Include.Contains("Version"))
                            {
                                string packageInfo = reference.Xml.Include;

                                DependencyNode childNode = new DependencyNode();
                                childNode.Artifact = packageInfo.Substring(0, packageInfo.IndexOf(","));
                                string version = packageInfo.Substring(packageInfo.IndexOf("Version=") + 8);
                                version = version.Substring(0, version.IndexOf(","));
                                version = version.Substring(0, version.LastIndexOf("."));
                                childNode.Version = version;
                                children.Add(childNode);
                            }
                        }
                        ProjectCollection.GlobalProjectCollection.UnloadProject(proj);
                    }
                    catch (InvalidProjectFileException e)
                    {
                        // .NET core default version
                        projectNode.Version = "1.0.0";

                        XmlDocument doc = new XmlDocument();
                        doc.Load(Options.TargetPath);

                        XmlNodeList versionNodes = doc.GetElementsByTagName("Version");
                        if (versionNodes != null && versionNodes.Count > 0)
                        {
                            foreach (XmlNode version in versionNodes)
                            {
                                if (version.NodeType != XmlNodeType.Comment)
                                {
                                    projectNode.Version = version.InnerText;
                                }
                            }
                        }
                        else
                        {
                            string prefix = "1.0.0";
                            string suffix = "";
                            XmlNodeList prefixNodes = doc.GetElementsByTagName("VersionPrefix");
                            if (prefixNodes != null && prefixNodes.Count > 0)
                            {
                                foreach (XmlNode prefixNode in prefixNodes)
                                {
                                    if (prefixNode.NodeType != XmlNodeType.Comment)
                                    {
                                        prefix = prefixNode.InnerText;
                                    }
                                }
                            }
                            XmlNodeList suffixNodes = doc.GetElementsByTagName("VersionSuffix");
                            if (suffixNodes != null && suffixNodes.Count > 0)
                            {
                                foreach (XmlNode suffixNode in suffixNodes)
                                {
                                    if (suffixNode.NodeType != XmlNodeType.Comment)
                                    {
                                        suffix = suffixNode.InnerText;
                                    }
                                }

                            }
                            projectNode.Version = String.Format("{0}-{1}", prefix, suffix); ;
                        }
                        XmlNodeList packagesNodes = doc.GetElementsByTagName("PackageReference");
                        if (packagesNodes.Count > 0)
                        {
                            foreach (XmlNode package in packagesNodes)
                            {
                                DependencyNode childNode = new DependencyNode();
                                XmlAttributeCollection attributes = package.Attributes;
                                if (attributes != null)
                                {
                                    XmlAttribute include = attributes["Include"];
                                    XmlAttribute version = attributes["Version"];
                                    if (include != null && version != null)
                                    {
                                        childNode.Artifact = include.Value;
                                        childNode.Version = version.Value;
                                        children.Add(childNode);
                                    }
                                }
                            }
                        }
                    }
                    if (children.Count != 0)
                    {
                        projectNode.Children = children;
                    }
                }
                else
                {
                    NuGet.PackageReferenceFile configFile = new NuGet.PackageReferenceFile(Options.PackagesConfigPath);
                    List<NuGet.PackageReference> packages = new List<NuGet.PackageReference>(configFile.GetPackageReferences());
                    foreach (NuGet.PackageReference packageRef in packages)
                    {
                        // Create component node
                        string componentName = packageRef.Id;
                        string componentVersion = packageRef.Version.ToString();
                        DependencyNode child = new DependencyNode();
                        child.Artifact = componentName;
                        child.Version = componentVersion;

                        HashSet<DependencyNode> childDependencies = new HashSet<DependencyNode>();
                        // Add references
                        List<PackageDependency> packageDependencies = GetPackageDependencies(packageRef, metadataResourceList);
                        foreach (PackageDependency packageDependency in packageDependencies)
                        {
                            // Create node from dependency info
                            string dependencyName = packageDependency.Id;
                            string dependencyVersion = GetDependencyVersion(packageDependency, packages);

                            DependencyNode dependency = new DependencyNode();
                            dependency.Artifact = dependencyName;
                            dependency.Version = dependencyVersion;
                            childDependencies.Add(dependency);
                        }
                        if (childDependencies.Count != 0)
                        {
                            child.Children = childDependencies;
                        }
                        children.Add(child);
                    }
                    if (children.Count != 0)
                    {
                        projectNode.Children = children;
                    }
                }
                Console.WriteLine("Finished processing project {0}", Options.ProjectName);
                return projectNode;
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

        private string CreateProjectPackageConfigPath(string projectDirectory)
        {
            List<string> pathSegments = new List<string>();
            pathSegments.Add(projectDirectory);
            pathSegments.Add("packages.config");
            return InspectorUtil.CreatePath(pathSegments);
        }

        private List<PackageMetadataResource> CreateMetaDataResourceList(List<Lazy<INuGetResourceProvider>> providers)
        {
            List<PackageMetadataResource> list = new List<PackageMetadataResource>();
            string[] splitRepoUrls = Options.PackagesRepoUrl.Split(new char[] { ',' });
            foreach (string repoUrl in splitRepoUrls)
            {
                string url = repoUrl.Trim();
                if (!String.IsNullOrWhiteSpace(url))
                {
                    PackageSource packageSource = new PackageSource(url);
                    SourceRepository sourceRepository = new SourceRepository(packageSource, providers);
                    PackageMetadataResource packageMetadataResource = sourceRepository.GetResource<PackageMetadataResource>();
                    list.Add(packageMetadataResource);
                }
            }

            return list;
        }

        private string GetDependencyVersion(PackageDependency packageDependency, List<NuGet.PackageReference> packages)
        {
            string version = null;
            foreach (NuGet.PackageReference packageRef in packages)
            {
                if (packageRef.Id == packageDependency.Id)
                {
                    version = packageRef.Version.ToString();
                    break;
                }
            }
            return version;
        }

        public List<PackageDependency> GetPackageDependencies(NuGet.PackageReference packageDependency, List<PackageMetadataResource> metadataResourceList)
        {
            HashSet<PackageDependency> dependencySet = new HashSet<PackageDependency>();
            foreach (PackageMetadataResource metadataResource in metadataResourceList)
            {
                //Gets all versions of package in package repository
                List<IPackageSearchMetadata> matchingPackages = new List<IPackageSearchMetadata>(metadataResource.GetMetadataAsync(packageDependency.Id, true, true, new Logger(), CancellationToken.None).Result);
                foreach (IPackageSearchMetadata matchingPackage in matchingPackages)
                {
                    // Check if the matching package is the same as the version defined
                    if (matchingPackage.Identity.Version.ToString() == packageDependency.Version.ToString())
                    {
                        // Gets every dependency set in the package
                        foreach (PackageDependencyGroup packageDependencySet in matchingPackage.DependencySets)
                        {
                            // Grab the dependency set for the target framework. We only care about majors and minors in the version
                            if (FrameworksMatch(packageDependencySet, packageDependency))
                            {
                                dependencySet.AddRange(packageDependencySet.Packages);
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            List<PackageDependency> dependencies = new List<PackageDependency>();
            dependencies.AddRange(dependencySet);
            return dependencies;
        }

        private bool FrameworksMatch(PackageDependencyGroup framework1, NuGet.PackageReference framework2)
        {
            bool majorMatch = framework1.TargetFramework.Version.Major == framework2.TargetFramework.Version.Major;
            bool minorMatch = framework1.TargetFramework.Version.Minor == framework2.TargetFramework.Version.Minor;
            return majorMatch && minorMatch;
        }

    }

    
}
