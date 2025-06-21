// ──────────────────────────────────────────────────────────────────────────────
// File: RandomBracket.cs
// Project: RCDragManagerProd
// Purpose: Blind-draw bracket generator with 1 BYE max per driver.
// ──────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd
{
    public static class RandomBracket
    {
        private static readonly Random rng = new Random();

        // keeps track of who has already received a BYE in the current event
        private static readonly HashSet<int> byeGiven = new HashSet<int>();

        /// <summary>Call at the start of every new RANDOMIZED event.</summary>
        public static void ResetByeTracker() => byeGiven.Clear();

        // ──────────────────────────────────────────────────────────────
        // ROUND 1
        // ──────────────────────────────────────────────────────────────
        public static List<RandomMatch> GenerateFirstRound(List<Driver> drivers)
        {
            // 1️⃣ shuffle the entrants
            List<Driver> shuffled = drivers.OrderBy(_ => rng.Next()).ToList();

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

            // 3️⃣ BYE (odd field) — give to first driver without a prior BYE
            if (i < shuffled.Count)
            {
                Driver byeDriver = shuffled[i];

                if (byeGiven.Contains(byeDriver.Id))
                {
                    byeDriver = shuffled.First(d => !byeGiven.Contains(d.Id));
                    shuffled[i] = byeDriver;                 // swap back original
                }

                byeGiven.Add(byeDriver.Id);

                matches.Add(new RandomMatch
                {
                    MatchId = matchId++,
                    Seed1 = byeDriver,
                    Seed2 = null,          // BYE slot
                    FromMatch1 = null,
                    FromMatch2 = null,
                    RoundLabel = "R1"
                });
            }

            return matches;
        }

        // ──────────────────────────────────────────────────────────────
        // SUBSEQUENT ROUNDS
        // ──────────────────────────────────────────────────────────────
        public static List<RandomMatch> GenerateNextRound(
            List<Driver> remainingDrivers,
            HashSet<(int, int)> pairingHistory)
        {
            var matches = new List<RandomMatch>();
            var pool = remainingDrivers.OrderBy(_ => rng.Next()).ToList();
            int matchId = 1;

            // 1️⃣ BYE if odd — choose someone who hasn't had one yet
            if (pool.Count % 2 == 1)
            {
                var eligible = pool.Where(d => !byeGiven.Contains(d.Id)).ToList();
                Driver byeDriver = eligible.Count > 0
                                   ? eligible[rng.Next(eligible.Count)]
                                   : pool[rng.Next(pool.Count)];   // everyone had a BYE

                byeGiven.Add(byeDriver.Id);
                pool.Remove(byeDriver);

                matches.Add(new RandomMatch
                {
                    MatchId = matchId++,
                    Seed1 = byeDriver,
                    Seed2 = null,
                    FromMatch1 = null,
                    FromMatch2 = null,
                    RoundLabel = "Next"
                });
            }

            // 2️⃣ pair remaining drivers, avoiding repeats where possible
            while (pool.Count > 1)
            {
                Driver p1 = pool[0];
                pool.RemoveAt(0);

                int idx = pool.FindIndex(p2 =>
                          !pairingHistory.Contains(NormalizePair(p1.Id, p2.Id)));

                if (idx == -1) idx = 0;                   // forced repeat

                Driver p2 = pool[idx];
                pool.RemoveAt(idx);

                matches.Add(new RandomMatch
                {
                    MatchId = matchId++,
                    Seed1 = p1,
                    Seed2 = p2,
                    FromMatch1 = null,
                    FromMatch2 = null,
                    RoundLabel = "Next"
                });
            }

            // (pool.Count can never be 1 here — handled via BYE block)

            return matches;
        }

        // ──────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────
        private static (int, int) NormalizePair(int a, int b) => a < b ? (a, b) : (b, a);

        private static (Guid, Guid) NormalizePair(Guid a, Guid b)
            => a.CompareTo(b) < 0 ? (a, b) : (b, a);
    }
}
