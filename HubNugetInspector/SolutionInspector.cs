using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Com.Blackducksoftware.Integration.Nuget.Inspector.HubNugetInspector
{
    class SolutionInspector
    {
        public string SolutionPath { get; set; }
        public bool Verbose { get; set; } = false;
        public string PackagesRepoUrl { get; set; }
        public string ProjectName { get; set; }
        public string VersionName { get; set; }
        public string OutputDirectory { get; set; }
        public string ExcludedModules { get; set; } = "";
        public bool IgnoreFailure { get; set; } = false;


        public void Setup()
        {
            string solutionDirectory = Directory.GetParent(SolutionPath).FullName;
            
            if (String.IsNullOrWhiteSpace(OutputDirectory))
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                OutputDirectory = $"{currentDirectory}{Path.DirectorySeparatorChar}{InspectorUtil.DEFAULT_OUTPUT_DIRECTORY}";
            }
            if (String.IsNullOrWhiteSpace(ProjectName))
            {
                ProjectName = Path.GetFileNameWithoutExtension(SolutionPath);
            }
            if (String.IsNullOrWhiteSpace(VersionName))
            {
                VersionName = InspectorUtil.GetProjectAssemblyVersion(InspectorUtil.DEFAULT_DATETIME_FORMAT, solutionDirectory);
            }
        }


        public bool Execute()
        {
            bool result = true;

          //  List<string> alreadyMergedComponents = new List<string>();
         //   List<BdioNode> mergedComponentList = new List<BdioNode>();

            try
            {
                // TODO: clean up this code to generate the BDIO first then perform the deploy and checks for each project


                Dictionary<string, string> projectData = ParseSolutionFile(SolutionPath);
                Console.WriteLine("Parsed Solution File");
                if (projectData.Count > 0)
                {
                    DependencyNode solutionNode = new DependencyNode();


                    List<DependencyNode> children = new List<DependencyNode>();
                    string solutionDirectory = Path.GetDirectoryName(SolutionPath);
                    Console.WriteLine("Solution directory: {0}", solutionDirectory);
                    foreach (string key in projectData.Keys)
                    {
                        if (String.IsNullOrWhiteSpace(OutputDirectory))
                        {
                            OutputDirectory = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}{key}";
                        }
                        else
                        {
                            OutputDirectory = $"{OutputDirectory}{Path.DirectorySeparatorChar}{key}";
                        }
                        string projectRelativePath = projectData[key];
                        List<string> projectPathSegments = new List<string>();
                        projectPathSegments.Add(solutionDirectory);
                        projectPathSegments.Add(projectRelativePath);

                        string projectPath = InspectorUtil.CreatePath(projectPathSegments);

                        if (String.IsNullOrWhiteSpace(ProjectName))
                        {
                            ProjectName = key;
                        }

                        ProjectInspector projectInspector = new ProjectInspector();
                        projectInspector.ProjectPath = projectPath;
                        projectInspector.Verbose = Verbose;
                        projectInspector.PackagesRepoUrl = PackagesRepoUrl;
                        projectInspector.ProjectName = ProjectName;
                        projectInspector.VersionName = VersionName;
                        projectInspector.OutputDirectory = OutputDirectory;

                        projectInspector.ExcludedModules = ExcludedModules;
                        projectInspector.IgnoreFailure = IgnoreFailure;
                        projectInspector.Setup();
                        DependencyNode projectNode =  projectInspector.getProjectNode();
                        children.Add(projectNode);
                        //TODO project inspector get dependencies to add to the total

                        //  if (String.IsNullOrWhiteSpace(VersionName))
                        //  {
                        //      HubVersionName = originalHubVersionName;
                        //  }

                        //  bool projectResult = base.Execute();

                        /**
                          result = result && projectResult;

                          if (projectResult && GenerateMergedBdio)
                          {
                              string bdioFilePath = $"{OutputDirectory}{Path.DirectorySeparatorChar}{HubProjectName}.jsonld";
                              string bdio = File.ReadAllText(bdioFilePath);
                              BdioContent bdioContent = BdioContent.Parse(bdio);

                              foreach (BdioComponent component in bdioContent.Components)
                              {
                                  if (!alreadyMergedComponents.Contains(component.BdioExternalIdentifier.ExternalId))
                                  {
                                      mergedComponentList.Add(component);
                                      alreadyMergedComponents.Add(component.BdioExternalIdentifier.ExternalId);
                                  }
                              }
                          }
                          **/
                    }
                    solutionNode.children = children;
                }
                else
                {
                    Console.WriteLine("No project data found for solution {0}", SolutionPath);
                }
            }
            catch (Exception ex)
            {
                if (IgnoreFailure)
                {
                    result = true;
                    Console.WriteLine("Error executing Build BOM task. Cause: {0}", ex);
                }
                else
                {
                    throw ex;
                }
            }
            
            return result;
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
