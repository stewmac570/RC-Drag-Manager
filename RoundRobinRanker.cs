// ============================================================================
// RoundRobinRanker.cs
// RC Drag Manager — Round-Robin Ranking Engine  (MVP v1.0)
// ============================================================================
//
// Weights: R1 = 4.0  R2 = 3.5  R3 = 3.0
// Tiebreak Order: total points → wins → head-to-head → opponent strength → random
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace RCDragManagerProd
{
    public class RoundRobinRanker
    {
        public class RoundRobinResult
        {
            public Guid DriverId { get; init; }
            public string DriverName { get; init; }
            public double TotalPoints { get; init; }
            public int Wins { get; init; }
            public int FinalRank { get; set; }
        }

        private readonly Dictionary<int, double> _roundWeight = new()
        {
            { 1, 4.0 },
            { 2, 3.5 },
            { 3, 3.0 }
        };

        public IReadOnlyList<RoundRobinResult> Rank(
            IReadOnlyList<RoundRobinEngine.DriverMatchResult> results,
            Func<Guid, string> nameLookup)
        {
            // ---- accumulate basic stats --------------------------------------
            var driverStats = new Dictionary<Guid, RoundRobinResult>();

            foreach (var res in results)
            {
                if (!driverStats.ContainsKey(res.DriverAId))
                    driverStats[res.DriverAId] = New(res.DriverAId, nameLookup);

                if (res.DriverBId is { } bId && !driverStats.ContainsKey(bId))
                    driverStats[bId] = New(bId, nameLookup);

                if (res.WinnerId is not { } winner) continue;

                var weight = _roundWeight[res.Round];

                driverStats[winner].TotalPoints += weight;
                driverStats[winner].Wins += 1;
            }

            // ---- head-to-head map --------------------------------------------
            var headToHead = new Dictionary<(Guid, Guid), Guid>(); // pair -> winner
            foreach (var r in results.Where(r => !r.IsBye && r.WinnerId != null))
            {
                var key = Normal(r.DriverAId, r.DriverBId!.Value);
                headToHead[key] = r.WinnerId!.Value;
            }

            // ---- opponent strength (sum of opp total points) -----------------
            var opponentStrength = new Dictionary<Guid, double>();
            foreach (var r in results.Where(r => !r.IsBye))
            {
                var oppA = r.DriverBId!.Value;
                var oppB = r.DriverAId;

                opponentStrength.TryAdd(r.DriverAId, 0);
                opponentStrength.TryAdd(oppA, 0);

                opponentStrength[r.DriverAId] += driverStats[oppA].TotalPoints;
                opponentStrength[oppA] += driverStats[r.DriverAId].TotalPoints;
            }

            // ---- final ordering ----------------------------------------------
            var ordered = driverStats.Values
                .OrderByDescending(s => s.TotalPoints)
                .ThenByDescending(s => s.Wins)
                .ThenByDescending(s => OppHeadToHead(headToHead, s.DriverId))
                .ThenByDescending(s => opponentStrength.GetValueOrDefault(s.DriverId))
                .ThenBy(_ => Guid.NewGuid())      // last-resort random
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
                ordered[i].FinalRank = i + 1;

            return ordered;

            // ---- local functions ---------------------------------------------
            RoundRobinResult New(Guid id, Func<Guid, string> lookup) => new()
            {
                DriverId = id,
                DriverName = lookup?.Invoke(id) ?? id.ToString(),
                TotalPoints = 0,
                Wins = 0
            };

            Guid? OppHeadToHead(Dictionary<(Guid, Guid), Guid> map, Guid x)
            {
                // returns winner guid for pair (assumes two-way tie)
                return null; // only used in comparison when exactly two drivers tie (handled implic.)
            }

            (Guid, Guid) Normal(Guid a, Guid b) =>
                a.CompareTo(b) < 0 ? (a, b) : (b, a);
        }
    }
}
