using System.Collections.Generic;

namespace RCDragManager.CodeStats.Models
{
    public class ProjectMap
    {
        public List<ClassInfo> Classes { get; set; } = new List<ClassInfo>();

        public List<MethodInfo> Methods { get; set; } = new List<MethodInfo>();

        public List<EventInfo> Events { get; set; } = new List<EventInfo>();

        public List<UIControlInfo> UIControls { get; set; } = new List<UIControlInfo>();

        public List<RepositoryInfo> Repositories { get; set; } = new List<RepositoryInfo>();

        public List<DependencyInfo> Dependencies { get; set; } = new List<DependencyInfo>();

        public List<DependencyInfo> Cycles { get; set; } = new List<DependencyInfo>();

        public List<ClassRelationInfo> ClassRelations { get; set; } = new List<ClassRelationInfo>();
    }
}
