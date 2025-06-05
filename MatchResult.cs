using System.Collections.Generic;
using System.Linq;

namespace RCDragManager
{
    public class MatchResult
    {
        private Dictionary<int, Driver> winners = new Dictionary<int, Driver>();

        public void SetWinner(int matchId, Driver winner)
        {
            winners[matchId] = winner;
        }

        public Driver GetWinner(int matchId)
        {
            return winners.ContainsKey(matchId) ? winners[matchId] : null;
        }

        public bool HasResult(int matchId)
        {
            return winners.ContainsKey(matchId);
        }

        public bool IsMatchResolved(int matchId)
        {
            return winners.ContainsKey(matchId);
        }

        public void ClearFromMatch(int matchId)
        {
            var keysToRemove = winners.Keys.Where(k => k >= matchId).ToList();
            foreach (var key in keysToRemove)
            {
                winners.Remove(key);
            }
        }

        public bool IsTournamentComplete(IReadOnlyList<ProLadder.LadderMatch> bracketMatches)
        {
            var finalMatch = bracketMatches.FirstOrDefault(m => m.RoundLabel == "F");
            if (finalMatch != null)
            {
                return winners.ContainsKey(finalMatch.MatchId);
            }
            return false;
        }
    }
}
