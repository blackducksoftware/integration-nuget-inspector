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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class DependencyNode
    {
        public string Artifact { get; set; }
        public string Version { get; set; }
        public List<DependencyNode> Children { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            StringWriter stringWriter = new StringWriter(stringBuilder);

            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (JsonWriter writer = new JsonTextWriter(stringWriter))
            {
                writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                serializer.Serialize(writer, this);
            }
            return stringBuilder.ToString();
        }

       
    
}
}
