using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.Blackducksoftware.Integration.Nuget.Inspector;
using Model =  Com.Blackducksoftware.Integration.Nuget.Inspector.Model;

namespace Com.Blackducksoftware.Integration.Nuget.DependencyResolvers
{
    class DependencyResult
    {
        public bool Success { get; set; } = true;
        public string ProjectVersion { get; set; } = null;
        public List<Model.PackageSet> Packages { get; set; } = new List<Model.PackageSet>();
        public List<Model.PackageId> Dependencies { get; set; } = new List<Model.PackageId>();
    }
}
