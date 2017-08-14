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
using NuGet.Versioning;

namespace Com.Blackducksoftware.Integration.Nuget.Search
{
    public class NugetDependency
    {
        public string Name;
        public VersionRange VersionRange;

        public NugetDependency(string name, VersionRange versionRange)
        {
            Name = name;
            VersionRange = versionRange;
        }

        public NugetDependency(PackageDependency dependency)
        {
            Name = dependency.Id;
            VersionRange = dependency.VersionRange;
        }
    }
    public class NugetFramework
    {
        public int Major;
        public int Minor;

        public NugetFramework(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }
    }
    class NugetSearchService
    {
        List<PackageMetadataResource> MetadataResourceList;

        public NugetSearchService(string packagesRepoUrl)
        {
            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support
            providers.AddRange(Repository.Provider.GetCoreV2());  // Add v2 API support
            MetadataResourceList = CreateMetaDataResourceList(providers, packagesRepoUrl);
        }

        public IPackageSearchMetadata FindPackage(String id, VersionRange versionRange)
        {
            foreach (PackageMetadataResource metadataResource in MetadataResourceList)
            {
                var metaResult = metadataResource.GetMetadataAsync(id, includePrerelease: true, includeUnlisted: true, log: new Logger(), token: CancellationToken.None).Result;
                var matchingPackages = new List<IPackageSearchMetadata>(metaResult);
                var versions = matchingPackages.Select(package => package.Identity.Version);
                var bestVersion = versionRange.FindBestMatch(versions);
                return matchingPackages.Where(package => package.Identity.Version == bestVersion).First();
            }

            return null;
        }

        public HashSet<DependencyNode> GetPackageDependencies(NugetDependency packageDependency, NugetFramework framework = null)
        {
            //HashSet<PackageDependency> dependencySet = new HashSet<PackageDependency>();
            HashSet<DependencyNode> dependencies = new HashSet<DependencyNode>();

            var package = FindPackage(packageDependency.Name, packageDependency.VersionRange);
            foreach (PackageDependencyGroup group in package.DependencySets)
            {
                if (framework == null || FrameworksMatch(group, framework))
                {
                    foreach (PackageDependency dependency in group.Packages)
                    {
                        var depPackage = FindPackage(dependency.Id, dependency.VersionRange);

                        DependencyNode dependencyNode = new DependencyNode();
                        dependencyNode.Artifact = depPackage.Identity.Id;
                        dependencyNode.Version = depPackage.Identity.Version.ToNormalizedString();
                        dependencyNode.Children = GetPackageDependencies(new NugetDependency(dependency.Id, dependency.VersionRange), framework);
                        dependencies.Add(dependencyNode);
                    }

                }
            }

            return dependencies;
            
        }



        private List<PackageMetadataResource> CreateMetaDataResourceList(List<Lazy<INuGetResourceProvider>> providers, string packagesRepoUrl)
        {
            List<PackageMetadataResource> list = new List<PackageMetadataResource>();
            string[] splitRepoUrls = packagesRepoUrl.Split(new char[] { ',' });
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

        private bool FrameworksMatch(PackageDependencyGroup framework1, NugetFramework framework2)
        {
            if (framework1.TargetFramework.IsAny)
            {
                return true;
            }
            else if (framework1.TargetFramework.IsAgnostic)
            {
                return true;
            }
            else if (framework1.TargetFramework.IsSpecificFramework)
            {
                bool majorMatch = framework1.TargetFramework.Version.Major == framework2.Major;
                bool minorMatch = framework1.TargetFramework.Version.Minor == framework2.Minor;
                return majorMatch && minorMatch;
            }
            else if (framework1.TargetFramework.IsUnsupported)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
