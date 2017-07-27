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

        public ProjectReferenceResolver(string projectPath)
        {
            ProjectPath = projectPath;
        }

        public DependencyResult Process()
        {
            try
            {
                HashSet<DependencyNode> children = new HashSet<DependencyNode>();
                Project proj = new Project(ProjectPath);

                foreach (ProjectItem reference in proj.GetItems("Reference"))
                {
                    if (reference.Xml != null && !String.IsNullOrWhiteSpace(reference.Xml.Include) && reference.Xml.Include.Contains("Version"))
                    {
                        string packageInfo = reference.Xml.Include;

                        DependencyNode childNode = new DependencyNode();
                        childNode.Artifact = packageInfo.Substring(0, packageInfo.IndexOf(","));
                        string version = packageInfo.Substring(packageInfo.IndexOf("Version=") + 8);
                        version = version.Substring(0, version.IndexOf(","));
                        version = version.Substring(0, version.LastIndexOf("."));
                        childNode.Version = version;
                        children.Add(childNode);
                    }
                }
                ProjectCollection.GlobalProjectCollection.UnloadProject(proj);

                return new DependencyResult()
                {
                    Success = true,
                    Nodes = children
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
