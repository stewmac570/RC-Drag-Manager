using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RandomMode;

namespace RCDragManagerProd.RoundRobinMode
{
    /// <summary>
    /// Seeds the first buyback round so two car entries owned by the same driver do
    /// not race while an outside-driver pairing remains available. Later buyback
    /// rounds are deliberately left to the winner paths of the elimination tree.
    /// </summary>
    public sealed class MultiCarOpeningRoundPlanner
    {
        public void Arrange(IList<RandomMatch> matches, IEnumerable<Driver> entrants,
                            Func<Driver, int> ownerDriverId,
                            ISet<(int, int)> pairingHistory)
        {
            if (matches == null || entrants == null || ownerDriverId == null) return;

            var firstRound = matches
                .Where(m => m != null && string.Equals(m.RoundLabel, RoundLabels.Normalize("LB-R1"), StringComparison.OrdinalIgnoreCase)
                            && m.FromMatch1 == 0 && m.FromMatch2 == 0)
                .OrderBy(m => m.MatchId)
                .ToList();
            if (firstRound.Count == 0) return;

            var pool = entrants.Where(d => d != null).OrderBy(d => d.Id).ToList();
            while (pool.Count < firstRound.Count * 2) pool.Add(null);

            var pairs = new List<(Driver First, Driver Second)>();
            if (!FindPairs(pool, ownerDriverId, pairingHistory ?? new HashSet<(int, int)>(), pairs)) return;

            for (int i = 0; i < firstRound.Count; i++)
            {
                firstRound[i].Seed1 = pairs[i].First;
                firstRound[i].Seed2 = pairs[i].Second;
            }
        }

        private static bool FindPairs(List<Driver> remaining, Func<Driver, int> ownerDriverId,
                                      ISet<(int, int)> pairingHistory,
                                      List<(Driver First, Driver Second)> pairs)
        {
            if (remaining.Count == 0) return true;

            var first = remaining[0];
            var candidates = remaining.Skip(1)
                .OrderBy(candidate => PairPriority(first, candidate, ownerDriverId, pairingHistory))
                .ThenBy(candidate => candidate?.Id ?? int.MaxValue)
                .ToList();

            foreach (var second in candidates)
            {
                var next = new List<Driver>(remaining);
                next.Remove(first);
                next.Remove(second);
                pairs.Add((first, second));
                if (FindPairs(next, ownerDriverId, pairingHistory, pairs)) return true;
                pairs.RemoveAt(pairs.Count - 1);
            }

            return false;
        }

        private static int PairPriority(Driver first, Driver second, Func<Driver, int> ownerDriverId,
                                        ISet<(int, int)> pairingHistory)
        {
            if (first == null || second == null) return 4;

            bool sameOwner = ownerDriverId(first) == ownerDriverId(second);
            bool previouslyRaced = pairingHistory.Contains(Normalize(first.Id, second.Id));
            if (!sameOwner && !previouslyRaced) return 0;
            if (!sameOwner) return 1;
            if (!previouslyRaced) return 2;
            return 3;
        }

        private static (int, int) Normalize(int a, int b) => a < b ? (a, b) : (b, a);
    }
}
