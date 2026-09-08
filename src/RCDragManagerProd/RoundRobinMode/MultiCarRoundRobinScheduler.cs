using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RaceEngines;

namespace RCDragManagerProd.RoundRobinMode
{
    /// <summary>
    /// Builds a car-aware RR fixture. Every round first attempts a complete
    /// matching with no two cars owned by the same driver. A same-driver pairing
    /// is introduced only when no such round can be formed; unused pairings are
    /// preferred while they remain available.
    /// </summary>
    public sealed class MultiCarRoundRobinScheduler
    {
        public List<EngineMatch> Build(IEnumerable<MultiCarRaceEntry> entries, int roundsToRun)
        {
            var field = (entries ?? Enumerable.Empty<MultiCarRaceEntry>())
                .Where(e => e != null && e.RaceEntryId > 0 && e.DriverId > 0)
                .OrderBy(e => e.RaceEntryId)
                .ToList();

            if (field.Count < 2 || roundsToRun <= 0) return new List<EngineMatch>();

            var pairHistory = new HashSet<(int, int)>();
            var byeCount = field.ToDictionary(e => e.RaceEntryId, _ => 0);
            var result = new List<EngineMatch>();
            int matchId = 1;

            for (int round = 1; round <= roundsToRun; round++)
            {
                List<(MultiCarRaceEntry, MultiCarRaceEntry)> pairs = null;
                MultiCarRaceEntry bye = null;

                foreach (var candidateBye in CandidateByes(field, byeCount))
                {
                    var available = candidateBye == null
                        ? field
                        : field.Where(e => e.RaceEntryId != candidateBye.RaceEntryId).ToList();

                    // A round-robin must exhaust fresh pairings before it repeats
                    // an earlier race. Therefore a same-owner race is forced only
                    // after no all-new, cross-owner matching remains; it is still
                    // preferable to repeating an outside opponent.
                    pairs = FindPairs(available, pairHistory, requireNewPair: true, allowSameDriver: false) ??
                            FindPairs(available, pairHistory, requireNewPair: true, allowSameDriver: true) ??
                            FindPairs(available, pairHistory, requireNewPair: false, allowSameDriver: false) ??
                            FindPairs(available, pairHistory, requireNewPair: false, allowSameDriver: true);
                    if (pairs != null)
                    {
                        bye = candidateBye;
                        break;
                    }
                }

                if (pairs == null)
                    throw new InvalidOperationException("Could not create a complete Multi-Car Round Robin round.");

                foreach (var (left, right) in pairs)
                {
                    result.Add(new EngineMatch
                    {
                        MatchId = matchId++,
                        Driver1 = left.ToCompetitor(),
                        Driver2 = right.ToCompetitor(),
                        RoundLabel = RoundLabels.Normalize("RR" + round)
                    });
                    pairHistory.Add(PairKey(left.RaceEntryId, right.RaceEntryId));
                }

                if (bye != null)
                {
                    byeCount[bye.RaceEntryId]++;
                    result.Add(new EngineMatch
                    {
                        MatchId = matchId++,
                        Driver1 = bye.ToCompetitor(),
                        Driver2 = null,
                        RoundLabel = RoundLabels.Normalize("RR" + round)
                    });
                }
            }

            return result;
        }

        private static IEnumerable<MultiCarRaceEntry> CandidateByes(
            List<MultiCarRaceEntry> field, Dictionary<int, int> byeCount)
        {
            if (field.Count % 2 == 0) return new MultiCarRaceEntry[] { null };
            return field.OrderBy(e => byeCount[e.RaceEntryId]).ThenBy(e => e.RaceEntryId);
        }

        private static List<(MultiCarRaceEntry, MultiCarRaceEntry)> FindPairs(
            IEnumerable<MultiCarRaceEntry> entries,
            HashSet<(int, int)> history,
            bool requireNewPair,
            bool allowSameDriver)
        {
            var remaining = entries.OrderBy(e => e.RaceEntryId).ToList();
            var pairs = new List<(MultiCarRaceEntry, MultiCarRaceEntry)>();
            return PairNext(remaining, history, requireNewPair, allowSameDriver, pairs) ? pairs : null;
        }

        private static bool PairNext(
            List<MultiCarRaceEntry> remaining,
            HashSet<(int, int)> history,
            bool requireNewPair,
            bool allowSameDriver,
            List<(MultiCarRaceEntry, MultiCarRaceEntry)> pairs)
        {
            if (remaining.Count == 0) return true;

            var left = remaining
                .OrderBy(e => remaining.Count(other => other.RaceEntryId != e.RaceEntryId &&
                    IsAllowed(e, other, history, requireNewPair, allowSameDriver)))
                .ThenBy(e => e.RaceEntryId)
                .First();
            remaining.Remove(left);

            var candidates = remaining
                .Where(right => IsAllowed(left, right, history, requireNewPair, allowSameDriver))
                .OrderBy(right => history.Contains(PairKey(left.RaceEntryId, right.RaceEntryId)))
                .ThenBy(right => right.RaceEntryId)
                .ToList();

            foreach (var right in candidates)
            {
                remaining.Remove(right);
                pairs.Add((left, right));
                if (PairNext(remaining, history, requireNewPair, allowSameDriver, pairs)) return true;
                pairs.RemoveAt(pairs.Count - 1);
                remaining.Add(right);
            }

            remaining.Add(left);
            return false;
        }

        private static bool IsAllowed(MultiCarRaceEntry left, MultiCarRaceEntry right,
                                      HashSet<(int, int)> history, bool requireNewPair,
                                      bool allowSameDriver) =>
            (allowSameDriver || left.DriverId != right.DriverId) &&
            (!requireNewPair || !history.Contains(PairKey(left.RaceEntryId, right.RaceEntryId)));

        private static (int, int) PairKey(int a, int b) => a < b ? (a, b) : (b, a);
    }
}
