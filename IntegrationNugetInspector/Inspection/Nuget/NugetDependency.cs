using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Com.Blackducksoftware.Integration.Nuget.Inspector;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v2;
using NuGet.Versioning;
using Model = Com.Blackducksoftware.Integration.Nuget.Inspector.Model;

namespace Com.Blackducksoftware.Integration.Nuget
{
    public class NugetDependency
    {
        public string Name;
        public VersionRange VersionRange;
        public NuGetFramework Framework = null;

        public NugetDependency(string name, VersionRange versionRange, NuGetFramework framework = null)
        {
            Name = name;
            VersionRange = versionRange;
            Framework = framework;
        }

        public NugetDependency(PackageDependency dependency)
        {
            Name = dependency.Id;
            VersionRange = dependency.VersionRange;
        }

        public Model.PackageSet ToEmptyPackageSet()
        {
            var packageSet = new Model.PackageSet();
            packageSet.PackageId = new Model.PackageId(Name, VersionRange.MinVersion.ToNormalizedString());
            return packageSet;
        }
    }
}
