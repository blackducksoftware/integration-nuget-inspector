using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class InspectionResultWriter
    {
        private InspectionResult Result;

        public InspectionResultWriter(InspectionResult result)
        {
            Result = result;
        }

        public string FilePath()
        {
            // TODO: fix file name
            return $"{Result.OutputDirectory}{Path.DirectorySeparatorChar}{Result.ResultName}_dependency_node.json";
        }

        public void Write()
        {
            Write(Result.OutputDirectory);
        }

        public void Write(string outputDirectory)
        {
            Write(outputDirectory, FilePath());
        }

        public void Write(string outputDirectory, string outputFilePath)
        {
            
            Directory.CreateDirectory(outputDirectory);

            // TODO: don't rely on toString to correctly serialize - make this JsonWriter and manually (or auto map if inclined) to JSON. Or implement with formatters of somekind.
            File.WriteAllText(outputFilePath, Result.Node.ToString());

        }
    }
}
