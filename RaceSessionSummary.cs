using System;

namespace RCDragManagerProd
{
    /// <summary>Lightweight row used by LoadSession list + repository.</summary>
    public sealed class RaceSessionSummary
    {
        public int Id { get; set; }
        public string EventName { get; set; } = "";
        public DateTime EventDate { get; set; }
        public string ClassType { get; set; } = "";
        public string RaceType { get; set; } = "";
        public override string ToString() => $"{EventDate:yyyy-MM-dd} — {EventName} ({ClassType} / {RaceType})";
    }
}
