using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RCDragManagerProd
{
    public class RandomMatchEngine
    {
        private List<RandomMatch> bracketMatches = new List<RandomMatch>();
        private MatchResult results = new MatchResult();

        public void LoadMatches(List<RandomMatch> matches)
        {
            bracketMatches = matches;
        }

        public IReadOnlyList<RandomMatch> GetMatches()
        {
            return bracketMatches;
        }

        public void SetWinner(int matchId, Driver winner)
        {
            var loser = GetLoserFromMatch(matchId, winner); // ⬅️ You'll need to determine the loser
            results.SetWinner(matchId, winner, loser);
        }
        private Driver GetLoserFromMatch(int matchId, Driver winner)
        {
            var match = bracketMatches.FirstOrDefault(m => m.MatchId == matchId);
            if (match == null) return null;

            if (match.Seed1 == winner) return match.Seed2;
            if (match.Seed2 == winner) return match.Seed1;

            return null;
        }



        public Driver GetWinner(int matchId)
        {
            return results.GetWinner(matchId);
        }

        public bool HasWinner(int matchId)
        {
            return results.HasResult(matchId);
        }

        public (Driver, Driver) ResolveDrivers(RandomMatch match)
        {
            Driver d1 = match.Seed1 ?? ResolveFrom(match.FromMatch1);
            Driver d2 = match.Seed2 ?? ResolveFrom(match.FromMatch2);

            if (d1 == null) d1 = new Driver { Name = "TBD" };
            if (d2 == null) d2 = new Driver { Name = "TBD" };

            return (d1, d2);
        }

        private Driver ResolveFrom(int? fromMatchId)
        {
            if (!fromMatchId.HasValue)
                return null;

            return results.HasResult(fromMatchId.Value)
                ? results.GetWinner(fromMatchId.Value)
                : null;
        }

        public bool IsTournamentComplete()
        {
            RandomMatch final = bracketMatches.LastOrDefault();
            return final != null && results.HasResult(final.MatchId);
        }

        public void RewindToMatch(int matchId)
        {
            results.ClearFromMatch(matchId);
        }
    }
}
