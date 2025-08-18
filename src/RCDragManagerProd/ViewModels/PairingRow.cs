using System.Diagnostics;

namespace RCDragManagerProd.ViewModels
{
    [DebuggerDisplay("{MatchNumber} {RoundLabel} | {Driver1} vs {Driver2} (Hdr={IsHeader})")]
    public sealed class PairingRow
    {
        /// <summary>Engine MatchId; –1 when this row is a round header.</summary>
        public int MatchId { get; set; }

        public string RoundLabel { get; set; } = string.Empty;

        public string Driver1 { get; set; } = string.Empty;

        public string Driver2 { get; set; } = string.Empty;

        /// <summary>True if this row is a header (“Round 1”, “SF”, …).</summary>
        public bool IsHeader { get; set; }

        public string MatchNumber { get; set; } = string.Empty;
    }
}