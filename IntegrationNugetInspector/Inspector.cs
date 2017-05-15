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

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    abstract class Inspector
    {
        public string TargetPath { get; set; }
        public bool Verbose { get; set; } = false;
        public string PackagesRepoUrl { get; set; }
        public string OutputDirectory { get; set; }
        public string ExcludedModules { get; set; } = "";
        public bool IgnoreFailure { get; set; } = false;
        public string Name { get; set; }


        abstract public string Execute();
        abstract public void Setup();
        abstract public DependencyNode GetNode();
        abstract public string WriteInfoFile(DependencyNode node);

    }
}
