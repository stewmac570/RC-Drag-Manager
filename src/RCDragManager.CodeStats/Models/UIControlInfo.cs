using System.Collections.Generic;

namespace RCDragManager.CodeStats.Models
{
    public class UIControlInfo
    {
        /// <summary>
        /// Field / instance name of the control (e.g. btnSave).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Full type name (e.g. System.Windows.Forms.Button).
        /// </summary>
        public string? TypeFullName { get; set; }

        /// <summary>
        /// Short type name (e.g. Button).
        /// </summary>
        public string? TypeShortName { get; set; }

        /// <summary>
        /// Namespace containing the form / control class.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Declaring form or user control class name.
        /// </summary>
        public string? DeclaringType { get; set; }

        /// <summary>
        /// Fully qualified declaring type (e.g. RCDragManagerProd.UI.Forms.Form1).
        /// </summary>
        public string? DeclaringFullName { get; set; }

        /// <summary>
        /// Relative file path of the .Designer.cs file from the root scan directory.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 1-based line number where the control is first created (new ...) in InitializeComponent.
        /// </summary>
        public int DeclarationLine { get; set; }

        /// <summary>
        /// Parent control/container (e.g. panelMain, this for root).
        /// </summary>
        public string? ParentName { get; set; }

        /// <summary>
        /// Event bindings for this control (Click → btnSave_Click, etc.).
        /// </summary>
        public List<UIControlEventBinding> Events { get; set; } = new List<UIControlEventBinding>();
    }

    public class UIControlEventBinding
    {
        public string EventName { get; set; } = string.Empty;

        public string HandlerName { get; set; } = string.Empty;

        public int LineNumber { get; set; }
    }
}
