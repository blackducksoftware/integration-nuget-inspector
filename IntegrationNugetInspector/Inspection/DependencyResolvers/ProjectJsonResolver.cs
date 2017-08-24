using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.Blackducksoftware.Integration.Nuget.Inspector;
using Model = Com.Blackducksoftware.Integration.Nuget.Inspector.Model;

namespace Com.Blackducksoftware.Integration.Nuget.DependencyResolvers
{
    class ProjectJsonResolver : DependencyResolver
    {
        private string ProjectName;
        private string ProjectJsonPath;

        public ProjectJsonResolver (string projectName, string projectJsonPath){
            ProjectName = projectName;
            ProjectJsonPath = projectJsonPath;
        }

        public DependencyResult Process()
        {
            var result = new DependencyResult();

            NuGet.ProjectModel.PackageSpec model = NuGet.ProjectModel.JsonPackageSpecReader.GetPackageSpec(ProjectName, ProjectJsonPath);
            IList<NuGet.LibraryModel.LibraryDependency> packages = model.Dependencies;

            foreach (NuGet.LibraryModel.LibraryDependency package in packages)
            {
                var set = new Model.PackageSet();
                set.PackageId = new Model.PackageId(package.Name, package.LibraryRange.VersionRange.OriginalString);
                result.Packages.Add(set);
                result.Dependencies.Add(set.PackageId);
            }
            return result;
        }
    }
}
