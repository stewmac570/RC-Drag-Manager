// ProLadder.Ladders.L06.cs
using System.Collections.Generic;
using System;

namespace RCDragManagerProd.Domain
{
    public partial class ProLadder
    {
        private static List<LadderMatch> GetLadder6()
        {
            return new List<LadderMatch>
            {
                new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 6, RoundLabel = "R1" },
                new LadderMatch { MatchId = 2, Seed1 = 2, Seed2 = 5, RoundLabel = "R1" },
                new LadderMatch { MatchId = 3, Seed1 = 3, Seed2 = 4, RoundLabel = "R1" },
                new LadderMatch { MatchId = 4, FromMatch1 = 1, Seed2 = 0, RoundLabel = "SF" },
                new LadderMatch { MatchId = 5, FromMatch1 = 2, FromMatch2 = 3, RoundLabel = "SF" },
                new LadderMatch { MatchId = 6, FromMatch1 = 4, FromMatch2 = 5, RoundLabel = "F" }
            };
        }
    }
}
