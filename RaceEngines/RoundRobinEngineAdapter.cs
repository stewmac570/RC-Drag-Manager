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


        public IReadOnlyList<string> GetRoundOrder() => _engine.GetRoundLabels();

        public void SetWinner(int matchId, Driver winner) => _engine.SetWinner(matchId, winner);

        public bool HasWinner(int matchId) => _engine.HasWinner(matchId);
    }

}
