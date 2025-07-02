// PairingRow.cs
// Lightweight DTO for the bracket ListView

namespace RCDragManagerProd.ViewModels
{
    public sealed class PairingRow
    {
        /// <summary>Engine MatchId; –1 when this row is a round header.</summary>
        public int MatchId { get; set; }

        public string RoundLabel { get; set; }

        public string Driver1 { get; set; }

        public string Driver2 { get; set; }

        /// <summary>True if this row is a header (“Round 1”, “SF”, …).</summary>
        public bool IsHeader { get; set; }
    }
}
