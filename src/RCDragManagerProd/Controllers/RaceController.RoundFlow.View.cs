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

        internal void GetLaneAdjustedNames(EngineMatch m, out string leftName, out string rightName)
        {
            leftName = m.Driver1?.Name ?? "BYE";
            rightName = m.Driver2?.Name ?? "BYE";

            if (m.Driver1 == null || m.Driver2 == null) return;

            int a;
            int b;

            a = Math.Min(m.Driver1.Id, m.Driver2.Id);
            b = Math.Max(m.Driver1.Id, m.Driver2.Id);

            string laneKey;
            laneKey = m.RoundLabel + "|M" + m.MatchId + "|" + a + "-" + b;

            bool swap;
            swap = laneFairness.ShouldSwap(laneKey, m.Driver1.Id, m.Driver2.Id);

            if (swap)
            {
                string temp;
                temp = leftName;
                leftName = rightName;
                rightName = temp;
            }
        }

        private string BuildMatchDisplayText(EngineMatch m)
        {
            string l;
            string r;

            GetLaneAdjustedNames(m, out l, out r);
            return "M" + m.MatchId + ":" + l + " vs " + r;
        }

        public IReadOnlyList<PairingRow> BuildCurrentBracketRows()
        {
            var rows = new List<PairingRow>();

            Logger.Log(
                "[ROWS] BUILD v2 � snapshotMatches=" + (_rrMatchesSnapshot?.Count.ToString() ?? "null") + ", " +
                "snapshotRounds=" + (_rrRoundOrderSnapshot?.Count.ToString() ?? "null") + ", " +
                "engine=" + (_engine?.GetType().Name ?? "null") + ", losersEngine=" + (_losersEngine?.GetType().Name ?? "null") + ", " +
                "revealed=[" + string.Join(",", _revealedRounds) + "]");

            void AppendFrom(IEnumerable<EngineMatch> matches, IEnumerable<string> roundOrder, string tag, bool filterByRevealed)
            {
                if (matches == null || roundOrder == null)
                {
                    Logger.Log("[ROWS] AppendFrom(" + tag + ") skipped (matches/roundOrder null)");
                    return;
                }

                var before = rows.Count;
                var mList = matches.ToList();
                var rList = roundOrder.ToList();

                foreach (var round in rList)
                {
                    var normalizedRound = RoundLabels.Normalize(round);
                    if (filterByRevealed && !_revealedRounds.Contains(round) && !_revealedRounds.Contains(normalizedRound)) continue;

                    rows.Add(new PairingRow { IsHeader = true, RoundLabel = normalizedRound });

                    foreach (var m in mList.Where(x => string.Equals(RoundLabels.Normalize(x.RoundLabel), normalizedRound, StringComparison.OrdinalIgnoreCase)))
                    {
                        string leftName;
                        string rightName;

                        GetLaneAdjustedNames(m, out leftName, out rightName);

                        rows.Add(new PairingRow
                        {
                            MatchNumber = null,
                            MatchId = m.MatchId,
                            Driver1 = leftName,
                            Driver2 = rightName,
                            RoundLabel = RoundLabels.Normalize(m.RoundLabel)
                        });
                    }
                }

                Logger.Log("[ROWS] AppendFrom(" + tag + ") ? added " + (rows.Count - before) + " items. Total=" + rows.Count);
            }

            // ── Phase A: Round Robin ─────────────────────────────────────────────
            // Show RR snapshot if captured (post-RR transition), else show live RR engine.
            // The live path uses _revealedRounds (insertion order = chronological race order)
            // rather than EngineGetRoundOrder so display order matches the race schedule.
            if (_rrMatchesSnapshot != null && _rrRoundOrderSnapshot != null)
            {
                AppendFrom(_rrMatchesSnapshot, _rrRoundOrderSnapshot, "RR-snapshot", filterByRevealed: false);
            }
            else if (_engine != null)
            {
                // Use _revealedRounds for ordering — insertion order is the correct display order.
                AppendFrom(EngineGetMatches(_engine), _revealedRounds, "Main-live", filterByRevealed: true);
            }

            // ── Phase B: Losers Bracket ──────────────────────────────────────────
            // Always rendered AFTER RR and BEFORE Finals so display order is chronological.
            // During Finals show all LB rounds (unfiltered); otherwise only revealed rounds.
            if (_losersEngine != null)
            {
                bool filterLb = !string.Equals(_session?.RaceType, "Finals", StringComparison.OrdinalIgnoreCase);
                AppendFrom(EngineGetMatches(_losersEngine), EngineGetRoundOrder(_losersEngine), "Losers", filterByRevealed: filterLb);
            }

            // ── Phase C: Finals engine ───────────────────────────────────────────
            // Once we have an RR snapshot AND the current engine is a separate Finals
            // engine (not the LB adapter), show those matches after LB.
            if (_rrMatchesSnapshot != null && _engine != null && !ReferenceEquals(_engine, _losersEngine))
            {
                AppendFrom(EngineGetMatches(_engine), EngineGetRoundOrder(_engine), "Finals-live", filterByRevealed: true);
            }

            int displayNo = 1;
            foreach (var r in rows)
            {
                if (!r.IsHeader)
                {
                    r.MatchNumber = "M" + displayNo;
                    displayNo++;
                }
            }

            Logger.Log("[ROWS] BuiltCurrentBracketRows ? items=" + rows.Count + ", matches(numbered)=" + (displayNo - 1));
            return rows;
        }

        public bool IsLaneSwapped(int matchId, string roundLabel, int driver1Id, int driver2Id)
        {
            int a;
            int b;

            a = Math.Min(driver1Id, driver2Id);
            b = Math.Max(driver1Id, driver2Id);

            string laneKey;
            laneKey = roundLabel + "|M" + matchId + "|" + a + "-" + b;

            return laneFairness.ShouldSwap(laneKey, driver1Id, driver2Id);
        }

        // Returns the next unresolved matches in revealed rounds (current first).
        public IReadOnlyList<EngineMatch> PeekUpcomingMatches(int count = 3)
        {
            try
            {
                if (_engine == null || count <= 0) return Array.Empty<EngineMatch>();

                // In RR active-round mode, show upcoming matches for the active round only.
                var list = EngineGetMatches(_engine)
                                  .Where(m => (_activeRound != null
                                                   ? string.Equals(m.RoundLabel, _activeRound, StringComparison.OrdinalIgnoreCase)
                                                   : _revealedRounds.Contains(m.RoundLabel))
                                              && !m.HasResult)
                                  .OrderBy(m => m.MatchId)
                                  .Take(count)
                                  .ToList();

                // IMPORTANT: log lane-adjusted names so debug matches what UI bracket shows
                string peek;
                peek = string.Join(", ", list.Select(m => BuildMatchDisplayText(m)));

                Logger.Log("[CTRL][PEEK] Upcoming count=" + list.Count + ", take=" + count + " ? [" + peek + "]");

                return list;
            }
            catch (Exception ex)
            {
                Logger.Log("[CTRL][PEEK][ERROR] " + ex);
                return Array.Empty<EngineMatch>();
            }
        }

        // NEW: UI should use this instead of building labels from raw EngineMatch order
        public string BuildNextUpLabelText(int count = 3)
        {
            var list = PeekUpcomingMatches(count).ToList();
            if (list.Count == 0) return "No current match.";

            // Current
            string current;
            current = BuildMatchDisplayText(list[0]);
            current = current.Substring(current.IndexOf(':') + 1).Trim(); // "Name vs Name"

            if (list.Count == 1) return current;

            // On Deck / In The Hole
            string onDeck;
            onDeck = BuildMatchDisplayText(list[1]);
            onDeck = onDeck.Substring(onDeck.IndexOf(':') + 1).Trim();

            if (list.Count == 2) return "On Deck � " + onDeck;

            string inTheHole;
            inTheHole = BuildMatchDisplayText(list[2]);
            inTheHole = inTheHole.Substring(inTheHole.IndexOf(':') + 1).Trim();

            return "On Deck � " + onDeck + " / In The Hole � " + inTheHole;
        }

        public string GetActiveRoundLabel()
        {
            EnsureReady();

            // In RR mode, _activeRound tracks the pace-gated round explicitly.
            if (_activeRound != null)
            {
                Logger.Log("[CTRL][EDIT] Active round (explicit RR) = '" + _activeRound + "'");
                return _activeRound;
            }

            // Non-RR fallback: last revealed round (rounds revealed one at a time).
            string active = null;
            foreach (var r in EngineGetRoundOrder(_engine))
            {
                if (_revealedRounds.Contains(r))
                    active = r;   // last revealed
            }

            Logger.Log("[CTRL][EDIT] Active round = '" + (active ?? "null") + "'");
            return active;
        }

        public bool IsMatchInActiveRound(int matchId)
        {
            var m = GetMatch(matchId);
            var active = GetActiveRoundLabel();

            bool ok;
            ok = m != null &&
                 !string.IsNullOrEmpty(active) &&
                 string.Equals(m.RoundLabel, active, StringComparison.OrdinalIgnoreCase);

            Logger.Log("[CTRL][EDIT] IsMatchInActiveRound(M" + matchId + ") ? " + ok);
            return ok;
        }
    }
}
