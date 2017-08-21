using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Blackducksoftware.Integration.Nuget.Inspector
{
    class DependencyNodeUtil
    {
        public static void CheckCircular(HashSet<DependencyNode> nodes, List<DependencyNode> parents = null)
        {
            if (nodes == null) return;

            foreach (var node in nodes)
            {
                List<DependencyNode> next;
                if (parents == null)
                {
                    next = new List<DependencyNode>();
                }
                else
                {
                    if (parents.Contains(node))
                    {
                        throw new Exception("CIRCULAR?");
                    }
                    next = new List<DependencyNode>(parents);
                }
                next.Add(node);
                CheckCircular(node.Children, next);
            }
        }

        public static void PrettyPrint(List<DependencyNode> nodes, List<bool> depthBranch = null)
        {
            if (depthBranch == null) depthBranch = new List<bool>();

            for (var i = 0; i < nodes.Count; i++)
            {
                for (var d = 0; d < depthBranch.Count; d++)
                {
                    if (depthBranch[d])
                    {
                        Console.Write("| ");
                    }
                    else
                    {
                        Console.Write("  ");
                    }
                }
                var node = nodes[i];
                
                var nextDepth = new List<bool>(depthBranch);
                if (i < nodes.Count - 1)
                {
                    Console.Write("+-");
                    nextDepth.Add(true);
                }
                else
                {//last
                    Console.Write("+-");
                    nextDepth.Add(false);
                }

                if (node.Children == null || node.Children.Count == 0)
                {
                    Console.Write("-");
                }
                else
                {
                    Console.Write("-");
                }

                Console.Write(" " + node.Artifact + " " + node.Version);
                Console.WriteLine();

                if (node.Children == null) continue;
                PrettyPrint(node.Children.ToList(), nextDepth);
            }



            //Console.Write("+ - - +");
        }
    }
}
