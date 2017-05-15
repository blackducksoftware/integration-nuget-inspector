using System;
using System.Collections.Generic;
using System.IO;


namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class InspectorUtil
    {
        public const string DEFAULT_OUTPUT_DIRECTORY = "blackduck";
        public const string DEFAULT_DATETIME_FORMAT = "yyyy-MM-dd_HH-mm-ss";

        public static string GetProjectAssemblyVersion(string dateFormat, string projectDirectory)
        {
            string version = DateTime.UtcNow.ToString(dateFormat);
            List<string> pathSegments = new List<string>();
            pathSegments.Add(projectDirectory);
            pathSegments.Add("Properties");
            pathSegments.Add("AssemblyInfo.cs");
            string path = CreatePath(pathSegments);
            if (File.Exists(path))
            {
                List<string> contents = new List<string>(File.ReadAllLines(path));
                var versionText = contents.FindAll(text => text.Contains("[assembly: AssemblyFileVersion"));
                foreach (string text in versionText)
                {
                    int firstParen = text.IndexOf("(");
                    int lastParen = text.LastIndexOf(")");
                    // exclude the '(' and the " characters
                    int start = firstParen + 2;
                    // exclude the ')' and the " characters
                    int end = lastParen - 1;
                    version = text.Substring(start, (end - start));
                }
            }
            return version;
        }

        public static string CreatePath(List<string> pathSegments)
        {
            return String.Join(String.Format("{0}", Path.DirectorySeparatorChar), pathSegments);
        }
    }
}
