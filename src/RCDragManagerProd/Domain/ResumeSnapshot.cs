using System.Collections.Generic;

namespace RCDragManagerProd.Domain
{
    /// <summary>
    /// Persisted bracket structure for one match, captured so a saved event can be
    /// resumed WITHOUT regenerating non-deterministic brackets (Random / Losers Bracket).
    /// Driver ids only — resolved back to <see cref="Driver"/> from the session roster on load.
    /// Plain POCO so System.Text.Json round-trips it without custom converters.
    /// </summary>
    public sealed class SavedMatch
    {
        public int MatchId { get; set; }
        public int? Driver1Id { get; set; }   // null = empty slot / BYE
        public int? Driver2Id { get; set; }
        public string RoundLabel { get; set; }
        public int? FromMatch1 { get; set; }
        public int? FromMatch2 { get; set; }

        // Result carried inline with the structure so each persisted bracket is
        // self-contained. This is essential once a phase's matches leave the live
        // engine (RR after the LB/Finals swap) and once match-id spaces overlap
        // across engines (RR vs Finals Pro Ladder) — a single global result map
        // keyed by match id would lose or alias those results. null = unplayed.
        public int? WinnerId { get; set; }
        public int? LoserId { get; set; }
    }

    /// <summary>
    /// Everything needed to reconstruct live engine state for an interrupted event.
    /// Results themselves live in <see cref="RaceSession.SavedResults"/>; this snapshot
    /// captures the bracket STRUCTURE (which the existing save path did not persist) plus
    /// the transient controller state that drives phase transitions.
    /// Null on sessions saved before this feature, or before a bracket was generated.
    /// </summary>
    public sealed class ResumeSnapshot
    {
        // The mode the event STARTED in (RaceType mutates to "Losers Bracket"/"Finals"
        // mid-event, so it cannot be used to regenerate the initial bracket).
        public string OriginalRaceType { get; set; }

        // "Main" (single-engine: RR/Random/Pro Ladder), "LosersBracket", or "Finals".
        public string CurrentPhase { get; set; }

        public List<SavedMatch> MainMatches { get; set; } = new List<SavedMatch>();
        public List<SavedMatch> LosersMatches { get; set; } = new List<SavedMatch>();

        // RR completion snapshot — required to seed the Finals once the LB resolves.
        public List<int> RrTop3Ids { get; set; } = new List<int>();
        public List<SavedMatch> RrSnapshotMatches { get; set; } = new List<SavedMatch>();
        public List<string> RrSnapshotRoundOrder { get; set; } = new List<string>();

        // Transient controller flags.
        public int? BuybackChampionOverrideId { get; set; }
        public bool FinalsPending { get; set; }
        public bool InLosersPhase { get; set; }
        public string ActiveRound { get; set; }
    }
}
