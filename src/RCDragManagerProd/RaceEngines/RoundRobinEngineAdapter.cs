using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.RoundRobinMode;

namespace RCDragManagerProd.RaceEngines
{
    /// <summary>
    /// Thin adapter over <see cref="RoundRobinEngine"/> that exposes IRaceEngine,
    /// with clean mapping and stable logging.
    /// </summary>
    public sealed class RoundRobinEngineAdapter : IRaceEngine
    {
        private readonly RoundRobinEngine _engine = new();

        // Controller calls this BEFORE GenerateBracket() (QMDRA variable rounds)
        public void SetRoundsToRun(int rounds)
        {
            if (rounds <= 0) rounds = 3;

            _engine.RoundsToRunRequested = rounds;
            Logger.Log($"[RR-ADAPTER] SetRoundsToRun → requested={rounds}");
        }


        public void LoadDrivers(List<Driver> drivers)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.LoadDrivers(count={drivers?.Count ?? 0})");
            Logger.Log($"[RR-ADAPTER] Loading {drivers?.Count ?? 0} driver(s)...");
            _engine.LoadDrivers(drivers ?? new List<Driver>());
        }

        public void GenerateBracket()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GenerateBracket()");
            Logger.Log("[RR-ADAPTER] Generating matches (circle method, clamped to n-1)...");

            _engine.GenerateMatches();
            Logger.Log($"[RR-ADAPTER] Bracket generated: {_engine.GetMatches().Count} match(es).");
        }

        public void Reset()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.Reset()");
            _engine.Reset();
            Logger.Log("[RR-ADAPTER] Engine reset.");
        }

        // Resume path: re-inject a previously-saved schedule instead of regenerating
        // (RR generation shuffles, so it cannot reproduce the saved pairings).
        public void InjectMatches(IReadOnlyList<EngineMatch> matches)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.InjectMatches(count={matches?.Count ?? 0})");
            _engine.InjectMatches(
                (matches ?? new List<EngineMatch>())
                    .Select(m => (m.MatchId, m.Driver1, m.Driver2, m.RoundLabel)));
        }

        public IReadOnlyList<EngineMatch> GetMatches()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetMatches()");
            var rows = _engine.GetMatches()
                              .Select(m => new EngineMatch
                              {
                                  MatchId = m.MatchId,
                                  Driver1 = m.D1,
                                  Driver2 = m.D2,
                                  RoundLabel = m.RoundLabel,
                                  HasResult = _engine.HasWinner(m.MatchId)
                              })
                              .OrderBy(m => m.MatchId)
                              .ToList();

            Logger.Log($"[RR-ADAPTER] GetMatches → {rows.Count} match(es).");
            return rows;
        }

        public IReadOnlyList<string> GetRoundOrder()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetRoundOrder()");
            var rounds = _engine.GetRoundLabels();
            Logger.Log($"[RR-ADAPTER] Round order: {string.Join(", ", rounds)}");
            return rounds;
        }

        public void SubmitWinner(int matchId, Driver winner)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.SubmitWinner(matchId={matchId}, winnerId={winner?.Id}, winner='{winner?.Name ?? "null"}')");
            if (winner == null || IsBye(winner))
            {
                Logger.Log($"[RR-ADAPTER] Reject SetWinner(M{matchId}) → BYE/Null.");
                return;
            }

            if (!_engine.TryGetMatch(matchId, out var left, out var right, out var round))
            {
                Logger.Log($"[RR-ADAPTER] Reject SetWinner(M{matchId}) → match not found.");
                return;
            }

            if ((left == null || left.Id != winner.Id) && (right == null || right.Id != winner.Id))
            {
                Logger.Log($"[RR-ADAPTER] Reject SetWinner(M{matchId}) → winner '{winner.Name}' not in match ({left?.Name ?? "BYE"} vs {right?.Name ?? "BYE"}).");
                return;
            }

            _engine.SetWinner(matchId, winner);
            Logger.Log($"[RR-ADAPTER] Winner set for M{matchId} ({round}) → {winner.Name}");
        }

        public void SetWinner(int matchId, Driver winner) => SubmitWinner(matchId, winner);

        public bool HasWinner(int matchId)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.HasWinner(matchId={matchId})");
            return _engine.HasWinner(matchId);
        }

        public Driver GetWinner(int matchId)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetWinner(matchId={matchId})");
            return _engine.GetWinner(matchId);
        }

        public Driver GetLoser(int matchId)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetLoser(matchId={matchId})");
            return _engine.GetLoser(matchId);
        }

        public bool IsComplete
        {
            get
            {
                Logger.Log($"[ENGINE-API] {GetType().Name}.IsComplete");
                return _engine.IsTournamentComplete();
            }
        }

        public Driver GetChampion()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetChampion()");
            var standings = _engine.GetStandings();
            return standings.Count == 0 ? null : standings[0].Driver;
        }

        public Driver GetRunnerUp()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetRunnerUp()");
            var standings = _engine.GetStandings();
            return standings.Count < 2 ? null : standings[1].Driver;
        }

        public IReadOnlyList<EngineMatch> GetRoundMatches(string roundLabel)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetRoundMatches(roundLabel='{roundLabel}')");
            var normalized = RoundLabels.Normalize(roundLabel);
            return GetMatches()
                .Where(m => string.Equals(RoundLabels.Normalize(m.RoundLabel), normalized, System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.MatchId)
                .ToList();
        }

        public bool TryGetMatch(int matchId, out EngineMatch match)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.TryGetMatch(matchId={matchId})");
            match = GetMatches().FirstOrDefault(m => m.MatchId == matchId);
            return match != null;
        }

        // convenience for controller
        public bool IsTournamentComplete() => _engine.IsTournamentComplete();

        public List<(Driver Driver, int Wins)> GetStandings()
        {
            var standings = _engine.GetStandings();
            Logger.Log("[RR-ADAPTER] Standings:");
            foreach (var (d, w) in standings) Logger.Log($"    {d?.Name} — {w} win(s)");
            return standings;
        }

        public List<Driver> GetTopRankedDrivers(int count)
        {
            var top = _engine.GetTopRankedDrivers(count);
            Logger.Log("[RR-ADAPTER] Top ranked: " +
                (top.Count == 0 ? "(none)" : string.Join(", ", top.Select(d => d.Name))));
            return top;
        }

        public bool IsBye(Driver d) =>
            ByePolicy.IsBye(d);
    }
}
