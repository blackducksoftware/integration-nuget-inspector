using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Com.Blackducksoftware.Integration.Nuget.Inspector;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v2;

namespace Com.Blackducksoftware.Integration.Nuget.DependencyResolvers
{
    class PackagesConfigResolver : DependencyResolver
    {

        private string PackagesConfigPath;
        private string PackagesRepoUrl;

        public PackagesConfigResolver(string packagesConfigPath, string packagesRepoUrl)
        {
            PackagesConfigPath = packagesConfigPath;
            PackagesRepoUrl = packagesRepoUrl;
        }

        public DependencyResult Process()
        {
            var result = new DependencyResult();

            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support
            providers.AddRange(Repository.Provider.GetCoreV2());  // Add v2 API support
            List<PackageMetadataResource> metadataResourceList = CreateMetaDataResourceList(providers);
            NuGet.PackageReferenceFile configFile = new NuGet.PackageReferenceFile(PackagesConfigPath);
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
                result.Nodes.Add(child);
            }
            return result;
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


        private List<PackageDependency> GetPackageDependencies(NuGet.PackageReference packageDependency, List<PackageMetadataResource> metadataResourceList)
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

        private List<PackageMetadataResource> CreateMetaDataResourceList(List<Lazy<INuGetResourceProvider>> providers)
        {
            List<PackageMetadataResource> list = new List<PackageMetadataResource>();
            string[] splitRepoUrls = PackagesRepoUrl.Split(new char[] { ',' });
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

        private bool FrameworksMatch(PackageDependencyGroup framework1, NuGet.PackageReference framework2)
        {
            bool majorMatch = framework1.TargetFramework.Version.Major == framework2.TargetFramework.Version.Major;
            bool minorMatch = framework1.TargetFramework.Version.Minor == framework2.TargetFramework.Version.Minor;
            return majorMatch && minorMatch;
        }
    }
}
