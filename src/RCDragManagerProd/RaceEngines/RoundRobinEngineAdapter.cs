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

        public void LoadDrivers(List<Driver> drivers)
        {
            Logger.Log($"[RR-ADAPTER] Loading {drivers?.Count ?? 0} driver(s)...");
            _engine.LoadDrivers(drivers ?? new List<Driver>());
        }

        public void GenerateBracket()
        {
            Logger.Log("[RR-ADAPTER] Generating matches (circle method, 3 rounds max)...");
            _engine.GenerateMatches();
            Logger.Log($"[RR-ADAPTER] Bracket generated: {_engine.GetMatches().Count} match(es).");
        }

        public void Reset()
        {
            _engine.Reset();
            Logger.Log("[RR-ADAPTER] Engine reset.");
        }

        public IReadOnlyList<EngineMatch> GetMatches()
        {
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
            var rounds = _engine.GetRoundLabels();
            Logger.Log($"[RR-ADAPTER] Round order: {string.Join(", ", rounds)}");
            return rounds;
        }

        public void SetWinner(int matchId, Driver winner)
        {
            if (winner == null || IsBye(winner))
            {
                Logger.Log($"[RR-ADAPTER] Reject SetWinner(M{matchId}) → BYE/Null.");
                return;
            }

            _engine.SetWinner(matchId, winner);
            Logger.Log($"[RR-ADAPTER] Winner set for M{matchId} → {winner.Name}");
        }

        public bool HasWinner(int matchId) => _engine.HasWinner(matchId);

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

        private static bool IsBye(Driver d) =>
            d == null || string.Equals(d.Name?.Trim(), "BYE", System.StringComparison.OrdinalIgnoreCase);
    }
}
