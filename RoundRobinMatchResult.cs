// ──────────────────────────────────────────────────────────────────────────────
// File: RoundRobinMatchResult.cs
// Purpose: DTO for a single round-robin pairing.
// ──────────────────────────────────────────────────────────────────────────────

namespace RCDragManagerProd
{
    /// <summary>
    /// Captures the outcome of one Round-Robin match.
    /// </summary>
    public class RoundRobinMatchResult
    {
        public int MatchId { get; set; }
        public int Driver1Id { get; set; }
        public int Driver2Id { get; set; }
        public int WinnerId { get; set; }     // 0 = unresolved
        public int LoserId { get; set; }      // 0 = unresolved or BYE
        public string RoundLabel { get; set; } = "";    // "R1","R2","R3"
    }

}
