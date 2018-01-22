using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Com.Blackducksoftware.Integration.Nuget.Inspector;

namespace Com.Blackducksoftware.Integration.Nuget.DependencyResolvers
{
    class ProjectReferenceResolver : DependencyResolver
    {

        private string ProjectPath;
        private NugetSearchService NugetSearchService;
        public ProjectReferenceResolver(string projectPath, NugetSearchService nugetSearchService)
        {
            ProjectPath = projectPath;
            NugetSearchService = nugetSearchService;
        }

        public DependencyResult Process()
        {
            try
            {
                var tree = new NugetTreeResolver(NugetSearchService);

                Project proj = new Project(ProjectPath);

                List<NugetDependency> deps = new List<NugetDependency>();
                foreach (ProjectItem reference in proj.GetItems("Reference"))
                {
                    if (reference.Xml != null && !String.IsNullOrWhiteSpace(reference.Xml.Include) && reference.Xml.Include.Contains("Version"))
                    {

                        string packageInfo = reference.Xml.Include;

                        var artifact = packageInfo.Substring(0, packageInfo.IndexOf(","));

                        string versionKey = "Version=";
                        int versionKeyIndex = packageInfo.IndexOf(versionKey);
                        int versionStartIndex = versionKeyIndex + versionKey.Length;
                        string packageInfoAfterVersionKey = packageInfo.Substring(versionStartIndex);

                        string seapirater = ",";
                        string version; 
                        if (packageInfoAfterVersionKey.Contains(seapirater))
                        {
                            int firstSeapirater = packageInfoAfterVersionKey.IndexOf(seapirater);
                            version = packageInfoAfterVersionKey.Substring(0, firstSeapirater);
                        }
                        else
                        {
                            version = packageInfoAfterVersionKey;
                        }

                        var dep = new NugetDependency(artifact, NuGet.Versioning.VersionRange.Parse(version));
                        deps.Add(dep);
                    }
                }
                ProjectCollection.GlobalProjectCollection.UnloadProject(proj);

                foreach (var dep in deps)
                {
                    tree.Add(dep);
                }

                return new DependencyResult()
                {
                    Success = true,
                    Packages = tree.GetPackageList()
                };
            }
            catch (InvalidProjectFileException e)
            {
                return new DependencyResult()
                {
                    Success = false
                };
            }
        }
    }
}
