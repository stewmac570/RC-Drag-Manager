using RCDragManagerProd.DicEx;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace RCDragManagerProd.RoundRobinMode
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
        // STEP 1: Public points accessor
        public static (double Win, double Loss, double Bye) PointsForRound(string lbl)
        {
            string key = (lbl ?? string.Empty).Trim().ToUpperInvariant();

            // Accept any "R{number}" (R1, R12, etc). Constant scoring for all rounds.
            // Policy: Win=4.0 Loss=1.0 Bye=2.0
            double win = 4.0;
            double loss = 1.0;
            double bye = 2.0;

            int roundNum = 0;

            if (key.StartsWith("R") && key.Length >= 2)
            {
                var numPart = key.Substring(1);
                int.TryParse(numPart, out roundNum);
            }

            if (roundNum <= 0)
            {
                Logger.Log($"[RR-PTS][WARN] Unparseable round label '{lbl}' → treating as R? (using constant points anyway)");
            }

            Logger.Log($"[RR-PTS] Round='{(string.IsNullOrEmpty(key) ? "(blank)" : key)}' ParsedN={roundNum} → Win={win:0.00}, Loss={loss:0.00}, BYE={bye:0.00}");
            return (win, loss, bye);
        }


        private readonly Dictionary<(int, int), int> _h2h = new();

        public List<DriverRankResult> Rank(
            List<RoundRobinMatch> matches,
            List<Driver> drivers,
            MatchResult results)
        {
            Logger.Log($"[RR-RANK] Starting ranking process — Drivers={drivers?.Count ?? 0}, Matches={matches?.Count ?? 0}");
            _h2h.Clear();


            var idToName = new Dictionary<int, string>();
            var stats = new Dictionary<int, Aggregate>();

            // Opponents actually faced (BYEs ignored)
            var faced = new Dictionary<int, HashSet<int>>();


            if (drivers != null)
            {
                foreach (var d in drivers)
                {
                    if (d == null) continue;
                    if (!idToName.ContainsKey(d.Id))
                    {
                        idToName[d.Id] = d.Name;
                        stats[d.Id] = new Aggregate();
                        faced[d.Id] = new HashSet<int>();

                    }
                    else
                    {
                        Logger.Log($"[RR-RANK] Duplicate driver Id detected ({d.Id}, '{d.Name}'). Keeping first '{idToName[d.Id]}'.");
                    }
                }
            }


            foreach (var m in matches)
            {
                var pts = GetPoints(m.RoundLabel);

                // BYE if either side missing
                bool isBye = m.Driver1 == null || m.Driver2 == null;

                {
                    int a = m.Driver1?.Id ?? 0;
                    int b = m.Driver2?.Id ?? 0;

                    if (a != 0 && b != 0 && faced.ContainsKey(a) && faced.ContainsKey(b))
                    {
                        faced[a].Add(b);
                        faced[b].Add(a);
                    }
                }


                var winner = results.GetWinner(m.MatchId);
                var loser = results.GetLoser(m.MatchId);

                int? winnerId = winner?.Id;
                int loserId = loser?.Id ?? 0;

                // 🔧 Derive loser if only winner recorded (non-BYE)
                if (!isBye && winnerId != null && loserId == 0)
                {
                    int d1 = m.Driver1?.Id ?? 0;
                    int d2 = m.Driver2?.Id ?? 0;

                    if (winnerId.Value == d1) loserId = d2;
                    else if (winnerId.Value == d2) loserId = d1;
                    else
                    {
                        // Winner doesn't match either participant — skip safely
                        string bad = idToName.ContainsKey(winnerId.Value) ? idToName[winnerId.Value] : (winner?.Name ?? "—");
                        Logger.Log($"[RR-PTS] M{m.MatchId}: winner '{bad}' not in pairing — skipping.");
                        continue;
                    }
                }

                string wName = (winnerId != null && idToName.ContainsKey(winnerId.Value)) ? idToName[winnerId.Value] : (winner?.Name ?? "—");
                string lName = (loserId != 0 && idToName.ContainsKey(loserId)) ? idToName[loserId] : (loser?.Name ?? "—");

                Logger.Log($"[RR-MATCH] Processing Match {m.MatchId} ({m.RoundLabel}) → W={wName}, L={lName}, Bye={isBye}");

                // BYE: award BYE points to the winner only
                if (isBye && winnerId != null)
                {
                    stats[winnerId.Value].Points += pts.Bye;
                    Logger.Log($"[RR-PTS] Match {m.MatchId} BYE → {wName} gains {pts.Bye:0.00} points");
                    continue;
                }

                // Normal resolved match
                if (winnerId != null && loserId != 0)
                {
                    var w = stats[winnerId.Value];
                    var l = stats[loserId];

                    w.Points += pts.Win;
                    w.Wins++;
                    w.Defeated.Add(loserId);

                    l.Points += pts.Loss;
                    l.Losses++;

                    _h2h[PairKey(winnerId.Value, loserId)] = winnerId.Value;

                    Logger.Log($"[RR-PTS] Match {m.MatchId} {wName} def {lName} → Win+{pts.Win:0.00}, Loss+{pts.Loss:0.00}");
                }
                else
                {
                    Logger.Log($"[RR-PTS] Match {m.MatchId} has no result yet");
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

            // Opponent Strength (SoS) = sum of FINAL points of opponents actually faced (BYEs ignored)
            var pointsById = table.ToDictionary(x => x.DriverId, x => x.Points);

            foreach (var r in table)
            {
                if (!faced.TryGetValue(r.DriverId, out var opps) || opps.Count == 0)
                {
                    r.OpponentStrength = 0;
                    continue;
                }

                double sum = 0;
                foreach (var oppId in opps)
                {
                    if (pointsById.TryGetValue(oppId, out var pts))
                        sum += pts;
                }
                r.OpponentStrength = sum;
            }


            // STEP 2 — Opponent Strength (SoS) = sum of FINAL points of opponents actually faced (BYEs ignored)

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

            for (int i = 0; i < table.Count; i++)
                table[i].Rank = i + 1;

            Logger.Log("[RR-RANK] Final sorted standings:");
            foreach (var r in table)
                Logger.Log($"  #{r.Rank} {idToName.GetValueOrDefault(r.DriverId, r.DriverId.ToString())}  Pts={r.Points:0.00}  W-L={r.Wins}-{r.Losses}  OS={r.OpponentStrength:0.00}");

            return table;
        }

        private static (int, int) PairKey(int a, int b) => (a < b) ? (a, b) : (b, a);

        private sealed class Aggregate
        {
            public double Points = 0;
            public int Wins = 0;
            public int Losses = 0;
            public HashSet<int> Defeated = new();
        }

        // Legacy alias
        private static (double Win, double Loss, double Bye) GetPoints(string lbl) =>
            PointsForRound(lbl);
    }
}
