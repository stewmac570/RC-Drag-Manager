// RaceController.RoundFlow.View.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;            // still used for a couple of info popups

using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.RandomMode;
using RCDragManagerProd.RoundRobinMode;
using RCDragManagerProd.UI.Forms;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Controllers
{
    public partial class RaceController
    {
        // Called by Form1 to rebuild the ListView each time the bracket changes.
        public IReadOnlyList<PairingRow> BuildCurrentBracketRows()
        {
            var rows = new List<PairingRow>();

            Logger.Log(
                $"[ROWS] BUILD v2 — snapshotMatches={_rrMatchesSnapshot?.Count.ToString() ?? "null"}, " +
                $"snapshotRounds={_rrRoundOrderSnapshot?.Count.ToString() ?? "null"}, " +
                $"engine={_engine?.GetType().Name ?? "null"}, losersEngine={_losersEngine?.GetType().Name ?? "null"}, " +
                $"revealed=[{string.Join(",", _revealedRounds)}]");

            void AppendFrom(IEnumerable<EngineMatch> matches, IEnumerable<string> roundOrder, string tag, bool filterByRevealed)
            {
                if (matches == null || roundOrder == null)
                {
                    Logger.Log($"[ROWS] AppendFrom({tag}) skipped (matches/roundOrder null)");
                    return;
                }

                var before = rows.Count;
                var mList = matches.ToList();
                var rList = roundOrder.ToList();

                foreach (var round in rList)
                {
                    if (filterByRevealed && !_revealedRounds.Contains(round)) continue;

                    rows.Add(new PairingRow { IsHeader = true, RoundLabel = round });

                    foreach (var m in mList.Where(x => x.RoundLabel == round))
                    {
                        rows.Add(new PairingRow
                        {
                            MatchNumber = null,
                            MatchId = m.MatchId,
                            Driver1 = m.Driver1?.Name ?? "BYE",
                            Driver2 = m.Driver2?.Name ?? "BYE",
                            RoundLabel = m.RoundLabel
                        });
                    }
                }

                Logger.Log($"[ROWS] AppendFrom({tag}) → added {rows.Count - before} items. Total={rows.Count}");
            }

            // 1) Round Robin — show ALL rounds if we have a snapshot; otherwise only revealed
            if (_rrMatchesSnapshot != null && _rrRoundOrderSnapshot != null)
            {
                AppendFrom(_rrMatchesSnapshot, _rrRoundOrderSnapshot, "RR-snapshot", filterByRevealed: false);
            }
            else if (_engine is RoundRobinEngineAdapter rrLive)
            {
                AppendFrom(rrLive.GetMatches(), rrLive.GetRoundOrder(), "RR-live", filterByRevealed: true);
            }
            // 2) Randomized — show only revealed rounds
            else if (_engine is RandomEngineAdapter rndLive)
            {
                AppendFrom(rndLive.GetMatches(), rndLive.GetRoundOrder(), "Random-live", filterByRevealed: true);
            }

            // 3) Losers Bracket — during Finals show all LB rounds; otherwise only revealed
            if (_losersEngine != null)
            {
                bool filterLb = !string.Equals(_session?.RaceType, "Finals", StringComparison.OrdinalIgnoreCase);
                AppendFrom(_losersEngine.GetMatches(), _losersEngine.GetRoundOrder(), "Losers", filterByRevealed: filterLb);
            }

            // 4) Finals (Pro Ladder) — only revealed (SF, then F)
            if (_engine is ProLadderEngineAdapter pro)
            {
                AppendFrom(pro.GetMatches(), pro.GetRoundOrder(), "Finals(Pro)", filterByRevealed: true);
            }

            int displayNo = 1;
            foreach (var r in rows)
                if (!r.IsHeader) r.MatchNumber = $"M{displayNo++}";

            Logger.Log($"[ROWS] BuiltCurrentBracketRows → items={rows.Count}, matches(numbered)={displayNo - 1}");
            return rows;
        }

        // Returns the next unresolved matches in revealed rounds (current first).
        public IReadOnlyList<EngineMatch> PeekUpcomingMatches(int count = 3)
        {
            try
            {
                if (_engine == null || count <= 0) return Array.Empty<EngineMatch>();

                var list = _engine.GetMatches()
                                  .Where(m => _revealedRounds.Contains(m.RoundLabel) && !m.HasResult)
                                  .OrderBy(m => m.MatchId)
                                  .Take(count)
                                  .ToList();

                Logger.Log($"[CTRL][PEEK] Upcoming count={list.Count}, take={count} → [{string.Join(", ", list.Select(m => $"M{m.MatchId}:{m.Driver1?.Name ?? "BYE"} vs {m.Driver2?.Name ?? "BYE"}"))}]");

                return list;
            }
            catch (Exception ex)
            {
                Logger.Log($"[CTRL][PEEK][ERROR] {ex}");
                return Array.Empty<EngineMatch>();
            }
        }

        public string GetActiveRoundLabel()
        {
            EnsureReady();
            string active = null;
            foreach (var r in _engine.GetRoundOrder())
                if (_revealedRounds.Contains(r))
                    active = r;   // last revealed
            Logger.Log($"[CTRL][EDIT] Active round = '{active ?? "null"}'");
            return active;
        }

        public bool IsMatchInActiveRound(int matchId)
        {
            var m = GetMatch(matchId);
            var active = GetActiveRoundLabel();
            bool ok = m != null && !string.IsNullOrEmpty(active) &&
                      string.Equals(m.RoundLabel, active, StringComparison.OrdinalIgnoreCase);
            Logger.Log($"[CTRL][EDIT] IsMatchInActiveRound(M{matchId}) → {ok}");
            return ok;
        }
    }
}
