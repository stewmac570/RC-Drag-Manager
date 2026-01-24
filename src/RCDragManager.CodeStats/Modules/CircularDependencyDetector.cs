using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class CircularDependencyDetector
    {
        private class CycleComponent
        {
            public List<string> Nodes { get; set; } = new List<string>();

            public List<DependencyInfo> Edges { get; set; } = new List<DependencyInfo>();
        }

        public static List<DependencyInfo> Find(List<DependencyInfo> graph)
        {
            Console.WriteLine("[SCAN] Circular Dependency Detector");

            List<DependencyInfo> resultEdges = new List<DependencyInfo>();

            Dictionary<string, List<string>> adjacency =
                new Dictionary<string, List<string>>(StringComparer.Ordinal);

            HashSet<string> nodes = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < graph.Count; i++)
            {
                DependencyInfo d = graph[i];

                if (!adjacency.TryGetValue(d.FromFullName, out List<string> list))
                {
                    list = new List<string>();
                    adjacency[d.FromFullName] = list;
                }

                if (!list.Contains(d.ToFullName))
                {
                    list.Add(d.ToFullName);
                }

                nodes.Add(d.FromFullName);
                nodes.Add(d.ToFullName);
            }

            List<HashSet<string>> sccs = TarjanStronglyConnectedComponents(nodes, adjacency);

            List<CycleComponent> cycles = new List<CycleComponent>();

            Dictionary<string, int> nodeToComponentIndex =
                new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < sccs.Count; i++)
            {
                HashSet<string> comp = sccs[i];

                if (comp.Count <= 1)
                {
                    continue;
                }

                CycleComponent cycle = new CycleComponent();

                foreach (string node in comp)
                {
                    cycle.Nodes.Add(node);
                    nodeToComponentIndex[node] = cycles.Count;
                }

                cycles.Add(cycle);
            }

            for (int i = 0; i < graph.Count; i++)
            {
                DependencyInfo d = graph[i];

                int compIndexFrom;
                int compIndexTo;

                if (!nodeToComponentIndex.TryGetValue(d.FromFullName, out compIndexFrom))
                {
                    continue;
                }

                if (!nodeToComponentIndex.TryGetValue(d.ToFullName, out compIndexTo))
                {
                    continue;
                }

                if (compIndexFrom != compIndexTo)
                {
                    continue;
                }

                CycleComponent cycle = cycles[compIndexFrom];
                cycle.Edges.Add(d);
                resultEdges.Add(d);
            }

            string? root = Directory.GetCurrentDirectory();
            WriteJson(root, cycles);
            WriteMarkdown(root, cycles);

            Console.WriteLine("[SCAN]   Cycles found : " + cycles.Count);

            return resultEdges;
        }

        private static List<HashSet<string>> TarjanStronglyConnectedComponents(
            HashSet<string> nodes,
            Dictionary<string, List<string>> adjacency)
        {
            List<HashSet<string>> result = new List<HashSet<string>>();

            Dictionary<string, int> index =
                new Dictionary<string, int>(StringComparer.Ordinal);

            Dictionary<string, int> lowlink =
                new Dictionary<string, int>(StringComparer.Ordinal);

            Stack<string> stack = new Stack<string>();
            HashSet<string> onStack = new HashSet<string>(StringComparer.Ordinal);

            int currentIndex = 0;

            foreach (string node in nodes)
            {
                if (!index.ContainsKey(node))
                {
                    StrongConnect(node, adjacency, index, lowlink, stack, onStack, ref currentIndex, result);
                }
            }

            return result;
        }

        private static void StrongConnect(
            string node,
            Dictionary<string, List<string>> adjacency,
            Dictionary<string, int> index,
            Dictionary<string, int> lowlink,
            Stack<string> stack,
            HashSet<string> onStack,
            ref int currentIndex,
            List<HashSet<string>> result)
        {
            index[node] = currentIndex;
            lowlink[node] = currentIndex;
            currentIndex = currentIndex + 1;

            stack.Push(node);
            onStack.Add(node);

            if (!adjacency.TryGetValue(node, out List<string> neighbours))
            {
                neighbours = new List<string>();
            }

            for (int i = 0; i < neighbours.Count; i++)
            {
                string w = neighbours[i];

                if (!index.ContainsKey(w))
                {
                    StrongConnect(w, adjacency, index, lowlink, stack, onStack, ref currentIndex, result);
                    lowlink[node] = Math.Min(lowlink[node], lowlink[w]);
                }
                else if (onStack.Contains(w))
                {
                    lowlink[node] = Math.Min(lowlink[node], index[w]);
                }
            }

            if (lowlink[node] == index[node])
            {
                HashSet<string> component = new HashSet<string>(StringComparer.Ordinal);

                while (true)
                {
                    string w = stack.Pop();
                    onStack.Remove(w);
                    component.Add(w);

                    if (string.Equals(w, node, StringComparison.Ordinal))
                    {
                        break;
                    }
                }

                result.Add(component);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Outputs: JSON + Markdown for cycles
        // ─────────────────────────────────────────────────────────────
        private static void WriteJson(string? root, List<CycleComponent> cycles)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return;
            }

            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Cycles.json");

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;

            string json = JsonSerializer.Serialize(cycles, options);
            File.WriteAllText(path, json);
        }

        private static void WriteMarkdown(string? root, List<CycleComponent> cycles)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return;
            }

            string dir = Path.Combine(root, "ProjectAnalysis");
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "Cycles.md");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Circular Dependencies");
            sb.AppendLine();
            sb.AppendLine("Total cycles (SCCs with >1 node): " + cycles.Count);
            sb.AppendLine();

            for (int i = 0; i < cycles.Count; i++)
            {
                CycleComponent c = cycles[i];

                sb.AppendLine("## Cycle " + (i + 1));
                sb.AppendLine();

                sb.AppendLine("### Nodes");
                sb.AppendLine();

                for (int j = 0; j < c.Nodes.Count; j++)
                {
                    sb.AppendLine("- " + c.Nodes[j]);
                }

                sb.AppendLine();
                sb.AppendLine("### Edges");
                sb.AppendLine();

                for (int j = 0; j < c.Edges.Count; j++)
                {
                    DependencyInfo d = c.Edges[j];

                    sb.Append("- ");
                    sb.Append(d.FromFullName);
                    sb.Append(" -> ");
                    sb.Append(d.ToFullName);
                    sb.Append("  [");
                    sb.Append(d.FromFilePath);
                    sb.Append(":");
                    sb.Append(d.LineNumber);
                    sb.AppendLine("]");
                }

                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
