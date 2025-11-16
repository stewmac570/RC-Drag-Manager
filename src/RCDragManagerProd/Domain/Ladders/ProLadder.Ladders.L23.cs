// ProLadder.Ladders.L23.cs
using System.Collections.Generic;
using System;

namespace RCDragManagerProd.Domain
{
    public partial class ProLadder
    {
        public static List<LadderMatch> GetLadder23()
        {
            return new List<LadderMatch>
            {
                // ROUND 1
                new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 0, RoundLabel = "R1" },
                new LadderMatch { MatchId = 2, Seed1 = 12, Seed2 = 13, RoundLabel = "R1" },
                new LadderMatch { MatchId = 3, Seed1 = 6, Seed2 = 19, RoundLabel = "R1" },
                new LadderMatch { MatchId = 4, Seed1 = 7, Seed2 = 18, RoundLabel = "R1" },
                new LadderMatch { MatchId = 5, Seed1 = 3, Seed2 = 22, RoundLabel = "R1" },
                new LadderMatch { MatchId = 6, Seed1 = 10, Seed2 = 15, RoundLabel = "R1" },
                new LadderMatch { MatchId = 7, Seed1 = 4, Seed2 = 21, RoundLabel = "R1" },
                new LadderMatch { MatchId = 8, Seed1 = 9, Seed2 = 16, RoundLabel = "R1" },
                new LadderMatch { MatchId = 9, Seed1 = 2, Seed2 = 23, RoundLabel = "R1" },
                new LadderMatch { MatchId = 10, Seed1 = 11, Seed2 = 14, RoundLabel = "R1" },
                new LadderMatch { MatchId = 11, Seed1 = 5, Seed2 = 20, RoundLabel = "R1" },
                new LadderMatch { MatchId = 12, Seed1 = 8, Seed2 = 17, RoundLabel = "R1" },

                // ROUND 2
                new LadderMatch { MatchId = 13, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "R2" },
                new LadderMatch { MatchId = 14, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "R2" },
                new LadderMatch { MatchId = 15, FromMatch1 = 5,  FromMatch2 = 6, RoundLabel = "R2" },
                new LadderMatch { MatchId = 16, FromMatch1 = 7, FromMatch2 = 8, RoundLabel = "R2" },
                new LadderMatch { MatchId = 17, FromMatch1 = 9, FromMatch2 = 10, RoundLabel = "R2" },
                new LadderMatch { MatchId = 18, FromMatch1 = 11, FromMatch2 = 12, RoundLabel = "R2" },

                // ROUND 3
                new LadderMatch { MatchId = 19, FromMatch1 = 13, FromMatch2 = 14, RoundLabel = "R3" },
                new LadderMatch { MatchId = 20, FromMatch1 = 15, FromMatch2 = 16, RoundLabel = "R3" },
                new LadderMatch { MatchId = 21, FromMatch1 = 17, FromMatch2 = 18, RoundLabel = "R3" },

                // SEMIFINALS
                new LadderMatch { MatchId = 22, FromMatch1 = 19, FromMatch2 = 20, RoundLabel = "SF" },
                new LadderMatch { MatchId = 23, FromMatch1 = 21, Seed2 = 0, RoundLabel = "SF" }, // BYE

                // FINAL
                new LadderMatch { MatchId = 24, FromMatch1 = 22, FromMatch2 = 23, RoundLabel = "F" }
            };
        }
    }
}
