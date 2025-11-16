// ProLadder.Ladders.L09.cs
using System.Collections.Generic;
using System;

namespace RCDragManagerProd.Domain
{
    public partial class ProLadder
    {
        private static List<LadderMatch> GetLadder9()
        {
            return new List<LadderMatch>
            {
                new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 0, RoundLabel = "R1" },
                new LadderMatch { MatchId = 2, Seed1 = 5, Seed2 = 6, RoundLabel = "R1" },
                new LadderMatch { MatchId = 3, Seed1 = 2, Seed2 = 9, RoundLabel = "R1" },
                new LadderMatch { MatchId = 4, Seed1 = 3, Seed2 = 8, RoundLabel = "R1" },
                new LadderMatch { MatchId = 5, Seed1 = 4, Seed2 = 7, RoundLabel = "R1" },

                new LadderMatch { MatchId = 6, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "R2" },
                new LadderMatch { MatchId = 7, FromMatch1 = 3, Seed2 = 0, RoundLabel = "R2" },
                new LadderMatch { MatchId = 8, FromMatch1 = 4, FromMatch2 = 5, RoundLabel = "R2" },

                new LadderMatch { MatchId = 9, FromMatch1 = 6, FromMatch2 = 7, RoundLabel = "SF" },
                new LadderMatch { MatchId = 10, FromMatch1 = 8, Seed2 = 0, RoundLabel = "SF" },

                new LadderMatch { MatchId = 11, FromMatch1 = 9, FromMatch2 = 10, RoundLabel = "F" }
            };
        }
    }
}
