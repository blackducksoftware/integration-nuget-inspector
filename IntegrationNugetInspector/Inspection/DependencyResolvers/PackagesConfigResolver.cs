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
        private Search.NugetSearchService NugetSearchService;

        public PackagesConfigResolver(string packagesConfigPath, string packagesRepoUrl, Search.NugetSearchService nugetSearchService)
        {
            PackagesConfigPath = packagesConfigPath;
            PackagesRepoUrl = packagesRepoUrl;
            NugetSearchService = nugetSearchService;
        }

        public DependencyResult Process()
        {
            var result = new DependencyResult();

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

                var version = new NuGet.Versioning.NuGetVersion(packageRef.Version.Version);
                var toFind = new Search.NugetDependency(packageRef.Id, new NuGet.Versioning.VersionRange(version, true, version, true));
                var framework = new Search.NugetFramework(packageRef.TargetFramework.Version.Major, packageRef.TargetFramework.Version.Minor);

                child.Children = NugetSearchService.GetPackageDependencies(toFind, framework);
                
                result.Nodes.Add(child);
            }
            return result;
        }
        
    }
}
