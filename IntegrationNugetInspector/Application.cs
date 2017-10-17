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
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;

//Need both Microsoft.Build.Utilities.Core and Microsoft.Build or you get an exception https://github.com/Microsoft/msbuild/issues/1889
namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class Application
    {

        public static void Main(string[] args)
        {
            try
            {
                Environment.SetEnvironmentVariable("VSINSTALLDIR", @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community");
                Environment.SetEnvironmentVariable("VisualStudioVersion", @"15.0");
                var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
                if (projectCollection.GetToolset("15.0") == null)
                {
                    throw new Exception("MSBuild 15 not found");
                }
                var dispatch = new InspectorDispatch();
                var runner = new CommandLineRunner(dispatch);
                runner.Execute(args);

             }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit(-1);
            }

        }

    }
}
