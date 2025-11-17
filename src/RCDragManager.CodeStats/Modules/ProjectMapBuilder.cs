using System;
using System.Collections.Generic;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class ProjectMapBuilder
    {
        public static ProjectMap Build(
            IReadOnlyList<ClassInfo> classes,
            IReadOnlyList<MethodInfo> methods,
            IReadOnlyList<EventInfo> events,
            IReadOnlyList<UIControlInfo> uiControls,
            IReadOnlyList<RepositoryInfo> repositories,
            IReadOnlyList<DependencyInfo> dependencies,
            IReadOnlyList<DependencyInfo> cycles)
        {
            Console.WriteLine("[BUILD] Project Map Builder (stub)");

            ProjectMap map = new ProjectMap();
            return map;
        }
    }
}
