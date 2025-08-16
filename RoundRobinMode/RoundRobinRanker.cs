using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.DicEx;

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
            (double Win, double Loss, double Bye) pts;

            switch (key)
            {
                case "R1": pts = (4.0, 1.0, 2.0); break;
                case "R2": pts = (3.5, 0.75, 1.5); break;
                case "R3": pts = (3.0, 0.5, 1.0); break;
                default:
                    pts = (0, 0, 0);
                    Logger.Log($"[RR-PTS] Unknown round label '{lbl}' → using Win=0, Loss=0, BYE=0");
                    break;
            }

            Logger.Log($"[RR-PTS] Schedule for '{(string.IsNullOrEmpty(key) ? "(blank)" : key)}': Win={pts.Win:0.00}, Loss={pts.Loss:0.00}, BYE={pts.Bye:0.00}");
            return pts;
        }

        private readonly Dictionary<(int, int), int> _h2h = new();

        public List<DriverRankResult> Rank(
            List<RoundRobinMatch> matches,
            List<Driver> drivers,
            MatchResult results)
        {
            Logger.Log($"[RR-RANK] Starting ranking process — Drivers={drivers?.Count ?? 0}, Matches={matches?.Count ?? 0}");

            var idToName = new Dictionary<int, string>();
            var stats = new Dictionary<int, Aggregate>();

            if (drivers != null)
            {
                foreach (var d in drivers)
                {
                    if (d == null) continue;
                    if (!idToName.ContainsKey(d.Id))
                    {
                        idToName[d.Id] = d.Name;
                        stats[d.Id] = new Aggregate();
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
                bool isBye = m.Driver1 == null || m.Driver2 == null;

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
