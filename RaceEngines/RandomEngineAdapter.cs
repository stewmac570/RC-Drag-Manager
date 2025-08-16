using System;
using System.Collections.Generic;
using System.Linq;

using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;    // only if returning VM rows
using RCDragManagerProd.Logging;      // Logger 
using RCDragManagerProd.RandomMode;

namespace RCDragManagerProd.RaceEngines
{
    public class RandomEngineAdapter : IRaceEngine
    {
        // ──────────────────── STATE ────────────────────
        private readonly RandomMatchEngine _engine;

        // Global BYE tracking across the whole randomized event
        private readonly Dictionary<int, int> _byeCount = new(); // DriverId -> BYEs seen
        private int? _lastByeRecipient = null;                    // prevent consecutive BYEs
        private readonly Random _rng = new Random();

        public RandomEngineAdapter()
        {
            _engine = new RandomMatchEngine();
            Logger.Log("[RND] Adapter created (internal engine).");
        }

        public RandomEngineAdapter(RandomMatchEngine engine)
        {
            _engine = engine ?? new RandomMatchEngine();
            Logger.Log("[RND] Adapter created (external engine supplied).");
        }

        // ──────────────────── IRaceEngine passthroughs ────────────────────
        public void LoadDrivers(List<Driver> drivers)
        {
            _byeCount.Clear();
            _lastByeRecipient = null;

            _engine.LoadDrivers(drivers ?? new List<Driver>());
            Logger.Log($"[RND] LoadDrivers: {drivers?.Count ?? 0} driver(s). BYE counters reset.");
        }

        public void GenerateBracket()
        {
            _engine.GenerateBracket();
            Logger.Log("[RND] GenerateBracket → underlying engine built initial round(s).");

            // Sync BYE counters from whatever the engine produced (e.g., if it inserted a BYE in R1)
            RecomputeByeCountsFromSchedule();
        }

        public void Reset()
        {
            _engine.Reset();
            _byeCount.Clear();
            _lastByeRecipient = null;
            Logger.Log("[RND] Reset: engine + BYE counters cleared.");
        }

        public void InjectMatches(List<RandomMatch> matches)
        {
            Logger.Log($"[RND] InjectMatches: {matches?.Count ?? 0} matches.");
            _engine.LoadMatches(matches ?? new List<RandomMatch>());
            RecomputeByeCountsFromSchedule();
        }

        public IReadOnlyList<EngineMatch> GetMatches()
        {
            return _engine.GetMatches().Select(m => new EngineMatch
            {
                MatchId = m.MatchId,
                Driver1 = m.Seed1,
                Driver2 = m.Seed2,
                RoundLabel = m.RoundLabel,
                FromMatch1 = m.FromMatch1,
                FromMatch2 = m.FromMatch2,
                HasResult = _engine.HasWinner(m.MatchId)
            }).ToList();
        }

        public IReadOnlyList<string> GetRoundOrder() => _engine.GetRoundOrder();

        public void SetWinner(int matchId, Driver winner) => _engine.SetWinner(matchId, winner);

        public bool HasWinner(int matchId) => _engine.HasWinner(matchId);

        public Driver GetWinner()
        {
            var matches = _engine.GetMatches();
            // Prefer an explicit "Final"
            var final = matches.FirstOrDefault(m =>
                (m.RoundLabel ?? "").IndexOf("final", StringComparison.OrdinalIgnoreCase) >= 0);

            if (final == null)
            {
                // Fallback: last labeled round in the order
                var order = _engine.GetRoundOrder();
                string lastRound = (order != null && order.Count > 0) ? order[order.Count - 1] : null;

                if (!string.IsNullOrEmpty(lastRound))
                    final = matches.LastOrDefault(m => string.Equals(m.RoundLabel, lastRound, StringComparison.Ordinal));
                if (final == null)
                {
                    Logger.Log("❌ RandomEngineAdapter.GetWinner → no final match found (label or order).");
                    return null;
                }
            }

            var w = _engine.GetWinner(final.MatchId);
            Logger.Log(w != null
                ? $"🏆 RandomEngineAdapter.GetWinner → {w.Name} (M{final.MatchId})"
                : $"⚠️ RandomEngineAdapter.GetWinner → Match {final.MatchId} has no winner");
            return w;
        }

        // ──────────────────── New: fair next-round builder ────────────────────
        /// <summary>
        /// Build the next randomized round with a fair BYE rule (no repeats/consecutive if avoidable).
        /// Call this from the controller when the user clicks "Generate Next Round" for Randomized races.
        /// </summary>
        public void GenerateNextRoundFair()
        {
            var all = _engine.GetMatches().ToList();
            var order = _engine.GetRoundOrder().ToList();
            if (order.Count == 0)
            {
                Logger.Log("❌ [RND] GenerateNextRoundFair: no existing rounds to build from.");
                return;
            }

            string lastRound = order[order.Count - 1];
            var lastRoundMatches = all.Where(m => string.Equals(m.RoundLabel, lastRound, StringComparison.Ordinal)).ToList();

            // winners that actually advanced
            var winners = new List<Driver>();
            foreach (var m in lastRoundMatches)
            {
                var w = _engine.HasWinner(m.MatchId) ? _engine.GetWinner(m.MatchId) : null;
                if (w != null) winners.Add(w);
            }

            Logger.Log($"[RND] GenerateNextRoundFair: lastRound='{lastRound}'  winners={winners.Count}");

            if (winners.Count == 0)
            {
                Logger.Log("⚠️ [RND] No resolved winners in the last round — next round not generated.");
                return;
            }

            // Label the next round
            string nextLabel = LabelForNextRound(order);
            var nextRound = new List<RandomMatch>();
            var pool = winners.ToList();

            // BYE selection (odd only)
            Driver byeRecipient = null;
            if ((pool.Count % 2) == 1)
            {
                byeRecipient = ChooseByeRecipientFair(pool);
                if (byeRecipient != null)
                {
                    _byeCount.TryGetValue(byeRecipient.Id, out int before);
                    _byeCount[byeRecipient.Id] = before + 1;
                    _lastByeRecipient = byeRecipient.Id;

                    Logger.Log($"[RND] BYE awarded to {byeRecipient.Name} (DriverId={byeRecipient.Id}) — " +
                               $"BYEs now={_byeCount[byeRecipient.Id]} (next round label={nextLabel}).");

                    // Create the BYE match
                    nextRound.Add(new RandomMatch
                    {
                        MatchId = NextMatchId(all),
                        Seed1 = byeRecipient,
                        Seed2 = null,          // BYE
                        RoundLabel = nextLabel,
                        FromMatch1 = null,
                        FromMatch2 = null
                    });

                    // remove from pairing pool
                    pool.RemoveAll(d => d.Id == byeRecipient.Id);
                }
                else
                {
                    Logger.Log("⚠️ [RND] No valid BYE recipient found — will pair all, engine may handle overhang.");
                }
            }

            // Shuffle and pair the rest
            pool = pool.OrderBy(_ => _rng.Next()).ToList();
            for (int i = 0; i + 1 < pool.Count; i += 2)
            {
                var d1 = pool[i];
                var d2 = pool[i + 1];

                if (d1.Id == d2.Id)
                {
                    Logger.Log($"🚫 [RND] Self-match prevented ({d1.Name}) — reshuffling pair.");
                    // simple swap with next if possible
                    if (i + 2 < pool.Count)
                    {
                        var tmp = pool[i + 1];
                        pool[i + 1] = pool[i + 2];
                        pool[i + 2] = tmp;
                        d2 = pool[i + 1];
                    }
                }

                nextRound.Add(new RandomMatch
                {
                    MatchId = NextMatchId(all) + nextRound.Count,
                    Seed1 = d1,
                    Seed2 = d2,
                    RoundLabel = nextLabel,
                    FromMatch1 = null,
                    FromMatch2 = null
                });
            }

            // Append new round to full schedule and write back
            var updated = all.Concat(nextRound).ToList();
            _engine.LoadMatches(updated);

            // Maintain/display round order
            if (!order.Contains(nextLabel, StringComparer.Ordinal))
                order.Add(nextLabel);
            Logger.Log($"[RND] Next round '{nextLabel}' generated → matches={nextRound.Count} (bye={(byeRecipient != null ? byeRecipient.Name : "none")}).");
        }

        // ──────────────────── helpers ────────────────────
        private void RecomputeByeCountsFromSchedule()
        {
            _byeCount.Clear();
            _lastByeRecipient = null;

            foreach (var m in _engine.GetMatches())
            {
                var d1 = m.Seed1;
                var d2 = m.Seed2;
                if (d1 != null && d2 == null)
                {
                    _byeCount[d1.Id] = _byeCount.TryGetValue(d1.Id, out var n) ? n + 1 : 1;
                    _lastByeRecipient = d1.Id; // last BYE we saw in the schedule
                }
                if (d2 != null && d1 == null)
                {
                    _byeCount[d2.Id] = _byeCount.TryGetValue(d2.Id, out var n) ? n + 1 : 1;
                    _lastByeRecipient = d2.Id;
                }
            }

            var dump = string.Join(", ", _byeCount.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}:{kv.Value}"));
            Logger.Log($"[RND] BYE count recomputed from schedule → {{{dump}}}");
        }

        private Driver ChooseByeRecipientFair(List<Driver> candidates)
        {
            if (candidates == null || candidates.Count == 0) return null;

            // Build (driver, byeCount) and find the minimum
            var stats = candidates.Select(d => (d, Count: _byeCount.TryGetValue(d.Id, out var n) ? n : 0)).ToList();
            int min = stats.Min(s => s.Count);

            // Prefer anyone with the minimum BYE count and NOT the last recipient
            var best = stats.Where(s => s.Count == min && s.d.Id != _lastByeRecipient).Select(s => s.d).ToList();

            if (best.Count == 0)
            {
                // If forced (e.g., only one driver left, or everyone had a BYE already), allow last recipient
                best = stats.Where(s => s.Count == min).Select(s => s.d).ToList();
                Logger.Log("⚠️ [RND] BYE fairness forced: only previous recipient available or all tied.");
            }

            var pick = best[_rng.Next(best.Count)];
            return pick;
        }

        private int NextMatchId(List<RandomMatch> all) =>
            (all != null && all.Count > 0) ? all.Max(m => m.MatchId) + 1 : 1;

        private string LabelForNextRound(List<string> order)
        {
            // If last is "Final", we shouldn’t be here
            string last = (order != null && order.Count > 0) ? order[order.Count - 1] : "Round 1";
            int index = ParseRoundIndex(last);
            int nextIndex = index + 1;

            // If exactly two winners, call it "Final"
            var lastWinners = _engine.GetMatches().Where(m => string.Equals(m.RoundLabel, last, StringComparison.Ordinal))
                                   .Count(m => _engine.HasWinner(m.MatchId));
            if (lastWinners == 2) return "Final";

            return $"Round {nextIndex}";
        }

        private int ParseRoundIndex(string roundLabel)
        {
            if (string.IsNullOrWhiteSpace(roundLabel)) return 1;
            if (roundLabel.StartsWith("Round ", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(roundLabel.Substring(6).Trim(), out var n) && n > 0) return n;
            }
            return 1;
        }
    }
}
