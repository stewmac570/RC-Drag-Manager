// ==========================================================================
// ProLadderEngineAdapter.cs
// RC Drag Manager  –  bridges the existing MatchEngine (NHRA Pro-Ladder)
// to the new IRaceEngine contract.
// ==========================================================================
//  • ZERO ladder logic is modified; this is pure “glue” code.
//  • Uses MatchEngine.Initialize(..), SetWinner(..), GetBracketMatches()
//    and Results.HasResult(..) exactly as they appear in the current source :contentReference[oaicite:0]{index=0}.
//  • Converts each ProLadder.LadderMatch into the neutral EngineMatch DTO
//    defined in IRaceEngine.cs.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd;               // Driver, MatchEngine
using RCDragManagerProd.RaceEngines;   // IRaceEngine, EngineMatch

namespace RCDragManagerProd.RaceEngines
{
    /// <summary>
    /// Adapter that lets <see cref="MatchEngine"/> fulfil the <see cref="IRaceEngine"/>
    /// interface expected by the new controller / UI.
    /// </summary>
    public sealed class ProLadderEngineAdapter : IRaceEngine
    {
        private MatchEngine _engine = new MatchEngine();
        private List<Driver> _drivers = null;
        private bool _ready = false;

        // ────────────────  IRaceEngine implementation  ────────────────
        public void LoadDrivers(List<Driver> drivers)
        {
            _drivers = drivers ?? throw new ArgumentNullException(nameof(drivers));
            _ready = false;          // must call GenerateBracket afterwards
        }

        public void GenerateBracket()
        {
            if (_drivers == null || _drivers.Count < 2)
                throw new InvalidOperationException("LoadDrivers must be called with two or more drivers.");

            _engine.Initialize(_drivers);   // builds the full Pro-Ladder bracket :contentReference[oaicite:1]{index=1}
            _ready = true;
        }

        public IReadOnlyList<EngineMatch> GetMatches()
        {
            EnsureReady();

            return _engine
                   .GetBracketMatches()                  // flat list of LadderMatch objects :contentReference[oaicite:2]{index=2}
                   .Select(MapToDto)
                   .ToList();
        }

        public void SetWinner(int matchId, Driver winner)
        {
            EnsureReady();
            _engine.SetWinner(matchId, winner);          // loser inferred by MatchEngine :contentReference[oaicite:3]{index=3}
        }

        public bool HasWinner(int matchId)
        {
            EnsureReady();
            return _engine.Results.HasResult(matchId);   // tracks results internally :contentReference[oaicite:4]{index=4}
        }

        public IReadOnlyList<string> GetRoundOrder()
        {
            EnsureReady();

            return _engine.GetBracketMatches()
                          .Select(m => m.RoundLabel)
                          .Distinct()
                          .OrderBy(LabelToIndex)
                          .ToList();
        }

        public void Reset()
        {
            _engine = new MatchEngine();   // simplest way to clear internal state
            _drivers = null;
            _ready = false;
        }

        // ─────────────────────  helpers  ─────────────────────
        private void EnsureReady()
        {
            if (!_ready)
                throw new InvalidOperationException("Bracket not generated – call GenerateBracket() first.");
        }

        private static EngineMatch MapToDto(ProLadder.LadderMatch src)
        {
            // Resolve actual Driver objects (or BYE placeholders) for display
            MatchEngine tempEngine = new MatchEngine();
            var (d1, d2) = tempEngine.ResolveDriversForMatch(src);   // gives Driver or BYE stub

            return new EngineMatch
            {
                MatchId = src.MatchId,
                Driver1 = d1,
                Driver2 = d2,
                RoundLabel = src.RoundLabel,
                FromMatch1 = src.FromMatch1,
                FromMatch2 = src.FromMatch2,
                HasResult = tempEngine.Results.HasResult(src.MatchId)
            };
        }

        private static int LabelToIndex(string lbl) => lbl switch
        {
            "R1" => 1,
            "R2" => 2,
            "R3" => 3,
            "R4" => 4,
            "R5" => 5,
            "QF" => 90,   // quarter-final
            "SF" => 98,   // semi-final
            "F" => 99,   // final
            _ => 100
        };
    }
}
