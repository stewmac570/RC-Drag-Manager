// ──────────────────────────────────────────────────────────────────────────────
// File: LosersBracketBuilder.cs
// Purpose: Generates a fixed single-elimination Losers Bracket
//          (random draw, no rematches, power-of-2 tree, BYE support).
// ──────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd
{
    public static class LosersBracketBuilder
    {
        private static readonly Random rng = new();

        /// <summary>
        /// Builds the complete Losers-Bracket tree.
        /// </summary>
        /// <param name="entrants">Buyback drivers (rank ≥ 4).</param>
        /// <param name="history">
        /// All previous pairings in *this* event – used to avoid rematches.
        /// Tuple is ordered (smaller Id first).
        /// </param>
        /// <param name="startMatchId">MatchId offset so we never clash with earlier rounds.</param>
        public static List<RandomMatch> Build(
            List<Driver> entrants,
            HashSet<(int, int)> history,
            int startMatchId = 1000)
        {
            // 1️⃣  shuffle & pad to power-of-2
            var pool = entrants.OrderBy(_ => rng.Next()).ToList();
            int targetSize = NextPowerOfTwo(pool.Count);
            while (pool.Count < targetSize) pool.Add(null);          // null ⇒ BYE

            // 2️⃣  pair “R1” while avoiding rematches
            int id = startMatchId;
            var allMatches = new List<RandomMatch>();
            var r1MatchIds = new List<int>();

            while (pool.Count > 0)
            {
                Driver p1 = pool[0];
                pool.RemoveAt(0);

                Driver p2 = FindOpponent(p1, pool, history);
                pool.Remove(p2); // safe even if p2 == null

                // ❌ Skip matches where both are null (BYE vs BYE)
                if (p1 == null && p2 == null) continue;

                allMatches.Add(new RandomMatch
                {
                    MatchId = id,
                    Seed1 = p1,
                    Seed2 = p2,
                    FromMatch1 = null,
                    FromMatch2 = null,
                    RoundLabel = "Losers Bracket R1"
                });

                r1MatchIds.Add(id);
                id++;
            }

            // 3️⃣ build subsequent rounds (fixed tree)
            var prevRound = r1MatchIds;
            int roundIndex = 2;
            while (prevRound.Count > 1)
            {
                var thisRound = new List<int>();

                for (int i = 0; i < prevRound.Count; i += 2)
                {
                    if (i + 1 >= prevRound.Count) break;

                    int matchId = id++;
                    thisRound.Add(matchId);

                    allMatches.Add(new RandomMatch
                    {
                        MatchId = matchId,
                        Seed1 = null,
                        Seed2 = null,
                        FromMatch1 = prevRound[i],
                        FromMatch2 = prevRound[i + 1],
                        RoundLabel = prevRound.Count == 2
                                     ? "Losers Bracket Final"
                                     : $"Losers Bracket R{roundIndex}"
                    });
                }


                prevRound = thisRound;
                roundIndex++;
            }

            return allMatches;
        }

        // ──────────────────────────────────────────────────────────────
        // helpers
        // ──────────────────────────────────────────────────────────────
        private static Driver FindOpponent(Driver p1,
                                           List<Driver> pool,
                                           HashSet<(int, int)> history)
        {
            if (p1 == null) return null;                  // BYE slot

            int idx = pool.FindIndex(d =>
                d == null || !history.Contains(Norm(p1.Id, d.Id)));

            return idx >= 0 ? pool[idx] : pool[0];        // fallback repeat allowed
        }

        private static (int, int) Norm(int a, int b) => a < b ? (a, b) : (b, a);

        private static int NextPowerOfTwo(int n)
        {
            int p = 1;
            while (p < n) p <<= 1;
            return p;
        }
    }
}
