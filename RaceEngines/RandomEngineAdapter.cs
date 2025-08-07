using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd;                  // for static Logger

namespace RCDragManagerProd.RaceEngines
{
    public class RandomEngineAdapter : IRaceEngine
    {
        // ────────────────────  STATE  ────────────────────
        private readonly RandomMatchEngine _engine;

        // ────────────────────  CTORS  ────────────────────

        /// <summary>
        /// Default constructor – creates its own internal RandomMatchEngine.
        /// </summary>
        public RandomEngineAdapter()
        {
            _engine = new RandomMatchEngine();
            Logger.Log("RandomEngineAdapter default ctor — new internal engine created");
        }

        /// <summary>
        /// Allows an externally prepared RandomMatchEngine (e.g. losers-bracket build) to be wrapped.
        /// </summary>
        public RandomEngineAdapter(RandomMatchEngine engine)
        {
            _engine = engine ?? new RandomMatchEngine();
            Logger.Log("RandomEngineAdapter ctor — external engine supplied");
        }

        // ────────────────────  PUBLIC API  ────────────────────

        public void LoadDrivers(List<Driver> drivers) => _engine.LoadDrivers(drivers);

        public void GenerateBracket() => _engine.GenerateBracket();

        public void Reset() => _engine.Reset();

        /// <summary>
        /// Inject a pre-built match list (used by the losers-bracket flow).
        /// </summary>
        public void InjectMatches(List<RandomMatch> matches)
        {
            Logger.Log($"Injecting {matches?.Count ?? 0} matches into RandomEngineAdapter");
            _engine.LoadMatches(matches);
        }

        public IReadOnlyList<EngineMatch> GetMatches()
        {
            return _engine.GetMatches()
                          .Select(m => new EngineMatch
                          {
                              MatchId = m.MatchId,
                              Driver1 = m.Seed1,
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

        public Driver GetWinner()
        {
            var finalMatch = _engine.GetMatches()
                                    .FirstOrDefault(m => m.RoundLabel?.ToLower().Contains("final") == true);

            if (finalMatch == null)
            {
                Logger.Log("❌ RandomEngineAdapter.GetWinner → no match containing 'final' found");
                return null;
            }

            var winner = _engine.GetWinner(finalMatch.MatchId);

            if (winner != null)
                Logger.Log($"🏆 RandomEngineAdapter.GetWinner → {winner.Name} (M{finalMatch.MatchId})");
            else
                Logger.Log($"⚠️ RandomEngineAdapter.GetWinner → Match {finalMatch.MatchId} has no winner");

            return winner;
        }



    }
}
