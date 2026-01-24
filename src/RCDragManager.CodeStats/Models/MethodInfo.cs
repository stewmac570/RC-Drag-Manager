using System;

namespace RCDragManager.CodeStats.Models
{
    public class MethodInfo
    {
        public string Name { get; set; } = string.Empty;

        public string? ReturnType { get; set; }

        public string? Namespace { get; set; }

        public string? DeclaringType { get; set; }

        public string? DeclaringFullName { get; set; }

        public string FilePath { get; set; } = string.Empty;

        public int LineNumber { get; set; }

        public string? ParameterSignature { get; set; }

        public bool IsAsync { get; set; }

        public override string ToString()
        {
            string nsPrefix = string.IsNullOrWhiteSpace(DeclaringFullName)
                ? string.Empty
                : DeclaringFullName + ".";

            string signature = Name + "(" + (ParameterSignature ?? string.Empty) + ")";

            return nsPrefix + signature;
        }
    }
}
