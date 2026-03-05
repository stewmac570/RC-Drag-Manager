using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.RandomMode;

namespace RCDragManagerProd.RaceEngines
{
    /// <summary>
    /// Adapter around <see cref="RandomMatchEngine"/> that implements IRaceEngine and
    /// adds fair BYE distribution + a helper to generate the next round from winners.
    /// </summary>
    public sealed class RandomEngineAdapter : IRaceEngine
    {
        // ── state ─────────────────────────────────────────────────────
        private readonly RandomMatchEngine _engine;
        private readonly Dictionary<int, int> _byeCount = new Dictionary<int, int>(); // DriverId -> BYEs so far
        private int? _lastByeRecipient;
        private readonly Random _rng = new Random();

        public RandomEngineAdapter() : this(new RandomMatchEngine()) { }

        public RandomEngineAdapter(RandomMatchEngine engine)
        {
            _engine = engine ?? new RandomMatchEngine();
            Logger.Log("[RND] Adapter ready.");
        }

        // ── IRaceEngine ───────────────────────────────────────────────
        public void LoadDrivers(List<Driver> drivers)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.LoadDrivers(count={drivers?.Count ?? 0})");
            _byeCount.Clear();
            _lastByeRecipient = null;

            _engine.LoadDrivers(drivers ?? new List<Driver>());
            Logger.Log($"[RND] LoadDrivers: {drivers?.Count ?? 0} driver(s). BYE counters reset.");
        }

        public void GenerateBracket()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GenerateBracket()");
            _engine.GenerateBracket();
            Logger.Log("[RND] GenerateBracket → underlying engine produced initial rounds.");
            RecomputeByeCountsFromSchedule();
        }

        public void Reset()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.Reset()");
            _engine.Reset();
            _byeCount.Clear();
            _lastByeRecipient = null;
            Logger.Log("[RND] Reset: engine + BYE counters cleared.");
        }

        public IReadOnlyList<EngineMatch> GetMatches()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetMatches()");
            // Project engine's match objects to the IRaceEngine surface model
            var src = _engine.GetMatches() ?? new List<RandomMatch>();
            var list = new List<EngineMatch>(src.Count);
            foreach (var m in src.OrderBy(x => x.MatchId))
            {
                list.Add(new EngineMatch
                {
                    MatchId = m.MatchId,
                    Driver1 = m.Seed1,
                    Driver2 = m.Seed2,
                    RoundLabel = RoundLabels.Normalize(m.RoundLabel),
                    FromMatch1 = m.FromMatch1,
                    FromMatch2 = m.FromMatch2,
                    HasResult = _engine.HasWinner(m.MatchId)
                });
            }
            return list;
        }

        public IReadOnlyList<string> GetRoundOrder()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetRoundOrder()");
            return _engine.GetRoundOrder()
                          .Select(RoundLabels.Normalize)
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .OrderBy(x => RoundLabels.CompareKey(x))
                          .ToList();
        }

        public void SubmitWinner(int matchId, Driver winner)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.SubmitWinner(matchId={matchId}, winnerId={winner?.Id}, winner='{winner?.Name ?? "null"}')");
            if (winner == null || IsBye(winner))
                throw new InvalidOperationException("Cannot set BYE as a match winner.");
            _engine.SetWinner(matchId, winner);
        }

        public void SetWinner(int matchId, Driver winner) => SubmitWinner(matchId, winner);

        public bool HasWinner(int matchId)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.HasWinner(matchId={matchId})");
            return _engine.HasWinner(matchId);
        }

        public Driver GetWinner(int matchId)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetWinner(matchId={matchId})");
            return _engine.GetWinner(matchId);
        }

        public Driver GetLoser(int matchId)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetLoser(matchId={matchId})");
            return _engine.GetLoser(matchId);
        }

        public bool IsComplete
        {
            get
            {
                Logger.Log($"[ENGINE-API] {GetType().Name}.IsComplete");
                return _engine.IsTournamentComplete();
            }
        }

        public Driver GetChampion()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetChampion()");
            return GetWinner();
        }

        public Driver GetRunnerUp()
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetRunnerUp()");
            var all = (_engine.GetMatches() ?? new List<RandomMatch>()).ToList();
            if (all.Count == 0) return null;

            var final = all.FirstOrDefault(m =>
                string.Equals(RoundLabels.Normalize(m.RoundLabel), "F", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(RoundLabels.Normalize(m.RoundLabel), "LB-F", StringComparison.OrdinalIgnoreCase))
                ?? all.OrderBy(m => m.MatchId).LastOrDefault();

            if (final == null || !_engine.HasWinner(final.MatchId)) return null;
            return _engine.GetLoser(final.MatchId);
        }

        public IReadOnlyList<EngineMatch> GetRoundMatches(string roundLabel)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.GetRoundMatches(roundLabel='{roundLabel}')");
            var normalized = RoundLabels.Normalize(roundLabel);
            return GetMatches()
                .Where(m => string.Equals(RoundLabels.Normalize(m.RoundLabel), normalized, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.MatchId)
                .ToList();
        }

        public bool TryGetMatch(int matchId, out EngineMatch match)
        {
            Logger.Log($"[ENGINE-API] {GetType().Name}.TryGetMatch(matchId={matchId})");
            match = GetMatches().FirstOrDefault(m => m.MatchId == matchId);
            return match != null;
        }

        // ── extras used by controller flow ────────────────────────────
        public void InjectMatches(List<RandomMatch> matches)
        {
            _engine.LoadMatches(matches ?? new List<RandomMatch>());
            Logger.Log($"[RND] InjectMatches: {matches?.Count ?? 0} matches loaded.");
            RecomputeByeCountsFromSchedule();
        }

        /// <summary>Returns the champion if the final appears resolved; otherwise null.</summary>
        public Driver GetWinner()
        {
            var all = _engine.GetMatches() ?? new List<RandomMatch>();
            if (all.Count == 0) return null;

            // Prefer a labeled final, else the last round seen in order.
            var final = all.FirstOrDefault(m =>
                string.Equals(RoundLabels.Normalize(m.RoundLabel), "F", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(RoundLabels.Normalize(m.RoundLabel), "LB-F", StringComparison.OrdinalIgnoreCase));

            if (final == null)
            {
                var order = _engine.GetRoundOrder();
                var lastRound = (order != null && order.Count > 0) ? order[order.Count - 1] : null;
                if (!string.IsNullOrEmpty(lastRound))
                    final = all.LastOrDefault(m => string.Equals(m.RoundLabel, lastRound, StringComparison.Ordinal));
                if (final == null) return null;
            }

            var champ = _engine.GetWinner(final.MatchId);
            Logger.Log($"[RND] GetWinner → {(champ != null ? champ.Name : "null")} from M{final.MatchId} ({final.RoundLabel})");
            return champ;
        }

        /// <summary>
        /// Build the next randomized round from winners of the last revealed round,
        /// distributing a BYE fairly (no consecutive if avoidable; minimize total BYEs).
        /// </summary>
        public void GenerateNextRoundFair()
        {
            // Take snapshots as Lists to avoid IReadOnlyList mutation issues.
            var all = (_engine.GetMatches() ?? new List<RandomMatch>()).ToList();
            var order = (_engine.GetRoundOrder() ?? new List<string>()).ToList();

            if (order.Count == 0)
            {
                Logger.Log("❌ [RND] GenerateNextRoundFair: no existing rounds to extend.");
                return;
            }

            string lastRound = order[order.Count - 1];
            var lastRoundMatches = all.Where(m => string.Equals(m.RoundLabel, lastRound, StringComparison.Ordinal)).ToList();

            var winners = new List<Driver>(lastRoundMatches.Count);
            foreach (var m in lastRoundMatches)
            {
                var w = _engine.HasWinner(m.MatchId) ? _engine.GetWinner(m.MatchId) : null;
                if (w != null) winners.Add(w);
            }

            Logger.Log($"[RND] GenerateNextRoundFair: lastRound='{lastRound}', winners={winners.Count}");
            if (winners.Count == 0) return;

            string nextLabel = LabelForNextRound(order, winners.Count);
            var nextRound = new List<RandomMatch>();
            var pool = winners.ToList();

            // BYE if odd
            if ((pool.Count % 2) == 1)
            {
                var byeRecipient = ChooseByeRecipientFair(pool);
                if (byeRecipient != null)
                {
                    IncrementBye(byeRecipient.Id);      // default remember=false
                    _lastByeRecipient = byeRecipient.Id;

                    nextRound.Add(new RandomMatch
                    {
                        MatchId = NextMatchId(all) + nextRound.Count,
                        Seed1 = byeRecipient,
                        Seed2 = null,
                        RoundLabel = nextLabel
                    });

                    pool.RemoveAll(d => d.Id == byeRecipient.Id);
                    Logger.Log($"[RND] BYE → {byeRecipient.Name} (DriverId={byeRecipient.Id}) for {nextLabel}");
                }
                else
                {
                    Logger.Log("⚠️ [RND] Unable to assign BYE fairly — continuing without explicit BYE.");
                }
            }

            // Pair remainder — simple shuffle, avoid self-match if the shuffle produced one.
            pool = pool.OrderBy(_ => _rng.Next()).ToList();
            for (int i = 0; i + 1 < pool.Count; i += 2)
            {
                var d1 = pool[i];
                var d2 = pool[i + 1];

                if (d1.Id == d2.Id && i + 2 < pool.Count)
                {
                    var tmp = pool[i + 1];
                    pool[i + 1] = pool[i + 2];
                    pool[i + 2] = tmp;
                    d2 = pool[i + 1];
                }

                nextRound.Add(new RandomMatch
                {
                    MatchId = NextMatchId(all) + nextRound.Count,
                    Seed1 = d1,
                    Seed2 = d2,
                    RoundLabel = nextLabel
                });
            }

            // Write back as a brand-new list so the engine can recompute any cached order.
            var updated = new List<RandomMatch>(all.Count + nextRound.Count);
            updated.AddRange(all);
            updated.AddRange(nextRound);
            _engine.LoadMatches(updated);

            Logger.Log($"[RND] Next round '{nextLabel}' generated: matches={nextRound.Count} (new total={updated.Count})");
            RecomputeByeCountsFromSchedule();
        }

        // ── helpers ───────────────────────────────────────────────────
        private void RecomputeByeCountsFromSchedule()
        {
            _byeCount.Clear();
            _lastByeRecipient = null;

            var all = _engine.GetMatches() ?? new List<RandomMatch>();
            foreach (var m in all)
            {
                var d1 = m.Seed1;
                var d2 = m.Seed2;
                if (d1 != null && d2 == null) IncrementBye(d1.Id, remember: true);
                if (d2 != null && d1 == null) IncrementBye(d2.Id, remember: true);
            }

            var dump = string.Join(", ", _byeCount.OrderBy(kv => kv.Key)
                                                  .Select(kv => kv.Key + ":" + kv.Value));
            Logger.Log($"[RND] BYE counters recomputed → {{ {dump} }}");
        }

        private void IncrementBye(int driverId, bool remember = false)
        {
            if (_byeCount.TryGetValue(driverId, out var n)) _byeCount[driverId] = n + 1;
            else _byeCount[driverId] = 1;
            if (remember) _lastByeRecipient = driverId;
        }

        private Driver ChooseByeRecipientFair(List<Driver> candidates)
        {
            if (candidates == null || candidates.Count == 0) return null;

            // Build (driver, byeCount) list
            var stats = new List<Tuple<Driver, int>>(candidates.Count);
            foreach (var d in candidates)
            {
                _byeCount.TryGetValue(d.Id, out var c);
                stats.Add(Tuple.Create(d, c));
            }

            var min = stats.Min(s => s.Item2);

            // Prefer anyone at the min count who didn't just get a bye last time.
            var best = stats.Where(s => s.Item2 == min && s.Item1.Id != _lastByeRecipient).Select(s => s.Item1).ToList();
            if (best.Count == 0)
                best = stats.Where(s => s.Item2 == min).Select(s => s.Item1).ToList();

            if (best.Count == 0) return null;
            return best[_rng.Next(best.Count)];
        }

        private static string LabelForNextRound(List<string> order, int lastWinnerCount)
        {
            var last = (order != null && order.Count > 0) ? RoundLabels.Normalize(order[order.Count - 1]) : "R1";
            var index = ParseRoundIndex(last);
            if (lastWinnerCount == 2) return "F";
            return "R" + (index + 1);
        }

        private static int ParseRoundIndex(string roundLabel)
        {
            if (string.IsNullOrWhiteSpace(roundLabel)) return 1;
            if (roundLabel.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(roundLabel.Substring(1).Trim(), out var n) && n > 0) return n;
            }
            if (roundLabel.StartsWith("Round ", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(roundLabel.Substring(6).Trim(), out var n) && n > 0) return n;
            }
            return 1;
        }

        private static int NextMatchId(List<RandomMatch> all)
        {
            if (all != null && all.Count > 0) return all.Max(m => m.MatchId) + 1;
            return 1;
        }

        public bool IsBye(Driver d)
        {
            return ByePolicy.IsBye(d);
        }
    }
}
