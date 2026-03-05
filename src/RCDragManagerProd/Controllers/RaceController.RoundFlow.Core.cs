// RaceController.RoundFlow.Core.cs
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
        // ────────── RR STANDINGS CACHE ──────────
        private string _rrStandingsCardCache;

        public bool TryShowRoundRobinStandings()
        {
            Logger.Log($"[RR][STANDINGS] TryShowRoundRobinStandings() called. CachePresent={!string.IsNullOrWhiteSpace(_rrStandingsCardCache)}");

            if (string.IsNullOrWhiteSpace(_rrStandingsCardCache))
                return false;

            ScrollableTextDialog.Show("Round Robin — Standings", _rrStandingsCardCache);
            return true;
        }

        // ──────────────────  PUBLIC API (CORE FLOW)  ──────────────────
        public void GenerateBracket(string raceType, List<Driver> drivers)
        {
            if (drivers == null || drivers.Count < 2)
            {
                Logger.Log("⛔ Cannot generate bracket — provided driver list is invalid.");
                return;
            }

            // normalize + default to RR if empty
            var rt = (raceType ?? _session?.RaceType ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(rt))
            {
                rt = "Round Robin";
                Logger.Log("[CTRL] raceType blank — defaulting to 'Round Robin'");
            }
            _session.RaceType = rt;

            Logger.Log($"[CTRL][DEBUG] GenerateBracket inputs → raceTypeArg='{raceType}', rt='{rt}', session.RaceType='{_session.RaceType}', RRVariant='{_session.RoundRobinVariant}', N={_session.RoundsToRun}");


            _drivers = drivers;
            _session.Drivers = new List<Driver>(_drivers);   // keep session + controller in sync

            _engine = RaceEngineFactory.Create(rt);
            Logger.Log($"[ENGINE] Created '{_engine.GetType().Name}' for raceType='{rt}' (drivers={_drivers.Count})");

            var isProLadderRequested =
                string.Equals(rt, "pro ladder", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(rt, "nhra pro ladder", StringComparison.OrdinalIgnoreCase);
            if (isProLadderRequested)
            {
                Logger.Log($"[ProLadderValidate] driverCount={_drivers.Count}");

                if (_drivers.Count < 3 || _drivers.Count > 32)
                {
                    Logger.Log($"[ProLadderValidate] driverCount={_drivers.Count} out of supported range (3–32)");
                    try
                    {
                        MessageBox.Show(
                            "Pro Ladder supports 3–32 drivers. Please adjust the driver count.",
                            "Invalid Driver Count",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch { /* ignore UI errors in headless runs */ }

                    CanAdvanceChanged?.Invoke(false);
                    CanPickWinnerChanged?.Invoke(false);
                    NextMatchReady?.Invoke(null);
                    return;
                }

                try
                {
                    var template = ProLadder.GetLadder(_drivers.Count);
                    if (template == null || template.Count == 0)
                    {
                        Logger.Log($"[ProLadderValidate] missing template for size={_drivers.Count}");
                        CanAdvanceChanged?.Invoke(false);
                        CanPickWinnerChanged?.Invoke(false);
                        NextMatchReady?.Invoke(null);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[ProLadderValidate] missing template for size={_drivers.Count} (probe failed: {ex.Message})");
                    CanAdvanceChanged?.Invoke(false);
                    CanPickWinnerChanged?.Invoke(false);
                    NextMatchReady?.Invoke(null);
                    return;
                }
            }

            // QMDRA: push requested rounds into RR engine before generating
            if (_engine is RoundRobinEngineAdapter rrAdapter)
            {
                Logger.Log("[CTRL][ENGINE-CAST] Using RoundRobinEngineAdapter for RR-only configuration (SetRoundsToRun).");
                var variant = (_session?.RoundRobinVariant ?? "Standard").Trim();
                var isQmdra = string.Equals(variant, "QMDRA", StringComparison.OrdinalIgnoreCase);

                if (isQmdra)
                {
                    int nRounds = _session?.RoundsToRun ?? 0;
                    if (nRounds <= 0) nRounds = 3;

                    Logger.Log("[EngineCall] " + _engine.GetType().Name + " SetRoundsToRun matchId=- round=-");
                    rrAdapter.SetRoundsToRun(nRounds);
                    Logger.Log($"[ENGINE][RR] QMDRA active → SetRoundsToRun({nRounds})");
                }
                else
                {
                    Logger.Log("[EngineCall] " + _engine.GetType().Name + " SetRoundsToRun matchId=- round=-");
                    rrAdapter.SetRoundsToRun(3);
                    Logger.Log($"[ENGINE][RR] Standard RR → SetRoundsToRun(3)");
                }
            }

            EngineLoadDrivers(_engine, _drivers);
            Logger.Log("[ENGINE] Drivers loaded into engine.");

            EngineGenerateBracket(_engine);
            Logger.Log("[ENGINE] Bracket generated.");

            var roundOrder = EngineGetRoundOrder(_engine);
            if (roundOrder == null || roundOrder.Count == 0)
            {
                Logger.Log("⛔ Bracket generated with no rounds — aborting reveal state update.");
                CanAdvanceChanged?.Invoke(false);
                CanPickWinnerChanged?.Invoke(false);
                NextMatchReady?.Invoke(null);
                return;
            }


            _revealedRounds.Clear();
            _revealedRounds.Add(roundOrder[0]);

            _winners.Clear();
            PushFullRefresh();
        }



        // Convenience wrapper (kept in case other callers use it)
        public void GenerateBracket(string raceType)
        {
            if (_session?.Drivers == null || _session.Drivers.Count < 2)
            {
                Logger.Log("⛔ Cannot generate bracket — session driver list is invalid.");
                return;
            }

            GenerateBracket(raceType, _session.Drivers); // defaulting happens inside
        }

        public void AdvanceRound()
        {
            Logger.Log($"[SNAP] AdvanceRound-entry  |  _engine={_engine?.GetType().Name ?? "null"}  |  _losersEngine={_losersEngine?.GetType().Name ?? "null"}  |  revealedRounds={string.Join(",", _revealedRounds)}");

            if (_engine == null)
            {
                Logger.Log("⛔ AdvanceRound aborted — engine is null");
                CanAdvanceChanged?.Invoke(false);
                return;
            }

            var next = EngineGetRoundOrder(_engine).FirstOrDefault(r => !_revealedRounds.Contains(r));
            if (string.IsNullOrEmpty(next))
            {
                Logger.Log("ℹ️  No further rounds to reveal on current engine.");
                CanAdvanceChanged?.Invoke(false);
                return;
            }

            _revealedRounds.Add(next);
            Logger.Log($"[ROUND] Revealing round: {next}");

            var rows = BuildCurrentBracketRows();
            BracketRedrawn?.Invoke(rows);
            Logger.Log($"[ROUND] Redrawn after reveal '{next}' with {rows.Count} rows (unified builder)");

            PushNextMatch();
            PushAdvanceState();

            Logger.Log("[FORM1] AdvanceRound() completed");
        }

        // ────────────────  INTERNAL HELPERS (CORE FLOW)  ────────────────
        private void PushFullRefresh()
        {
            var rows = BuildCurrentBracketRows();
            BracketRedrawn?.Invoke(rows);
            WinnersUpdated?.Invoke(_winners);
            PushNextMatch();
            PushAdvanceState();
            CanPickWinnerChanged?.Invoke(true);
        }

        public void PushNextMatch()
        {
            EnsureReady();

            var next = EngineGetMatches(_engine)
                              .Where(m => _revealedRounds.Contains(m.RoundLabel) && !m.HasResult)
                              .OrderBy(m => m.MatchId)
                              .FirstOrDefault();

            if (next == null)
            {
                CanPickWinnerChanged?.Invoke(false);
                NextMatchReady?.Invoke(null);

                // Final standings log (RR only)
                var allMatches = EngineGetMatches(_engine);
                bool allResolved = allMatches.All(m => EngineHasWinner(_engine, m.MatchId, m.RoundLabel));
                if (allResolved && _engine is RoundRobinEngineAdapter rr)
                {
                    Logger.Log("[CTRL][ENGINE-CAST] Using RoundRobinEngineAdapter for RR-only standings output.");
                    Logger.Log("[EngineCall] " + _engine.GetType().Name + " GetStandings matchId=- round=-");
                    var standings = rr.GetStandings();
                    Logger.Log("[ROUND ROBIN] Final standings:");
                    foreach (var (driver, wins) in standings)
                        Logger.Log($"  {driver.Name} - {wins} win(s)");
                }
                return;
            }

            NextMatchReady?.Invoke(ToPairingRow(next));
            CanPickWinnerChanged?.Invoke(true);
        }

        private void PushAdvanceState()
        {
            if (_revealedRounds.Count == 0)
            {
                Logger.Log("[DEBUG] PushAdvanceState: no rounds revealed — cannot advance");
                CanAdvanceChanged?.Invoke(false);
                return;
            }

            var visibleMatches = EngineGetMatches(_engine)
                                        .Where(m => _revealedRounds.Contains(m.RoundLabel))
                                        .ToList();

            bool allVisibleResolved = visibleMatches.All(m => m.HasResult);
            bool moreRoundsExist = EngineGetRoundOrder(_engine).Any(r => !_revealedRounds.Contains(r));
            bool canAdvance = allVisibleResolved && moreRoundsExist;

            Logger.Log($"[DEBUG] PushAdvanceState: visible={visibleMatches.Count}, resolved={visibleMatches.Count(m => m.HasResult)}, moreRoundsExist={moreRoundsExist}, canAdvance={canAdvance}");

            CanAdvanceChanged?.Invoke(canAdvance);

            // ── RR → Buyback or Auto-Advance to Finals ─────────────────────
            if (_engine is RoundRobinEngineAdapter rr)
            {
                Logger.Log("[CTRL][ENGINE-CAST] Using RoundRobinEngineAdapter for RR-only progression rules.");
                var variant = (_session?.RoundRobinVariant ?? "Standard").Trim();
                var isQmdra = string.Equals(variant, "QMDRA", StringComparison.OrdinalIgnoreCase);

                // ---------------------------------------------------------
                // QMDRA RR completion: stop after N rounds revealed
                // Complete when:
                //   - revealedRounds.Count >= N
                //   - all matches in revealed rounds are resolved
                // Then: seed ALL drivers to finals in RR ranking order (no buyback)
                // ---------------------------------------------------------
                if (isQmdra)
                {
                    int n = _session?.RoundsToRun ?? 0;
                    if (n <= 0)
                    {
                        Logger.Log("[RR][QMDRA][ERROR] Variant=QMDRA but RoundsToRun is missing/invalid. Blocking finals transition.");
                        return;
                    }

                    var visibleMatchesQ = EngineGetMatches(_engine)
                                            .Where(m => _revealedRounds.Contains(m.RoundLabel))
                                            .ToList();

                    bool allVisibleResolvedQ = visibleMatchesQ.All(m => m.HasResult);
                    bool roundsReached = _revealedRounds.Count >= n;

                    Logger.Log($"[RR][QMDRA] Check → Revealed={_revealedRounds.Count}, N={n}, roundsReached={roundsReached}, visibleMatches={visibleMatchesQ.Count}, allVisibleResolved={allVisibleResolvedQ}");

                    if (roundsReached && allVisibleResolvedQ)
                    {
                        // standings display is allowed (keep your existing scorecard)
                        try
                        {
                            var card = RoundRobinScorecardLogger.BuildScorecard(rr, _matchResult);
                            _rrStandingsCardCache = card;
                            ScrollableTextDialog.Show("Round Robin — Standings", card);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"[RR][QMDRA] Scorecard popup failed: {ex.Message}");
                        }
                        RoundRobinScorecardLogger.Log(rr, _matchResult);

                        int totalDrivers = _session?.Drivers?.Count ?? 0;
                        if (totalDrivers <= 0) totalDrivers = EngineGetMatches(_engine).SelectMany(m => new[] { m.Driver1, m.Driver2 }).Where(d => d != null).Distinct().Count();

                        Logger.Log("[EngineCall] " + _engine.GetType().Name + " GetTopRankedDrivers matchId=- round=-");
                        var rankedAll = rr.GetTopRankedDrivers(totalDrivers);

                        Logger.Log($"[RR][QMDRA] COMPLETE → Advancing ALL drivers to finals. RankedCount={rankedAll.Count}, SessionDrivers={totalDrivers}");
                        Logger.Log("[RR][QMDRA] Finals seed order: " + (rankedAll.Count == 0 ? "(none)" : string.Join(", ", rankedAll.Select(d => d.Name))));

                        InjectFinalsAllAdvance(rankedAll);
                        return;
                    }

                    // Not complete yet → normal flow continues (no buyback in QMDRA ever)
                }

                // ---------------------------------------------------------
                // Standard RR completion (existing behavior)
                // ---------------------------------------------------------
                bool allRRResolved =
                    EngineGetRoundOrder(_engine).All(r => _revealedRounds.Contains(r)) &&
                    EngineGetMatches(_engine).All(m => m.HasResult);

                Logger.Log($"[DEBUG] PushAdvanceState (RoundRobin): allRRResolved={allRRResolved}");

                if (allRRResolved)
                {

                    Logger.Log("[EngineCall] " + _engine.GetType().Name + " GetTopRankedDrivers matchId=- round=-");
                    _rrTop3 = rr.GetTopRankedDrivers(3);
                    var names = (_rrTop3 != null && _rrTop3.Count > 0)
                        ? string.Join(", ", _rrTop3.Select(d => d.Name))
                        : "(none)";
                    Logger.Log($"[RR] Top-3 snapshot captured on RR completion: {names}");

                    _rrMatchesSnapshot = EngineGetMatches(_engine).ToList();
                    _rrRoundOrderSnapshot = EngineGetRoundOrder(_engine).ToList();

                    // Popup scorecard + keep detailed log
                    try
                    {
                        var card = RoundRobinScorecardLogger.BuildScorecard(rr, _matchResult);
                        _rrStandingsCardCache = card;
                        ScrollableTextDialog.Show("Round Robin — Standings", card);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[RR] Scorecard popup failed: {ex.Message}");
                    }
                    RoundRobinScorecardLogger.Log(rr, _matchResult);

                    var eligible = GetEligibleBuybackDrivers(); // uses _rrTop3 snapshot
                    if (eligible.Count >= 2)
                    {
                        CanOfferBuybackChanged?.Invoke(true);
                        return; // wait for user action
                    }

                    // Not enough for buyback → auto-advance to Finals with wildcard
                    Driver wildcard = null;
                    if (eligible.Count == 1)
                        wildcard = eligible[0];
                    else
                    {
                        Logger.Log("[EngineCall] " + _engine.GetType().Name + " GetTopRankedDrivers matchId=- round=-");
                        var top4 = rr.GetTopRankedDrivers(4);
                        if (top4 != null && top4.Count >= 4) wildcard = top4[3];
                    }

                    if (wildcard == null)
                    {
                        Logger.Log("❌ Auto-advance failed — could not determine wildcard finalist.");
                        return;
                    }

                    Logger.Log($"[RR] Not enough drivers for buyback (eligible={eligible.Count}). Auto-advancing with wildcard: {wildcard.Name}.");
                    try
                    {
                        MessageBox.Show(
                            $"Not enough drivers for Buyback.\nAdvancing directly to Finals with wildcard: {wildcard.Name}.",
                            "Buyback Skipped",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch { /* ignore UI errors in headless runs */ }

                    _buybackChampionOverride = wildcard;   // consumed by InjectFinal4Bracket()
                    InjectFinal4Bracket();                 // swaps to ProLadder and draws SF
                    return;
                }
            }

            // ── Losers Bracket complete → gate Finals ─────────────────────
            if (_inLosersPhase && _losersEngine != null)
            {

                bool isLbComplete = EngineGetMatches(_losersEngine).All(m => EngineHasWinner(_losersEngine, m.MatchId, m.RoundLabel));
=======
                Logger.Log($"[ENGINE-CALL] {_losersEngine.GetType().Name}.IsComplete");
                bool isLbComplete = _losersEngine.IsComplete;

                Logger.Log($"[DEBUG] PushAdvanceState (LB): inLosersPhase={_inLosersPhase}, resolvedLB={isLbComplete}");

                if (isLbComplete)
                {
                    Logger.Log("✅ Losers bracket complete.");
                    _inLosersPhase = false;

                    _finalsPending = true;
                    CanStartFinalsChanged?.Invoke(true);
                    Logger.Log("🟢 Finals pending — waiting for 'Generate Bracket' to seed finals.");
                    return;
                }
            }

            // ── Legacy fallback (if LB Final manually checked) ────────────
            if (_session.RaceType == "Losers Bracket" && _revealedRounds.Any(r => string.Equals(RoundLabels.Normalize(r), "LB-F", StringComparison.OrdinalIgnoreCase)))
            {
                var finalMatch = EngineGetMatches(_engine).LastOrDefault();
                if (finalMatch != null && finalMatch.HasResult)
                {
                    Logger.Log("🧩 LB Final match resolved — injecting Final-4 bracket (fallback)...");
                    InjectFinal4Bracket();
                }
            }

            // ── Finals wrap-up — emit summary once ────────────────────────
            if (!_tournamentClosed && string.Equals(_session?.RaceType, "Finals", StringComparison.OrdinalIgnoreCase))
            {
                var all = EngineGetMatches(_engine).OrderBy(m => m.MatchId).ToList();
                var final = all.FirstOrDefault(m => string.Equals(m.RoundLabel, "F", StringComparison.OrdinalIgnoreCase))
                         ?? all.LastOrDefault();

                if (final != null && final.HasResult)
                {
                    var winner = _matchResult.GetWinner(final.MatchId);
                    Logger.Log($"[FINALS] Summary lookup → winner={(winner != null ? winner.Name : "null")} for M{final.MatchId}");

                    Driver runnerUp = null;
                    if (winner != null)
                    {
                        var d1 = final.Driver1;
                        var d2 = final.Driver2;

                        if (d1 != null && d2 != null)
                        {
                            if (d1.Id == winner.Id) runnerUp = d2;
                            else if (d2.Id == winner.Id) runnerUp = d1;
                        }
                    }
                    Logger.Log($"[FINALS] Runner-up resolution: M{final.MatchId}, winnerId={(winner != null ? winner.Id.ToString() : "null")}, runnerUpId={(runnerUp != null ? runnerUp.Id.ToString() : "null")}");

                    var summary = new RaceSummary
                    {
                        EventName = _session?.EventName ?? "Quick Session",
                        Bracket = "Finals (Pro Ladder)",
                        Winner = winner,
                        RunnerUp = runnerUp,
                        TotalDrivers = _session?.Drivers?.Count ?? 0,
                        TotalMatches = all.Count,
                        CompletedAt = DateTime.Now
                    };

                    Logger.Log($"🏆 Tournament complete — Winner: {winner?.Name}, Runner-Up: {runnerUp?.Name}");
                    _tournamentClosed = true;

                    CanPickWinnerChanged?.Invoke(false);
                    CanAdvanceChanged?.Invoke(false);

                    TournamentCompleted?.Invoke(summary);
                }
            }
        }

        // ────────────────  CORE HELPERS (SHARED)  ────────────────
        private static PairingRow ToPairingRow(EngineMatch m) => new PairingRow
        {
            MatchId = m.MatchId,
            RoundLabel = m.RoundLabel,
            Driver1 = m.Driver1?.Name ?? "BYE",
            Driver2 = m.Driver2?.Name ?? "BYE",
            IsHeader = false
        };

        private void EnsureReady()
        {
            if (_engine == null)
                throw new InvalidOperationException("GenerateBracket must be called first.");
        }

        public EngineMatch GetMatch(int matchId)
        {
            if (_engine == null)
            {
                Logger.Log($"[LOOKUP] GetMatch({matchId}) called while engine=null — returning null");
                return null;
            }


            var match = EngineGetMatches(_engine, matchId: matchId).FirstOrDefault(m => m.MatchId == matchId);
=======
            Logger.Log($"[ENGINE-CALL] {_engine.GetType().Name}.TryGetMatch(matchId={matchId})");
            _engine.TryGetMatch(matchId, out var match);

            Logger.Log(match != null
                ? $"[LOOKUP] GetMatch({matchId}) → Round={match.RoundLabel}"
                : $"[LOOKUP] GetMatch({matchId}) → NOT FOUND");
            return match;
        }
    }
}
