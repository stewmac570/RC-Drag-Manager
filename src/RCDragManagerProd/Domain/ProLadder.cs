using System.Collections.Generic;
using System;



namespace RCDragManagerProd.Domain
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
                case 17: return GetLadder17();
                case 18: return GetLadder18();
                case 19: return GetLadder19();
                case 20: return GetLadder20();
                case 21: return GetLadder21();
                case 22: return GetLadder22();
                case 23: return GetLadder23();
                case 24: return GetLadder24();
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
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 4, RoundLabel = "SF" },
        new LadderMatch { MatchId = 2, Seed1 = 2, Seed2 = 3, RoundLabel = "SF" },
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

        private static List<LadderMatch> GetLadder18()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 8, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 = 5, Seed2 = 14, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 6, Seed2 = 13, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 2, Seed2 = 17, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 9, Seed2 = 10, RoundLabel = "R1" },
        new LadderMatch { MatchId = 6, Seed1 = 3, Seed2 = 16, RoundLabel = "R1" },
        new LadderMatch { MatchId = 7, Seed1 = 8, Seed2 = 11, RoundLabel = "R1" },
        new LadderMatch { MatchId = 8, Seed1 = 4, Seed2 = 15, RoundLabel = "R1" },
        new LadderMatch { MatchId = 9, Seed1 = 7, Seed2 = 12, RoundLabel = "R1" },

        // ROUND 2
        new LadderMatch { MatchId = 10, FromMatch1 = 1, Seed2 = 0, RoundLabel = "R2" }, // BYE
        new LadderMatch { MatchId = 11, FromMatch1 = 2, FromMatch2 = 3, RoundLabel = "R2" },
        new LadderMatch { MatchId = 12, FromMatch1 = 4, FromMatch2 = 5, RoundLabel = "R2" },
        new LadderMatch { MatchId = 13, FromMatch1 = 6, FromMatch2 = 7, RoundLabel = "R2" },
        new LadderMatch { MatchId = 14, FromMatch1 = 8, FromMatch2 = 9, RoundLabel = "R2" },

        // ROUND 3
        new LadderMatch { MatchId = 15, FromMatch1 = 10, FromMatch2 = 11, RoundLabel = "R3" },
        new LadderMatch { MatchId = 16, FromMatch1 = 12, Seed2 = 0, RoundLabel = "R3" }, // BYE
        new LadderMatch { MatchId = 17, FromMatch1 = 13, FromMatch2 = 14, RoundLabel = "R3" },

        // SEMIFINALS
        new LadderMatch { MatchId = 18, FromMatch1 = 15, FromMatch2 = 16, RoundLabel = "SF" },
        new LadderMatch { MatchId = 19, FromMatch1 = 17, Seed2 = 0, RoundLabel = "SF" }, // BYE

        // FINAL
        new LadderMatch { MatchId = 20, FromMatch1 = 18, FromMatch2 = 19, RoundLabel = "F" }
    };
        }

        public static List<LadderMatch> GetLadder19()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 0, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 = 10, Seed2 = 11, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 5, Seed2 = 16, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 6, Seed2 = 15, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 2, Seed2 = 19, RoundLabel = "R1" },
        new LadderMatch { MatchId = 6, Seed1 = 9, Seed2 = 12, RoundLabel = "R1" },
        new LadderMatch { MatchId = 7, Seed1 = 3, Seed2 = 18, RoundLabel = "R1" },
        new LadderMatch { MatchId = 8, Seed1 = 8, Seed2 = 13, RoundLabel = "R1" },
        new LadderMatch { MatchId = 9, Seed1 = 4, Seed2 = 17, RoundLabel = "R1" },
        new LadderMatch { MatchId = 10, Seed1 = 7, Seed2 = 14, RoundLabel = "R1" }, // BYE

        // ROUND 2
        new LadderMatch { MatchId = 11, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "R2" },
        new LadderMatch { MatchId = 12, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "R2" },
        new LadderMatch { MatchId = 13, FromMatch1 = 5, FromMatch2 = 6, RoundLabel = "R2" },
        new LadderMatch { MatchId = 14, FromMatch1 = 7, FromMatch2 = 8, RoundLabel = "R2" },
        new LadderMatch { MatchId = 15, FromMatch1 = 9, FromMatch2 = 10, RoundLabel = "R2" },

        // ROUND 3
        new LadderMatch { MatchId = 16, FromMatch1 = 11, FromMatch2 = 12, RoundLabel = "R3" },
        new LadderMatch { MatchId = 17, FromMatch1 = 13, Seed2 = 0, RoundLabel = "R3" },
        new LadderMatch { MatchId = 18, FromMatch1 = 14, FromMatch2 = 15, RoundLabel = "R3" }, // BYE

        // SEMIFINALS
        new LadderMatch { MatchId = 19, FromMatch1 = 16, FromMatch2 = 17, RoundLabel = "SF" },
        new LadderMatch { MatchId = 20, FromMatch1 = 18, Seed2 = 0, RoundLabel = "SF" }, // BYE

        // FINAL
        new LadderMatch { MatchId = 21, FromMatch1 = 19, FromMatch2 = 20, RoundLabel = "F" }
    };
        }

        public static List<LadderMatch> GetLadder20()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 20, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 = 10, Seed2 = 11, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 3, Seed2 = 18, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 8, Seed2 = 13, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 4, Seed2 = 17, RoundLabel = "R1" },
        new LadderMatch { MatchId = 6, Seed1 = 7, Seed2 = 14, RoundLabel = "R1" },
        new LadderMatch { MatchId = 7, Seed1 = 2, Seed2 = 19, RoundLabel = "R1" },
        new LadderMatch { MatchId = 8, Seed1 = 9, Seed2 = 12, RoundLabel = "R1" },
        new LadderMatch { MatchId = 9, Seed1 = 5, Seed2 = 16, RoundLabel = "R1" },
        new LadderMatch { MatchId = 10, Seed1 = 6, Seed2 = 15, RoundLabel = "R1" }, 

        // ROUND 2
        new LadderMatch { MatchId = 11, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "R2" },
        new LadderMatch { MatchId = 12, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "R2" },
        new LadderMatch { MatchId = 13, FromMatch1 = 5, FromMatch2 = 6, RoundLabel = "R2" },
        new LadderMatch { MatchId = 14, FromMatch1 = 7, FromMatch2 = 8, RoundLabel = "R2" },
        new LadderMatch { MatchId = 15, FromMatch1 = 9, FromMatch2 = 10, RoundLabel = "R2" },

        // ROUND 3
        new LadderMatch { MatchId = 16, FromMatch1 = 11, Seed2 = 0, RoundLabel = "R3" },
        new LadderMatch { MatchId = 17, FromMatch1 = 12, FromMatch2 = 13, RoundLabel = "R3" },
        new LadderMatch { MatchId = 18, FromMatch1 = 14, FromMatch2 = 15, RoundLabel = "R3" },

        // SEMIFINALS
        new LadderMatch { MatchId = 19, FromMatch1 = 16, FromMatch2 = 17, RoundLabel = "SF" },
        new LadderMatch { MatchId = 20, FromMatch1 = 18, Seed2 = 0, RoundLabel = "SF" }, // BYE

        // FINAL
        new LadderMatch { MatchId = 21, FromMatch1 = 19, FromMatch2 = 20, RoundLabel = "F" }
    };
        }

        public static List<LadderMatch> GetLadder21()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 0, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 = 11, Seed2 = 12, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 6, Seed2 = 17, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 7, Seed2 = 16, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 2, Seed2 = 21, RoundLabel = "R1" },
        new LadderMatch { MatchId = 6, Seed1 = 5, Seed2 = 18, RoundLabel = "R1" },
        new LadderMatch { MatchId = 7, Seed1 = 8, Seed2 = 15, RoundLabel = "R1" },
        new LadderMatch { MatchId = 8, Seed1 = 3, Seed2 = 20, RoundLabel = "R1" },
        new LadderMatch { MatchId = 9, Seed1 = 10, Seed2 = 13, RoundLabel = "R1" },
        new LadderMatch { MatchId = 10, Seed1 = 4, Seed2 = 19, RoundLabel = "R1" },
        new LadderMatch { MatchId = 11, Seed1 = 9, Seed2 = 14, RoundLabel = "R1" }, 

        // ROUND 2
        new LadderMatch { MatchId = 12, FromMatch1 = 1, FromMatch2 = 2, RoundLabel = "R2" },
        new LadderMatch { MatchId = 13, FromMatch1 = 3, FromMatch2 = 4, RoundLabel = "R2" },
        new LadderMatch { MatchId = 14, FromMatch1 = 5,  Seed2 = 0, RoundLabel = "R2" },
        new LadderMatch { MatchId = 15, FromMatch1 = 6, FromMatch2 = 7, RoundLabel = "R2" },
        new LadderMatch { MatchId = 16, FromMatch1 = 8, FromMatch2 = 9, RoundLabel = "R2" },
        new LadderMatch { MatchId = 17, FromMatch1 = 10, FromMatch2 = 11, RoundLabel = "R2" },

        // ROUND 3
        new LadderMatch { MatchId = 18, FromMatch1 = 12, FromMatch2 = 13, RoundLabel = "R3" },
        new LadderMatch { MatchId = 19, FromMatch1 = 14, FromMatch2 = 15, RoundLabel = "R3" },
        new LadderMatch { MatchId = 20, FromMatch1 = 16, FromMatch2 = 17, RoundLabel = "R3" },

        // SEMIFINALS
        new LadderMatch { MatchId = 21, FromMatch1 = 18, FromMatch2 = 19, RoundLabel = "SF" },
        new LadderMatch { MatchId = 22, FromMatch1 = 20, Seed2 = 0, RoundLabel = "SF" }, // BYE

        // FINAL
        new LadderMatch { MatchId = 23, FromMatch1 = 21, FromMatch2 = 22, RoundLabel = "F" }
    };
        }

        public static List<LadderMatch> GetLadder22()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 22, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 =6, Seed2 = 17, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 7, Seed2 = 16, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 3, Seed2 = 20, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 10, Seed2 = 13, RoundLabel = "R1" },
        new LadderMatch { MatchId = 6, Seed1 = 4, Seed2 = 19, RoundLabel = "R1" },
        new LadderMatch { MatchId = 7, Seed1 = 9, Seed2 = 14, RoundLabel = "R1" },
        new LadderMatch { MatchId = 8, Seed1 = 2, Seed2 = 21, RoundLabel = "R1" },
        new LadderMatch { MatchId = 9, Seed1 = 11, Seed2 = 12, RoundLabel = "R1" },
        new LadderMatch { MatchId = 10, Seed1 = 5, Seed2 = 18, RoundLabel = "R1" },
        new LadderMatch { MatchId = 11, Seed1 = 8, Seed2 = 15, RoundLabel = "R1" }, 

        // ROUND 2
        new LadderMatch { MatchId = 12, FromMatch1 = 1, Seed2 = 0, RoundLabel = "R2" },
        new LadderMatch { MatchId = 13, FromMatch1 = 2, FromMatch2 = 3, RoundLabel = "R2" },
        new LadderMatch { MatchId = 14, FromMatch1 = 4,  FromMatch2 = 5, RoundLabel = "R2" },
        new LadderMatch { MatchId = 15, FromMatch1 = 6, FromMatch2 = 7, RoundLabel = "R2" },
        new LadderMatch { MatchId = 16, FromMatch1 = 8, FromMatch2 = 9, RoundLabel = "R2" },
        new LadderMatch { MatchId = 17, FromMatch1 = 10, FromMatch2 = 11, RoundLabel = "R2" },

        // ROUND 3
        new LadderMatch { MatchId = 18, FromMatch1 = 12, FromMatch2 = 13, RoundLabel = "R3" },
        new LadderMatch { MatchId = 19, FromMatch1 = 14, FromMatch2 = 15, RoundLabel = "R3" },
        new LadderMatch { MatchId = 20, FromMatch1 = 16, FromMatch2 = 17, RoundLabel = "R3" },

        // SEMIFINALS
        new LadderMatch { MatchId = 21, FromMatch1 = 18, FromMatch2 = 19, RoundLabel = "SF" },
        new LadderMatch { MatchId = 22, FromMatch1 = 20, Seed2 = 0, RoundLabel = "SF" }, // BYE

        // FINAL
        new LadderMatch { MatchId = 23, FromMatch1 = 21, FromMatch2 = 22, RoundLabel = "F" }
    };
        }


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


        public static List<LadderMatch> GetLadder24()
        {
            return new List<LadderMatch>
    {
        // ROUND 1
        new LadderMatch { MatchId = 1, Seed1 = 1, Seed2 = 24, RoundLabel = "R1" },
        new LadderMatch { MatchId = 2, Seed1 = 12, Seed2 = 13, RoundLabel = "R1" },
        new LadderMatch { MatchId = 3, Seed1 = 6, Seed2 = 19, RoundLabel = "R1" },
        new LadderMatch { MatchId = 4, Seed1 = 7, Seed2 = 18, RoundLabel = "R1" },
        new LadderMatch { MatchId = 5, Seed1 = 2, Seed2 = 23, RoundLabel = "R1" },
        new LadderMatch { MatchId = 6, Seed1 = 11, Seed2 = 14, RoundLabel = "R1" },
        new LadderMatch { MatchId = 7, Seed1 = 5, Seed2 = 20, RoundLabel = "R1" },
        new LadderMatch { MatchId = 8, Seed1 = 8, Seed2 = 17, RoundLabel = "R1" },
        new LadderMatch { MatchId = 9, Seed1 = 3, Seed2 = 22, RoundLabel = "R1" },
        new LadderMatch { MatchId = 10, Seed1 = 10, Seed2 = 15, RoundLabel = "R1" },
        new LadderMatch { MatchId = 11, Seed1 = 4, Seed2 = 21, RoundLabel = "R1" },
        new LadderMatch { MatchId = 12, Seed1 = 9, Seed2 = 16, RoundLabel = "R1" },

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
        new LadderMatch { MatchId = 22, FromMatch1 = 19, Seed2 = 0, RoundLabel = "SF" },
        new LadderMatch { MatchId = 23, FromMatch1 = 20, FromMatch2 = 21, RoundLabel = "SF" },

        // FINAL
        new LadderMatch { MatchId = 24, FromMatch1 = 22, FromMatch2 = 23, RoundLabel = "F" }
    };
        }



    }
}
