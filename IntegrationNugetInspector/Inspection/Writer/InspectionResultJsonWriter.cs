using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class InspectionResultJsonWriter
    {
        private InspectionResult Result;

        public InspectionResultJsonWriter(InspectionResult result)
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

            using (var fs = new FileStream(outputFilePath, FileMode.Create))
            {
                using (var sw = new StreamWriter(fs))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Ignore;
                    JsonTextWriter writer = new JsonTextWriter(sw);
                    serializer.Serialize(writer, Result.Node);
                }
            }
        }

    }
}
