using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCDragManagerProd.RaceEngines;

namespace RCDragManagerProd
{
    public static class RoundRobinScorecardLogger
    {

        // Display-only weighting (keeps base points dominant)
        private const double WEIGHT_WINS = 0.01;       // +0.01 per win
        private const double WEIGHT_H2H = 0.001;      // +0.001 per head-to-head net win within the tie group
        private const double WEIGHT_SOS = 0.000001;   // +0.000001 per SoS point
        private sealed class Line
        {
            public string RoundLabel;
            public string Outcome;      // "W", "L", "BYE"
            public double Points;
            public string Opponent;
            public int? OpponentId;
        }

        // ─────────────────────────────────────────────────────────────
        // Public: Writes detailed scorecards to Logger.
        // ─────────────────────────────────────────────────────────────
        public static void Log(RoundRobinEngineAdapter rr, MatchResult results)
        {
            var matches = rr?.GetMatches()?.ToList() ?? new List<EngineMatch>();
            if (matches.Count == 0)
            {
                Logger.Log("[RR-SCORE] No matches to score.");
                return;
            }

            var drivers = matches.SelectMany(m => new[] { m.Driver1, m.Driver2 })
                                 .Where(d => d != null)
                                 .GroupBy(d => d.Id)
                                 .Select(g => g.First())
                                 .OrderBy(d => d.Name)
                                 .ToList();

            var idToName = drivers.ToDictionary(d => d.Id, d => d.Name);
            var rounds = matches.Select(m => m.RoundLabel).Distinct().OrderBy(x => x).ToList();

            // Points schedule
            Logger.Log("[RR-SCORE] Points schedule:");
            foreach (var r in rounds)
            {
                var pts = PointsFor(r);
                Logger.Log($"  {r}: W={pts.Win:0.00}  L={pts.Loss:0.00}  BYE={pts.Bye:0.00}");
            }

            var lines = new Dictionary<int, List<Line>>();
            var totals = new Dictionary<int, double>();
            var wins = new Dictionary<int, int>();
            var losses = new Dictionary<int, int>();
            var defeated = new Dictionary<int, HashSet<int>>();

            // helpers (no local funcs)
            Action<int> ensure = id =>
            {
                if (!lines.ContainsKey(id)) lines[id] = new List<Line>();
                if (!totals.ContainsKey(id)) totals[id] = 0;
                if (!wins.ContainsKey(id)) wins[id] = 0;
                if (!losses.ContainsKey(id)) losses[id] = 0;
                if (!defeated.ContainsKey(id)) defeated[id] = new HashSet<int>();
            };

            Action<int, Line> addLine = (id, ln) =>
            {
                if (!lines.TryGetValue(id, out var lst))
                {
                    lst = new List<Line>();
                    lines[id] = lst;
                }
                lst.Add(ln);
            };

            // Score each match
            foreach (var m in matches)
            {
                var pts = PointsFor(m.RoundLabel);
                var w = results.GetWinner(m.MatchId);
                var l = results.GetLoser(m.MatchId);
                var d1 = m.Driver1;
                var d2 = m.Driver2;

                // BYE (one side null) — only when a winner exists
                if ((d1 == null || d2 == null) && w != null)
                {
                    ensure(w.Id);
                    addLine(w.Id, new Line
                    {
                        RoundLabel = m.RoundLabel,
                        Outcome = "BYE",
                        Points = pts.Bye,
                        Opponent = "BYE",
                        OpponentId = null
                    });
                    totals[w.Id] += pts.Bye;
                    continue;
                }

                // Not resolved yet
                if (w == null || l == null) continue;

                ensure(w.Id);
                ensure(l.Id);

                // Winner
                addLine(w.Id, new Line
                {
                    RoundLabel = m.RoundLabel,
                    Outcome = "W",
                    Points = pts.Win,
                    Opponent = idToName.TryGetValue(l.Id, out var ln) ? ln : (l.Name ?? "—"),
                    OpponentId = l.Id
                });
                totals[w.Id] += pts.Win;
                wins[w.Id] += 1;
                defeated[w.Id].Add(l.Id);

                // Loser
                addLine(l.Id, new Line
                {
                    RoundLabel = m.RoundLabel,
                    Outcome = "L",
                    Points = pts.Loss,
                    Opponent = idToName.TryGetValue(w.Id, out var wn) ? wn : (w.Name ?? "—"),
                    OpponentId = w.Id
                });
                totals[l.Id] += pts.Loss;
                losses[l.Id] += 1;
            }

            // SoS (sum of opponents' final totals actually faced; BYE=0)
            var totalsSnapshot = totals.ToDictionary(k => k.Key, v => v.Value);
            var sos = drivers.ToDictionary(d => d.Id, _ => 0.0);
            foreach (var d in drivers)
            {
                if (!lines.TryGetValue(d.Id, out var lns)) continue;
                double sum = 0;
                foreach (var ln in lns)
                {
                    if (ln?.OpponentId.HasValue == true &&
                        totalsSnapshot.TryGetValue(ln.OpponentId.Value, out var oppPts))
                    {
                        sum += oppPts;
                    }
                }
                sos[d.Id] = sum;
            }

            // Detailed scorecards
            Logger.Log("[RR-SCORE] Detailed scorecards:");
            var ordered = drivers
                .OrderByDescending(d => totals.TryGetValue(d.Id, out var tp) ? tp : 0.0)
                .ThenByDescending(d => wins.TryGetValue(d.Id, out var w) ? w : 0)
                .ThenBy(d => d.Name)
                .ToList();

            int rank = 1;
            foreach (var d in ordered)
            {
                totals.TryGetValue(d.Id, out var tp);
                wins.TryGetValue(d.Id, out var w);
                losses.TryGetValue(d.Id, out var l);
                var defNames = (defeated.TryGetValue(d.Id, out var set) && set.Count > 0)
                    ? string.Join(",", set.Select(x => idToName.TryGetValue(x, out var nm) ? nm : x.ToString()))
                    : "-";

                Logger.Log($"  #{rank++} {d.Name} — Pts={tp:0.00} (W-L {w}-{l})  SoS={(sos.TryGetValue(d.Id, out var sv) ? sv : 0.0):0.00}");

                if (lines.TryGetValue(d.Id, out var lns))
                {
                    foreach (var ln in lns.OrderBy(x => x.RoundLabel))
                        Logger.Log($"      {ln.RoundLabel}: {ln.Outcome}(+{ln.Points:0.00}) vs {ln.Opponent}");
                }
                Logger.Log($"      Defeated: {defNames}");
            }

            // Tie notes (adjacent equals by Pts & Wins)
            Logger.Log("[RR-SCORE] Tie-break notes (H2H among ties):");
            for (int i = 0; i < ordered.Count - 1; i++)
            {
                var a = ordered[i];
                var b = ordered[i + 1];

                totals.TryGetValue(a.Id, out var at);
                totals.TryGetValue(b.Id, out var bt);
                wins.TryGetValue(a.Id, out var aw);
                wins.TryGetValue(b.Id, out var bw);

                if (Math.Abs(at - bt) < 1e-9 && aw == bw)
                {
                    var note = HeadToHead(results, matches, a.Id, b.Id, idToName);
                    Logger.Log($"  {a.Name} vs {b.Name} → {note}");
                }
            }

            // Engine order (for comparison)
            var engineOrder = rr.GetTopRankedDrivers(drivers.Count);
            Logger.Log("[RR-SCORE] Engine final order:");
            int n = 1;
            foreach (var d in engineOrder)
                Logger.Log($"  #{n++} {d.Name}");
        }

        // ─────────────────────────────────────────────────────────────
        // Public: Builds scorecard text for popup MessageBox.
        // ─────────────────────────────────────────────────────────────
        public static string BuildScorecard(RoundRobinEngineAdapter rr, MatchResult results)
        {
            var matches = rr?.GetMatches()?.ToList() ?? new List<EngineMatch>();
            if (matches.Count == 0) return "No Round Robin matches to score.";

            var drivers = matches.SelectMany(m => new[] { m.Driver1, m.Driver2 })
                                 .Where(d => d != null)
                                 .GroupBy(d => d.Id)
                                 .Select(g => g.First())
                                 .OrderBy(d => d.Name)
                                 .ToList();

            var idToName = drivers.ToDictionary(d => d.Id, d => d.Name);

            // ── aggregates ────────────────────────────────────────────────
            var lines = new Dictionary<int, List<Line>>();
            var totals = new Dictionary<int, double>();
            var wins = new Dictionary<int, int>();
            var losses = new Dictionary<int, int>();
            var defeated = new Dictionary<int, HashSet<int>>();

            Action<int> ensure = id =>
            {
                if (!lines.ContainsKey(id)) lines[id] = new List<Line>();
                if (!totals.ContainsKey(id)) totals[id] = 0;
                if (!wins.ContainsKey(id)) wins[id] = 0;
                if (!losses.ContainsKey(id)) losses[id] = 0;
                if (!defeated.ContainsKey(id)) defeated[id] = new HashSet<int>();
            };

            Action<int, Line> addLine = (id, ln) =>
            {
                if (!lines.TryGetValue(id, out var lst))
                {
                    lst = new List<Line>();
                    lines[id] = lst;
                }
                lst.Add(ln);
            };

            foreach (var m in matches)
            {
                var p = PointsFor(m.RoundLabel);
                var w = results.GetWinner(m.MatchId);
                var l = results.GetLoser(m.MatchId);
                var d1 = m.Driver1;
                var d2 = m.Driver2;

                if ((d1 == null || d2 == null) && w != null)
                {
                    ensure(w.Id);
                    addLine(w.Id, new Line { RoundLabel = m.RoundLabel, Outcome = "BYE", Points = p.Bye, Opponent = "BYE", OpponentId = null });
                    totals[w.Id] += p.Bye;
                    continue;
                }

                if (w == null || l == null) continue;

                ensure(w.Id);
                ensure(l.Id);

                addLine(w.Id, new Line { RoundLabel = m.RoundLabel, Outcome = "W", Points = p.Win, Opponent = idToName.TryGetValue(l.Id, out var ln) ? ln : (l.Name ?? "—"), OpponentId = l.Id });
                totals[w.Id] += p.Win;
                wins[w.Id] += 1;
                defeated[w.Id].Add(l.Id);

                addLine(l.Id, new Line { RoundLabel = m.RoundLabel, Outcome = "L", Points = p.Loss, Opponent = idToName.TryGetValue(w.Id, out var wn) ? wn : (w.Name ?? "—"), OpponentId = w.Id });
                totals[l.Id] += p.Loss;
                losses[l.Id] += 1;
            }

            // Strength of Schedule (sum opponents' totals actually faced; BYE excluded)
            var sos = drivers.ToDictionary(d => d.Id, _ => 0.0);
            foreach (var d in drivers)
            {
                if (!lines.TryGetValue(d.Id, out var lns)) continue;
                double sum = 0;
                foreach (var ln in lns)
                {
                    if (ln?.OpponentId.HasValue == true && totals.TryGetValue(ln.OpponentId.Value, out var oppPts))
                        sum += oppPts;
                }
                sos[d.Id] = sum;
            }

            // Head-to-Head score within tie groups (same TotalPts & Wins)
            var h2hScore = drivers.ToDictionary(d => d.Id, _ => 0); // net wins vs others in tie group
                                                                    // group key uses rounded points to avoid double precision noise
            var groups = drivers.GroupBy(d =>
            {
                totals.TryGetValue(d.Id, out var tp);
                wins.TryGetValue(d.Id, out var w);
                return $"{tp:0.00}|{w}";
            });

            foreach (var g in groups)
            {
                var groupList = g.ToList();
                if (groupList.Count <= 1) continue;

                foreach (var a in groupList)
                {
                    foreach (var b in groupList)
                    {
                        if (a.Id == b.Id) continue;

                        // find a vs b match (there will be at most one in RR)
                        foreach (var m in matches)
                        {
                            int d1 = m.Driver1?.Id ?? -1;
                            int d2 = m.Driver2?.Id ?? -1;
                            if (!((d1 == a.Id && d2 == b.Id) || (d1 == b.Id && d2 == a.Id)))
                                continue;

                            var w = results.GetWinner(m.MatchId);
                            if (w == null) continue;

                            if (w.Id == a.Id) h2hScore[a.Id] += 1;
                            else if (w.Id == b.Id) h2hScore[a.Id] -= 1;

                            break; // found the head-to-head
                        }
                    }
                }
            }

            // Build popup with CompositeScore that encodes tie-breaks numerically
            var sb = new StringBuilder();
            sb.AppendLine("Round Robin — Standings");
            sb.AppendLine($"Schedule: R1(W=4 L=1 BYE=2), R2(W=3.5 L=0.75 BYE=1.5), R3(W=3 L=0.5 BYE=1)");
            sb.AppendLine($"Composite = Pts"
                + $" + Wins×{WEIGHT_WINS:0.####}"
                + $" + H2H×{WEIGHT_H2H:0.####}"
                + $" + SoS×{WEIGHT_SOS:0.######}");
            sb.AppendLine();

            var ordered = drivers
                .OrderByDescending(d => totals.TryGetValue(d.Id, out var tp) ? tp : 0.0)
                .ThenByDescending(d => wins.TryGetValue(d.Id, out var w) ? w : 0)
                .ThenBy(d => d.Name)
                .ToList();

            int rank = 1;
            foreach (var d in ordered)
            {
                totals.TryGetValue(d.Id, out var tp);
                wins.TryGetValue(d.Id, out var w);
                losses.TryGetValue(d.Id, out var l);
                var s = sos.TryGetValue(d.Id, out var sVal) ? sVal : 0.0;
                var h = h2hScore.TryGetValue(d.Id, out var hVal) ? hVal : 0;

                double comp = tp + (w * WEIGHT_WINS) + (h * WEIGHT_H2H) + (s * WEIGHT_SOS);

                var name = idToName.TryGetValue(d.Id, out var nm) ? nm : (d.Name ?? d.Id.ToString());
                sb.AppendLine($"#{rank++} {name} — Score={comp:0.000000}  (Pts={tp:0.00}, W={w}, H2H={h}, SoS={s:0.00})");


                if (lines.TryGetValue(d.Id, out var lnz))
                {
                    foreach (var ln in lnz.OrderBy(x => x.RoundLabel))
                        sb.AppendLine($"   {ln.RoundLabel}: {ln.Outcome}(+{ln.Points:0.00}) vs {ln.Opponent}");
                }

                if (defeated.TryGetValue(d.Id, out var def) && def.Count > 0)
                {
                    var names = def.Select(id => idToName.TryGetValue(id, out var nm) ? nm : id.ToString());
                    sb.AppendLine($"   Defeated: {string.Join(", ", names)}");
                }
                sb.AppendLine();
            }

            // Quick tie notes (optional – keeps your old explanation)
            sb.AppendLine("Tie-break notes (display Composite encodes these):");
            for (int i = 0; i < ordered.Count - 1; i++)
            {
                var a = ordered[i];
                var b = ordered[i + 1];

                totals.TryGetValue(a.Id, out var at);
                totals.TryGetValue(b.Id, out var bt);
                wins.TryGetValue(a.Id, out var aw);
                wins.TryGetValue(b.Id, out var bw);

                if (Math.Abs(at - bt) < 1e-9 && aw == bw)
                    sb.AppendLine($"  {idToName[a.Id]} vs {idToName[b.Id]} → H2H={(h2hScore[a.Id] - h2hScore[b.Id])}, SoS={(sos[a.Id] - sos[b.Id]):0.00}");
            }

            return sb.ToString().TrimEnd();
        }


        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────
        private static (double Win, double Loss, double Bye) PointsFor(string lbl)
        {
            switch ((lbl ?? "").ToUpperInvariant())
            {
                case "R1": return (4.0, 1.0, 2.0);
                case "R2": return (3.5, 0.75, 1.5);
                case "R3": return (3.0, 0.5, 1.0);
                default: return (0, 0, 0);
            }
        }

        private static string HeadToHead(MatchResult results, List<EngineMatch> matches, int aId, int bId, Dictionary<int, string> idToName)
        {
            foreach (var m in matches)
            {
                int d1 = m.Driver1?.Id ?? -1;
                int d2 = m.Driver2?.Id ?? -1;
                if (!((d1 == aId && d2 == bId) || (d1 == bId && d2 == aId))) continue;

                var w = results.GetWinner(m.MatchId);
                if (w == null) continue;
                return idToName.TryGetValue(w.Id, out var nm) ? nm : (w.Name ?? w.Id.ToString());
            }
            return "no H2H";
        }
    }
}
