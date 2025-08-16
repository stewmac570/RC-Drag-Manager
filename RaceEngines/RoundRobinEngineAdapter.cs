using System.Collections.Generic;
using System.Linq;
using System;
using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;
using RCDragManagerProd.Logging; 
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.RoundRobinMode;
using RCDragManagerProd.Repositories;




namespace RCDragManagerProd.RaceEngines
{
    public class RoundRobinEngineAdapter : IRaceEngine
    {
        private readonly RoundRobinEngine _engine = new RoundRobinEngine();

        public void LoadDrivers(List<Driver> drivers)
        {
            Logger.Log($"[RR-ADAPTER] Loading {drivers?.Count ?? 0} driver(s) into engine...");
            _engine.LoadDrivers(drivers);
            Logger.Log("[RR-ADAPTER] Driver load complete.");
        }

        public void GenerateBracket()
        {
            Logger.Log("[RR-ADAPTER] Generating Round Robin bracket...");
            _engine.GenerateMatches();
            Logger.Log($"[RR-ADAPTER] Bracket generated with {_engine.GetMatches().Count} match(es).");
        }

        public void Reset()
        {
            Logger.Log("[RR-ADAPTER] Resetting engine state...");
            _engine.Reset();
            Logger.Log("[RR-ADAPTER] Engine reset complete.");
        }

        public IReadOnlyList<EngineMatch> GetMatches()
        {
            Logger.Log("[RR-ADAPTER] Retrieving matches from engine...");
            var matches = _engine.GetMatches()
                                 .Select(m => new EngineMatch
                                 {
                                     MatchId = m.MatchId,
                                     Driver1 = m.D1,   // use D1 not Driver1
                                     Driver2 = m.D2,   // use D2 not Driver2
                                     RoundLabel = m.RoundLabel,
                                     HasResult = _engine.HasWinner(m.MatchId)
                                 })
                                 .ToList();

            Logger.Log($"[RR-ADAPTER] Retrieved {matches.Count} match(es).");
            foreach (var m in matches)
            {
                Logger.Log($"    M{m.MatchId} {m.RoundLabel} → " +
                           $"{(m.Driver1?.Name ?? "(BYE)")} vs {(m.Driver2?.Name ?? "(BYE)")}, " +
                           $"HasResult={m.HasResult}");
            }

            return matches;
        }

        public List<(Driver Driver, int Wins)> GetStandings()
        {
            Logger.Log("[RR-ADAPTER] Building standings from match results...");
            var standings = _engine.GetMatches()
                .Where(m => _engine.HasWinner(m.MatchId))
                .Select(m => _engine.GetWinner(m.MatchId))
                .Where(w => w != null)
                .GroupBy(d => d.Id)
                .Select(g =>
                {
                    var driver = _engine.GetAllDrivers().FirstOrDefault(d => d.Id == g.Key);
                    int wins = g.Count();
                    Logger.Log($"[RR-ADAPTER] Driver {driver?.Name} (ID {g.Key}) has {wins} win(s).");
                    return (Driver: driver, Wins: wins);
                })
                .Where(x => x.Driver != null)
                .OrderByDescending(x => x.Wins)
                .ToList();

            Logger.Log("[RR-ADAPTER] Standings build complete.");
            return standings;
        }

        public IReadOnlyList<string> GetRoundOrder()
        {
            Logger.Log("[RR-ADAPTER] Retrieving round order from engine...");
            var rounds = _engine.GetRoundLabels();
            Logger.Log($"[RR-ADAPTER] Round order: {string.Join(", ", rounds)}");
            return rounds;
        }

        public void SetWinner(int matchId, Driver winner)
        {
            Logger.Log($"[RR-ADAPTER] Setting winner for match {matchId} → {winner?.Name ?? "null"}");
            _engine.SetWinner(matchId, winner);
            Logger.Log($"[RR-ADAPTER] Winner set for match {matchId}.");
        }

        public bool HasWinner(int matchId)
        {
            bool has = _engine.HasWinner(matchId);
            Logger.Log($"[RR-ADAPTER] Match {matchId} has winner? {has}");
            return has;
        }

        public bool IsTournamentComplete()
        {
            bool complete = _engine.IsTournamentComplete();
            Logger.Log($"[RR-ADAPTER] Tournament complete? {complete}");
            return complete;
        }

        public List<Driver> GetTopRankedDrivers(int count)
        {
            Logger.Log($"[RR-ADAPTER] Retrieving top {count} ranked driver(s)...");
            var top = _engine.GetTopRankedDrivers(count);
            Logger.Log("[RR-ADAPTER] Top drivers: " +
                       (top.Count == 0 ? "(none)" : string.Join(", ", top.Select(d => d.Name))));
            return top;
        }
    }
}
