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
            int count = drivers.Count;
            int rounds = (int)Math.Ceiling(Math.Log(count) / Math.Log(2));
            int bracketSize = (int)Math.Pow(2, rounds);
            int byes = bracketSize - count;

            List<Driver> shuffled = drivers.OrderBy(d => rng.Next()).ToList();
            List<Driver> byeDrivers = shuffled.Take(byes).ToList();
            List<Driver> active = shuffled.Skip(byes).ToList();

            List<RandomMatch> matches = new List<RandomMatch>();
            int matchId = 1;

            foreach (Driver driver in byeDrivers)
            {
                matches.Add(new RandomMatch
                {
                    MatchId = matchId++,
                    Seed1 = driver,
                    Seed2 = null,
                    FromMatch1 = null,
                    FromMatch2 = null,
                    RoundLabel = "R1"
                });
            }

            for (int i = 0; i < active.Count; i += 2)
            {
                matches.Add(new RandomMatch
                {
                    MatchId = matchId++,
                    Seed1 = active[i],
                    Seed2 = active[i + 1],
                    FromMatch1 = null,
                    FromMatch2 = null,
                    RoundLabel = "R1"
                });
            }

            return matches;
        }

        public static List<RandomMatch> GenerateNextRound(List<Driver> remainingDrivers, HashSet<string> pairingHistory)
        {
            List<RandomMatch> matches = new List<RandomMatch>();
            List<Driver> pool = remainingDrivers.OrderBy(x => rng.Next()).ToList();
            int matchId = 1;

            while (pool.Count > 1)
            {
                Driver p1 = pool[0];
                pool.RemoveAt(0);

                Driver opponent = null;
                foreach (Driver candidate in pool)
                {
                    if (!pairingHistory.Contains(NormalizePair(p1.Id, candidate.Id)))
                    {
                        opponent = candidate;
                        break;
                    }
                }

                if (opponent == null)
                {
                    opponent = pool[0]; // fallback
                }

                pool.Remove(opponent);

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

        private static string NormalizePair(int a, int b)
        {
            return (a < b) ? a + "-" + b : b + "-" + a;
        }
    }
}
