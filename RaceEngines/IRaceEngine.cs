// ==========================================================================
// IRaceEngine.cs
// RC Drag Manager — common contract for all bracket engines
// ==========================================================================
//  • The UI (Form1) and the new RaceController will depend ONLY on this
//    interface, never on concrete engines.
//  • Every existing engine (MatchEngine, RandomMatchEngine, RoundRobinEngine,
//    LosersBracketEngine, etc.) will get a small *Adapter* class that
//    implements IRaceEngine and forwards the calls.
// ==========================================================================

using System.Collections.Generic;

namespace RCDragManagerProd.RaceEngines
{
    /// <summary>
    /// Normalised contract every race-logic engine (Pro-Ladder, Random Draw,
    /// Round Robin, etc.) must satisfy.  Zero WinForms or persistence details
    /// — pure bracket logic.
    /// </summary>
    public interface IRaceEngine
    {
        // ── life-cycle ────────────────────────────────────────────────
        void LoadDrivers(List<Driver> drivers);   // store roster only
        void GenerateBracket();                   // build Round-1 pairings
        void Reset();                             // wipe all state

        // ── data access ───────────────────────────────────────────────
        IReadOnlyList<EngineMatch> GetMatches();  // flat list, all rounds
        IReadOnlyList<string> GetRoundOrder();

        // ── results ───────────────────────────────────────────────────
        void SetWinner(int matchId, Driver winner);
        bool HasWinner(int matchId);
    }

    /// <summary>
    /// Neutral DTO returned by <see cref="IRaceEngine.GetMatches"/> so the
    /// controller and UI never depend on engine-specific types.
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
