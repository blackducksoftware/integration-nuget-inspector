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

        public PackagesConfigResolver(string packagesConfigPath, string packagesRepoUrl, NugetSearchService nugetSearchService)
        {
            PackagesConfigPath = packagesConfigPath;
            PackagesRepoUrl = packagesRepoUrl;
            NugetSearchService = nugetSearchService;
        }

        public DependencyResult Process()
        {

            NuGet.PackageReferenceFile configFile = new NuGet.PackageReferenceFile(PackagesConfigPath);
            List<NuGet.PackageReference> packages = new List<NuGet.PackageReference>(configFile.GetPackageReferences());

            try
            {
                return ProcessFlat(packages);
            }catch (Exception flatException)
            {
                Console.WriteLine("There was an issue processing packages.config as flat: " + flatException.Message);
                try
                {
                    return ProcessTree(packages);
                }catch (Exception treeException)
                {
                    Console.WriteLine("There was an issue processing packages.config as a tree: " + treeException.Message);
                    return ProcessSimply(packages);
                }
            }
            
        }

        //Not-optimal but unlikely to fail, nested versions may be incorrect as it's not processed the same way as nuget (flat).
        DependencyResult ProcessTree(List<NuGet.PackageReference> packages)
        {
            var resolver = new NugetTreeResolver(NugetSearchService);
            var result = new DependencyResult();
            foreach (NuGet.PackageReference packageRef in packages)
            {
                string componentName = packageRef.Id;
                string componentVersion = packageRef.Version.ToString();
                DependencyNode child = new DependencyNode();
                child.Artifact = componentName;
                child.Version = componentVersion;

                var version = new NuGet.Versioning.NuGetVersion(packageRef.Version.Version);
                var toFind = new NugetDependency(packageRef.Id, new NuGet.Versioning.VersionRange(version, true, version, true));
                var framework = new NugetFramework(packageRef.TargetFramework.Version.Major, packageRef.TargetFramework.Version.Minor);

                child.Children = resolver.Build(toFind, framework);

                result.Nodes.Add(child);
            }
            return result;
        }

        //Preferred, processes the packages config properly, as a flat set of packages. Could fail same way nuget does. 
        DependencyResult ProcessFlat(List<NuGet.PackageReference> packages)
        {
            var resolver = new NugetFlatResolver(NugetSearchService);
            var result = new DependencyResult();

            foreach (NuGet.PackageReference packageRef in packages)
            {
                string componentName = packageRef.Id;
                string componentVersion = packageRef.Version.ToString();
                var version = new NuGet.Versioning.NuGetVersion(packageRef.Version.Version);
                var versionRange = new NuGet.Versioning.VersionRange(version, true, version, true);
                var framework = new NugetFramework(packageRef.TargetFramework.Version.Major, packageRef.TargetFramework.Version.Minor);
                resolver.Add(componentName, versionRange, framework);
            }

            foreach (NuGet.PackageReference packageRef in packages)
            {
                string componentName = packageRef.Id;
                result.Nodes.Add(resolver.Build(componentName));
            }

            return result;
        }

        //Can not possibly fail --- a second time. Don't use the nuget service, just return the packages.
        DependencyResult ProcessSimply(List<NuGet.PackageReference> packages)
        {
            var result = new DependencyResult();

            foreach (NuGet.PackageReference packageRef in packages)
            {
                string componentName = packageRef.Id;
                string componentVersion = packageRef.Version.ToString();
                var node = new DependencyNode();
                node.Artifact = componentName;
                node.Version = componentVersion;
                result.Nodes.Add(node);
            }

            return result;
        }

    }
}
