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
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector.HubNugetInspector
{
    class ProjectInspector
    {
        public const string DEFAULT_OUTPUT_DIRECTORY = "blackduck";
        public const string DEFAULT_DATETIME_FORMAT = "yyyy-MM-dd_HH-mm-ss";
        public string ProjectPath { get; set; }
        public bool Verbose { get; set; } = false;
        public string PackagesRepoUrl { get; set; }
        public string ProjectName { get; set; }
        public string VersionName { get; set; }
        public string OutputDirectory { get; set; }
        public string ExcludedModules { get; set; } = "";
        public bool IgnoreFailure { get; set; } = false;
        public string PackagesConfigPath { get; set; }

        public string Execute()
        {
            string projectInfoFilePath = "";
            try
            {
                Setup();
                List<DependencyNode> dependencies = gatherProjectDependencies();
                projectInfoFilePath = writeProjectInfoFile(dependencies);
            }
            catch (Exception ex)
            {
                if (IgnoreFailure)
                {
                    Console.WriteLine("Error executing Build BOM task on project {0}, cause: {1}", ProjectName, ex);
                }
                else
                {
                    throw ex;
                }
            } 
            return projectInfoFilePath;
        }

        
        public void Setup()
        {
            string projectDirectory = Directory.GetParent(ProjectPath).FullName;
            if (String.IsNullOrWhiteSpace(PackagesConfigPath))
            {
                PackagesConfigPath = CreateProjectPackageConfigPath(projectDirectory);
            }
            if (String.IsNullOrWhiteSpace(OutputDirectory))
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                OutputDirectory = $"{currentDirectory}{Path.DirectorySeparatorChar}{DEFAULT_OUTPUT_DIRECTORY}";
            }
            if (String.IsNullOrWhiteSpace(ProjectName))
            {
               ProjectName = Path.GetFileNameWithoutExtension(ProjectPath);
            }
            if (String.IsNullOrWhiteSpace(VersionName))
            {
                VersionName = GetProjectAssemblyVersion(projectDirectory);
            }
        }

        public List<DependencyNode> gatherProjectDependencies()
        {

            return null;
        }


        public string writeProjectInfoFile(List<DependencyNode> dependencies)
        {
            string outputFilePath = "";
            if (IsExcluded())
            {
                Console.WriteLine("Project {0} excluded from task", ProjectName);
            }
            else
            {
                Console.WriteLine("Processing Project: {0}", ProjectName);

                // Creates output directory if it doesn't already exist
                Directory.CreateDirectory(OutputDirectory);

                // Define output files
                // TODO: fix file name
                outputFilePath = $"{OutputDirectory}{Path.DirectorySeparatorChar}{ProjectName}_info.json";

                //    BdioContent bdioContent = BuildBOM();
                //    File.WriteAllText(bdioFilePath, bdioContent.ToString());
                
                Console.WriteLine("Finished processing project {0}", ProjectName);
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
                return excludedSet.Contains(ProjectName.Trim());
            }
        }

       

        public string CreatePath(List<string> pathSegments)
        {
            return String.Join(String.Format("{0}", Path.DirectorySeparatorChar), pathSegments);
        }

        private string GetProjectAssemblyVersion(string projectDirectory)
        {
            string version = DateTime.UtcNow.ToString(DEFAULT_DATETIME_FORMAT);
            List<string> pathSegments = new List<string>();
            pathSegments.Add(projectDirectory);
            pathSegments.Add("Properties");
            pathSegments.Add("AssemblyInfo.cs");
            string path = CreatePath(pathSegments);

            if (File.Exists(path))
            {
                List<string> contents = new List<string>(File.ReadAllLines(path));
                var versionText = contents.FindAll(text => text.Contains("[assembly: AssemblyVersion"));
                foreach (string text in versionText)
                {
                    int firstParen = text.IndexOf("(");
                    int lastParen = text.LastIndexOf(")");
                    // exclude the '(' and the " characters
                    int start = firstParen + 2;
                    // exclude the ')' and the " characters
                    int end = lastParen - 1;
                    version = text.Substring(start, (end - start));
                }
            }
            return version;
        }

        private string CreateProjectPackageConfigPath(string projectDirectory)
        {
            List<string> pathSegments = new List<string>();
            pathSegments.Add(projectDirectory);
            pathSegments.Add("packages.config");
            return CreatePath(pathSegments);
        }


        #region Make Flat Dependency List

        public List<string> CreateFlatList()
        {
            // Load the packages.config file into a list of Packages
            NuGet.PackageReferenceFile configFile = new NuGet.PackageReferenceFile(PackagesConfigPath);
            List<NuGet.PackageReference> packages = new List<NuGet.PackageReference>(configFile.GetPackageReferences());

            List<string> externalIds = new List<string>();
            foreach (NuGet.PackageReference packageReference in packages)
            {
                string externalId = "";// bdioPropertyHelper.CreateNugetExternalId(packageReference.Id, packageReference.Version.ToString());
                externalIds.Add(externalId);
            }
            return externalIds;
        }

        #endregion

        #region Generate BDIO

        public List<DependencyNode> BuildBOM()
        {
            // Load the packages.config file into a list of Packages
            NuGet.PackageReferenceFile configFile = new NuGet.PackageReferenceFile(PackagesConfigPath);

            // Setup NuGet API
            // Snippets taken from https://daveaglick.com/posts/exploring-the-nuget-v3-libraries-part-2 with modifications
            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support
            providers.AddRange(Repository.Provider.GetCoreV2());  // Add v2 API support
            List<PackageMetadataResource> metadataResourceList = CreateMetaDataResourceList(providers);

            // Create BDIO
            //  BdioContent bdioContent = BuildBOMFromMetadata(new List<NuGet.PackageReference>(configFile.GetPackageReferences()), metadataResourceList);
            //  return bdioContent;
            return null;
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

        public void BuildBOMFromMetadata(List<NuGet.PackageReference> packages, List<PackageMetadataResource> metadataResourceList)
        {
            //BdioPropertyHelper bdioPropertyHelper = new BdioPropertyHelper();
            //BdioNodeFactory bdioNodeFactory = new BdioNodeFactory(bdioPropertyHelper);
            //BdioContent bdio = new BdioContent();

            // Create bdio bill of materials node
           // BdioBillOfMaterials bdioBillOfMaterials = bdioNodeFactory.CreateBillOfMaterials(HubCodeLocationName, HubProjectName, HubVersionName);

            // Create bdio project node
            //string projectBdioId = bdioPropertyHelper.CreateBdioId(HubProjectName, HubVersionName);
            //BdioExternalIdentifier projectExternalIdentifier = bdioPropertyHelper.CreateNugetExternalIdentifier(HubProjectName, HubVersionName); // Note: Could be different. Look at config file
            //BdioProject bdioProject = bdioNodeFactory.CreateProject(HubProjectName, HubVersionName, projectBdioId, projectExternalIdentifier);

            // Create relationships for every bdio node
           // List<BdioNode> bdioComponents = new List<BdioNode>();
           // foreach (NuGet.PackageReference packageRef in packages)
            //{
                // Create component node
           //     string componentName = packageRef.Id;
           //     string componentVersion = packageRef.Version.ToString();
           //     string componentBdioId = bdioPropertyHelper.CreateBdioId(componentName, componentVersion);
           //     BdioExternalIdentifier componentExternalIdentifier = bdioPropertyHelper.CreateNugetExternalIdentifier(componentName, componentVersion);
           //     BdioComponent component = bdioNodeFactory.CreateComponent(componentName, componentVersion, componentBdioId, componentExternalIdentifier);

                // Add references
          //      List<PackageDependency> packageDependencies = GetPackageDependencies(packageRef, metadataResourceList);
          //      foreach (PackageDependency packageDependency in packageDependencies)
          //      {
                    // Create node from dependency info
          //          string dependencyName = packageDependency.Id;
          //          string dependencyVersion = GetDependencyVersion(packageDependency, packages);
          //          string dependencyBdioId = bdioPropertyHelper.CreateBdioId(dependencyName, dependencyVersion);
          //          BdioExternalIdentifier dependencyExternalIdentifier = bdioPropertyHelper.CreateNugetExternalIdentifier(dependencyName, dependencyVersion);
          //          BdioComponent dependency = bdioNodeFactory.CreateComponent(dependencyName, dependencyVersion, dependencyBdioId, dependencyExternalIdentifier);

                    // Add relationship
          //          bdioPropertyHelper.AddRelationship(component, dependency);
          //      }

           //     bdioComponents.Add(component);
          //  }

         //   bdio.BillOfMaterials = bdioBillOfMaterials;
         //   bdio.Project = bdioProject;
         //   bdio.Components = bdioComponents;

         //   return bdio;
        }

      //  public void WriteBdio(BdioContent bdio, TextWriter textWriter)
      //  {
      //      BdioWriter writer = new BdioWriter(textWriter);
      //      writer.WriteBdioNode(bdio.BillOfMaterials);
      //      writer.WriteBdioNode(bdio.Project);
      //      writer.WriteBdioNodes(bdio.Components);
      //      writer.Dispose();
      //  }

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

        #endregion
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
