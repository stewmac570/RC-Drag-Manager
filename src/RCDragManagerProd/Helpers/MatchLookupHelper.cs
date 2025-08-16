using System;
using System.IO;
using System.Linq;

using RCDragManagerProd.Domain;        // only if a helper touches models


namespace RCDragManagerProd.Helpers
{
    public static class MatchLookupHelper
    {
        public static ProLadder.LadderMatch FindMatchInSession(RaceSession session, int matchId)
        {
            // This assumes only ProLadder is used — update later if RandomBracket needed
            var matches = ProLadder.GetLadder(session.DriverEntries.Count);
            return matches.FirstOrDefault(m => m.MatchId == matchId);
        }
    }
}
