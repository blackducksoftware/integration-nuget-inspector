using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.Blackducksoftware.Integration.Nuget.Inspector;

namespace Com.Blackducksoftware.Integration.Nuget.DependencyResolvers
{
    class ProjectLockJsonResolver : DependencyResolver
    {
        private string ProjectLockJsonPath;

        public ProjectLockJsonResolver(string projectLockJsonPath)
        {
            ProjectLockJsonPath = projectLockJsonPath;
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

        public DependencyResult Process()
        {
            var result = new DependencyResult();

            Dictionary<LibraryId, DependencyNode> libraryMap = new Dictionary<LibraryId, DependencyNode>();
            Dictionary<DependencyNode, List<NuGet.Packaging.Core.PackageDependency>> dependencyMap = new Dictionary<DependencyNode, List<NuGet.Packaging.Core.PackageDependency>>();

            NuGet.ProjectModel.LockFile lockFile = NuGet.ProjectModel.LockFileUtilities.GetLockFile(ProjectLockJsonPath, null);
            
            foreach (var target in lockFile.Targets)
            {
                foreach (var library in target.Libraries)
                {
                    string name = library.Name;
                    string version = library.Version.ToNormalizedString();
                    var id = new LibraryId(name, version);

                    if (!libraryMap.ContainsKey(id)){
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
            
            result.Nodes = new HashSet<DependencyNode>(parentless);

            //Alt strategy... gives flat list...
            //foreach (var library in lockFile.Libraries)
            //{
            //    DependencyNode child = new DependencyNode();
            //    child.Artifact = library.Name;
            //    child.Version = library.Version.ToNormalizedString();
            //    result.Nodes.Add(child);
            //}

            return result;
        }
        
    }
}
