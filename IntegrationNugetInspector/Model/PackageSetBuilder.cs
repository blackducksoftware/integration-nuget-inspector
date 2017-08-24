using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector.Model
{
    public class PackageSetBuilder
    {
        private Dictionary<PackageId, PackageSet> packageSets = new Dictionary<PackageId, PackageSet>();


        public PackageSet GetOrCreatePackageSet(PackageId package)
        {
            PackageSet set;
            if (packageSets.TryGetValue(package, out set))
            {
                return set;
            }
            else
            {
                set = new PackageSet();
                set.PackageId = package;
                set.Dependencies = new HashSet<PackageId>();
                packageSets[package] = set;
                return set;
            }
        }

        public void AddOrUpdatePackage(PackageId id)
        {
            var set = GetOrCreatePackageSet(id);
        }

        public void AddOrUpdatePackage(PackageId id, PackageId dependency)
        {
            var set = GetOrCreatePackageSet(id);
            set.Dependencies.Add(dependency);
        }

        public void AddOrUpdatePackage(PackageId id, HashSet<PackageId> dependencies)
        {
            var set = GetOrCreatePackageSet(id);
            set.Dependencies.UnionWith(dependencies);
        }

        public List<PackageSet> GetPackageList()
        {
            return packageSets.Values.ToList();
        }

        private class VersionPair
        {
            public string rawVersion;
            public NuGet.Versioning.NuGetVersion version;
        }
        public string GetBestVersion(NuGet.Versioning.VersionRange range)
        {
            var versions = packageSets.Select(pkg =>
            {
                NuGet.Versioning.NuGetVersion version = null;
                NuGet.Versioning.NuGetVersion.TryParse(pkg.Key.Version, out version);
                return new VersionPair() { rawVersion = pkg.Key.Version, version = version };

            });

            var best = range.FindBestMatch(versions.Select(ver => ver.version));

            return versions.Where(ver => ver.version == best).Select(ver => ver.rawVersion).FirstOrDefault();
        }

    }
}
