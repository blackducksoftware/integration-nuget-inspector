using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class SolutionInspector : Inspector
    {

        override public string Execute()
        {
            string solutionInfoFilePath = "";
            try
            {
                Setup();
                DependencyNode solutionNode = GetNode();
                solutionInfoFilePath = WriteInfoFile(solutionNode);
            }
            catch (Exception ex)
            {
                if (IgnoreFailure)
                {
                    Console.WriteLine("Error executing Build BOM task on project {0}, cause: {1}", Name, ex);
                }
                else
                {
                    throw ex;
                }
            }
            return solutionInfoFilePath;
        }

        override public void Setup()
        {           
            if (String.IsNullOrWhiteSpace(OutputDirectory))
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                OutputDirectory = $"{currentDirectory}{Path.DirectorySeparatorChar}{InspectorUtil.DEFAULT_OUTPUT_DIRECTORY}";
            }
            if (String.IsNullOrWhiteSpace(Name))
            {
                Name = Path.GetFileNameWithoutExtension(TargetPath);
            }
        }


        override public DependencyNode GetNode()
        {
            DependencyNode solutionNode = new DependencyNode();
            solutionNode.Artifact = Name;
            try
            {
                Dictionary<string, string> projectData = ParseSolutionFile(TargetPath);
                Console.WriteLine("Parsed Solution File");
                if (projectData.Count > 0)
                {
                    List<DependencyNode> children = new List<DependencyNode>();
                    string solutionDirectory = Path.GetDirectoryName(TargetPath);
                    Console.WriteLine("Solution directory: {0}", solutionDirectory);
                    foreach (string projectName in projectData.Keys)
                    {
                        string projectRelativePath = projectData[projectName];
                        List<string> projectPathSegments = new List<string>();
                        projectPathSegments.Add(solutionDirectory);
                        projectPathSegments.Add(projectRelativePath);

                        string projectPath = InspectorUtil.CreatePath(projectPathSegments);

                        ProjectInspector projectInspector = new ProjectInspector();
                        projectInspector.TargetPath = projectPath;
                        projectInspector.Verbose = Verbose;
                        projectInspector.PackagesRepoUrl = PackagesRepoUrl;
                        projectInspector.Name = projectName;

                        projectInspector.ExcludedModules = ExcludedModules;
                        projectInspector.IgnoreFailure = IgnoreFailure;
                        projectInspector.Setup();
                        DependencyNode projectNode =  projectInspector.GetNode();
                        if (projectNode != null)
                        {
                            children.Add(projectNode);
                        }
                    }
                    solutionNode.Children = children;
                }
                else
                {
                    Console.WriteLine("No project data found for solution {0}", TargetPath);
                }
            }
            catch (Exception ex)
            {
                if (IgnoreFailure)
                {
                    
                    Console.WriteLine("Error executing Build BOM task. Cause: {0}", ex);
                }
                else
                {
                    throw ex;
                }
            }
            
            return solutionNode;
        }

        override public string WriteInfoFile(DependencyNode solutionNode)
        {
            string outputFilePath = "";

            // Creates output directory if it doesn't already exist
            Directory.CreateDirectory(OutputDirectory);

            // Define output files
            // TODO: fix file name
            outputFilePath = $"{OutputDirectory}{Path.DirectorySeparatorChar}{Name}_dependency_node.json";
            File.WriteAllText(outputFilePath, solutionNode.ToString());
            
            return outputFilePath;
        }

        private Dictionary<string, string> ParseSolutionFile(string solutionPath)
        {
            Dictionary<string, string> projectDataMap = new Dictionary<string, string>();
            // Visual Studio right now is not resolving the Microsoft.Build.Construction.SolutionFile type
            // parsing the solution file manually for now.
            if (File.Exists(solutionPath))
            {
                List<string> contents = new List<string>(File.ReadAllLines(solutionPath));
                var projectLines = contents.FindAll(text => text.StartsWith("Project("));
                foreach (string projectText in projectLines)
                {
                    int equalIndex = projectText.IndexOf("=");
                    if (equalIndex > -1)
                    {
                        string projectValuesCSV = projectText.Substring(equalIndex + 1);
                        projectValuesCSV = projectValuesCSV.Replace("\"", "");
                        string[] projectValues = projectValuesCSV.Split(new char[] { ',' });

                        if (projectValues.Length >= 2)
                        {
                            projectDataMap[projectValues[0].Trim()] = projectValues[1].Trim();
                        }
                    }
                }
                Console.WriteLine("Black Duck I/O Generation Found {0} Project elements, processed {1} project elements for data", projectLines.Count(), projectDataMap.Count());
            }
            else
            {
                throw new BlackDuckInspectorException("Solution File " + solutionPath + " not found");
            }

            return projectDataMap;
        }
        
    }
}
