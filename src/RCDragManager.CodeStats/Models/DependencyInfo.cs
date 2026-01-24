using System;

namespace RCDragManager.CodeStats.Models
{
    public class DependencyInfo
    {
        /// <summary>
        /// Fully-qualified type that depends on something (e.g. RCDragManagerProd.UI.Forms.MainForm).
        /// </summary>
        public string FromFullName { get; set; } = string.Empty;

        /// <summary>
        /// Fully-qualified type being depended on.
        /// </summary>
        public string ToFullName { get; set; } = string.Empty;

        /// <summary>
        /// Relative file path of the "from" type.
        /// </summary>
        public string FromFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Relative file path of the "to" type, if resolvable.
        /// </summary>
        public string? ToFilePath { get; set; }

        /// <summary>
        /// Name of the member (method/field/property) where the dependency is observed, if known.
        /// </summary>
        public string? FromMemberName { get; set; }

        /// <summary>
        /// Kind of dependency (MethodReturn, MethodParameter, FieldOrPropertyType, Other).
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// 1-based line number where this dependency was observed.
        /// </summary>
        public int LineNumber { get; set; }

        public override string ToString()
        {
            return FromFullName + " -> " + ToFullName + " (" + Kind + ")";
        }
    }
}
