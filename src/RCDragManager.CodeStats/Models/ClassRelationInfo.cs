using System.Collections.Generic;

namespace RCDragManager.CodeStats.Models
{
    public class ClassRelationInfo
    {
        public string FullName { get; set; } = string.Empty;

        public string? BaseType { get; set; }

        public List<string> Interfaces { get; set; } = new List<string>();

        public List<string> ComposesTypes { get; set; } = new List<string>();

        public string FilePath { get; set; } = string.Empty;
    }
}
