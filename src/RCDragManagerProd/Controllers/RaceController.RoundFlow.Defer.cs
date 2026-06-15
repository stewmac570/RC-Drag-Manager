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
            return matches
                .OrderBy(m => _deferredMatchIds.Contains(m.MatchId) ? 1 : 0)
                .ThenBy(m =>
                {
                    int i = _deferredMatchIds.IndexOf(m.MatchId);
                    return i < 0 ? 0 : i;
                })
                .ThenBy(m => m.MatchId);
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
