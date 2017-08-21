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
        private NugetSearchService NugetSearchService;

        public PackagesConfigResolver(string packagesConfigPath, NugetSearchService nugetSearchService)
        {
            PackagesConfigPath = packagesConfigPath;
            NugetSearchService = nugetSearchService;
        }

        public DependencyResult Process()
        {

            List<NugetDependency> dependencies = GetDependencies();
            HashSet<DependencyNode> nodes = CreateDependencyNodes(dependencies);

            var result = new DependencyResult();
            result.Nodes = nodes;
            return result;
        }

        private HashSet<DependencyNode> CreateDependencyNodes(List<NugetDependency> dependencies)
        {
            try
            {
                var flatResolver = new NugetFlatResolver(NugetSearchService);
                var nodes = flatResolver.ProcessAll(dependencies);
                return nodes;
            }
            catch (Exception flatException)
            {
                Console.WriteLine("There was an issue processing packages.config as flat: " + flatException.Message);
                try
                {
                    var treeResolver = new NugetTreeResolver(NugetSearchService);
                    var nodes = treeResolver.ProcessAll(dependencies);
                    return nodes;
                }
                catch (Exception treeException)
                {
                    Console.WriteLine("There was an issue processing packages.config as a tree: " + treeException.Message);
                    var nodes = new HashSet<DependencyNode>(dependencies.Select(dependency => dependency.ToDependencyNode()));
                    return nodes;
                }
            }
        }

        private List<NugetDependency> GetDependencies()
        {
            NuGet.PackageReferenceFile configFile = new NuGet.PackageReferenceFile(PackagesConfigPath);
            List<NuGet.PackageReference> packages = new List<NuGet.PackageReference>(configFile.GetPackageReferences());

            var dependencies = new List<NugetDependency>();

            foreach (var packageRef in packages)
            {
                string componentName = packageRef.Id;
                var version = new NuGet.Versioning.NuGetVersion(packageRef.Version.Version);
                var versionRange = new NuGet.Versioning.VersionRange(version, true, version, true);
                var framework = new NugetFramework(packageRef.TargetFramework.Version.Major, packageRef.TargetFramework.Version.Minor);

                var dep = new NugetDependency(componentName, versionRange, framework);
                dependencies.Add(dep);
            }

            return dependencies;
        }
        

    }
}
