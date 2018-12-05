using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.Blackducksoftware.Integration.Nuget.Inspector;
using Model = Com.Blackducksoftware.Integration.Nuget.Inspector.Model;

namespace Com.Blackducksoftware.Integration.Nuget
{
    class NugetLockFileResolver
    {
        private NuGet.ProjectModel.LockFile LockFile;

        public NugetLockFileResolver(NuGet.ProjectModel.LockFile lockFile)
        {
            LockFile = lockFile;
        }

        private NuGet.Versioning.NuGetVersion BestVersion(string name, NuGet.Versioning.VersionRange range, IList<NuGet.ProjectModel.LockFileTargetLibrary> libraries)
        {
            var versions = libraries.Where(lib => lib.Name == name).Select(lib => lib.Version);
            var bestMatch = range.FindBestMatch(versions);
            if (bestMatch == null)
            {
                if (versions.Count() == 1)
                {
                    return versions.First();
                }
                else
                {
                    Console.WriteLine($"WARNING: Unable to find a version to satisfy range {range.PrettyPrint()} for the dependency " + name);
                    Console.WriteLine($"Instead will return the minimum range demanded: " + range.MinVersion.ToFullString());
                    return range.MinVersion;
                }
            }
            else
            {
                return bestMatch;
            }
        }


        public DependencyResolvers.DependencyResult Process()
        {
            var builder = new Model.PackageSetBuilder();
            var result = new DependencyResolvers.DependencyResult();

            foreach (var target in LockFile.Targets)
            {
                foreach (var library in target.Libraries)
                {
                    string name = library.Name;
                    string version = library.Version.ToNormalizedString();
                    var packageId = new Model.PackageId(name, version);

                    HashSet<Model.PackageId> dependencies = new HashSet<Model.PackageId>();
                    foreach (var dep in library.Dependencies)
                    {
                        var depId = new Model.PackageId(dep.Id, BestVersion(dep.Id, dep.VersionRange, target.Libraries).ToNormalizedString());
                        dependencies.Add(depId);
                    }

                    builder.AddOrUpdatePackage(packageId, dependencies);
                }
                
            }

            

            if (LockFile.PackageSpec.Dependencies.Count != 0)
            {
                foreach (var dep in LockFile.PackageSpec.Dependencies)
                {
                    var version = builder.GetBestVersion(dep.Name, dep.LibraryRange.VersionRange);
                    result.Dependencies.Add(new Model.PackageId(dep.Name, version));
                }
            }
            else
            {
                foreach (var framework in LockFile.PackageSpec.TargetFrameworks)
                {
                    foreach (var dep in framework.Dependencies)
                    {
                        var version = builder.GetBestVersion(dep.Name, dep.LibraryRange.VersionRange);
                        result.Dependencies.Add(new Model.PackageId(dep.Name, version));
                    }
                }
            }

            if (result.Dependencies.Count == 0)
            {
                Console.WriteLine("Found no dependencies for lock file: " + LockFile.Path);
            }

            result.Packages = builder.GetPackageList();
            return result;
        }

    }
}
