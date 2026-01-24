using System;
using System.Collections.Generic;
using RCDragManager.CodeStats.Models;

namespace RCDragManager.CodeStats.Modules
{
    public static class ProjectMapBuilder
    {
        public static ProjectMap Build(
            List<ClassInfo> classes,
            List<MethodInfo> methods,
            List<EventInfo> events,
            List<UIControlInfo> uiControls,
            List<RepositoryInfo> repositories,
            List<DependencyInfo> dependencies,
            List<DependencyInfo> cycles,
            List<ClassRelationInfo> classRelations)
        {
            Console.WriteLine("[BUILD] Project Map Builder");

            ProjectMap map = new ProjectMap();

            map.Classes.AddRange(classes);
            map.Methods.AddRange(methods);
            map.Events.AddRange(events);
            map.UIControls.AddRange(uiControls);
            map.Repositories.AddRange(repositories);
            map.Dependencies.AddRange(dependencies);
            map.Cycles.AddRange(cycles);
            map.ClassRelations.AddRange(classRelations);

            return map;
        }
    }
}
