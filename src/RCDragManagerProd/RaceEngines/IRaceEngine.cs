using System.Collections.Generic;
using RCDragManagerProd.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;    // only if returning VM rows


namespace RCDragManagerProd.RaceEngines
{

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
