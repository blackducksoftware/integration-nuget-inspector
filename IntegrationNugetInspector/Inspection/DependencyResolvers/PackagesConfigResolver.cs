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
using Model = Com.Blackducksoftware.Integration.Nuget.Inspector.Model;

namespace Com.Blackducksoftware.Integration.Nuget.DependencyResolvers
{
    class PackagesConfigResolver : DependencyResolver
    {

        private string PackagesConfigPath;
        private NugetSearchService NugetSearchService;

        public PackagesConfigResolver(string packagesConfigPath, NugetSearchService nugetSearchService)
        {
            PackagesConfigPath = packagesConfigPath;
            NugetSearchService = nugetSearchService;
        }

        public DependencyResult Process()
        {

            List<NugetDependency> dependencies = GetDependencies();

            var result = new DependencyResult();
            result.Packages = CreatePackageSets(dependencies);

            result.Dependencies = new List<Model.PackageId>();
            foreach (var package in result.Packages)
            {
                var anyPackageReferences = result.Packages.Where(pkg => pkg.Dependencies.Contains(package.PackageId)).Any();
                if (!anyPackageReferences)
                {
                    result.Dependencies.Add(package.PackageId);
                }
            }

            return result;
        }

        


        private List<NugetDependency> GetDependencies()
        {
            NuGet.PackageReferenceFile configFile = new NuGet.PackageReferenceFile(PackagesConfigPath);
            List<NuGet.PackageReference> packages = new List<NuGet.PackageReference>(configFile.GetPackageReferences());

            var dependencies = new List<NugetDependency>();

            foreach (var packageRef in packages)
            {
                string componentName = packageRef.Id;
                var version = new NuGet.Versioning.NuGetVersion(packageRef.Version.Version, packageRef.Version.SpecialVersion, packageRef.Version.Metadata);
                var versionRange = new NuGet.Versioning.VersionRange(version, true, version, true);
                var framework = NuGet.Frameworks.NuGetFramework.Parse(packageRef.TargetFramework.FullName);
                
                var dep = new NugetDependency(componentName, versionRange, framework);
                dependencies.Add(dep);
            }

            return dependencies;
        }

        private List<Model.PackageSet> CreatePackageSets(List<NugetDependency> dependencies)
        {
            try
            {
                var flatResolver = new NugetFlatResolver(NugetSearchService);
                var packages = flatResolver.ProcessAll(dependencies);
                return packages;
            }
            catch (Exception flatException)
            {
                Console.WriteLine("There was an issue processing packages.config as flat: " + flatException.Message);
                try
                {
                    var treeResolver = new NugetTreeResolver(NugetSearchService);
                    treeResolver.AddAll(dependencies);
                    return treeResolver.GetPackageList();
                }
                catch (Exception treeException)
                {
                    Console.WriteLine("There was an issue processing packages.config as a tree: " + treeException.Message);
                    var packages = new List<Model.PackageSet>(dependencies.Select(dependency => dependency.ToEmptyPackageSet()));
                    return packages;
                }
            }
        }

        

    }
}
