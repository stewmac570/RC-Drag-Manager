using System;

namespace RCDragManagerProd.WPF.ViewModels
{
    /// <summary>One saved session/event row in the Load window's list.</summary>
    public sealed class LoadEventRow
    {
        public int Id { get; set; }
        public bool IsMultiClass { get; set; }
        public string EventName { get; set; }
        public string Kind { get; set; }       // "Single class" / "Multi-class"
        public string Detail { get; set; }     // race/class type, or class count
        public DateTime EventDate { get; set; }

        public string DateText =>
            EventDate == DateTime.MinValue ? "Unknown date" : EventDate.ToString("ddd d MMM yyyy");
    }
}
