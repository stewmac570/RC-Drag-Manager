using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;

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
            Logger.Log($"[ENGINE-API] {GetType().Name}.LoadDrivers(count={drivers?.Count ?? 0})");
            _drivers = drivers ?? throw new ArgumentNullException(nameof(drivers));
            _ready = false; // must call GenerateBracket afterwards
        }

        public void GenerateBracket()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GenerateBracket()");
            if (_drivers == null || _drivers.Count < 2)
                throw new InvalidOperationException("LoadDrivers must be called with two or more drivers.");

            _engine.Initialize(_drivers);   // builds the full Pro-Ladder bracket
            _ready = true;
        }

        public IReadOnlyList<EngineMatch> GetMatches()
        {
            EnsureReady();
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetMatches()");

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
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetRoundOrder()");

            // Distinct by label, ordered by bracket progression
            return _engine.GetBracketMatches()
                          .Select(m => m.RoundLabel)
                          .Distinct()
                          .OrderBy(LabelToIndex)
                          .ToList();
        }

        public void SubmitWinner(int matchId, Driver winner)
        {
            EnsureReady();
            Logger.Log($"[ENGINE-API] {GetType().Name}.SubmitWinner(matchId={matchId}, winnerId={winner?.Id}, winner='{winner?.Name ?? "null"}')");
            if (winner == null || IsBye(winner))
                throw new InvalidOperationException("Cannot set BYE as a match winner.");

            _engine.SetWinner(matchId, winner);
        }

        public void SetWinner(int matchId, Driver winner)
        {
            SubmitWinner(matchId, winner);
        }

        public bool HasWinner(int matchId)
        {
            EnsureReady();
            Logger.Log($"[ENGINE-API] {GetType().Name}.HasWinner(matchId={matchId})");
            return _engine.Results.HasResult(matchId);
        }

        public Driver GetWinner(int matchId)
        {
            EnsureReady();
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetWinner(matchId={matchId})");
            return _engine.Results.GetWinner(matchId);
        }

        public Driver GetLoser(int matchId)
        {
            EnsureReady();
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetLoser(matchId={matchId})");
            return _engine.Results.GetLoser(matchId);
        }

        public bool IsComplete
        {
            get
            {
                EnsureReady();
                Logger.Log($"[ENGINE-API] {GetType().Name}.IsComplete");
                return _engine.IsTournamentComplete();
            }
        }

        public Driver GetChampion()
        {
            EnsureReady();
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetChampion()");
            var final = GetMatches()
                .FirstOrDefault(m => string.Equals(m.RoundLabel, "F", StringComparison.OrdinalIgnoreCase))
                ?? GetMatches().OrderBy(m => m.MatchId).LastOrDefault();
            return final == null ? null : _engine.Results.GetWinner(final.MatchId);
        }

        public Driver GetRunnerUp()
        {
            EnsureReady();
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetRunnerUp()");
            var final = GetMatches()
                .FirstOrDefault(m => string.Equals(m.RoundLabel, "F", StringComparison.OrdinalIgnoreCase))
                ?? GetMatches().OrderBy(m => m.MatchId).LastOrDefault();
            if (final == null) return null;

            var winner = _engine.Results.GetWinner(final.MatchId);
            if (winner == null) return null;
            if (final.Driver1 != null && final.Driver1.Id == winner.Id) return final.Driver2;
            if (final.Driver2 != null && final.Driver2.Id == winner.Id) return final.Driver1;
            return null;
        }

        public IReadOnlyList<EngineMatch> GetRoundMatches(string roundLabel)
        {
            EnsureReady();
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetRoundMatches(roundLabel='{roundLabel}')");
            var normalized = RoundLabels.Normalize(roundLabel);
            return GetMatches()
                .Where(m => string.Equals(RoundLabels.Normalize(m.RoundLabel), normalized, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.MatchId)
                .ToList();
        }

        public bool TryGetMatch(int matchId, out EngineMatch match)
        {
            EnsureReady();
            Logger.Log($"[ENGINE-API] {GetType().Name}.TryGetMatch(matchId={matchId})");
            match = GetMatches().FirstOrDefault(m => m.MatchId == matchId);
            return match != null;
        }

        public void Reset()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.Reset()");
            _engine = new MatchEngine(); // simplest way to clear internal state
            _drivers = null;
            _ready = false;
        }

        // ────────────────  helpers  ────────────────
        private void EnsureReady()
        {
            if (!_ready) throw new InvalidOperationException("Bracket not generated — call GenerateBracket() first.");
        }

        public bool IsBye(Driver d) =>
            ByePolicy.IsBye(d);

        private EngineMatch MapToDto(ProLadder.LadderMatch src)
        {
            // Let the MatchEngine resolve seeds/upstream winners; then normalize BYEs
            var (rd1, rd2) = _engine.ResolveDriversForMatch(src);

            return new EngineMatch
            {
                MatchId = src.MatchId,
                Driver1 = rd1,
                Driver2 = rd2,
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
