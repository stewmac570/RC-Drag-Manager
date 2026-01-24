// ProLadder.Ladders.L17.cs
using System.Collections.Generic;
using System;

namespace RCDragManagerProd.Domain
{
    public partial class ProLadder
    {
        private static List<LadderMatch> GetLadder17()
        {
            return new List<LadderMatch>
            {
                // ROUND 1
                new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 0, RoundLabel = "R1" },
                new LadderMatch { MatchId = 2, Seed1 = 9, Seed2 = 10, RoundLabel = "R1" },
                new LadderMatch { MatchId = 3, Seed1 = 5, Seed2 = 14, RoundLabel = "R1" },
                new LadderMatch { MatchId = 4, Seed1 = 6, Seed2 = 13, RoundLabel = "R1" },
                new LadderMatch { MatchId = 5, Seed1 = 2, Seed2 = 17, RoundLabel = "R1" },
                new LadderMatch { MatchId = 6, Seed1 = 4, Seed2 = 15, RoundLabel = "R1" },
                new LadderMatch { MatchId = 7, Seed1 = 7, Seed2 = 12, RoundLabel = "R1" },
                new LadderMatch { MatchId = 8, Seed1 = 3, Seed2 = 16, RoundLabel = "R1" },
                new LadderMatch { MatchId = 9, Seed1 = 8, Seed2 = 11, RoundLabel = "R1" },

                // ROUND 2
                new LadderMatch { MatchId = 10, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "R2" },
                new LadderMatch { MatchId = 11, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "R2" },
                new LadderMatch { MatchId = 12, FromMatch1 = 5, Seed2 = 0, RoundLabel = "R2" }, // BYE
                new LadderMatch { MatchId = 13, FromMatch1 = 6, FromMatch2 = 7, RoundLabel = "R2" },
                new LadderMatch { MatchId = 14, FromMatch1 = 8, FromMatch2 = 9, RoundLabel = "R2" },

                // ROUND 3
                new LadderMatch { MatchId = 15, FromMatch1 = 10, FromMatch2 = 11, RoundLabel = "R3" },
                new LadderMatch { MatchId = 16, FromMatch1 = 12, FromMatch2 = 13, RoundLabel = "R3" },
                new LadderMatch { MatchId = 17, FromMatch1 = 14, Seed2 = 0, RoundLabel = "R3" }, // BYE

                // SEMIFINALS
                new LadderMatch { MatchId = 18, FromMatch1 = 15, Seed2 = 0, RoundLabel = "SF" }, // BYE
                new LadderMatch { MatchId = 19, FromMatch1 = 16, FromMatch2 = 17, RoundLabel = "SF" },

                // FINAL
                new LadderMatch { MatchId = 20, FromMatch1 = 18, FromMatch2 = 19, RoundLabel = "F" }
            };
        }
    }
}
