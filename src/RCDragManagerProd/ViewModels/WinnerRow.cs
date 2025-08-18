// WinnerRow.cs
// DTO for the “Winners / Results” ListView

namespace RCDragManagerProd.ViewModels
{
    // <summary>
    /// Represents a single row in the Winners / Results ListView.
    public sealed class WinnerRow
    {
        public int MatchId { get; set; } // Engine MatchId; –1 when this row is a header.
        public string RoundLabel { get; set; } = string.Empty; // e.g. "Round 1", "Semi-Final", etc.
        public string Loser { get; set; } // Name of the losing driver
        public string Winner { get; set; } // Name of the winning driver
    }
}
