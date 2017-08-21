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
    public class NugetDependency
    {
        public string Name;
        public VersionRange VersionRange;
        public NugetFramework Framework = null;

        public NugetDependency(string name, VersionRange versionRange, NugetFramework framework = null)
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

        public DependencyNode ToDependencyNode()
        {
            var node = new DependencyNode();
            node.Artifact = Name;
            node.Version = VersionRange.MinVersion.ToNormalizedString();
            return node;
        }
    }
}
