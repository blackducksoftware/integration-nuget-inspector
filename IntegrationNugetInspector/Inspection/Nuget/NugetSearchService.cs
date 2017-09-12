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

namespace Com.Blackducksoftware.Integration.Nuget
{
   
    public class NugetSearchService
    {
        List<PackageMetadataResource> MetadataResourceList;

        public NugetSearchService(string packagesRepoUrl)
        {
            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support
            providers.AddRange(Repository.Provider.GetCoreV2());  // Add v2 API support
            MetadataResourceList = CreateMetaDataResourceList(providers, packagesRepoUrl);
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
                    try
                    {
                        PackageMetadataResource packageMetadataResource = sourceRepository.GetResource<PackageMetadataResource>();
                        list.Add(packageMetadataResource);
                    }catch (Exception e)
                    {
                        Console.WriteLine("Error loading NuGet Package Meta Data Resource resoure for url: " + url);
                        if (e.InnerException != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                }
            }

            return list;
        }

        public IEnumerable<PackageDependency> PackagesForGroupsWithFramework(IEnumerable<PackageDependencyGroup> groups, NugetFramework framework)
        {
            if (framework == null || groups.Count() == 0)
            {
                return groups.SelectMany(group => group.Packages);
            }

            var anyDirectMatch = groups.Where(group => FrameworksMatch(group, framework));

            if (anyDirectMatch.Count() > 0)
            {
                return anyDirectMatch.SelectMany(group => group.Packages);
            }

            var matchingNames = groups
                .Where(group => group.TargetFramework.Framework == framework.Identifier);

            if (matchingNames.Count() == 0)
            {
                Console.WriteLine($"No matching dependency groups with the given framework name were found {framework.Identifier}!");
                return groups.SelectMany(group => group.Packages);
            }

            var matchingLessThanMajor = matchingNames
                .Where(group => group.TargetFramework.Version.Major <= framework.Major);

            if (matchingLessThanMajor.Count() == 0)
            {
                Console.WriteLine($"No matching dependency groups with the given framework and equal to or less major were found {framework.Identifier} Major {framework.Major}!");
                return matchingNames.SelectMany(group => group.Packages); //we know there was at least one matching name
            }

            var maxMajor = matchingLessThanMajor.Max(group => group.TargetFramework.Version.Major);

            var matchingMaxMajors = matchingNames
                .Where(group => group.TargetFramework.Version.Major == maxMajor);

            if (matchingMaxMajors.Count() == 0)
            {
                Console.WriteLine($"No matching dependency groups with the actual major were found {framework.Identifier} Major {framework.Major}!");
                return matchingLessThanMajor.SelectMany(group => group.Packages);//we know there was at keast one matching framework name
            }

            var maxMinor = matchingMaxMajors.Max(group => group.TargetFramework.Version.Minor);

            var maxingMaxMinors = matchingMaxMajors.Where(group => group.TargetFramework.Version.Minor == maxMinor);

            if (maxingMaxMinors.Count() == 1) //expect exactly 1 - matching name, with max minor and major.
            {
                return maxingMaxMinors.SelectMany(group => group.Packages);
            }
            else
            {
                Console.WriteLine($"No matching framework with Minor {framework.Identifier} Minor {framework.Minor}!");
                return matchingMaxMajors.SelectMany(group => group.Packages); //we know there was at least 1 matching major
            }

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
