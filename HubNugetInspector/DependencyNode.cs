using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector.HubNugetInspector
{
    class DependencyNode
    {
        public string Namespace { get; set; }
        public string GroupId { get; set; }
        public string ArtifactId { get; set; }
        public string Version { get; set; }
        public List<DependencyNode> children { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            StringWriter stringWriter = new StringWriter(stringBuilder);

            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (JsonWriter writer = new JsonTextWriter(stringWriter))
            {
                writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                writer.WriteStartArray();
                foreach (DependencyNode child in children)
                {
                    serializer.Serialize(writer, child);
                }
                writer.WriteEndArray();
            }
            return stringBuilder.ToString();
        }

       
    
}
}
