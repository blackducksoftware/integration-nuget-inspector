using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{

    //Given a generic InspectionOptions, InspectorDispatch is responsible for instantiating the correct Inspector (Project or Solution)
    class InspectorDispatch
    {

        public InspectorDispatch()
        {

        }
        
        public InspectionResult Inspect(InspectionOptions options)
        {
            return NewInspector(options)?.Inspect();
        }

        public IInspector NewInspector(InspectionOptions options)
        {
            
            if (Directory.Exists(options.TargetPath))
            {
                Console.WriteLine("Searching for a solution file to process...");
                string[] solutionPaths = Directory.GetFiles(options.TargetPath, "*.sln");

                if (solutionPaths != null && solutionPaths.Length >= 1)
                {
                    var solutionOp = new SolutionInspectionOptions(options);
                    solutionOp.TargetPath = solutionPaths[0];
                    return new SolutionInspector(solutionOp);
                }
                else
                {
                    Console.WriteLine("No Solution file found.  Searching for a project file...");
                    string[] projectPaths = Directory.GetFiles(options.TargetPath, "*.*proj");
                    if (projectPaths != null && projectPaths.Length > 0)
                    {
                        string projectPath = projectPaths[0];
                        Console.WriteLine("Found project {0}", projectPath);
                        var projectOp = new ProjectInspectionOptions(options);
                        projectOp.TargetPath = options.TargetPath;
                        return new ProjectInspector(projectOp);
                    }
                    else
                    {
                        Console.WriteLine("No Project file found. Finished.");
                        return null;
                    }
                }
            }
            else if (File.Exists(options.TargetPath))
            {
                if (options.TargetPath.Contains(".sln"))
                {
                    var solutionOp = new SolutionInspectionOptions(options);
                    solutionOp.TargetPath = options.TargetPath;
                    return new SolutionInspector(solutionOp);
                }
                else
                {
                    var projectOp = new ProjectInspectionOptions(options);
                    projectOp.TargetPath = options.TargetPath;
                    return new ProjectInspector(projectOp);
                }
            }

            return null;
        }

    }
}
