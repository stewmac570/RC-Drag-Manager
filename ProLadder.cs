using System.Collections.Generic;

namespace RCDragManagerProd
{
    public class ProLadder
    {
        public class LadderMatch
        {
            public int MatchId { get; set; }
            public int? Seed1 { get; set; }
            public int? Seed2 { get; set; }
            public int? FromMatch1 { get; set; }
            public int? FromMatch2 { get; set; }
            public string RoundLabel { get; set; }
        }

        public static List<LadderMatch> GetLadder(int fieldSize)
        {
            switch (fieldSize)
            {
                case 3: return GetLadder3();
                case 4: return GetLadder4();
                case 5: return GetLadder5();
                case 6: return GetLadder6();
                case 7: return GetLadder7();
                case 8: return GetLadder8();
                case 9: return GetLadder9();
                case 10: return GetLadder10();
                case 11: return GetLadder11();
                case 12: return GetLadder12();
                case 13: return GetLadder13();
                case 14: return GetLadder14();
                case 15: return GetLadder15();
                case 16: return GetLadder16();
                default: return new List<LadderMatch>();
            }
        }

        private static List<LadderMatch> GetLadder3()
        {
            return new List<LadderMatch>
            {
                new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 0, RoundLabel = "R1" },
                new LadderMatch { MatchId = 2, Seed1 = 2, Seed2 = 3, RoundLabel = "R1" },
                new LadderMatch { MatchId = 3, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "F" }
            };
        }

        private static List<LadderMatch> GetLadder4()
        {
            return new List<LadderMatch>
            {
                new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 4, RoundLabel = "R1" },
                new LadderMatch { MatchId = 2, Seed1 = 2, Seed2 = 3, RoundLabel = "R1" },
                new LadderMatch { MatchId = 3, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "F" }
            };
        }

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

        private static List<LadderMatch> GetLadder7()
        {
            return new List<LadderMatch>
            {
                new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 0, RoundLabel = "R1" },
                new LadderMatch { MatchId = 2, Seed1 = 4, Seed2 = 5, RoundLabel = "R1" },
                new LadderMatch { MatchId = 3, Seed1 = 2, Seed2 = 7, RoundLabel = "R1" },
                new LadderMatch { MatchId = 4, Seed1 = 3, Seed2 = 6, RoundLabel = "R1" },
                new LadderMatch { MatchId = 5, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "SF" },
                new LadderMatch { MatchId = 6, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "SF" },
                new LadderMatch { MatchId = 7, FromMatch1 = 5, FromMatch2 = 6, RoundLabel = "F" }
            };
        }

        private static List<LadderMatch> GetLadder8()
        {
            return new List<LadderMatch>
            {
                new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 8, RoundLabel = "R1" },
                new LadderMatch { MatchId = 2, Seed1 = 4, Seed2 = 5, RoundLabel = "R1" },
                new LadderMatch { MatchId = 3, Seed1 = 2, Seed2 = 7, RoundLabel = "R1" },
                new LadderMatch { MatchId = 4, Seed1 = 3, Seed2 = 6, RoundLabel = "R1" },
                new LadderMatch { MatchId = 5, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "SF" },
                new LadderMatch { MatchId = 6, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "SF" },
                new LadderMatch { MatchId = 7, FromMatch1 = 5, FromMatch2 = 6, RoundLabel = "F" }
            };
        }

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

        private static List<LadderMatch> GetLadder10()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 10, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 = 3, Seed2 = 8, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 4, Seed2 = 7, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 2, Seed2 = 9, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 5, Seed2 = 6, RoundLabel = "R1" },

        // ROUND 2
        new LadderMatch { MatchId = 6, FromMatch1 = 1, Seed2 = 0, RoundLabel = "R2" },
        new LadderMatch { MatchId = 7, FromMatch1 = 2, FromMatch2 = 3, RoundLabel = "R2" },
        new LadderMatch { MatchId = 8, FromMatch1 = 4, FromMatch2 = 5, RoundLabel = "R2" },

        // SEMIFINALS
        new LadderMatch { MatchId = 9, FromMatch1 = 6, FromMatch2 = 7, RoundLabel = "SF" },
        new LadderMatch { MatchId = 10, FromMatch1 = 8, Seed2 = 0, RoundLabel = "SF" },

        // FINAL
        new LadderMatch { MatchId = 11, FromMatch1 = 9, FromMatch2 = 10, RoundLabel = "F" }
    };
        }
        private static List<LadderMatch> GetLadder11()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 0, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 = 6, Seed2 = 7, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 3, Seed2 = 10, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 4, Seed2 = 9, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 2, Seed2 = 11, RoundLabel = "R1" },
        new LadderMatch { MatchId = 6, Seed1 = 5, Seed2 = 8, RoundLabel = "R1" },

        // ROUND 2
        new LadderMatch { MatchId = 7, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "R2" },
        new LadderMatch { MatchId = 8, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "R2" },
        new LadderMatch { MatchId = 9, FromMatch1 = 5, FromMatch2 = 6, RoundLabel = "R2" },

        // SEMIFINALS
        new LadderMatch { MatchId = 10, FromMatch1 = 7, FromMatch2 = 8, RoundLabel = "SF" },
        new LadderMatch { MatchId = 11, FromMatch1 = 9, Seed2 = 0, RoundLabel = "SF" },

        // FINAL
        new LadderMatch { MatchId = 12, FromMatch1 = 10, FromMatch2 = 11, RoundLabel = "F" }
    };
        }

        private static List<LadderMatch> GetLadder12()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 12, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 = 6, Seed2 = 7, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 2, Seed2 = 11, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 5, Seed2 = 8, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 3, Seed2 = 10, RoundLabel = "R1" },
        new LadderMatch { MatchId = 6, Seed1 = 4, Seed2 = 9, RoundLabel = "R1" },

        // ROUND 2
        new LadderMatch { MatchId = 7, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "R2" },
        new LadderMatch { MatchId = 8, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "R2" },
        new LadderMatch { MatchId = 9, FromMatch1 = 5, FromMatch2 = 6, RoundLabel = "R2" },

        // SEMIFINALS
        new LadderMatch { MatchId = 10, FromMatch1 = 7, Seed2 = 0, RoundLabel = "SF" },
        new LadderMatch { MatchId = 11, FromMatch1 = 8, FromMatch2 = 9, RoundLabel = "SF" },

        // FINAL
        new LadderMatch { MatchId = 12, FromMatch1 = 10, FromMatch2 = 11, RoundLabel = "F" }
    };
        }

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

        private static List<LadderMatch> GetLadder14()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 14, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 = 4, Seed2 = 11, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 5, Seed2 = 10, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 2, Seed2 = 13, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 7, Seed2 = 8, RoundLabel = "R1" },
        new LadderMatch { MatchId = 6, Seed1 = 3, Seed2 = 12, RoundLabel = "R1" },
        new LadderMatch { MatchId = 7, Seed1 = 6, Seed2 = 9, RoundLabel = "R1" },

        // ROUND 2
        new LadderMatch { MatchId = 8, FromMatch1 = 1, Seed2 = 0, RoundLabel = "R2" },
        new LadderMatch { MatchId = 9, FromMatch1 = 2, FromMatch2 = 3, RoundLabel = "R2" },
        new LadderMatch { MatchId = 10, FromMatch1 = 4, FromMatch2 = 5, RoundLabel = "R2" },
        new LadderMatch { MatchId = 11, FromMatch1 = 6, FromMatch2 = 7, RoundLabel = "R2" },

        // SEMIFINALS
        new LadderMatch { MatchId = 12, FromMatch1 = 8, FromMatch2 = 9, RoundLabel = "SF" },
        new LadderMatch { MatchId = 13, FromMatch1 = 10, FromMatch2 = 11, RoundLabel = "SF" },

        // FINAL
        new LadderMatch { MatchId = 14, FromMatch1 = 12, FromMatch2 = 13, RoundLabel = "F" }
    };
        }

        private static List<LadderMatch> GetLadder15()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 0, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 = 8, Seed2 = 9, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 4, Seed2 = 13, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 5, Seed2 = 12, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 2, Seed2 = 15, RoundLabel = "R1" },
        new LadderMatch { MatchId = 6, Seed1 = 7, Seed2 = 10, RoundLabel = "R1" },
        new LadderMatch { MatchId = 7, Seed1 = 3, Seed2 = 14, RoundLabel = "R1" },
        new LadderMatch { MatchId = 8, Seed1 = 6, Seed2 = 11, RoundLabel = "R1" },

        // ROUND 2
        new LadderMatch { MatchId = 9, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "R2" },
        new LadderMatch { MatchId = 10, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "R2" },
        new LadderMatch { MatchId = 11, FromMatch1 = 5, FromMatch2 = 6, RoundLabel = "R2" },
        new LadderMatch { MatchId = 12, FromMatch1 = 7, FromMatch2 = 8, RoundLabel = "R2" },

        // SEMIFINALS
        new LadderMatch { MatchId = 13, FromMatch1 = 9, FromMatch2 = 10, RoundLabel = "SF" },
        new LadderMatch { MatchId = 14, FromMatch1 = 11, FromMatch2 = 12, RoundLabel = "SF" },

        // FINAL
        new LadderMatch { MatchId = 15, FromMatch1 = 13, FromMatch2 = 14, RoundLabel = "F" }
    };
        }
        private static List<LadderMatch> GetLadder16()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 16, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 = 8, Seed2 = 9, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 4, Seed2 = 13, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 5, Seed2 = 12, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 2, Seed2 = 15, RoundLabel = "R1" },
        new LadderMatch { MatchId = 6, Seed1 = 7, Seed2 = 10, RoundLabel = "R1" },
        new LadderMatch { MatchId = 7, Seed1 = 3, Seed2 = 14, RoundLabel = "R1" },
        new LadderMatch { MatchId = 8, Seed1 = 6, Seed2 = 11, RoundLabel = "R1" },

        // ROUND 2
        new LadderMatch { MatchId = 9, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "R2" },
        new LadderMatch { MatchId = 10, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "R2" },
        new LadderMatch { MatchId = 11, FromMatch1 = 5, FromMatch2 = 6, RoundLabel = "R2" },
        new LadderMatch { MatchId = 12, FromMatch1 = 7, FromMatch2 = 8, RoundLabel = "R2" },

        // SEMIFINALS
        new LadderMatch { MatchId = 13, FromMatch1 = 9, FromMatch2 = 10, RoundLabel = "SF" },
        new LadderMatch { MatchId = 14, FromMatch1 = 11, FromMatch2 = 12, RoundLabel = "SF" },

        // FINAL
        new LadderMatch { MatchId = 15, FromMatch1 = 13, FromMatch2 = 14, RoundLabel = "F" }
    };
        }


    }
}
