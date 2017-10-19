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
using NuGet.Frameworks;
namespace Com.Blackducksoftware.Integration.Nuget
{
   
    public class NugetSearchService
    {
        List<PackageMetadataResource> MetadataResourceList = new List<PackageMetadataResource>();
        List<DependencyInfoResource> DependencyInfoResourceList = new List<DependencyInfoResource>();
        private CompatibilityProvider frameworkCompatibilityProvider = new CompatibilityProvider(DefaultFrameworkNameProvider.Instance);

        public NugetSearchService(string packagesRepoUrl)
        {
            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support
            providers.AddRange(Repository.Provider.GetCoreV2());  // Add v2 API support
            CreateResourceLists(providers, packagesRepoUrl);
        }

        public IPackageSearchMetadata FindBestPackage(String id, VersionRange versionRange)
        {
            List<IPackageSearchMetadata> matchingPackages = FindPackages(id);
            if (matchingPackages == null) return null;

            var versions = matchingPackages.Select(package => package.Identity.Version);
            var bestVersion = versionRange.FindBestMatch(versions);
            return matchingPackages.Where(package => package.Identity.Version == bestVersion).FirstOrDefault();
        }

        public List<IPackageSearchMetadata> FindPackages(String id)
        {
            
            foreach (PackageMetadataResource metadataResource in MetadataResourceList)
            {
                try
                {
                    var metaResult = metadataResource.GetMetadataAsync(id, includePrerelease: true, includeUnlisted: true, log: new Logger(), token: CancellationToken.None).Result;
                    var matchingPackages = new List<IPackageSearchMetadata>(metaResult);
                    if (matchingPackages.Count > 0)
                    {
                        return matchingPackages;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("A meta data resource was unable to load it's packages: " + ex.Message);
                }
            }

            return null;
        }
       
        private void CreateResourceLists(List<Lazy<INuGetResourceProvider>> providers, string packagesRepoUrl)
        {
            string[] splitRepoUrls = packagesRepoUrl.Split(new char[] { ',' });
            foreach (string repoUrl in splitRepoUrls)
            {
                string url = repoUrl.Trim();
                if (!String.IsNullOrWhiteSpace(url))
                {
                    PackageSource packageSource = new PackageSource(url);
                    SourceRepository sourceRepository = new SourceRepository(packageSource, providers);
                    try
                    {
                        PackageMetadataResource packageMetadataResource = sourceRepository.GetResource<PackageMetadataResource>();
                        MetadataResourceList.Add(packageMetadataResource);
                    }catch (Exception e)
                    {
                        Console.WriteLine("Error loading NuGet Package Meta Data Resource resoure for url: " + url);
                        if (e.InnerException != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                    try
                    {
                        DependencyInfoResource dependencyInfoResource = sourceRepository.GetResource<DependencyInfoResource>();
                        DependencyInfoResourceList.Add(dependencyInfoResource);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error loading NuGet Dependency Resource resoure for url: " + url);
                        if (e.InnerException != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                }
            }

        }

        public IEnumerable<PackageDependency> DependenciesForPackage(PackageIdentity identity, NuGet.Frameworks.NuGetFramework framework)
        {
            foreach (DependencyInfoResource dependencyInfoResource in DependencyInfoResourceList)
            {
                try
                {
                    var infoTask = dependencyInfoResource.ResolvePackage(identity, framework, log: new Logger(), token: CancellationToken.None);
                    var result = infoTask.Result;
                    return result.Dependencies;
                }
                catch (Exception e)
                {
                    Console.WriteLine("A dependency resource was unable to load for package: " + identity);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine(e.InnerException.Message);
                    }
                }
            }
            return new List<PackageDependency>();
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
