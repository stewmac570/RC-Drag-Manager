// RaceController.RoundFlow.Defer.cs
//
// "Push to end of round" — when a racer needs more time, the Race Director can send
// the current (next-up) match to the back of its round so the others run first.
//
// This is a controller-level ordering concern only: engines are never reordered
// (they stay on the no-touch list). A per-round list of deferred match ids acts as a
// secondary sort key applied wherever the within-round race order is read
// (PushNextMatch, PeekUpcomingMatches, BuildCurrentBracketRows, the live "next up").
//
// Live-session only: the order is intentionally NOT persisted. A Save + reload
// returns matches to their natural MatchId order.
using System;
using System.Collections.Generic;
using System.Linq;

using RCDragManagerProd.Domain;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Controllers
{
    public partial class RaceController
    {
        // Match ids the operator has pushed to the back of the current round, in the
        // order they were pushed (first pushed = first among the deferred tail).
        private readonly List<int> _deferredMatchIds = new List<int>();

        /// <summary>Fires with true when the current round still has 2+ unraced matches
        /// (so "push to end of round" can do something), false otherwise.</summary>
        public event Action<bool> CanDeferChanged;

        /// <summary>True when the match this round currently determines is racing in the
        /// active round scope (RR active round, or any revealed round otherwise).</summary>
        private bool InActiveRaceScope(EngineMatch m) =>
            _activeRound != null
                ? string.Equals(m.RoundLabel, _activeRound, StringComparison.OrdinalIgnoreCase)
                : _revealedRounds.Contains(m.RoundLabel);

        /// <summary>Applies the operator's "push to end of round" ordering: non-deferred
        /// matches first (in MatchId order), then deferred matches in the order they were
        /// pushed back. Stable, so callers that already filtered keep their other ordering.</summary>
        internal IEnumerable<EngineMatch> ApplyRaceOrder(IEnumerable<EngineMatch> matches)
        {
            var spacing = BuildSpacingOrder();
            return matches
                .OrderBy(m => _deferredMatchIds.Contains(m.MatchId) ? 1 : 0)
                .ThenBy(m =>
                {
                    int i = _deferredMatchIds.IndexOf(m.MatchId);
                    return i < 0 ? 0 : i;
                })
                .ThenBy(m => spacing.TryGetValue(SpacingKey(m), out var rank) ? rank : int.MaxValue)
                .ThenBy(m => m.MatchId);
        }

        // ── Driver spacing ─────────────────────────────────────────────────────
        //
        // Race day 2026-10-03: a driver with several cars raced two of them back to
        // back, and a driver could run the last race of one round and the first of
        // the next. Within each round the races are put in the order that gives every
        // person the most races between their runs, counting the end of the previous
        // round. A bye is a solo pass, so it counts as a run.
        //
        // The order depends only on who is in each race, never on results, so it is
        // the same every time it is worked out (including after a resume) and a
        // round's order never moves once later rounds appear.

        private static string SpacingKey(EngineMatch m) =>
            RoundLabels.Normalize(m.RoundLabel ?? string.Empty).ToUpperInvariant() + "|" + m.MatchId;

        internal Dictionary<string, int> BuildSpacingOrder()
        {
            var ranks = new Dictionary<string, int>();
            List<EngineMatch> all;
            try { all = CollectAllRevealedMatchesAcrossPhases(); }
            catch { return ranks; }
            if (all == null || all.Count == 0) return ranks;

            var rounds = all.Select(m => m.RoundLabel ?? string.Empty)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

            var lastRun = new Dictionary<int, int>();   // person -> position of their last race
            int position = 0;

            foreach (var round in rounds)
            {
                var pending = all.Where(m => string.Equals(m.RoundLabel ?? string.Empty, round, StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(m => m.MatchId)
                                 .ToList();
                int rank = 0;

                while (pending.Count > 0)
                {
                    // First choice: a race where nobody raced in the previous race. Among
                    // those, the race whose people have the most races still to place in
                    // this round goes first: leaving a driver with four cars until last
                    // is what forces back-to-back runs. Then the longest wait.
                    EngineMatch best = null;
                    bool bestRested = false;
                    int bestGap = int.MinValue, bestLoad = int.MinValue;
                    foreach (var m in pending)
                    {
                        var people = PeopleIn(m);
                        int gap = people.Count == 0
                            ? int.MaxValue
                            : people.Min(p => lastRun.TryGetValue(p, out var at) ? position - at : int.MaxValue);
                        bool rested = gap > 1;
                        int load = people.Sum(p => pending.Count(o => !ReferenceEquals(o, m) && PeopleIn(o).Contains(p)));

                        bool better = best == null ||
                                      (rested && !bestRested) ||
                                      (rested == bestRested && (load > bestLoad ||
                                                                (load == bestLoad && gap > bestGap)));
                        if (better)
                        {
                            best = m; bestRested = rested; bestGap = gap; bestLoad = load;
                        }
                    }

                    ranks[SpacingKey(best)] = rank++;
                    foreach (var p in PeopleIn(best)) lastRun[p] = position;
                    position++;
                    pending.Remove(best);
                }
            }
            return ranks;
        }

        /// <summary>The real people in a race. In a Multi-Car class a competitor is a
        /// car, so it is mapped to the driver who owns it.</summary>
        private List<int> PeopleIn(EngineMatch m)
        {
            var people = new List<int>(2);
            foreach (var d in new[] { m.Driver1, m.Driver2 })
            {
                if (ByePolicy.IsBye(d)) continue;
                people.Add(PersonFor(d.Id));
            }
            return people;
        }

        private int PersonFor(int competitorId)
        {
            if (!IsMultiCarRoundRobin || _session?.DriverEntries == null) return competitorId;
            var entry = _session.DriverEntries.FirstOrDefault(e => e != null && e.RaceEntryId == competitorId);
            // Negative so an unmapped car can never collide with a real driver id.
            return entry != null && entry.DriverID > 0 ? entry.DriverID : -competitorId;
        }

        /// <summary>Sends the current (next-up) match to the back of its round. No-op when
        /// fewer than two unraced matches remain (nothing to run ahead of it).</summary>
        public void PushCurrentMatchToEndOfRound()
        {
            if (_engine == null)
            {
                Logger.Log("[CTRL][DEFER] PushCurrentMatchToEndOfRound ignored — no engine.");
                return;
            }

            var unresolved = ApplyRaceOrder(
                    EngineGetMatches(_engine).Where(m => InActiveRaceScope(m) && !m.HasResult))
                .ToList();

            if (unresolved.Count < 2)
            {
                Logger.Log("[CTRL][DEFER] PushCurrentMatchToEndOfRound ignored — fewer than 2 races left in round.");
                return;
            }

            var current = unresolved[0];
            _deferredMatchIds.Remove(current.MatchId); // re-add at the tail if already deferred
            _deferredMatchIds.Add(current.MatchId);

            Logger.Log($"[CTRL][DEFER] Pushed M{current.MatchId} ({current.RoundLabel}) to end of round. " +
                       $"DeferOrder=[{string.Join(",", _deferredMatchIds)}]");

            BracketRedrawn?.Invoke(BuildCurrentBracketRows());
            PushNextMatch();              // re-points "Next Up" + refreshes CanDefer
            QueueLiveUpdate("PushToEndOfRound");
        }

        /// <summary>Recomputes whether "push to end of round" is currently actionable.</summary>
        internal void PushDeferState()
        {
            bool canDefer = false;
            if (_engine != null)
            {
                int unresolved = EngineGetMatches(_engine).Count(m => InActiveRaceScope(m) && !m.HasResult);
                canDefer = unresolved >= 2;
            }
            CanDeferChanged?.Invoke(canDefer);
        }

        /// <summary>Drops all push-to-back ordering. Called when the round advances or the
        /// bracket is (re)generated/reset — deferrals are scoped to a single live round.</summary>
        internal void ClearDeferrals()
        {
            if (_deferredMatchIds.Count == 0) return;
            _deferredMatchIds.Clear();
            Logger.Log("[CTRL][DEFER] Cleared deferrals.");
        }
    }
}
