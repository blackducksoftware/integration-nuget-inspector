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
    //Simply builds a tree of dependency nodes from a package using best guess of the correct versions.
    public class NugetTreeResolver
    {
        
        private NugetSearchService nuget;
        
        public NugetTreeResolver(NugetSearchService service)
        {
            nuget = service;
        }

        public HashSet<DependencyNode> Build (NugetDependency packageDependency, NugetFramework framework = null)
        {
            HashSet<DependencyNode> dependencies = new HashSet<DependencyNode>();

            var package = nuget.FindBestPackage(packageDependency.Name, packageDependency.VersionRange);
            if (package == null) {
                Console.WriteLine($"Unable to find package for '{packageDependency.Name}' version '{packageDependency.VersionRange}'");
                return dependencies;
            }
                     
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

                        DependencyNode dependencyNode = new DependencyNode();
                        dependencyNode.Artifact = depPackage.Identity.Id;
                        dependencyNode.Version = depPackage.Identity.Version.ToNormalizedString();
                        dependencyNode.Children = Build(new NugetDependency(dependency.Id, dependency.VersionRange), framework);
                        dependencies.Add(dependencyNode);
                    }

                }
            }

            return dependencies;

        }
    }

}
