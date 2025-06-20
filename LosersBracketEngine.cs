// ============================================================================
// LosersBracketEngine.cs
// RC Drag Manager — Simple Single-Elim “Second-Chance” Bracket (MVP v1.0)
// ============================================================================
//
// Takes everyone not ranked Top-3 from a round-robin and runs a blind
// single-elimination to crown one winner destined for the Pro Ladder.
// BYEs are inserted only as needed.  No rematches occur inside this bracket.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd
{
    public class LosersBracketEngine
    {
        private readonly Random _rng = new();

        public Guid RunBracket(IReadOnlyCollection<Guid> driverIds,
                               Func<Guid, Guid, Guid> raceCallback)
        {
            if (driverIds.Count < 1)
                throw new ArgumentException("No drivers supplied.", nameof(driverIds));

            // shuffle
            var pool = driverIds.OrderBy(_ => _rng.Next()).ToList();

            // bump to next power-of-two
            int rounds =
            (int)Math.Ceiling(Math.Log(pool.Count, 2));
            int size = 1 << rounds;
            int byes = size - pool.Count;

            // disperse BYEs
            for (int i = 0; i < byes; i++)
            {
                pool.Insert(i * 2, Guid.Empty); // Guid.Empty marks a BYE slot
            }

            // run rounds
            while (pool.Count > 1)
            {
                var next = new List<Guid>();

                for (int i = 0; i < pool.Count; i += 2)
                {
                    var a = pool[i];
                    var b = pool[i + 1];

                    if (a == Guid.Empty) { next.Add(b); continue; }
                    if (b == Guid.Empty) { next.Add(a); continue; }

                    var winner = raceCallback(a, b);
                    next.Add(winner);
                }

                pool = next;
            }

            return pool[0]; // champion to feed back into Pro Ladder
        }
    }
}
