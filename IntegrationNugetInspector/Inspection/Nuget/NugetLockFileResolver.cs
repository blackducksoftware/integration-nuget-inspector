using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.Blackducksoftware.Integration.Nuget.Inspector;

namespace Com.Blackducksoftware.Integration.Nuget
{
    class NugetLockFileResolver
    {
        private NuGet.ProjectModel.LockFile LockFile;

        public NugetLockFileResolver(NuGet.ProjectModel.LockFile lockFile)
        {
            LockFile = lockFile;
        }

        public class LibraryId : IEquatable<LibraryId>
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public NuGet.Versioning.NuGetVersion NuGetVersion { get; set; }

            public LibraryId(string name, string version)
            {
                Name = name;
                Version = version;
                NuGetVersion = new NuGet.Versioning.NuGetVersion(version);
            }
            public override int GetHashCode()
            {
                return Name.GetHashCode() ^ Version.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                return Equals(obj as LibraryId);
            }
            public bool Equals(LibraryId obj)
            {
                return obj != null && obj.Name == this.Name && obj.Version == this.Version;
            }
        }

        public HashSet<DependencyNode> Process()
        {
            Dictionary<LibraryId, DependencyNode> libraryMap = new Dictionary<LibraryId, DependencyNode>();
            Dictionary<DependencyNode, List<NuGet.Packaging.Core.PackageDependency>> dependencyMap = new Dictionary<DependencyNode, List<NuGet.Packaging.Core.PackageDependency>>();
            
            foreach (var target in LockFile.Targets)
            {
                foreach (var library in target.Libraries)
                {
                    string name = library.Name;
                    string version = library.Version.ToNormalizedString();
                    var id = new LibraryId(name, version);

                    if (!libraryMap.ContainsKey(id))
                    {
                        var node = new DependencyNode();
                        node.Artifact = name;
                        node.Version = version;
                        libraryMap[id] = node;
                        dependencyMap[node] = library.Dependencies.ToList();
                    }
                    else
                    {
                        //TODO: Verify the already added one has the same dependencies. 
                    }
                }
            }


            List<DependencyNode> parentless = libraryMap.Values.ToList();

            foreach (var pair in dependencyMap)
            {
                var node = pair.Key;
                var deps = pair.Value;

                foreach (var dep in deps)
                {
                    DependencyNode found = null;
                    foreach (var libPair in libraryMap)
                    {
                        var id = libPair.Key;
                        if (id.Name == dep.Id && dep.VersionRange.Satisfies(id.NuGetVersion))
                        {
                            if (found == null)
                            {
                                found = libPair.Value;
                            }
                            else
                            {
                                Console.WriteLine($"Duplicate libraries matching the {dep.Id} with range {dep.VersionRange.OriginalString} were found.");
                            }
                        }
                    }
                    if (found != null)
                    {
                        if (node.Children == null) node.Children = new HashSet<DependencyNode>();
                        node.Children.Add(found);
                        parentless.Remove(found);
                    }
                    else
                    {
                        Console.WriteLine($"No library found for {dep.Id} with range {dep.VersionRange.OriginalString}.");
                    }
                }
            }

            var result = new HashSet<DependencyNode>(parentless);

            return result;
        }

    }
}
