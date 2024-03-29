﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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

        private NuGet.Versioning.NuGetVersion BestLibraryVersion(string name, NuGet.Versioning.VersionRange range, IList<NuGet.ProjectModel.LockFileLibrary> libraries)
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
                    if (range.HasUpperBound && !range.HasLowerBound)
                    {
                        Console.WriteLine($"Instead will return the maximum range demanded: " + range.MaxVersion.ToFullString());
                        return range.MaxVersion;
                    }
                    else
                    {
                        Console.WriteLine($"Instead will return the minimum range demanded: " + range.MinVersion.ToFullString());
                        return range.MinVersion;
                    }

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
                        var id = dep.Id;
                        var vr = dep.VersionRange;
                        //vr.Float.FloatBehavior = NuGet.Versioning.NuGetVersionFloatBehavior.
                        var lb = target.Libraries;
                        var bs = BestVersion(id, vr, lb);
                        if (bs == null)
                        {
                            Console.WriteLine(dep.Id);
                            bs = BestVersion(id, vr, lb);
                        }
                        else
                        {
                            var depId = new Model.PackageId(id, bs.ToNormalizedString());
                            dependencies.Add(depId);
                        }

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

            foreach (var projectFileDependencyGroup in LockFile.ProjectFileDependencyGroups)
            {
                foreach (var projectFileDependency in projectFileDependencyGroup.Dependencies)
                {
                    var projectDependencyParsed = ParseProjectFileDependencyGroup(projectFileDependency);
                    var libraryVersion = BestLibraryVersion(projectDependencyParsed.GetName(), projectDependencyParsed.GetVersionRange(), LockFile.Libraries);
                    String version = null;
                    if (libraryVersion != null)
                    {
                        version = libraryVersion.ToNormalizedString();
                    }
                    result.Dependencies.Add(new Model.PackageId(projectDependencyParsed.GetName(), version));
                }
            }


            if (result.Dependencies.Count == 0)
            {
                Console.WriteLine("Found no dependencies for lock file: " + LockFile.Path);
            }

            result.Packages = builder.GetPackageList();
            return result;
        }

        public ProjectFileDependency ParseProjectFileDependencyGroup(String projectFileDependency)
        {
            //Reverse engineered from: https://github.com/NuGet/NuGet.Client/blob/538727480d93b7d8474329f90ccb9ff3b3543714/src/NuGet.Core/NuGet.LibraryModel/LibraryRange.cs#L68
            //With some hints from https://github.com/dotnet/NuGet.BuildTasks/pull/23/files
            ProjectNameVersion nameVersion = ParseProjectFileDependencyGroupTokens(projectFileDependency, " >= ");
            if (nameVersion != null)
            {
                return new ProjectFileDependency(nameVersion.name, MinVersionOrFloat(nameVersion.version, true /* Include min version. */));
            }

            nameVersion = ParseProjectFileDependencyGroupTokens(projectFileDependency, " > ");
            if (nameVersion != null)
            {
                return new ProjectFileDependency(nameVersion.name, MinVersionOrFloat(nameVersion.version, false /* Do not include min version. */));
            }
            nameVersion = ParseProjectFileDependencyGroupTokens(projectFileDependency, " <= ");
            if (nameVersion != null)
            {
                var maxVersion = NuGet.Versioning.NuGetVersion.Parse(nameVersion.version);
                return new ProjectFileDependency(nameVersion.name, new NuGet.Versioning.VersionRange(null, false, maxVersion, true /* Include Max */));
            }
            nameVersion = ParseProjectFileDependencyGroupTokens(projectFileDependency, " < ");
            if (nameVersion != null)
            {
                var maxVersion = NuGet.Versioning.NuGetVersion.Parse(nameVersion.version);
                return new ProjectFileDependency(nameVersion.name, new NuGet.Versioning.VersionRange(null, false, maxVersion, false /* Do NOT Include Max */));
            }
            throw new Exception("Unable to parse project file dependency group, please contact support: " + projectFileDependency);
        }

        private class ProjectNameVersion
        {
            public String name;
            public String version;
        }

        private ProjectNameVersion ParseProjectFileDependencyGroupTokens(string input, string tokens)
        {
            if (input.Contains(tokens))
            {
                String[] pieces =  input.Split(new[] { tokens }, StringSplitOptions.None);
                return new ProjectNameVersion()
                {
                    name = pieces[0].Trim(),
                    version = pieces[1].Trim()
                };
            }
            else
            {
                return null;
            }
        }

        private NuGet.Versioning.VersionRange MinVersionOrFloat(String versionValueRaw, bool includeMin)
        {
            //could be Floating or MinVersion
            NuGet.Versioning.NuGetVersion minVersion;
            if (NuGet.Versioning.NuGetVersion.TryParse(versionValueRaw, out minVersion))
            {
                return new NuGet.Versioning.VersionRange(minVersion, includeMin);
            }
            else
            {
                return NuGet.Versioning.VersionRange.Parse(versionValueRaw, true);
            }
        }

        public class ProjectFileDependency
        {
            private readonly String name;
            private readonly NuGet.Versioning.VersionRange versionRange;

            public ProjectFileDependency(string name, NuGet.Versioning.VersionRange versionRange)
            {
                this.name = name;
                this.versionRange = versionRange;
            }

            public String GetName()
            {
                return name;
            }

            public NuGet.Versioning.VersionRange GetVersionRange()
            {
                return versionRange;
            }
        }

    }

}