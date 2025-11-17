using System;
using System.Collections.Generic;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class CircularDependencyDetector
    {
        public static List<DependencyInfo> Find(List<DependencyInfo> graph)
        {
            Console.WriteLine("[SCAN] Circular Dependency Detector (stub)");
            return new List<DependencyInfo>();
        }
    }
}
