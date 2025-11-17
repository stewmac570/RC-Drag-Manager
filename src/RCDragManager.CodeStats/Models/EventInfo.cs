using System;

namespace RCDragManager.CodeStats.Models
{
    public class EventInfo
    {
        /// <summary>
        /// Name of the event (for event fields) or method (for handlers).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// "EventField" for 'event Foo Bar;' or "HandlerMethod" for methods like btnSave_Click.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// Event handler type for event fields (e.g. EventHandler, MouseEventHandler).
        /// </summary>
        public string? EventType { get; set; }

        /// <summary>
        /// Namespace containing the event / handler.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Declaring class (e.g. Form1).
        /// </summary>
        public string? DeclaringType { get; set; }

        /// <summary>
        /// Fully qualified declaring type (e.g. RCDragManagerProd.UI.Forms.Form1).
        /// </summary>
        public string? DeclaringFullName { get; set; }

        /// <summary>
        /// Relative file path from the scan root.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 1-based line number of the declaration.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Parameter signature for handler methods.
        /// </summary>
        public string? HandlerSignature { get; set; }

        /// <summary>
        /// Parsed control name from handler method (btnSave_Click → btnSave).
        /// </summary>
        public string? ControlName { get; set; }

        /// <summary>
        /// Parsed event name from handler method (btnSave_Click → Click).
        /// </summary>
        public string? ControlEventName { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(DeclaringFullName))
            {
                return DeclaringFullName + "." + Name;
            }

            return Name;
        }
    }
}
