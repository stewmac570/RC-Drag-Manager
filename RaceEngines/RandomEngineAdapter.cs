using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd.RaceEngines
{
    public class RandomEngineAdapter : IRaceEngine
    {
        private readonly RandomMatchEngine _engine = new RandomMatchEngine();

        public void LoadDrivers(List<Driver> drivers) => _engine.LoadDrivers(drivers);

        public void GenerateBracket() => _engine.GenerateBracket();

        public void Reset() => _engine.Reset();

        public IReadOnlyList<EngineMatch> GetMatches()
        {
            return _engine.GetMatches()
                          .Select(m => new EngineMatch
                          {
                              MatchId = m.MatchId,
                              Driver1 = m.Seed1,       // ✅ correct
                              Driver2 = m.Seed2,
                              RoundLabel = m.RoundLabel,
                              FromMatch1 = m.FromMatch1,
                              FromMatch2 = m.FromMatch2,
                              HasResult = _engine.HasWinner(m.MatchId)
                          })
                          .ToList();
        }

        public IReadOnlyList<string> GetRoundOrder() => _engine.GetRoundOrder();

        public void SetWinner(int matchId, Driver winner) => _engine.SetWinner(matchId, winner);

        public bool HasWinner(int matchId) => _engine.HasWinner(matchId);
    }
}
