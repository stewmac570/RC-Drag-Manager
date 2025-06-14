using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd
{
    public class MatchResult
    {
        private Dictionary<int, (Driver Winner, Driver Loser)> results = new Dictionary<int, (Driver, Driver)>();


        public void SetWinner(int matchId, Driver winner, Driver loser)
        {
            results[matchId] = (winner, loser);
        }


        public Driver GetWinner(int matchId)
        {
            return results.ContainsKey(matchId) ? results[matchId].Winner : null;
        }


        public bool HasResult(int matchId)
        {
            return results.ContainsKey(matchId);

        }

        public bool IsMatchResolved(int matchId)
        {
            return results.ContainsKey(matchId);

        }

        public void ClearFromMatch(int matchId)
        {
            var keysToRemove = results.Keys.Where(k => k >= matchId).ToList();
            foreach (var key in keysToRemove)
            {
                results.Remove(key);
            }
        }

        public bool IsTournamentComplete(IReadOnlyList<ProLadder.LadderMatch> bracketMatches)
        {
            var finalMatch = bracketMatches.FirstOrDefault(m => m.RoundLabel == "F");
            if (finalMatch != null)
            {
                return results.ContainsKey(finalMatch.MatchId);

            }
            return false;
        }
        public Driver GetLoser(int matchId)
        {
            return results.ContainsKey(matchId) ? results[matchId].Loser : null;
        }

    }
}
