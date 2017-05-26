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


namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class InspectorUtil
    {
        public const string DEFAULT_OUTPUT_DIRECTORY = "blackduck";

        public static string GetProjectAssemblyVersion(string projectDirectory)
        {
            string version = null;
            List<string> pathSegments = new List<string>();
            pathSegments.Add(projectDirectory);
            string[] assemblyInfoPaths = Directory.GetFiles(projectDirectory, "*AssemblyInfo.*", SearchOption.AllDirectories);
            foreach (string path in assemblyInfoPaths)
            {
                Console.WriteLine("Assembly path {0}", path);
                if (File.Exists(path))
                {
                    List<string> contents = new List<string>(File.ReadAllLines(path));
                    List<string> versionText = contents.FindAll(text => text.Contains("AssemblyFileVersion"));
                    Console.WriteLine("Found AssemblyVersion {0}", versionText.Count);
                    if (versionText == null || versionText.Count == 0)
                    {
                        Console.WriteLine("Could not find the AssemblyFileVersion");
                        versionText = contents.FindAll(text => text.Contains("AssemblyVersion"));
                        Console.WriteLine("Found AssemblyVersion {0}", versionText.Count);
                    }
                    if (versionText != null)
                    {
                        foreach (string text in versionText)
                        {
                            String versionLine = text.Trim();
                            if (!versionLine.StartsWith("//"))
                            {
                                int firstParen = versionLine.IndexOf("(");
                                int lastParen = versionLine.LastIndexOf(")");
                                // exclude the '(' and the " characters
                                int start = firstParen + 2;
                                // exclude the ')' and the " characters
                                int end = lastParen - 1;
                                version = versionLine.Substring(start, (end - start));
                                Console.WriteLine("Version {0}", version);
                                break;
                            }
                        }
                    }
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
