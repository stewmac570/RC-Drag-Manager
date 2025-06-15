using System;
using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd
{
    public class MatchEngine
    {
        private List<Driver> allDrivers = new List<Driver>();
        private List<ProLadder.LadderMatch> bracketMatches = new List<ProLadder.LadderMatch>();
        private Dictionary<int, Driver> seedMap = new Dictionary<int, Driver>();

        public MatchResult Results { get; private set; } = new MatchResult();

        public void Initialize(List<Driver> drivers)
        {
            allDrivers = drivers.OrderBy(d => d.QualTime).ToList();

            for (int i = 0; i < allDrivers.Count; i++)
            {
                allDrivers[i].Seed = i + 1;
            }

            bracketMatches = ProLadder.GetLadder(allDrivers.Count);
            seedMap = allDrivers
                .Where(d => d.Seed.HasValue)
                .ToDictionary(d => d.Seed.Value, d => d);
        }

        public void SetWinner(int matchId, Driver winner, Driver loser)
        {
            Results.SetWinner(matchId, winner, loser);
        }
        public void SetWinner(int matchId, Driver winner)
        {
            Results.SetWinner(matchId, winner, null);
        }

        public IReadOnlyList<ProLadder.LadderMatch> GetBracketMatches()
        {
            return bracketMatches;
        }

        public bool IsTournamentComplete()
        {
            return Results.IsTournamentComplete(bracketMatches);
        }

        public void AdvanceToNextRound()
        {
            // Nothing needed here anymore.
        }

public (Driver, Driver) ResolveDriversForMatch(ProLadder.LadderMatch match)
{
    var d1 = ResolveDriver(match.Seed1, match.FromMatch1);
    var d2 = ResolveDriver(match.Seed2, match.FromMatch2);
    return (d1 ?? new Driver { Name = "BYE" }, d2 ?? new Driver { Name = "BYE" });
}

        

        private Driver ResolveDriver(int? seed, int? fromMatch)
        {
            if (seed.HasValue && seed.Value > 0)
                return seedMap.ContainsKey(seed.Value) ? seedMap[seed.Value] : null;

            if (fromMatch.HasValue && Results.HasResult(fromMatch.Value))
                return Results.GetWinner(fromMatch.Value);

            return null;
        }

        public void RewindToMatchRound(int matchId)
        {
            Results.ClearFromMatch(matchId);
        }
    }
}
