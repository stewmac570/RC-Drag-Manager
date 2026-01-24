namespace RCDragManager.CodeStats.Models
{
    public class ClassInfo
    {
        public string Name { get; set; } = string.Empty;

        public string? Namespace { get; set; }

        public string FilePath { get; set; } = string.Empty;

        // Convenience: fully-qualified name
        public string FullName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Namespace))
                {
                    return Name;
                }

                return Namespace + "." + Name;
            }
        }
    }
}
