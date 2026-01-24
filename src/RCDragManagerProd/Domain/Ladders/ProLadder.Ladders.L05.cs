// ProLadder.Ladders.L05.cs
using System.Collections.Generic;
using System;

namespace RCDragManagerProd.Domain
{
    public partial class ProLadder
    {
        private static List<LadderMatch> GetLadder5()
        {
            return new List<LadderMatch>
            {
                new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 0, RoundLabel = "R1" },
                new LadderMatch { MatchId = 2, Seed1 = 3, Seed2 = 4, RoundLabel = "R1" },
                new LadderMatch { MatchId = 3, Seed1 = 2, Seed2 = 5, RoundLabel = "R1" },
                new LadderMatch { MatchId = 4, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "SF" },
                new LadderMatch { MatchId = 5, FromMatch1 = 3, Seed2 = 0, RoundLabel = "SF" },
                new LadderMatch { MatchId = 6, FromMatch1 = 4, FromMatch2 = 5, RoundLabel = "F" }
            };
        }
    }
}
