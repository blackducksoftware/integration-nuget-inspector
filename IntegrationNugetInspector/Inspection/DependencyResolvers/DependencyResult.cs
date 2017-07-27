using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.Blackducksoftware.Integration.Nuget.Inspector;

namespace Com.Blackducksoftware.Integration.Nuget.DependencyResolvers
{
    class DependencyResult
    {
        public bool Success { get; set; } = true;
        public string ProjectVersion { get; set; } = null;
        public HashSet<DependencyNode> Nodes { get; set; } = new HashSet<DependencyNode>();

    }
}
