// ProLadder.Ladders.L13.cs
using System.Collections.Generic;
using System;

namespace RCDragManagerProd.Domain
{
    public partial class ProLadder
    {
        private static List<LadderMatch> GetLadder13()
        {
            return new List<LadderMatch>
            {
                // ROUND 1
                new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 0, RoundLabel = "R1" },
                new LadderMatch { MatchId = 2, Seed1 = 7, Seed2 = 8, RoundLabel = "R1" },
                new LadderMatch { MatchId = 3, Seed1 = 4, Seed2 = 11, RoundLabel = "R1" },
                new LadderMatch { MatchId = 4, Seed1 = 5, Seed2 = 10, RoundLabel = "R1" },
                new LadderMatch { MatchId = 5, Seed1 = 2, Seed2 = 13, RoundLabel = "R1" },
                new LadderMatch { MatchId = 6, Seed1 = 3, Seed2 = 12, RoundLabel = "R1" },
                new LadderMatch { MatchId = 7, Seed1 = 6, Seed2 = 9, RoundLabel = "R1" },

                // ROUND 2
                new LadderMatch { MatchId = 8, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "R2" },
                new LadderMatch { MatchId = 9, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "R2" },
                new LadderMatch { MatchId = 10, FromMatch1 = 5, Seed2 = 0, RoundLabel = "R2" },
                new LadderMatch { MatchId = 11, FromMatch1 = 6, FromMatch2 = 7, RoundLabel = "R2" },

                // SEMIFINALS
                new LadderMatch { MatchId = 12, FromMatch1 = 8, FromMatch2 = 9, RoundLabel = "SF" },
                new LadderMatch { MatchId = 13, FromMatch1 = 10, FromMatch2 = 11, RoundLabel = "SF" },

                // FINAL
                new LadderMatch { MatchId = 14, FromMatch1 = 12, FromMatch2 = 13, RoundLabel = "F" }
            };
        }
    }
}
