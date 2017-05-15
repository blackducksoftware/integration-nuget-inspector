using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    interface Inspector
    {

        string Execute();
        void Setup();
        DependencyNode GetNode();
        string WriteInfoFile(DependencyNode node);

    }
}
