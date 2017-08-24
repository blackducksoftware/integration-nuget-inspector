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
using Model = Com.Blackducksoftware.Integration.Nuget.Inspector.Model;

namespace Com.Blackducksoftware.Integration.Nuget
{
    //Simply builds a tree of dependency nodes from a package using best guess of the correct versions.
    public class NugetTreeResolver
    {
        
        private NugetSearchService nuget;
        private Model.PackageSetBuilder builder = new Model.PackageSetBuilder();

        public NugetTreeResolver(NugetSearchService service)
        {
            nuget = service;
        }

        public List<Model.PackageSet> GetPackageList()
        {
            return builder.GetPackageList();
        }

        public void AddAll(List<NugetDependency> packages)
        {
            foreach (NugetDependency package in packages)
            {
                Add(package);
            }
        }
        
        public void Add (NugetDependency packageDependency, NugetFramework framework = null)
        {

            var package = nuget.FindBestPackage(packageDependency.Name, packageDependency.VersionRange);
            if (package == null) {
                Console.WriteLine($"Unable to find package for '{packageDependency.Name}' version '{packageDependency.VersionRange}'");
                var version = packageDependency.VersionRange.MinVersion.ToNormalizedString();
                builder.AddOrUpdatePackage(new Model.PackageId(packageDependency.Name, version));
                return;
            }

            var packageId = new Model.PackageId(packageDependency.Name, package.Identity.Version.ToNormalizedString());
            HashSet<Model.PackageId> dependencies = new HashSet<Model.PackageId>();

            foreach (PackageDependencyGroup group in package.DependencySets)
            {
                if (framework == null || nuget.FrameworksMatch(group, framework))
                {
                    foreach (PackageDependency dependency in group.Packages)
                    {
                        var depPackage = nuget.FindBestPackage(dependency.Id, dependency.VersionRange);
                        if (depPackage == null)
                        {
                            Console.WriteLine($"Unable to find package for '{dependency.Id}' version '{dependency.VersionRange}'");
                            continue;
                        }

                        dependencies.Add(new Model.PackageId(depPackage.Identity.Id, depPackage.Identity.Version.ToNormalizedString()));

                        Add(new NugetDependency(dependency.Id, dependency.VersionRange), framework);
                    }
                }
            }

            builder.AddOrUpdatePackage(packageId, dependencies);

        }
    }

}
