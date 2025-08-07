using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd.RaceEngines
{
    public class RoundRobinEngineAdapter : IRaceEngine
    {
        private readonly RoundRobinEngine _engine = new RoundRobinEngine();

        public void LoadDrivers(List<Driver> drivers) => _engine.LoadDrivers(drivers);

        public void GenerateBracket() => _engine.GenerateMatches();

        public void Reset() => _engine.Reset();

        public IReadOnlyList<EngineMatch> GetMatches()
        {
            return _engine.GetMatches()
                          .Select(m => new EngineMatch
                          {
                              MatchId = m.MatchId,
                              Driver1 = m.D1,   // <-- Use D1 not Driver1
                              Driver2 = m.D2,   // <-- Use D2 not Driver2
                              RoundLabel = m.RoundLabel,
                              HasResult = _engine.HasWinner(m.MatchId)
                          })
                          .ToList();
        }

        public List<(Driver Driver, int Wins)> GetStandings()
        {
            Logger.Log("[RR] Building standings from match results...");

            var standings = _engine.GetMatches()
                .Where(m => _engine.HasWinner(m.MatchId))
                .Select(m => _engine.GetWinner(m.MatchId))
                .Where(w => w != null)
                .GroupBy(d => d.Id)
                .Select(g =>
                {
                    var driver = _engine.GetAllDrivers().FirstOrDefault(d => d.Id == g.Key);
                    int wins = g.Count();
                    Logger.Log($"[RR] Driver {driver?.Name} (ID {g.Key}) has {wins} win(s).");
                    return (Driver: driver, Wins: wins);
                })
                .Where(x => x.Driver != null)
                .OrderByDescending(x => x.Wins)
                .ToList();

            Logger.Log("[RR] Standings built.");
            return standings;
        }




        public IReadOnlyList<string> GetRoundOrder() => _engine.GetRoundLabels();

        public void SetWinner(int matchId, Driver winner) => _engine.SetWinner(matchId, winner);

        public bool HasWinner(int matchId) => _engine.HasWinner(matchId);

        public bool IsTournamentComplete() => _engine.IsTournamentComplete();

        public List<Driver> GetTopRankedDrivers(int count)
    => _engine.GetTopRankedDrivers(count);
    
    }
}
