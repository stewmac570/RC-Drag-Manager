using System;

namespace RCDragManagerProd.ViewModels
{
    /// <summary>Lightweight row used by LoadSession list + repository.</summary>
    public sealed class RaceSessionSummary
    {
        public int Id { get; set; } // Engine Id; –1 when this row is a header.
        public string EventName { get; set; } = ""; // Name of the event (e.g. "Spring Nationals 2023")
        public DateTime EventDate { get; set; } = DateTime.MinValue; // Date of the event (e.g. 2023-04-15)
        public string ClassType { get; set; } = ""; // Class type of the event (e.g. "Pro Mod", "Sportsman", etc.)
        public string RaceType { get; set; } = ""; 
        public override string ToString() => $"{EventDate:yyyy-MM-dd} — {EventName} ({ClassType} / {RaceType})"; // For display in the LoadSession list
    }
}
