using System;
using System.Collections.Generic;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class DependencyGraphAnalyzer
    {
        public static List<DependencyInfo> Analyze(
            List<ClassInfo> classes,
            List<MethodInfo> methods)
        {
            Console.WriteLine("[SCAN] Dependency Graph Analyzer (stub)");
            return new List<DependencyInfo>();
        }
    }
}
