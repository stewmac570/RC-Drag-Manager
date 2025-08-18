using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.RaceEngines
{
    /// <summary>
    /// Adapter that lets <see cref="MatchEngine"/> fulfill the <see cref="IRaceEngine"/> interface
    /// expected by the controller/UI.
    /// </summary>
    public sealed class ProLadderEngineAdapter : IRaceEngine
    {
        private MatchEngine _engine = new MatchEngine();
        private List<Driver> _drivers;
        private bool _ready;

        // ────────────────  IRaceEngine  ────────────────
        public void LoadDrivers(List<Driver> drivers)
        {
            _drivers = drivers ?? throw new ArgumentNullException(nameof(drivers));
            _ready = false; // must call GenerateBracket afterwards
        }

        public void GenerateBracket()
        {
            if (_drivers == null || _drivers.Count < 2)
                throw new InvalidOperationException("LoadDrivers must be called with two or more drivers.");

            _engine.Initialize(_drivers);   // builds the full Pro-Ladder bracket
            _ready = true;
        }

        public IReadOnlyList<EngineMatch> GetMatches()
        {
            EnsureReady();

            // Map engine matches to neutral DTO; skip BYE-vs-BYE
            return _engine.GetBracketMatches()
                          .Select(MapToDto)
                          .Where(m => !IsBye(m.Driver1) || !IsBye(m.Driver2))
                          .OrderBy(m => m.MatchId)
                          .ToList();
        }

        public IReadOnlyList<string> GetRoundOrder()
        {
            EnsureReady();

            // Distinct by label, ordered by bracket progression
            return _engine.GetBracketMatches()
                          .Select(m => m.RoundLabel)
                          .Distinct()
                          .OrderBy(LabelToIndex)
                          .ToList();
        }

        public void SetWinner(int matchId, Driver winner)
        {
            EnsureReady();
            if (winner == null || IsBye(winner))
                throw new InvalidOperationException("Cannot set BYE as a match winner.");

            _engine.SetWinner(matchId, winner);
        }

        public bool HasWinner(int matchId)
        {
            EnsureReady();
            return _engine.Results.HasResult(matchId);
        }

        public void Reset()
        {
            _engine = new MatchEngine(); // simplest way to clear internal state
            _drivers = null;
            _ready = false;
        }

        // ────────────────  helpers  ────────────────
        private void EnsureReady()
        {
            if (!_ready) throw new InvalidOperationException("Bracket not generated — call GenerateBracket() first.");
        }

        private static bool IsBye(Driver d) =>
            d == null || string.Equals(d.Name?.Trim(), "BYE", StringComparison.OrdinalIgnoreCase);

        private EngineMatch MapToDto(ProLadder.LadderMatch src)
        {
            // Let the MatchEngine resolve seeds/upstream winners; then normalize BYEs
            var (rd1, rd2) = _engine.ResolveDriversForMatch(src);
            var d1 = rd1 ?? new Driver { Name = "BYE" };
            var d2 = rd2 ?? new Driver { Name = "BYE" };

            return new EngineMatch
            {
                MatchId = src.MatchId,
                Driver1 = d1,
                Driver2 = d2,
                RoundLabel = src.RoundLabel,
                FromMatch1 = src.FromMatch1,
                FromMatch2 = src.FromMatch2,
                HasResult = _engine.Results.HasResult(src.MatchId)
            };
        }

        private static int LabelToIndex(string lbl) => lbl switch
        {
            "R1" => 1,
            "R2" => 2,
            "R3" => 3,
            "R4" => 4,
            "R5" => 5,
            "QF" => 90,  // quarter-final
            "SF" => 98,  // semi-final
            "F" => 99,  // final
            _ => 100
        };
    }
}
