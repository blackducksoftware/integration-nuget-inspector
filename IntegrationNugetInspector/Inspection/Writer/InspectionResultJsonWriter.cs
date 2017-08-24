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
        private Model.Inspection Inspection;

        public InspectionResultJsonWriter(InspectionResult result)
        {
            Result = result;
            Inspection = new Model.Inspection();
            Inspection.Containers = result.Containers;
        }

        public string FilePath()
        {
            // TODO: fix file name
            return $"{Result.OutputDirectory}{Path.DirectorySeparatorChar}{Result.ResultName}_inspection.json";
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
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(writer, Inspection);
                }
            }
        }

    }
}
