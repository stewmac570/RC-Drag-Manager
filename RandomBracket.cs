using System;
using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd
{
    public static class RandomBracket
    {
        private static readonly Random rng = new Random();

        public static List<RandomMatch> GenerateFirstRound(List<Driver> drivers)
        {
            // 1️⃣ shuffle the entrants
            List<Driver> shuffled = drivers.OrderBy(d => rng.Next()).ToList();

            List<RandomMatch> matches = new List<RandomMatch>();
            int matchId = 1;
            int i = 0;

            // 2️⃣ pair straight down the list
            while (i + 1 < shuffled.Count)
            {
                matches.Add(new RandomMatch
                {
                    MatchId = matchId++,
                    Seed1 = shuffled[i],
                    Seed2 = shuffled[i + 1],
                    FromMatch1 = null,
                    FromMatch2 = null,
                    RoundLabel = "R1"
                });
                i += 2;
            }

            // 3️⃣ if odd driver-count, give the last driver a single BYE
            if (i < shuffled.Count)
            {
                matches.Add(new RandomMatch
                {
                    MatchId = matchId++,
                    Seed1 = shuffled[i],
                    Seed2 = null,          // BYE slot
                    FromMatch1 = null,
                    FromMatch2 = null,
                    RoundLabel = "R1"
                });
            }

            return matches;
        }




        public static List<RandomMatch> GenerateNextRound(List<Driver> remainingDrivers, HashSet<(int, int)> pairingHistory)
        {
            List<RandomMatch> matches = new List<RandomMatch>();
            List<Driver> pool = remainingDrivers.OrderBy(x => rng.Next()).ToList();
            int matchId = 1;

            while (pool.Count > 1)
            {
                Driver p1 = pool[0];
                pool.RemoveAt(0);

                Driver opponent = null;
                for (int i = 0; i < pool.Count; i++)
                {
                    var candidate = pool[i];
                    var pair = NormalizePair(p1.Id, candidate.Id);
                    if (!pairingHistory.Contains(pair))
                    {
                        opponent = candidate;
                        pool.RemoveAt(i);
                        break;
                    }
                }

                if (opponent == null)
                {
                    opponent = pool[0];
                    pool.RemoveAt(0);
                }

                matches.Add(new RandomMatch
                {
                    MatchId = matchId++,
                    Seed1 = p1,
                    Seed2 = opponent,
                    FromMatch1 = null,
                    FromMatch2 = null,
                    RoundLabel = "Next"
                });
            }

            if (pool.Count == 1)
            {
                matches.Add(new RandomMatch
                {
                    MatchId = matchId++,
                    Seed1 = pool[0],
                    Seed2 = null,
                    FromMatch1 = null,
                    FromMatch2 = null,
                    RoundLabel = "Next"
                });
            }

            return matches;
        }

        private static (int, int) NormalizePair(int a, int b)
        {
            return a < b ? (a, b) : (b, a);
        }



        private static (Guid, Guid) NormalizePair(Guid a, Guid b)
        {
            return a.CompareTo(b) < 0 ? (a, b) : (b, a);
        }

    }
}
