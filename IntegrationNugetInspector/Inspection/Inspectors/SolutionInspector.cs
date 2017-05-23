/*******************************************************************************
 * Copyright (C) 2017 Black Duck Software, Inc.
 * http://www.blackducksoftware.com/
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements. See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership. The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied. See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *******************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class SolutionInspector : IInspector
    {
        public SolutionInspectionOptions Options;

        public SolutionInspector(SolutionInspectionOptions options)
        {
            Options = options;

            if (Options == null)
            {
                throw new Exception("Must provide a valid options object.");
            }

            if (String.IsNullOrWhiteSpace(Options.OutputDirectory))
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                Options.OutputDirectory = $"{currentDirectory}{Path.DirectorySeparatorChar}{InspectorUtil.DEFAULT_OUTPUT_DIRECTORY}";
            }
            if (String.IsNullOrWhiteSpace(Options.SolutionName))
            {
                Options.SolutionName = Path.GetFileNameWithoutExtension(Options.TargetPath);
            }
        }

        public InspectionResult Inspect()
        {
            try
            {
                return new InspectionResult()
                {
                    Status = InspectionResult.ResultStatus.Success,
                    ResultName = Options.SolutionName,
                    OutputDirectory = Options.OutputDirectory,
                    Node = GetNode()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}", ex.ToString());
                if (Options.IgnoreFailure)
                {
                    Console.WriteLine("Error executing Build BOM task on project {0}, cause: {1}", Options.SolutionName, ex);
                    return new InspectionResult()
                    {
                        Status = InspectionResult.ResultStatus.Success
                    };
                }
                else
                {
                    return new InspectionResult()
                    {
                        Status = InspectionResult.ResultStatus.Error,
                        Exception = ex
                    };
                }
            }
            
        }
        


        public DependencyNode GetNode()
        {
            DependencyNode solutionNode = new DependencyNode();
            solutionNode.Artifact = Options.SolutionName;
            try
            {
                Dictionary<string, string> projectData = ParseSolutionFile(Options.TargetPath);
                Console.WriteLine("Parsed Solution File");
                if (projectData.Count > 0)
                {
                    HashSet<DependencyNode> children = new HashSet<DependencyNode>();
                    string solutionDirectory = Path.GetDirectoryName(Options.TargetPath);
                    Console.WriteLine("Solution directory: {0}", solutionDirectory);
                    foreach (string projectName in projectData.Keys)
                    {
                        string projectRelativePath = projectData[projectName];
                        List<string> projectPathSegments = new List<string>();
                        projectPathSegments.Add(solutionDirectory);
                        projectPathSegments.Add(projectRelativePath);

                        string projectPath = InspectorUtil.CreatePath(projectPathSegments);

                        ProjectInspector projectInspector = new ProjectInspector(new ProjectInspectionOptions()
                        {
                            ExcludedModules = Options.ExcludedModules,
                            IgnoreFailure = Options.IgnoreFailure,
                            OutputDirectory = Options.OutputDirectory,
                            PackagesRepoUrl = Options.PackagesRepoUrl,
                            ProjectName = projectName,
                            TargetPath = projectPath,
                            Verbose = Options.Verbose,
                        });

                        var projectInspection =  projectInspector.Inspect();
                        if (projectInspection != null && projectInspection.Node != null)
                        {
                            children.Add(projectInspection.Node);
                        }
                    }
                    solutionNode.Children = children;
                }
                else
                {
                    Console.WriteLine("No project data found for solution {0}", Options.TargetPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                if (Options.IgnoreFailure)
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
