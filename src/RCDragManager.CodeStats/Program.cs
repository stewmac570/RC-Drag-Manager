using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using RCDragManager.CodeStats.Models;
using RCDragManager.CodeStats.Modules;

namespace RCDragManager.CodeStats
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(" RC Drag Manager - CodeStats Analyzer");
            Console.WriteLine("--------------------------------------------------");

            string root;

            if (args.Length > 0)
            {
                root = args[0];
            }
            else
            {
                root = Directory.GetCurrentDirectory();
            }

            Console.WriteLine("[LOG] Using root directory: " + root);

            if (!Directory.Exists(root))
            {
                Console.WriteLine("[FATAL] Directory does not exist.");
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<ClassInfo> classResults = ClassScanner.Scan(root);
            List<MethodInfo> methodResults = MethodScanner.Scan(root);
            List<EventInfo> eventResults = EventScanner.Scan(root);
            List<UIControlInfo> uiResults = UIControlScanner.Scan(root);
            List<RepositoryInfo> repoResults = RepositoryScanner.Scan(root);
            List<DependencyInfo> dependencyGraph = DependencyGraphAnalyzer.Analyze(root, classResults, methodResults);
            List<DependencyInfo> circular = CircularDependencyDetector.Find(dependencyGraph);

            List<ClassRelationInfo> classRelations = ClassRelationAnalyzer.Analyze(root, classResults, dependencyGraph);
            UIEventMapExporter.Export(root, uiResults, eventResults);

            ProjectMap projectMap = ProjectMapBuilder.Build(
                classResults,
                methodResults,
                eventResults,
                uiResults,
                repoResults,
                dependencyGraph,
                circular,
                classRelations
            );

            JsonExporter.Export(projectMap, root);
            MarkdownExporter.Export(projectMap, root);

            stopwatch.Stop();
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(" CodeStats Complete in " + stopwatch.Elapsed.TotalSeconds.ToString("0.000") + "s");
            Console.WriteLine("--------------------------------------------------");
        }
    }
}
