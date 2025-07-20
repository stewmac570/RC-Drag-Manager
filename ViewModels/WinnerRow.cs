// WinnerRow.cs
// DTO for the “Winners / Results” ListView

namespace RCDragManagerProd.ViewModels
{
    public sealed class WinnerRow
    {
        public int MatchId { get; set; }
        public string RoundLabel { get; set; }  // ✅ Add this!
        public string Loser { get; set; }
        public string Winner { get; set; }
    }
}
