using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.RaceEngines;

namespace RCDragManagerProd.RoundRobinMode
{
    public static partial class RoundRobinScorecardLogger
    {
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
            var byes = new Dictionary<int, int>();
            var defeated = new Dictionary<int, HashSet<int>>();

            Action<int> ensure = id =>
            {
                if (!lines.ContainsKey(id)) lines[id] = new List<Line>();
                if (!totals.ContainsKey(id)) totals[id] = 0;
                if (!wins.ContainsKey(id)) wins[id] = 0;
                if (!losses.ContainsKey(id)) losses[id] = 0;
                if (!byes.ContainsKey(id)) byes[id] = 0;
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
                    byes[w.Id] += 1;
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

            // Build popup with per-driver detail and tiebreaker explanations
            var sb = new StringBuilder();
            sb.AppendLine("Round Robin — Detailed Scorecard");
            sb.AppendLine($"Scoring: RR1(W=4 L=1 BYE=2), RR2(W=3.5 L=0.75 BYE=1.5), RR3(W=3 L=0.5 BYE=1)");
            sb.AppendLine("Tiebreakers: (1) Head-to-head  (2) Opponent Strength (SoS = sum of opponents' final points)");
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
                byes.TryGetValue(d.Id, out var byeCount);
                var s = sos.TryGetValue(d.Id, out var sVal) ? sVal : 0.0;
                var h = h2hScore.TryGetValue(d.Id, out var hVal) ? hVal : 0;

                double comp = tp + (w * WEIGHT_WINS) + (h * WEIGHT_H2H) + (s * WEIGHT_SOS);

                var name = idToName.TryGetValue(d.Id, out var nm) ? nm : (d.Name ?? d.Id.ToString());
                sb.AppendLine($"#{rank++} {name}");
                sb.AppendLine($"   Total: {tp:0.00} pts  |  W={w}  L={l}  BYE={byeCount}  |  SoS={s:0.00}  [rank score={comp:0.000000}]");

                if (lines.TryGetValue(d.Id, out var lnz))
                {
                    foreach (var ln in lnz.OrderBy(x => RoundLabels.CompareKey(x.RoundLabel)))
                        sb.AppendLine($"   {ln.RoundLabel}: {ln.Outcome} (+{ln.Points:0.00}) vs {ln.Opponent}");

                    var sosDetails = lnz
                        .Where(x => x.OpponentId.HasValue)
                        .OrderBy(x => RoundLabels.CompareKey(x.RoundLabel))
                        .Select(x =>
                        {
                            totals.TryGetValue(x.OpponentId.Value, out var oppPts);
                            return $"{x.Opponent}={oppPts:0.00}";
                        })
                        .ToList();
                    if (sosDetails.Count > 0)
                        sb.AppendLine($"   SoS: {string.Join(" + ", sosDetails)} = {s:0.00}");
                }

                sb.AppendLine();
            }

            bool anyTie = false;
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
                    if (!anyTie) { sb.AppendLine("Tiebreaker resolutions:"); anyTie = true; }

                    var aName = idToName[a.Id];
                    var bName = idToName[b.Id];

                    if (h2hScore[a.Id] != h2hScore[b.Id])
                    {
                        var h2hWinner = HeadToHead(results, matches, a.Id, b.Id, idToName);
                        sb.AppendLine($"  {aName} vs {bName} — tied on {at:0.00} pts, {aw} wins");
                        sb.AppendLine($"    Resolved by head-to-head: {h2hWinner} won the direct match");
                    }
                    else
                    {
                        sb.AppendLine($"  {aName} vs {bName} — tied on {at:0.00} pts, {aw} wins, H2H even");
                        sb.AppendLine($"    Resolved by opponent strength: {aName}={sos[a.Id]:0.00} pts vs {bName}={sos[b.Id]:0.00} pts");
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
