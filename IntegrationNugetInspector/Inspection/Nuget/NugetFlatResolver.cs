﻿using System;
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
    //Given a list of dependencies, resolve them such that all packages are shared in a flat list
    //Essentially means that no nodes in the tree that refer to the same package may have different versions.
    //As closely follows the packages.config strategy as it is outlined here: 
    //https://docs.microsoft.com/en-us/nuget/consume-packages/dependency-resolution#dependency-resolution-with-packagesconfig
    public class NugetFlatResolver
    {

        private class ResolutionData
        {
            public string Id;
            public NuGetVersion CurrentVersion;
            public VersionRange ExternalVersionRange = null;
            public Dictionary<string, VersionRange> Dependencies = new Dictionary<string, VersionRange>();
        }

        private NugetSearchService nuget;
        private Dictionary<string, ResolutionData> resolutionData = new Dictionary<string, ResolutionData>();

        public NugetFlatResolver(NugetSearchService service)
        {
            nuget = service;
        }

        private List<VersionRange> FindAllVersionRangesFor(string name)
        {
            name = name.ToLower();
            List<VersionRange> result = new List<VersionRange>();
            foreach (var pkg in resolutionData.Values)
            {
                foreach (var depPair in pkg.Dependencies)
                {
                    if (depPair.Key == name)
                    {
                        result.Add(depPair.Value);
                    }
                }
            }
            return result;
        }

        public List<Model.PackageSet> ProcessAll(List<NugetDependency> packages)
        {
            foreach (NugetDependency package in packages)
            {
                Add(package.Name, package.VersionRange, package.Framework);
            }

            var builder = new Model.PackageSetBuilder();
            foreach (ResolutionData data in resolutionData.Values)
            {
                var deps = new HashSet<Model.PackageId>();
                foreach(var dep in data.Dependencies.Keys)
                {
                    deps.Add(new Model.PackageId(dep, resolutionData[dep].CurrentVersion.ToNormalizedString()));
                }
                builder.AddOrUpdatePackage(new Model.PackageId(data.Id, data.CurrentVersion.ToNormalizedString()), deps);
            }
            
            return builder.GetPackageList();
        }

        public void Add(string name, VersionRange range, NugetFramework framework)
        {
            name = name.ToLower();
            Resolve(name, framework, range);
        }

        private void Resolve(string name, NugetFramework framework = null, VersionRange overrideRange = null)
        {
            name = name.ToLower();
            ResolutionData data;
            if (resolutionData.ContainsKey(name))
            {
                data = resolutionData[name];
                if (overrideRange != null)
                {
                    if (data.ExternalVersionRange == null)
                    {
                        data.ExternalVersionRange = overrideRange;
                    }
                    else
                    {
                        throw new Exception("Can't set more than one external version range.");
                    }
                }
            }
            else
            {
                data = new ResolutionData();
                data.ExternalVersionRange = overrideRange;
                resolutionData[name] = data;
            }

            var allVersions = FindAllVersionRangesFor(name);
            if (data.ExternalVersionRange != null)
            {
                allVersions.Add(data.ExternalVersionRange);
            }
            var combo = VersionRange.CommonSubSet(allVersions);
            var best = nuget.FindBestPackage(name, combo);

            if (best == null)
            {
                throw new Exception($"Unable to find package for '{name}' with range '{combo.ToString()}'. Likely a conflict exists in packages.config or the nuget metadata service configured incorrectly.");
            }

            data.Id = best.Identity.Id;
            data.CurrentVersion = best.Identity.Version;
            data.Dependencies.Clear();

            foreach (PackageDependencyGroup group in best.DependencySets)
            {
                if (framework == null || nuget.FrameworksMatch(group, framework))
                {
                    foreach (PackageDependency dependency in group.Packages)
                    {
                        data.Dependencies.Add(dependency.Id.ToLower(), dependency.VersionRange);
                        Resolve(dependency.Id, framework);
                    }
                }
            }

        }

    }

}
