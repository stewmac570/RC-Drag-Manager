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
        // ---------- RR STANDINGS CACHE ----------
        private string _rrStandingsCardCache;

        public bool TryShowRoundRobinStandings()
        {
            Logger.Log($"[RR][STANDINGS] TryShowRoundRobinStandings() called. CachePresent={!string.IsNullOrWhiteSpace(_rrStandingsCardCache)}");

            if (string.IsNullOrWhiteSpace(_rrStandingsCardCache))
                return false;

            _standingsDialogService.Show("Round Robin � Standings", _rrStandingsCardCache);
            return true;
        }

        // ------------------  PUBLIC API (CORE FLOW)  ------------------
        public void GenerateBracket(string raceType, List<Driver> drivers)
        {
            if (IsCompleted)
            {
                Logger.Log($"[CTRL][REJECT] GenerateBracket blocked for completed race '{_session.EventName}'.");
                CanAdvanceChanged?.Invoke(false);
                CanPickWinnerChanged?.Invoke(false);
                return;
            }

            if (_engine != null)
            {
                Logger.Log($"[CTRL][REJECT] GenerateBracket blocked because a '{_engine.GetType().Name}' race is already active (requested type='{raceType}').");
                return;
            }

            if (drivers == null || drivers.Count < 2)
            {
                Logger.Log("? Cannot generate bracket � provided driver list is invalid.");
                return;
            }

            // normalize + default to RR if empty
            var rt = (raceType ?? _session?.RaceType ?? string.Empty).Trim();
            // Reset() blanks _session.RaceType, so a reset+regenerate arrives here
            // with no race type. Fall back to the class's original mode before
            // defaulting, otherwise every reset silently became a Round Robin.
            if (string.IsNullOrWhiteSpace(rt))
                rt = (_session?.OriginalRaceType ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(rt))
            {
                rt = RaceTypes.RoundRobin;
                Logger.Log("[CTRL] raceType blank � defaulting to 'Round Robin'");
            }
            _session.RaceType = rt;

            // Capture the starting mode exactly once — RaceType mutates during the event,
            // but resume needs the original to regenerate the initial bracket.
            if (string.IsNullOrWhiteSpace(_session.OriginalRaceType))
                _session.OriginalRaceType = rt;

            Logger.Log($"[CTRL][DEBUG] GenerateBracket inputs ? raceTypeArg='{raceType}', rt='{rt}', session.RaceType='{_session.RaceType}', RRVariant='{_session.RoundRobinVariant}', N={_session.RoundsToRun}");


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
                    Logger.Log($"[ProLadderValidate] driverCount={_drivers.Count} out of supported range (3�32)");
                    try
                    {
                        MessageBox.Show(
                            "Pro Ladder supports 3�32 drivers. Please adjust the driver count.",
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
                    Logger.Log($"[ENGINE][RR] QMDRA active ? SetRoundsToRun({nRounds})");
                }
                else
                {
                    Logger.Log("[EngineCall] " + _engine.GetType().Name + " SetRoundsToRun matchId=- round=-");
                    int naturalMax = Math.Max(1, _drivers.Count - 1);
                    int standardRounds = Math.Min(_session?.RoundsToRun ?? 3, naturalMax);
                    rrAdapter.SetRoundsToRun(standardRounds);
                    Logger.Log($"[ENGINE][RR] Standard RR ? SetRoundsToRun({standardRounds}) (drivers={_drivers.Count}, naturalMax={naturalMax})");
                }
            }

            EngineLoadDrivers(_engine, _drivers);
            Logger.Log("[ENGINE] Drivers loaded into engine.");

            EngineGenerateBracket(_engine);
            Logger.Log("[ENGINE] Bracket generated.");

            var roundOrder = EngineGetRoundOrder(_engine);
            if (roundOrder == null || roundOrder.Count == 0)
            {
                Logger.Log("? Bracket generated with no rounds � aborting reveal state update.");
                CanAdvanceChanged?.Invoke(false);
                CanPickWinnerChanged?.Invoke(false);
                NextMatchReady?.Invoke(null);
                return;
            }


            _revealedRounds.Clear();
            if (string.Equals(rt, RaceTypes.RoundRobin, StringComparison.OrdinalIgnoreCase))
            {
                // Pre-reveal all RR rounds upfront so the full schedule is visible immediately.
                // Winner input is gated by _activeRound, not by _revealedRounds.
                foreach (var r in roundOrder)
                    _revealedRounds.Add(r);
                _activeRound = roundOrder[0];
                Logger.Log($"[RR] Pre-revealed {roundOrder.Count} rounds. _activeRound='{_activeRound}'. Rounds=[{string.Join(",", roundOrder)}]");
            }
            else
            {
                _revealedRounds.Add(roundOrder[0]);
                // _activeRound stays null for non-RR modes
            }

            _winners.Clear();
            ClearDeferrals();
            PushFullRefresh();
            // NOTE: no live /api/reset here. Reset is event-wide on the server (it clears
            // every class in the event bucket), so resetting on each class's bracket
            // generation wiped sibling classes off the live site. The QueueLiveUpdate
            // below fully overwrites this class's state on the server (matches + cleared
            // winners), so a per-class reset is unnecessary; stale state from a previous
            // event run is cleared by the server's new-session guard (shared EventId).
            QueueLiveUpdate("GenerateBracket");
        }



        // Convenience wrapper (kept in case other callers use it)
        public void GenerateBracket(string raceType)
        {
            if (_session?.Drivers == null || _session.Drivers.Count < 2)
            {
                Logger.Log("? Cannot generate bracket � session driver list is invalid.");
                return;
            }

            GenerateBracket(raceType, _session.Drivers); // defaulting happens inside
        }

        public void AdvanceRound()
        {
            if (IsCompleted)
            {
                Logger.Log($"[CTRL][REJECT] AdvanceRound blocked for completed race '{_session.EventName}'.");
                CanAdvanceChanged?.Invoke(false);
                return;
            }

            Logger.Log($"[SNAP] AdvanceRound-entry  |  _engine={_engine?.GetType().Name ?? "null"}  |  _losersEngine={_losersEngine?.GetType().Name ?? "null"}  |  revealedRounds={string.Join(",", _revealedRounds)}");

            if (_engine == null)
            {
                Logger.Log("? AdvanceRound aborted � engine is null");
                CanAdvanceChanged?.Invoke(false);
                return;
            }

            // Push-to-back ordering is scoped to a single round — drop it as we move on.
            ClearDeferrals();

            if (_activeRound != null)
            {
                // RR pre-reveal mode: advance the active round instead of revealing a new one.
                // All rounds are already in _revealedRounds (full schedule visible from day 1).
                var orderedRounds = EngineGetRoundOrder(_engine).ToList();
                int idx = orderedRounds.IndexOf(_activeRound);
                if (idx >= 0 && idx + 1 < orderedRounds.Count)
                {
                    _activeRound = orderedRounds[idx + 1];
                    Logger.Log($"[RR] AdvanceRound: _activeRound advanced to '{_activeRound}'");
                }
                else
                {
                    _activeRound = null;
                    Logger.Log("[RR] AdvanceRound: past final RR round — _activeRound=null");
                }

                var rrRows = BuildCurrentBracketRows();
                BracketRedrawn?.Invoke(rrRows);
                Logger.Log($"[ROUND] Redrawn after RR active-round advance (active='{_activeRound ?? "null"}') with {rrRows.Count} rows");

                PushNextMatch();
                PushAdvanceState();
                QueueLiveUpdate("AdvanceRound");
                Logger.Log("[FORM1] AdvanceRound() completed (RR active-round path)");
                return;
            }

            // Non-RR path: reveal the next round label.
            var next = EngineGetRoundOrder(_engine).FirstOrDefault(r => !_revealedRounds.Contains(r));
            if (string.IsNullOrEmpty(next))
            {
                Logger.Log("??  No further rounds to reveal on current engine.");
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
            QueueLiveUpdate("AdvanceRound");

            Logger.Log("[FORM1] AdvanceRound() completed");
        }

        // ----------------  INTERNAL HELPERS (CORE FLOW)  ----------------
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
            PushDeferState();

            // In RR active-round mode, only the active round can supply the next match.
            // In all other modes, use _revealedRounds as before. ApplyRaceOrder honours
            // any "push to end of round" the operator has applied.
            var next = ApplyRaceOrder(
                              EngineGetMatches(_engine).Where(m => InActiveRaceScope(m) && !m.HasResult))
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
                Logger.Log("[DEBUG] PushAdvanceState: no rounds revealed � cannot advance");
                CanAdvanceChanged?.Invoke(false);
                return;
            }

            bool canAdvance;
            if (_activeRound != null)
            {
                // RR pre-reveal mode: "Generate Next Round" enables when the active round is
                // fully resolved AND there is a subsequent round to advance to.
                var activeMatches = EngineGetMatches(_engine)
                    .Where(m => string.Equals(m.RoundLabel, _activeRound, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                bool allActiveResolved = activeMatches.Count > 0 && activeMatches.All(m => m.HasResult);
                var roundOrderList = EngineGetRoundOrder(_engine).ToList();
                int activeIdx = roundOrderList.IndexOf(_activeRound);
                bool nextRoundExists = activeIdx >= 0 && activeIdx + 1 < roundOrderList.Count;
                canAdvance = allActiveResolved && nextRoundExists;
                Logger.Log($"[DEBUG] PushAdvanceState (RR active-round): activeRound='{_activeRound}', activeMatches={activeMatches.Count}, resolved={activeMatches.Count(m => m.HasResult)}, nextRoundExists={nextRoundExists}, canAdvance={canAdvance}");
            }
            else
            {
                // Non-RR path (or RR after last round is done): original revealed-round logic.
                var visibleMatches = EngineGetMatches(_engine)
                    .Where(m => _revealedRounds.Contains(m.RoundLabel))
                    .ToList();
                bool allVisibleResolved = visibleMatches.All(m => m.HasResult);
                bool moreRoundsExist = EngineGetRoundOrder(_engine).Any(r => !_revealedRounds.Contains(r));
                canAdvance = allVisibleResolved && moreRoundsExist;
                Logger.Log($"[DEBUG] PushAdvanceState: visible={visibleMatches.Count}, resolved={visibleMatches.Count(m => m.HasResult)}, moreRoundsExist={moreRoundsExist}, canAdvance={canAdvance}");
            }

            CanAdvanceChanged?.Invoke(canAdvance);

            // -- RR ? Buyback or Auto-Advance to Finals ---------------------
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

                    Logger.Log($"[RR][QMDRA] Check ? Revealed={_revealedRounds.Count}, N={n}, roundsReached={roundsReached}, visibleMatches={visibleMatchesQ.Count}, allVisibleResolved={allVisibleResolvedQ}");

                    if (roundsReached && allVisibleResolvedQ)
                    {
                        // standings display is allowed (keep your existing scorecard)
                        try
                        {
                            var card = RoundRobinScorecardLogger.BuildScorecard(rr, _matchResult);
                            _rrStandingsCardCache = card;
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

                        Logger.Log($"[RR][QMDRA] COMPLETE ? Advancing ALL drivers to finals. RankedCount={rankedAll.Count}, SessionDrivers={totalDrivers}");
                        Logger.Log("[RR][QMDRA] Finals seed order: " + (rankedAll.Count == 0 ? "(none)" : string.Join(", ", rankedAll.Select(d => d.Name))));

                        CaptureRoundRobinResultSnapshot(rr);
                        if (!_rrCompletionAnnounced)
                        {
                            _rrCompletionAnnounced = true;
                            RoundRobinCompleted?.Invoke();
                        }

                        // Gate the Finals behind an explicit click. This used to call
                        // InjectFinalsAllAdvance here, and because RoundRobinCompleted
                        // opens a modal standings window, closing that window dropped the
                        // RD straight into the Finals with no way to stop.
                        _pendingFinalsRanking = rankedAll;
                        _finalsPending = true;
                        FinalsPendingReason = FinalsReasonRoundRobinAllAdvance;
                        CanStartFinalsChanged?.Invoke(true);
                        Logger.Log("[RR][QMDRA] Finals pending — waiting for the RD to start them.");
                        return;
                    }

                    // Not complete yet ? normal flow continues (no buyback in QMDRA ever)
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
                    CaptureRoundRobinResultSnapshot(rr);

                    // Popup scorecard + keep detailed log
                    try
                    {
                        var card = RoundRobinScorecardLogger.BuildScorecard(rr, _matchResult);
                        _rrStandingsCardCache = card;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[RR] Scorecard popup failed: {ex.Message}");
                    }
                    RoundRobinScorecardLogger.Log(rr, _matchResult);
                    if (!_rrCompletionAnnounced)
                    {
                        _rrCompletionAnnounced = true;
                        RoundRobinCompleted?.Invoke();
                    }

                    var eligible = GetEligibleBuybackDrivers(); // uses _rrTop3 snapshot
                    if (eligible.Count >= 2)
                    {
                        CanOfferBuybackChanged?.Invoke(true);
                        return; // wait for user action
                    }

                    // Not enough for buyback ? Finals with a wildcard, on the RD's click
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
                        Logger.Log("? Finals gate not raised � could not determine wildcard finalist.");
                        return;
                    }

                    // Gate the Finals behind an explicit click. This used to pop a raw
                    // WinForms MessageBox from the controller and then call
                    // InjectFinal4Bracket outright; the RD never got to choose.
                    Logger.Log($"[RR] Not enough drivers for buyback (eligible={eligible.Count}). Wildcard finalist: {wildcard.Name}.");
                    _buybackChampionOverride = wildcard;   // consumed by InjectFinal4Bracket()
                    _finalsPending = true;
                    FinalsPendingReason = FinalsReasonBuybackSkipped;
                    FinalsPendingWildcardName = wildcard.Name;
                    CanStartFinalsChanged?.Invoke(true);
                    Logger.Log("[RR] Finals pending — waiting for the RD to start them.");
                    return;
                }
            }

            // -- Losers Bracket complete ? gate Finals ---------------------
            if (_inLosersPhase && _losersEngine != null)
            {
                bool isLbComplete = EngineGetMatches(_losersEngine).All(m => EngineHasWinner(_losersEngine, m.MatchId, m.RoundLabel));
                Logger.Log($"[DEBUG] PushAdvanceState (LB): inLosersPhase={_inLosersPhase}, resolvedLB={isLbComplete}");

                if (isLbComplete)
                {
                    Logger.Log("? Losers bracket complete.");
                    _inLosersPhase = false;

                    _finalsPending = true;
                    FinalsPendingReason = FinalsReasonLosersBracketComplete;
                    CanStartFinalsChanged?.Invoke(true);
                    Logger.Log("?? Finals pending � waiting for 'Generate Bracket' to seed finals.");
                    return;
                }
            }

            // -- Legacy fallback (if LB Final manually checked) ------------
            if (_session.RaceType == RaceTypes.LosersBracket && _revealedRounds.Any(r => string.Equals(RoundLabels.Normalize(r), "LB-F", StringComparison.OrdinalIgnoreCase)))
            {
                var finalMatch = EngineGetMatches(_engine).LastOrDefault();
                if (finalMatch != null && finalMatch.HasResult)
                {
                    Logger.Log("?? LB Final match resolved � injecting Final-4 bracket (fallback)...");
                    InjectFinal4Bracket();
                }
            }

            // -- Finals wrap-up � emit summary once ------------------------
            if (!_tournamentClosed && string.Equals(_session?.RaceType, RaceTypes.Finals, StringComparison.OrdinalIgnoreCase))
            {
                var all = EngineGetMatches(_engine).OrderBy(m => m.MatchId).ToList();
                var final = all.FirstOrDefault(m => string.Equals(m.RoundLabel, "F", StringComparison.OrdinalIgnoreCase))
                         ?? all.LastOrDefault();

                if (final != null && final.HasResult)
                {
                    var winner = _matchResult.GetWinner(final.MatchId);
                    Logger.Log($"[FINALS] Summary lookup ? winner={(winner != null ? winner.Name : "null")} for M{final.MatchId}");

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
                        EventName = _session?.EventName ?? "Unsaved class",
                        Bracket = "Finals (Pro Ladder)",
                        Winner = winner,
                        RunnerUp = runnerUp,
                        TotalDrivers = _session?.Drivers?.Count ?? 0,
                        TotalMatches = all.Count,
                        CompletedAt = DateTime.Now,
                        MatchResults = _matchResult.GetAllResults()
                    };

                    CaptureCurrentResultSnapshot();
                    CaptureCompletedResult(winner, runnerUp, summary.CompletedAt);
                    Logger.Log($"?? Tournament complete � Winner: {winner?.Name}, Runner-Up: {runnerUp?.Name}");
                    _tournamentClosed = true;

                    CanPickWinnerChanged?.Invoke(false);
                    CanAdvanceChanged?.Invoke(false);

                    TournamentCompleted?.Invoke(summary);
                }
            }
        }

        // ----------------  CORE HELPERS (SHARED)  ----------------
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
                Logger.Log($"[LOOKUP] GetMatch({matchId}) called while engine=null � returning null");
                return null;
            }

            var match = EngineGetMatches(_engine, matchId: matchId).FirstOrDefault(m => m.MatchId == matchId);
            Logger.Log(match != null
                ? $"[LOOKUP] GetMatch({matchId}) ? Round={match.RoundLabel}"
                : $"[LOOKUP] GetMatch({matchId}) ? NOT FOUND");
            return match;
        }
    }
}
