using System.Collections.Generic;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.RaceEngines
{
    /// <summary>
    /// Minimal contract every race engine must implement for the controller/UI.
    /// </summary>
    public interface IRaceEngine
    {
        // ── life-cycle ────────────────────────────────────────────────
        /// <summary>Load/replace the driver roster. Does not build pairings.</summary>
        void LoadDrivers(List<Driver> drivers);

        /// <summary>Generate initial bracket/rounds from the loaded drivers.</summary>
        void GenerateBracket();

        /// <summary>Clear internal state so the engine can be reused safely.</summary>
        void Reset();

        // ── data access ───────────────────────────────────────────────
        /// <summary>Flat list of all matches (all rounds).</summary>
        IReadOnlyList<EngineMatch> GetMatches();

        /// <summary>Ordered list of round labels (e.g., R1, R2, …, SF, F).</summary>
        IReadOnlyList<string> GetRoundOrder();

        // ── results ───────────────────────────────────────────────────
        /// <summary>Record the winner for a given match.</summary>
        void SetWinner(int matchId, Driver winner);

        /// <summary>True if a winner has been recorded for the given match.</summary>
        bool HasWinner(int matchId);
    }

    /// <summary>
    /// Neutral DTO used by the controller/UI — no engine-specific types leak out.
    /// </summary>
    public sealed class EngineMatch
    {
        public int MatchId { get; set; }
        public Driver Driver1 { get; set; }
        public Driver Driver2 { get; set; }
        public string RoundLabel { get; set; }
        public int? FromMatch1 { get; set; }
        public int? FromMatch2 { get; set; }
        public bool HasResult { get; set; }
    }
}
