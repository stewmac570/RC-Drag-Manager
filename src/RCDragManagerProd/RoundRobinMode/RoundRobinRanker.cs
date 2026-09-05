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

        /// <summary>
        /// <see cref="RoundRobinRanker.HeadToHeadBonus"/> for each driver level with this
        /// one on <see cref="Points"/> that this one beat.
        /// </summary>
        public double HeadToHeadBonus { get; set; }

        /// <summary>
        /// What the placing is actually sorted on: points, plus the head-to-head bonus,
        /// plus the beaten-drivers score scaled down. Every part is shown as its own
        /// column, so the order on screen is arithmetic the operator can check rather
        /// than a sequence of hidden tiebreak rules.
        /// </summary>
        public double TotalScore { get; set; }
    }

    public sealed class RoundRobinRanker
    {
        /// <summary>
        /// Banked once per driver you are level with on points and beat.
        ///
        /// Sized so it can never overturn a whole win/loss point: a driver races three
        /// times, so the most they can bank is 0.3, and the beaten-drivers part adds at
        /// most 0.03 on top. A driver on 10 points can never be caught by one on 9.
        /// </summary>
        public const double HeadToHeadBonus = 0.1;

        /// <summary>
        /// Scales the beaten-drivers score into the total. Their combined points top out
        /// around 30, so this contributes at most 0.03 — small enough that it can only
        /// separate drivers the head-to-head bonus left level.
        /// </summary>
        public const double BeatenDriversWeight = 0.001;

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

                // BYE if either side missing
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
                    if (!stats.ContainsKey(winnerId.Value)) continue;
                    stats[winnerId.Value].Points += pts.Bye;
                    Logger.Log($"[RR-PTS] Match {m.MatchId} BYE → {wName} gains {pts.Bye:0.00} points");
                    continue;
                }

                // Normal resolved match
                if (winnerId != null && loserId != 0)
                {
                    if (!stats.ContainsKey(winnerId.Value) || !stats.ContainsKey(loserId)) continue;
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

            // Opponent score = the final win/loss points of the drivers you BEAT.
            //
            // This used to add every driver you raced, win or lose. That rewarded losing
            // to good drivers: in one meet the driver placed 5th with a single win had
            // the highest opponent score on the sheet, purely from who beat him. Counting
            // only your wins makes the number mean "I beat good drivers", which is worth
            // something. Beating the same driver twice still counts once.
            var pointsById = table.ToDictionary(x => x.DriverId, x => x.Points);

            foreach (var r in table)
            {
                double sum = 0;
                foreach (var beatenId in r.DefeatedIds)
                {
                    if (pointsById.TryGetValue(beatenId, out var pts))
                        sum += pts;
                }
                r.OpponentStrength = sum;
            }

            // Head-to-head as a scored column rather than a hidden sort step: a driver
            // banks the bonus once for each driver they are level with on points and
            // beat. A three-way tie therefore rewards beating two of them more than
            // beating one, which the old pairwise comparison could not express.
            var levelOnPoints = table.GroupBy(r => r.Points)
                                     .ToDictionary(g => g.Key, g => g.Select(r => r.DriverId).ToHashSet());

            foreach (var r in table)
            {
                var level = levelOnPoints[r.Points];
                int beatenAndLevel = r.DefeatedIds.Count(id => id != r.DriverId && level.Contains(id));
                r.HeadToHeadBonus = beatenAndLevel * HeadToHeadBonus;
                r.TotalScore = r.Points + r.HeadToHeadBonus + (r.OpponentStrength * BeatenDriversWeight);
            }

            // One number decides the order. The weights above guarantee each part can
            // only ever separate drivers the part above it left level.
            table.Sort((a, b) =>
            {
                int cmp = b.TotalScore.CompareTo(a.TotalScore);
                return cmp != 0 ? cmp : a.DriverId.CompareTo(b.DriverId);
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
