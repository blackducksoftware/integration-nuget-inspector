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
using Com.Synopsys.Integration.Nuget.Inspection.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Com.Synopsys.Integration.Nuget.Inspection.Util.AssemblyInfoVersionParser;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class InspectorUtil
    {
        public const string DEFAULT_OUTPUT_DIRECTORY = "blackduck";

        public static string GetProjectAssemblyVersion(string projectDirectory)
        {
            try
            {
                List<AssemblyVersionResult> results = Directory.GetFiles(projectDirectory, "*AssemblyInfo.*", SearchOption.AllDirectories).ToList()
                    .Select(path => {
                        if (!path.EndsWith(".obj"))
                        {
                            if (File.Exists(path))
                            {
                                return AssemblyInfoVersionParser.ParseVersion(path);
                            }
                        }
                        return null;
                    })
                    .Where(it => it != null)
                    .ToList();

                AssemblyVersionResult selected = null;
                if (results.Any(it => it.confidence == ConfidenceLevel.HIGH))
                {
                    selected = results.First(it => it.confidence == ConfidenceLevel.HIGH);
                }
                else if (results.Any(it => it.confidence == ConfidenceLevel.MEDIUM))
                {
                    selected = results.First(it => it.confidence == ConfidenceLevel.MEDIUM);
                }
                else if (results.Count > 0)
                {
                    selected = results.First();
                }

                if (selected != null)
                {
                    Console.WriteLine($"Selected version '{selected.version}' from '{selected.path}'.");
                    return selected.version;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to find version for project directory: " + projectDirectory);
                Console.WriteLine("The issue was: " + e.Message);
            }

            return null;
        }

        public static string CreatePath(List<string> pathSegments)
        {
            return String.Join(String.Format("{0}", Path.DirectorySeparatorChar), pathSegments);
        }
    }
}
