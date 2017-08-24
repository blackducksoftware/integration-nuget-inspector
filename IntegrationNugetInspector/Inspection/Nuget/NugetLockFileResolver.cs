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
            return range.FindBestMatch(versions);
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

            foreach (var framework in LockFile.PackageSpec.TargetFrameworks)
            {
                foreach (var dep in framework.Dependencies)
                {
                    var version = builder.GetBestVersion(dep.LibraryRange.VersionRange);
                    result.Dependencies.Add(new Model.PackageId(dep.Name, version));
                }
            }

            result.Packages = builder.GetPackageList();
            return result;
        }

    }
}
