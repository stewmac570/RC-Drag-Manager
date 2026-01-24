// ProLadder.Ladders.L04.cs
using System.Collections.Generic;
using System;

namespace RCDragManagerProd.Domain
{
    public partial class ProLadder
    {
        private static List<LadderMatch> GetLadder4()
        {
            return new List<LadderMatch>
            {
                new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 4, RoundLabel = "SF" },
                new LadderMatch { MatchId = 2, Seed1 = 2, Seed2 = 3, RoundLabel = "SF" },
                new LadderMatch { MatchId = 3, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "F" }
            };
        }
    }
}
