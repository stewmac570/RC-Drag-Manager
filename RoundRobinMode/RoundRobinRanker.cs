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
        public double OpponentStrength { get; set; }
    }

    public sealed class RoundRobinRanker
    {
        private readonly Dictionary<(int, int), int> _h2h = new();

        public List<DriverRankResult> Rank(
            List<RoundRobinMatch> matches,
            List<Driver> drivers,
            MatchResult results)
        {
            var stats = drivers.ToDictionary(d => d.Id, _ => new Aggregate());

            foreach (var m in matches)
            {
                var pts = GetPoints(m.RoundLabel);

                bool isBye = m.Driver1 == null || m.Driver2 == null;

                var winner = results.GetWinner(m.MatchId);
                var loser = results.GetLoser(m.MatchId);

                int? winnerId = winner?.Id;
                int? loserId = loser?.Id ?? 0;

                if (isBye && winnerId != null)
                {
                    stats[winnerId.Value].Points += pts.Bye;
                    continue;
                }


                if (winnerId != null && loserId != 0)
                {
                    var w = stats[winnerId.Value];
                    var l = stats[loserId.Value];

                    w.Points += pts.Win;
                    w.Wins += 1;
                    w.Defeated.Add(loserId.Value);

                    l.Points += pts.Loss;
                    l.Losses += 1;

                    _h2h[PairKey(winnerId.Value, loserId.Value)] = winnerId.Value;
                }


            }

            var table = stats.Select(kvp => new DriverRankResult
            {
                DriverId = kvp.Key,
                Points = kvp.Value.Points,
                Wins = kvp.Value.Wins,
                Losses = kvp.Value.Losses,
                DefeatedIds = kvp.Value.Defeated.ToArray(),
                OpponentStrength = 0
            }).ToList();

            var pointLookup = table.ToDictionary(x => x.DriverId, x => x.Points);
            foreach (var r in table)
            {
                double total = 0;
                foreach (var m in matches)
                {
                    if (m.Driver1 != null && pointLookup.ContainsKey(m.Driver1.Id))
                        total += pointLookup[m.Driver1.Id];
                    if (m.Driver2 != null && pointLookup.ContainsKey(m.Driver2.Id))
                        total += pointLookup[m.Driver2.Id];
                }
                r.OpponentStrength = total;
            }

            table.Sort((a, b) =>
            {
                int cmp = b.Points.CompareTo(a.Points); if (cmp != 0) return cmp;
                cmp = b.Wins.CompareTo(a.Wins); if (cmp != 0) return cmp;

                if (_h2h.TryGetValue(PairKey(a.DriverId, b.DriverId), out int winner))
                {
                    if (winner == a.DriverId) return -1;
                    if (winner == b.DriverId) return 1;
                }

                cmp = b.OpponentStrength.CompareTo(a.OpponentStrength); if (cmp != 0) return cmp;

                return a.DriverId.CompareTo(b.DriverId);
            });

            for (int i = 0; i < table.Count; i++) table[i].Rank = i + 1;
            return table;
        }

        private static (int, int) PairKey(int a, int b) => (a < b) ? (a, b) : (b, a);

        private static (double Win, double Loss, double Bye) GetPoints(string lbl) =>
            lbl?.ToUpperInvariant() switch
            {
                "R1" => (4.0, 1.0, 2.0),
                "R2" => (3.5, 0.75, 1.5),
                "R3" => (3.0, 0.5, 1.0),
                _ => (0, 0, 0)
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
