﻿/*******************************************************************************
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
                Console.WriteLine(ex.ToString());
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
                List<ProjectFile> files = FindProjectFilesFromSolutionFile(TargetPath, ExcludedProjectTypeGUIDs);
                Console.WriteLine("Parsed Solution File");
                if (files.Count > 0)
                {
                    HashSet<DependencyNode> children = new HashSet<DependencyNode>();
                    string solutionDirectory = Path.GetDirectoryName(TargetPath);
                    Console.WriteLine("Solution directory: {0}", solutionDirectory);
                    foreach (ProjectFile project in files)
                    {
                        string projectRelativePath = project.Path;
                        List<string> projectPathSegments = new List<string>();
                        projectPathSegments.Add(solutionDirectory);
                        projectPathSegments.Add(projectRelativePath);

                        string projectPath = InspectorUtil.CreatePath(projectPathSegments);

                        ProjectInspector projectInspector = new ProjectInspector();
                        projectInspector.TargetPath = projectPath;
                        projectInspector.Verbose = Verbose;
                        projectInspector.PackagesRepoUrl = PackagesRepoUrl;
                        projectInspector.Name = project.Name;

                        projectInspector.ExcludedModules = ExcludedModules;
                        projectInspector.IgnoreFailure = IgnoreFailure;
                        projectInspector.Setup();
                        DependencyNode projectNode = projectInspector.GetNode();
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
                Console.WriteLine(ex.ToString());
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

        private class ProjectFile
        {
            public string TypeGUID;
            public string Name;
            public string GUID;
            public string Path;

            public static ProjectFile Parse(string projectLine)
            {
                //projectLine format: Project(type) = name, file, guid
                //projectLine example: Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "NUnitFramework", "NUnitFramework", "{5D8A9D62-C11C-45B2-8965-43DE8160B558}"


                var equalSplit = projectLine.Split('=').Select(s => s.Trim()).ToList();
                if (equalSplit.Count() < 2) return null;

                var file = new ProjectFile();
                string leftSide = equalSplit[0];
                string rightSide = equalSplit[1];
                if (leftSide.StartsWith("Project(\"") && leftSide.EndsWith("\")"))
                {
                    file.TypeGUID = MiddleOfString(leftSide, "Project(\"".Length, "\")".Length);
                }
                var opts = rightSide.Split(',').Select(s => s.Trim()).ToList();
                if (opts.Count() >= 1) file.Name = MiddleOfString(opts[0], 1, 1); //strip quotes
                if (opts.Count() >= 2) file.Path = MiddleOfString(opts[1], 1, 1); //strip quotes
                if (opts.Count() >= 3) file.GUID = MiddleOfString(opts[2], 1, 1); //strip quotes

                return file;
            }

            private static string MiddleOfString(string source, int fromLeft, int fromRight)
            {
                var left = source.Substring(fromLeft);
                return left.Substring(0, left.Length - fromRight);
            }

        }

        private List<string> ExcludedProjectTypeGUIDs = new List<string>() {
            "{2150E333-8FDC-42A3-9474-1A3956D46DE8}"    //Ignore 'Solution Folders'
        };

        private List<ProjectFile> FindProjectFilesFromSolutionFile(string solutionPath, List<string> excludedTypeGUIDs)
        {
            var projects = new List<ProjectFile>();
            // Visual Studio right now is not resolving the Microsoft.Build.Construction.SolutionFile type
            // parsing the solution file manually for now.
            if (File.Exists(solutionPath))
            {
                List<string> contents = new List<string>(File.ReadAllLines(solutionPath));
                var projectLines = contents.FindAll(text => text.StartsWith("Project("));
                foreach (string projectText in projectLines)
                {
                    ProjectFile file = ProjectFile.Parse(projectText);
                    if (file != null)
                    {
                        if (!excludedTypeGUIDs.Contains(file.TypeGUID))
                        {
                           projects.Add(file);
                        }
                    }
                }
                Console.WriteLine("Black Duck I/O Generation Found {0} Project elements, processed {1} project elements for data", projectLines.Count(), projects.Count());
            }
            else
            {
                throw new BlackDuckInspectorException("Solution File " + solutionPath + " not found");
            }

            return projects;
        }

        
        
    }
}
