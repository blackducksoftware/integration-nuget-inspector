using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    abstract class Inspector
    {
        public string TargetPath { get; set; }
        public bool Verbose { get; set; } = false;
        public string PackagesRepoUrl { get; set; }
        public string OutputDirectory { get; set; }
        public string ExcludedModules { get; set; } = "";
        public bool IgnoreFailure { get; set; } = false;
        public string Name { get; set; }


        abstract public string Execute();
        abstract public void Setup();
        abstract public DependencyNode GetNode();
        abstract public string WriteInfoFile(DependencyNode node);

    }
}
