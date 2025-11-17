using System;
using System.IO;
using RCDragManager.CodeStats.Modules;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(" RC Drag Manager - CodeStats Analyzer");
            Console.WriteLine("--------------------------------------------------");

            string root = args.Length > 0
                ? args[0]
                : Directory.GetCurrentDirectory();

            Console.WriteLine($"[LOG] Using root directory: {root}");

            if (!Directory.Exists(root))
            {
                Console.WriteLine("[FATAL] Directory does not exist.");
                return;
            }

            // ─────────────────────────────────────────────────────────────
            // Phase orchestration
            // ─────────────────────────────────────────────────────────────
            var classResults = ClassScanner.Scan(root);
            var methodResults = MethodScanner.Scan(root);
            var eventResults = EventScanner.Scan(root);
            var uiResults = UIControlScanner.Scan(root);
            var repoResults = RepositoryScanner.Scan(root);
            var dependencyGraph = DependencyGraphAnalyzer.Analyze(classResults, methodResults);
            var circular = CircularDependencyDetector.Find(dependencyGraph);

            var projectMap = ProjectMapBuilder.Build(
                classResults,
                methodResults,
                eventResults,
                uiResults,
                repoResults,
                dependencyGraph,
                circular
            );

            JsonExporter.Export(projectMap, root);
            MarkdownExporter.Export(projectMap, root);

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(" CodeStats Complete");
            Console.WriteLine("--------------------------------------------------");
        }
    }
}
