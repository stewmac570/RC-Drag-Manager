// ──────────────────────────────────────────────────────────────────────────────
// File: RoundRobinRanker.cs
// Project: RCDragManagerProd
// Purpose: Calculate standings for a 3-round Round-Robin event
// ──────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd
{
    public class DriverRankResult
    {
        public int DriverId { get; set; }
        public int Rank { get; set; }
        public double Points { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int[] DefeatedIds { get; set; } = Array.Empty<int>();
    }

    public sealed class RoundRobinRanker
    {
        private static readonly Random _rng = new();

        // winner lookup for each driver pair
        private readonly Dictionary<(int, int), int> _h2h = new();

        public List<DriverRankResult> Rank(
            List<RoundRobinMatchResult> results,
            List<Driver> drivers)
        {
            // aggregate
            var stats = drivers.ToDictionary(d => d.Id, _ => new Aggregate());

            foreach (var m in results.Where(r => r.WinnerId != 0))
            {
                int loserId = (m.WinnerId == m.Driver1Id) ? m.Driver2Id : m.Driver1Id;
                if (!stats.ContainsKey(m.WinnerId) || !stats.ContainsKey(loserId))
                    continue;

                double pts = PointsForRound(m.RoundLabel);

                var w = stats[m.WinnerId];
                w.Points += pts;
                w.Wins += 1;
                w.Defeated.Add(loserId);

                stats[loserId].Losses += 1;

                _h2h[PairKey(m.WinnerId, loserId)] = m.WinnerId;
            }

            var table = stats.Select(kvp => new DriverRankResult
            {
                DriverId = kvp.Key,
                Points = kvp.Value.Points,
                Wins = kvp.Value.Wins,
                Losses = kvp.Value.Losses,
                DefeatedIds = kvp.Value.Defeated.ToArray()
            }).ToList();

            table.Sort((a, b) =>
            {
                int cmp = b.Points.CompareTo(a.Points); if (cmp != 0) return cmp;
                cmp = b.Wins.CompareTo(a.Wins); if (cmp != 0) return cmp;

                if (_h2h.TryGetValue(PairKey(a.DriverId, b.DriverId), out int winner))
                {
                    if (winner == a.DriverId) return -1;
                    if (winner == b.DriverId) return 1;
                }

                // TODO opponent-strength tie-break

                return _rng.Next(-1, 2);
            });

            for (int i = 0; i < table.Count; i++) table[i].Rank = i + 1;
            return table;
        }

        // helpers
        private static (int, int) PairKey(int a, int b) => (a < b) ? (a, b) : (b, a);

        private static double PointsForRound(string lbl) => lbl?.ToUpperInvariant() switch
        {
            "R1" => 4.0,
            "R2" => 3.5,
            "R3" => 3.0,
            _ => 0
        };

        private sealed class Aggregate
        {
            public double Points = 0;
            public int Wins = 0;
            public int Losses = 0;
            public HashSet<int> Defeated = new();
        }
    }
}
