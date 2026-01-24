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

            _drivers = drivers;
            _session.Drivers = new List<Driver>(_drivers);   // keep session + controller in sync

            _engine = RaceEngineFactory.Create(rt);
            Logger.Log($"[ENGINE] Created '{_engine.GetType().Name}' for raceType='{rt}' (drivers={_drivers.Count})");

            _engine.LoadDrivers(_drivers);
            Logger.Log("[ENGINE] Drivers loaded into engine.");

            _engine.GenerateBracket();
            Logger.Log("[ENGINE] Bracket generated.");

            _revealedRounds.Clear();
            _revealedRounds.Add(_engine.GetRoundOrder().First());

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

            var next = _engine.GetRoundOrder().FirstOrDefault(r => !_revealedRounds.Contains(r));
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

            var next = _engine.GetMatches()
                              .Where(m => _revealedRounds.Contains(m.RoundLabel) && !m.HasResult)
                              .OrderBy(m => m.MatchId)
                              .FirstOrDefault();

            if (next == null)
            {
                CanPickWinnerChanged?.Invoke(false);
                NextMatchReady?.Invoke(null);

                // Final standings log (RR only)
                if (_engine is RoundRobinEngineAdapter rr && rr.GetMatches().All(m => rr.HasWinner(m.MatchId)))
                {
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

            var visibleMatches = _engine.GetMatches()
                                        .Where(m => _revealedRounds.Contains(m.RoundLabel))
                                        .ToList();

            bool allVisibleResolved = visibleMatches.All(m => m.HasResult);
            bool moreRoundsExist = _engine.GetRoundOrder().Any(r => !_revealedRounds.Contains(r));
            bool canAdvance = allVisibleResolved && moreRoundsExist;

            Logger.Log($"[DEBUG] PushAdvanceState: visible={visibleMatches.Count}, resolved={visibleMatches.Count(m => m.HasResult)}, moreRoundsExist={moreRoundsExist}, canAdvance={canAdvance}");

            CanAdvanceChanged?.Invoke(canAdvance);

            // ── RR → Buyback or Auto-Advance to Finals ─────────────────────
            if (_engine is RoundRobinEngineAdapter rr)
            {
                bool allRRResolved =
                    rr.GetRoundOrder().All(r => _revealedRounds.Contains(r)) &&
                    rr.GetMatches().All(m => m.HasResult);

                Logger.Log($"[DEBUG] PushAdvanceState (RoundRobin): allRRResolved={allRRResolved}");

                if (allRRResolved)
                {
                    _rrTop3 = rr.GetTopRankedDrivers(3);
                    var names = (_rrTop3 != null && _rrTop3.Count > 0)
                        ? string.Join(", ", _rrTop3.Select(d => d.Name))
                        : "(none)";
                    Logger.Log($"[RR] Top-3 snapshot captured on RR completion: {names}");

                    _rrMatchesSnapshot = rr.GetMatches().ToList();
                    _rrRoundOrderSnapshot = rr.GetRoundOrder().ToList();

                    // Popup scorecard + keep detailed log
                    try
                    {
                        var card = RoundRobinScorecardLogger.BuildScorecard(rr, _matchResult);
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
                bool isLbComplete = _losersEngine.GetMatches().All(m => _losersEngine.HasWinner(m.MatchId));
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
            if (_session.RaceType == "Losers Bracket" && _revealedRounds.Contains("Losers Bracket Final"))
            {
                var finalMatch = _engine.GetMatches().LastOrDefault();
                if (finalMatch != null && finalMatch.HasResult)
                {
                    Logger.Log("🧩 LB Final match resolved — injecting Final-4 bracket (fallback)...");
                    InjectFinal4Bracket();
                }
            }

            // ── Finals wrap-up — emit summary once ────────────────────────
            if (!_tournamentClosed && string.Equals(_session?.RaceType, "Finals", StringComparison.OrdinalIgnoreCase))
            {
                var all = _engine.GetMatches().OrderBy(m => m.MatchId).ToList();
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
                        runnerUp = (d1 != null && !ReferenceEquals(d1, winner)) ? d1 : d2;
                    }

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

            var match = _engine.GetMatches().FirstOrDefault(m => m.MatchId == matchId);
            Logger.Log(match != null
                ? $"[LOOKUP] GetMatch({matchId}) → Round={match.RoundLabel}"
                : $"[LOOKUP] GetMatch({matchId}) → NOT FOUND");
            return match;
        }
    }
}
