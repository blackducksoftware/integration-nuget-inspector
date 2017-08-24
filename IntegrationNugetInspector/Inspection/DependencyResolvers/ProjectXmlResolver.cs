﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Com.Blackducksoftware.Integration.Nuget.Inspector;

namespace Com.Blackducksoftware.Integration.Nuget.DependencyResolvers
{
    class ProjectXmlResolver : DependencyResolver
    {

        private string ProjectPath;
        private NugetSearchService NugetSearchService;

        public ProjectXmlResolver(string projectPath, NugetSearchService nugetSearchService)
        {
            ProjectPath = projectPath;
            NugetSearchService = nugetSearchService;
        }

        public DependencyResult Process()
        {
            var result = new DependencyResult();
            var tree = new NugetTreeResolver(NugetSearchService);

            // .NET core default version
            result.ProjectVersion = "1.0.0";

            XmlDocument doc = new XmlDocument();
            doc.Load(ProjectPath);

            XmlNodeList versionNodes = doc.GetElementsByTagName("Version");
            if (versionNodes != null && versionNodes.Count > 0)
            {
                foreach (XmlNode version in versionNodes)
                {
                    if (version.NodeType != XmlNodeType.Comment)
                    {
                        result.ProjectVersion = version.InnerText;
                    }
                }
            }
            else
            {
                string prefix = "1.0.0";
                string suffix = "";
                XmlNodeList prefixNodes = doc.GetElementsByTagName("VersionPrefix");
                if (prefixNodes != null && prefixNodes.Count > 0)
                {
                    foreach (XmlNode prefixNode in prefixNodes)
                    {
                        if (prefixNode.NodeType != XmlNodeType.Comment)
                        {
                            prefix = prefixNode.InnerText;
                        }
                    }
                }
                XmlNodeList suffixNodes = doc.GetElementsByTagName("VersionSuffix");
                if (suffixNodes != null && suffixNodes.Count > 0)
                {
                    foreach (XmlNode suffixNode in suffixNodes)
                    {
                        if (suffixNode.NodeType != XmlNodeType.Comment)
                        {
                            suffix = suffixNode.InnerText;
                        }
                    }

                }
                result.ProjectVersion = String.Format("{0}-{1}", prefix, suffix); ;
            }
            XmlNodeList packagesNodes = doc.GetElementsByTagName("PackageReference");
            if (packagesNodes.Count > 0)
            {
                foreach (XmlNode package in packagesNodes)
                {
                    XmlAttributeCollection attributes = package.Attributes;
                    if (attributes != null)
                    {
                        XmlAttribute include = attributes["Include"];
                        XmlAttribute version = attributes["Version"];
                        if (include != null && version != null)
                        {
                            var dep = new NugetDependency(include.Value, NuGet.Versioning.VersionRange.Parse(version.Value));
                            tree.Add(dep);
                        }
                    }
                }
            }

            result.Packages = tree.GetPackageList();

            return result;
        }
    }
}
