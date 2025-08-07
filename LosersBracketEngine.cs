// ============================================================================
// LosersBracketEngine.cs
// RC Drag Manager — Single-Elim Losers-Bracket (buy-back) helper
// ✔ Uses Driver objects (no Guid confusion)
// ✔ BYE support via nullable Driver slots
// ✔ Detailed logging at each major step
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd
{
    public class LosersBracketEngine
    {
        // Static RNG so the static RunBracket can access it
        private static readonly Random _rng = new();

        /// <summary>
        /// Runs a blind single-elimination bracket for the supplied drivers.
        /// </summary>
        /// <param name="drivers">All entrants (buy-backs, etc.). Count must be ≥ 1.</param>
        /// <param name="raceCallback">
        /// A delegate your UI/controller supplies that takes two <see cref="Driver"/>s
        /// and returns the winner.
        /// </param>
        /// <returns>The bracket champion (who will advance to the final ladder).</returns>
        public static Driver RunBracket(
            IReadOnlyCollection<Driver> drivers,
            Func<Driver, Driver, Driver> raceCallback)
        {
            if (drivers == null || drivers.Count < 1)
                throw new ArgumentException("No drivers supplied.", nameof(drivers));

            // 1️⃣  Shuffle the field.
            var pool = drivers.OrderBy(_ => _rng.Next()).ToList<Driver?>();
            Logger.Log($"[LB] Starting losers bracket with {pool.Count} drivers.");

            // 2️⃣  Pad to next power-of-two with BYEs (null slots).
            int targetSize = NextPowerOfTwo(pool.Count);
            while (pool.Count < targetSize)
                pool.Add(null);

            Logger.Log($"[LB] Bracket size padded to {targetSize} (BYEs inserted = {targetSize - drivers.Count}).");

            // 3️⃣  Run rounds until one champion remains.
            int round = 1;
            while (pool.Count > 1)
            {
                Logger.Log($"[LB] ---------- Round {round} | Field = {pool.Count} ----------");

                var next = new List<Driver?>();
                for (int i = 0; i < pool.Count; i += 2)
                {
                    var a = pool[i];
                    var b = pool[i + 1];

                    // BYE handling
                    if (a == null) { next.Add(b); continue; }
                    if (b == null) { next.Add(a); continue; }

                    // Real race — ask the controller UI who won
                    var winner = raceCallback(a, b);
                    if (winner == null)
                        throw new InvalidOperationException("raceCallback returned null!");

                    Logger.Log($"[LB]   Match: {a.Name} vs {b.Name} → Winner: {winner.Name}");
                    next.Add(winner);
                }

                pool = next;
                round++;
            }

            var champion = pool[0]!; // never null here
            Logger.Log($"[LB] Losers-Bracket champion: {champion.Name}");
            return champion;
        }

        // -------------------------------------------------------------------
        // Utility helpers
        // -------------------------------------------------------------------
        private static int NextPowerOfTwo(int n)
        {
            int p = 1;
            while (p < n) p <<= 1;
            return p;
        }
    }
}
