using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class ProjectInspector : Inspector
    {
        public string VersionName { get; set; }
        public string PackagesConfigPath { get; set; }

        override public string Execute()
        {
            string projectInfoFilePath = "";
            try
            {
                Setup();
                DependencyNode projectNode = GetNode();
                projectInfoFilePath = WriteInfoFile(projectNode);
            }
            catch (Exception ex)
            {
                if (IgnoreFailure)
                {
                    Console.WriteLine("Error executing Build BOM task on project {0}, cause: {1}", Name, ex);
                }
                else
                {
                    throw ex;
                }
            } 
            return projectInfoFilePath;
        }


        override public void Setup()
        {
            string projectDirectory = Directory.GetParent(TargetPath).FullName;
            if (String.IsNullOrWhiteSpace(PackagesConfigPath))
            {
                PackagesConfigPath = CreateProjectPackageConfigPath(projectDirectory);
            }
            if (String.IsNullOrWhiteSpace(OutputDirectory))
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                OutputDirectory = $"{currentDirectory}{Path.DirectorySeparatorChar}{InspectorUtil.DEFAULT_OUTPUT_DIRECTORY}";
            }
            if (String.IsNullOrWhiteSpace(Name))
            {
               Name = Path.GetFileNameWithoutExtension(TargetPath);
            }
            if (String.IsNullOrWhiteSpace(VersionName))
            {
                VersionName = InspectorUtil.GetProjectAssemblyVersion(InspectorUtil.DEFAULT_DATETIME_FORMAT, projectDirectory);
            }
        }

        override public DependencyNode GetNode()
        {
            if (IsExcluded())
            {
                Console.WriteLine("Project {0} excluded from task", Name);
                return null;
            }
            else
            {
                List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
                providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support
                providers.AddRange(Repository.Provider.GetCoreV2());  // Add v2 API support
                List<PackageMetadataResource> metadataResourceList = CreateMetaDataResourceList(providers);
                NuGet.PackageReferenceFile configFile = new NuGet.PackageReferenceFile(PackagesConfigPath);
                List<NuGet.PackageReference> packages = new List<NuGet.PackageReference>(configFile.GetPackageReferences());

                Console.WriteLine("Processing Project: {0}", Name);
                DependencyNode projectNode = new DependencyNode();
                projectNode.Artifact = Name;
                projectNode.Version = VersionName;

                List<DependencyNode> children = new List<DependencyNode>();
                foreach (NuGet.PackageReference packageRef in packages)
                {
                    // Create component node
                    string componentName = packageRef.Id;
                    string componentVersion = packageRef.Version.ToString();
                    DependencyNode child = new DependencyNode();
                    child.Artifact = componentName;
                    child.Version = componentVersion;

                    List<DependencyNode> childDependencies = new List<DependencyNode>();
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
                Console.WriteLine("Finished processing project {0}", Name);
                return projectNode;
            }
        }


        override public string WriteInfoFile(DependencyNode projectNode)
        {
            string outputFilePath = "";
            if (!IsExcluded())
            {
                // Creates output directory if it doesn't already exist
                Directory.CreateDirectory(OutputDirectory);

                // Define output files
                // TODO: fix file name
                outputFilePath = $"{OutputDirectory}{Path.DirectorySeparatorChar}{Name}_dependency_node.json";
                File.WriteAllText(outputFilePath, projectNode.ToString());
            }
            return outputFilePath;
        }

        public bool IsExcluded()
        {

            if (String.IsNullOrWhiteSpace(ExcludedModules))
            {
                return false;
            }
            else
            {
                ISet<string> excludedSet = new HashSet<string>();
                string[] projectNameArray = this.ExcludedModules.Split(new char[] { ',' });
                foreach (string projectName in projectNameArray)
                {
                    excludedSet.Add(projectName.Trim());
                }
                return excludedSet.Contains(Name.Trim());
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
            string[] splitRepoUrls = PackagesRepoUrl.Split(new char[]{','});

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
            foreach(PackageMetadataResource metadataResource in metadataResourceList)
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

    // For the NuGet API
    public class Logger : NuGet.Common.ILogger
    {
        public void LogDebug(string data) => Trace.WriteLine($"DEBUG: {data}");
        public void LogVerbose(string data) => Trace.WriteLine($"VERBOSE: {data}");
        public void LogInformation(string data) => Trace.WriteLine($"INFORMATION: {data}");
        public void LogMinimal(string data) => Trace.WriteLine($"MINIMAL: {data}");
        public void LogWarning(string data) => Trace.WriteLine($"WARNING: {data}");
        public void LogError(string data) => Trace.WriteLine($"ERROR: {data}");
        public void LogSummary(string data) => Trace.WriteLine($"SUMMARY: {data}");

        public void LogInformationSummary(string data)
        {
            Trace.WriteLine($"INFORMATION SUMMARY: {data}");
        }

        public void LogErrorSummary(string data)
        {
            Trace.WriteLine($"ERROR SUMMARY: {data}");
        }
    }
}
