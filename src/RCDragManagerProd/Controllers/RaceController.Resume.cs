// RaceController.Resume.cs
using System;
using System.Collections.Generic;
using System.Linq;

using RCDragManagerProd.Domain;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.RandomMode;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Controllers
{
    public partial class RaceController
    {
        /// <summary>
        /// Rebuilds live engine/controller state from a saved <see cref="ResumeSnapshot"/>
        /// so an interrupted event continues exactly where it left off. Call once, right
        /// after constructing the controller for a loaded session.
        ///
        /// Deterministic brackets (Pro Ladder, Round Robin, Finals) are regenerated; the
        /// non-deterministic ones (Random, Losers Bracket) are re-injected from the saved
        /// structure so pairings are not re-randomised. Results are then replayed by driver
        /// pair (not by raw MatchId) so a single path covers both reconstruction styles.
        /// </summary>
        public void RestoreFromSave()
        {
            if (IsCompleted)
            {
                _tournamentClosed = true;
                Logger.Log($"[RESUME] Completed race '{_session.EventName}' opened results-only; live engine restore skipped.");
                BracketRedrawn?.Invoke(Array.Empty<RCDragManagerProd.ViewModels.PairingRow>());
                WinnersUpdated?.Invoke(Array.Empty<RCDragManagerProd.ViewModels.WinnerRow>());
                NextMatchReady?.Invoke(null);
                CanAdvanceChanged?.Invoke(false);
                CanPickWinnerChanged?.Invoke(false);
                CanOfferBuybackChanged?.Invoke(false);
                CanStartFinalsChanged?.Invoke(false);
                return;
            }

            if (_session?.Resume == null)
            {
                Logger.Log("[RESUME] No resume snapshot — nothing to restore (fresh or pre-feature session).");
                return;
            }

            var snap = _session.Resume;
            if (string.IsNullOrWhiteSpace(snap.OriginalRaceType) ||
                snap.MainMatches == null || snap.MainMatches.Count == 0)
            {
                Logger.Log("[RESUME] Snapshot has no bracket structure — skipping restore.");
                return;
            }

            Logger.Log($"[RESUME] Restoring phase='{snap.CurrentPhase}', original='{snap.OriginalRaceType}'.");

            var roster = BuildResumeRoster();
            _drivers = (_session.Drivers != null && _session.Drivers.Count > 0)
                ? new List<Driver>(_session.Drivers)
                : roster.Values.ToList();

            // Snapshots / RR top-3 (consumed later by the Finals seeding + standings views).
            if (snap.RrTop3Ids != null && snap.RrTop3Ids.Count > 0)
                _rrTop3 = snap.RrTop3Ids.Where(roster.ContainsKey).Select(id => roster[id]).ToList();
            if (snap.RrSnapshotMatches != null && snap.RrSnapshotMatches.Count > 0)
                _rrMatchesSnapshot = snap.RrSnapshotMatches.Select(sm => ToEngineMatch(sm, roster)).ToList();
            if (snap.RrSnapshotRoundOrder != null && snap.RrSnapshotRoundOrder.Count > 0)
                _rrRoundOrderSnapshot = new List<string>(snap.RrSnapshotRoundOrder);

            switch (snap.CurrentPhase)
            {
                case "Finals":
                    RestoreFinalsEngine(snap, roster);
                    break;
                case "LosersBracket":
                    RestoreLosersEngine(snap, roster);
                    break;
                default:
                    RestoreMainEngine(snap, roster);
                    break;
            }

            if (_engine == null)
            {
                Logger.Log("[RESUME][ERROR] Engine reconstruction failed — aborting restore.");
                return;
            }

            // Revealed rounds + transient flags.
            _revealedRounds.Clear();
            foreach (var r in (_session.SavedRevealedRounds ?? new List<string>()))
                _revealedRounds.Add(RoundLabels.Normalize(r));

            _activeRound = string.IsNullOrWhiteSpace(snap.ActiveRound) ? null : snap.ActiveRound;
            _inLosersPhase = snap.InLosersPhase;
            _finalsPending = snap.FinalsPending;
            _buybackChampionOverride =
                (snap.BuybackChampionOverrideId is int bid && roster.ContainsKey(bid)) ? roster[bid] : null;

            Logger.Log($"[RESUME] Reconstructed: engine={_engine.GetType().Name}, losers={_losersEngine?.GetType().Name ?? "null"}, " +
                       $"revealed=[{string.Join(",", _revealedRounds)}], activeRound='{_activeRound ?? "-"}', " +
                       $"winners={_winners.Count}, finalsPending={_finalsPending}.");

            // Single refresh so the UI shows the resumed bracket and the correct next action.
            PushFullRefresh();
            if (_finalsPending)
                CanStartFinalsChanged?.Invoke(true);
        }

        // ── phase reconstructors ─────────────────────────────────────────────

        private void RestoreMainEngine(ResumeSnapshot snap, Dictionary<int, Driver> roster)
        {
            _engine = RaceEngineFactory.Create(snap.OriginalRaceType);

            EngineLoadDrivers(_engine, _drivers);

            if (_engine is RandomEngineAdapter rnd)
            {
                // Random is non-deterministic — re-inject the exact saved pairings.
                rnd.InjectMatches(ToRandomMatches(snap.MainMatches, roster));
                Logger.Log($"[RESUME][MAIN] Re-injected {snap.MainMatches.Count} Random match(es).");
            }
            else if (_engine is RoundRobinEngineAdapter rr)
            {
                // Round Robin shuffles + random-rotates on generation, so it is just as
                // non-deterministic as Random — re-inject the saved schedule verbatim.
                rr.InjectMatches(ToEngineMatches(snap.MainMatches, roster));
                Logger.Log($"[RESUME][MAIN] Re-injected {snap.MainMatches.Count} Round Robin match(es).");
            }
            else
            {
                // Pro Ladder regenerates deterministically (seeded by qual time).
                EngineGenerateBracket(_engine);
                Logger.Log("[RESUME][MAIN] Regenerated deterministic bracket.");
            }

            ReplayResults(_engine, snap.MainMatches, roster);
        }

        private void RestoreLosersEngine(ResumeSnapshot snap, Dictionary<int, Driver> roster)
        {
            // In the LB phase the active engine IS the losers engine. Prefer the dedicated
            // LosersMatches structure, falling back to MainMatches (when _engine == _losersEngine
            // at save time both held the same LB structure).
            var lbStructure = (snap.LosersMatches != null && snap.LosersMatches.Count > 0)
                ? snap.LosersMatches
                : snap.MainMatches;

            var lbDrivers = (_session.BuybackDrivers != null && _session.BuybackDrivers.Count > 0)
                ? new List<Driver>(_session.BuybackDrivers)
                : DistinctDriversFrom(lbStructure, roster);

            var lb = new RandomEngineAdapter();
            EngineLoadDrivers(lb, lbDrivers, "LB-R1");
            lb.InjectMatches(ToRandomMatches(lbStructure, roster));

            _losersEngine = lb;
            _engine = lb;

            ReplayRrHistory(roster);          // RR rows are shown from the snapshot — re-seed their winners.
            ReplayResults(lb, lbStructure, roster);
            Logger.Log($"[RESUME][LB] Re-injected {lbStructure.Count} LB match(es); drivers={lbDrivers.Count}.");
        }

        private void RestoreFinalsEngine(ResumeSnapshot snap, Dictionary<int, Driver> roster)
        {
            // The Finals Pro Ladder is deterministic from its finalists. The finalists are
            // exactly the drivers in the saved finals structure, so regenerate from them and
            // replay — no need to re-derive the LB champion.
            var finalists = DistinctDriversFrom(snap.MainMatches, roster);
            if (finalists.Count < 2)
            {
                Logger.Log("[RESUME][FINALS][ERROR] Could not resolve finalists from saved structure.");
                return;
            }

            // Reconstruct the losers engine too (if it existed) so post-finals standings/summary
            // that read the LB champion still resolve.
            if (snap.LosersMatches != null && snap.LosersMatches.Count > 0)
            {
                var lbDrivers = (_session.BuybackDrivers != null && _session.BuybackDrivers.Count > 0)
                    ? new List<Driver>(_session.BuybackDrivers)
                    : DistinctDriversFrom(snap.LosersMatches, roster);
                var lb = new RandomEngineAdapter();
                EngineLoadDrivers(lb, lbDrivers, "LB-R1");
                lb.InjectMatches(ToRandomMatches(snap.LosersMatches, roster));
                _losersEngine = lb;
                ReplayResults(lb, snap.LosersMatches, roster);
            }

            var pro = new ProLadderEngineAdapter();
            EngineLoadDrivers(pro, finalists);
            EngineGenerateBracket(pro);
            _engine = pro;

            ReplayRrHistory(roster);          // RR rows are shown from the snapshot — re-seed their winners.
            ReplayResults(pro, snap.MainMatches, roster);
            Logger.Log($"[RESUME][FINALS] Regenerated Pro Ladder finals; finalists={finalists.Count}.");
        }

        // ── replay ───────────────────────────────────────────────────────────

        /// <summary>
        /// Applies saved winners onto a freshly built engine. Matches are located by
        /// (round label + unordered driver pair) rather than raw MatchId, and applied in
        /// round order with a re-fetch each step so elimination brackets populate their
        /// later rounds as earlier winners advance.
        /// </summary>
        private void ReplayResults(IRaceEngine engine, List<SavedMatch> structure, Dictionary<int, Driver> roster)
        {
            if (engine == null || structure == null) return;

            var ordered = structure
                .Where(sm => sm.WinnerId is int)
                .OrderBy(sm => RoundLabels.CompareKey(sm.RoundLabel ?? string.Empty))
                .ThenBy(sm => sm.MatchId)
                .ToList();

            foreach (var sm in ordered)
            {
                if (!roster.TryGetValue(sm.WinnerId.Value, out var winner) || winner == null)
                    continue;
                Driver loser = (sm.LoserId is int lid && roster.TryGetValue(lid, out var l)) ? l : null;

                var live = EngineGetMatches(engine).ToList();
                var lm = FindLiveMatch(live, sm);
                if (lm == null)
                {
                    Logger.Log($"[RESUME][REPLAY][WARN] No live match for saved M{sm.MatchId} ({sm.RoundLabel}); skipping.");
                    continue;
                }

                EngineSetWinner(engine, lm.MatchId, winner, lm.RoundLabel);
                _matchResult.SetWinner(lm.MatchId, winner, loser);
                _winners.Add(new ViewModels.WinnerRow
                {
                    MatchId = lm.MatchId,
                    RoundLabel = lm.RoundLabel,
                    Winner = winner.Name,
                    Loser = loser?.Name ?? "BYE"
                });
            }
        }

        /// <summary>
        /// Replays the Round Robin history into <see cref="_matchResult"/> during the LB/Finals
        /// phases. Those RR matches are no longer in any live engine (the engine was swapped to
        /// LB then Finals), but the bracket view renders them from <see cref="_rrMatchesSnapshot"/>
        /// and reads their winners from <see cref="_matchResult"/> by the original RR match id —
        /// so the results must be re-seeded directly, with no engine round-trip.
        /// </summary>
        private void ReplayRrHistory(Dictionary<int, Driver> roster)
        {
            var rr = _session.Resume?.RrSnapshotMatches;
            if (rr == null) return;

            foreach (var sm in rr.Where(m => m.WinnerId is int))
            {
                if (!roster.TryGetValue(sm.WinnerId.Value, out var winner) || winner == null)
                    continue;
                Driver loser = (sm.LoserId is int lid && roster.TryGetValue(lid, out var l)) ? l : null;

                _matchResult.SetWinner(sm.MatchId, winner, loser);
                _winners.Add(new ViewModels.WinnerRow
                {
                    MatchId = sm.MatchId,
                    RoundLabel = sm.RoundLabel,
                    Winner = winner.Name,
                    Loser = loser?.Name ?? "BYE"
                });
            }
        }

        private static EngineMatch FindLiveMatch(List<EngineMatch> live, SavedMatch sm)
        {
            var round = RoundLabels.Normalize(sm.RoundLabel ?? string.Empty);
            return live.FirstOrDefault(m =>
                string.Equals(RoundLabels.Normalize(m.RoundLabel ?? string.Empty), round, StringComparison.OrdinalIgnoreCase) &&
                SamePair(m.Driver1?.Id, m.Driver2?.Id, sm.Driver1Id, sm.Driver2Id));
        }

        private static bool SamePair(int? aId1, int? aId2, int? bId1, int? bId2)
        {
            return (aId1 == bId1 && aId2 == bId2) || (aId1 == bId2 && aId2 == bId1);
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private Dictionary<int, Driver> BuildResumeRoster()
        {
            var map = new Dictionary<int, Driver>();

            void Add(Driver d)
            {
                if (d == null || d.Id == 0) return;
                if (!map.ContainsKey(d.Id)) map[d.Id] = d;
            }

            if (_session.Drivers != null)
                foreach (var d in _session.Drivers) Add(d);
            if (_session.BuybackDrivers != null)
                foreach (var d in _session.BuybackDrivers) Add(d);
            if (_session.TopDriversSnapshot != null)
                foreach (var d in _session.TopDriversSnapshot) Add(d);

            // Fallback: reconstruct any driver only present in the entry list.
            if (_session.DriverEntries != null)
            {
                foreach (var e in _session.DriverEntries)
                {
                    if (e == null || e.DriverID == 0 || map.ContainsKey(e.DriverID)) continue;
                    map[e.DriverID] = new Driver
                    {
                        Id = e.DriverID,
                        Name = e.DriverName,
                        QualTime = e.QualifyingTime
                    };
                }
            }

            return map;
        }

        private static List<Driver> DistinctDriversFrom(List<SavedMatch> structure, Dictionary<int, Driver> roster)
        {
            var seen = new HashSet<int>();
            var list = new List<Driver>();
            foreach (var sm in structure.OrderBy(s => RoundLabels.CompareKey(s.RoundLabel ?? string.Empty)).ThenBy(s => s.MatchId))
            {
                foreach (var id in new[] { sm.Driver1Id, sm.Driver2Id })
                {
                    if (id is int v && v != 0 && seen.Add(v) && roster.TryGetValue(v, out var d))
                        list.Add(d);
                }
            }
            return list;
        }

        private List<RandomMatch> ToRandomMatches(List<SavedMatch> structure, Dictionary<int, Driver> roster)
        {
            return structure
                .OrderBy(sm => RoundLabels.CompareKey(sm.RoundLabel ?? string.Empty))
                .ThenBy(sm => sm.MatchId)
                .Select(sm => new RandomMatch
                {
                    MatchId = sm.MatchId,
                    Seed1 = ResolveDriver(sm.Driver1Id, roster),
                    Seed2 = ResolveDriver(sm.Driver2Id, roster),
                    RoundLabel = sm.RoundLabel,
                    FromMatch1 = sm.FromMatch1,
                    FromMatch2 = sm.FromMatch2
                })
                .ToList();
        }

        private static EngineMatch ToEngineMatch(SavedMatch sm, Dictionary<int, Driver> roster) => new EngineMatch
        {
            MatchId = sm.MatchId,
            Driver1 = ResolveDriver(sm.Driver1Id, roster),
            Driver2 = ResolveDriver(sm.Driver2Id, roster),
            RoundLabel = sm.RoundLabel,
            FromMatch1 = sm.FromMatch1,
            FromMatch2 = sm.FromMatch2
        };

        private static List<EngineMatch> ToEngineMatches(List<SavedMatch> structure, Dictionary<int, Driver> roster)
        {
            return (structure ?? new List<SavedMatch>())
                .OrderBy(sm => RoundLabels.CompareKey(sm.RoundLabel ?? string.Empty))
                .ThenBy(sm => sm.MatchId)
                .Select(sm => ToEngineMatch(sm, roster))
                .ToList();
        }

        private static Driver ResolveDriver(int? id, Dictionary<int, Driver> roster)
        {
            if (id is int v && v != 0 && roster.TryGetValue(v, out var d)) return d;
            return null;
        }
    }
}
