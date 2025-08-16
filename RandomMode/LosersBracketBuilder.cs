
using System;
using System.Collections.Generic;
using System.Linq;

using RCDragManagerProd.Domain;
using RCDragManagerProd.RaceEngines;   // if it talks to core engine types
using RCDragManagerProd.Logging;      // Logger


namespace RCDragManagerProd.RandomMode
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
        /// 
        public static List<RandomMatch> Build(
     List<Driver> entrants,
     HashSet<(int, int)> history,
     int startMatchId = 1000)
        {
            // 0) inputs
            if (entrants == null) entrants = new List<Driver>();
            history ??= new HashSet<(int, int)>();

            // 1) Shuffle and pad to power-of-two with nulls (= BYEs)
            var pool = entrants.OrderBy(_ => rng.Next()).ToList();
            int targetSize = NextPowerOfTwo(pool.Count);
            while (pool.Count < targetSize) pool.Add(null);

            int id = startMatchId;                         // << declared ONCE
            var allMatches = new List<RandomMatch>();
            var r1MatchIds = new List<int>();            // << declared ONCE

            Logger.Log($"[LB-BUILD] entrants={entrants.Count}, padded={pool.Count}, startId={id}");

            // 2) R1 pairings — step in pairs; SKIP BYE-vs-BYE to avoid infinite loop
            for (int i = 0; i < pool.Count; i += 2)
            {
                var p1 = pool[i];
                var p2 = (i + 1 < pool.Count) ? pool[i + 1] : null;

                // If both are BYEs, skip this slot completely
                if (p1 == null && p2 == null)
                {
                    Logger.Log("[LB-BUILD] Skipping BYE v BYE slot in R1");
                    continue;
                }

                var matchId = id++;
                r1MatchIds.Add(matchId);

                allMatches.Add(new RandomMatch
                {
                    MatchId = matchId,
                    Seed1 = p1,                 // may be null (BYE)
                    Seed2 = p2,                 // may be null (BYE)
                    FromMatch1 = 0,
                    FromMatch2 = 0,
                    RoundLabel = "Losers Bracket R1"
                });

                Logger.Log($"[LB-BUILD] R1 M{matchId}: {(p1?.Name ?? "BYE")} vs {(p2?.Name ?? "BYE")}");
            }
            Logger.Log($"[LB-BUILD] R1 complete: matches={r1MatchIds.Count}");

            // 3) Subsequent rounds — carry odd last match forward as a BYE match
            var prevRound = r1MatchIds;
            int roundIndex = 2;

            while (prevRound.Count > 1)
            {
                var thisRound = new List<int>();

                for (int i = 0; i < prevRound.Count; i += 2)
                {
                    int matchId = id++;

                    if (i + 1 < prevRound.Count)
                    {
                        // Normal pairing of two prior winners
                        thisRound.Add(matchId);
                        allMatches.Add(new RandomMatch
                        {
                            MatchId = matchId,
                            Seed1 = null,
                            Seed2 = null,
                            FromMatch1 = prevRound[i],
                            FromMatch2 = prevRound[i + 1],
                            RoundLabel = (prevRound.Count == 2)
                                         ? "Losers Bracket Final"
                                         : $"Losers Bracket R{roundIndex}"
                        });

                        Logger.Log($"[LB-BUILD] R{roundIndex} M{matchId}: Winner M{prevRound[i]} vs Winner M{prevRound[i + 1]}");
                    }
                    else
                    {
                        // ODD carry → create a BYE match for the lone prior winner
                        thisRound.Add(matchId);
                        allMatches.Add(new RandomMatch
                        {
                            MatchId = matchId,
                            Seed1 = null,
                            Seed2 = null,      // BYE
                            FromMatch1 = prevRound[i],
                            FromMatch2 = 0,
                            RoundLabel = $"Losers Bracket R{roundIndex}"
                        });

                        Logger.Log($"[LB-BUILD] R{roundIndex} M{matchId}: Winner M{prevRound[i]} vs BYE (carry)");
                    }
                }

                roundIndex++;
                prevRound = thisRound;
            }

            Logger.Log($"[LB-BUILD] Done. Total matches={allMatches.Count}, lastId={id - 1}");
            return allMatches;
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
