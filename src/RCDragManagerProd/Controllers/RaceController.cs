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
    public sealed class RaceController
    {
        // ────────────────────  STATE  ────────────────────
        private readonly RaceSession _session;

        private IRaceEngine _engine;
        private IRaceEngine _losersEngine;
        private bool _inLosersPhase;

        private List<Driver> _drivers;
        private readonly HashSet<string> _revealedRounds = new();
        private readonly List<WinnerRow> _winners = new();

        public RaceSession Session => _session;
        private readonly MatchResult _matchResult = new();
        private MatchResult _results => _matchResult;

        public bool IsInLosersBracketPhase =>
            _session != null && _session.BuybackDrivers != null && _session.BuybackDrivers.Count >= 2;

        public bool HasBracketStarted => _engine != null;

        private Driver _buybackChampionOverride;

        // Per-round RR logging guard
        private readonly HashSet<string> _rrLoggedRounds = new HashSet<string>();

        // Round-robin snapshot (captured at completion)
        private List<Driver> _rrTop3;

        // ────────────────────  EVENTS  ────────────────────
        public event Action<IReadOnlyList<PairingRow>> BracketRedrawn;
        public event Action<PairingRow> NextMatchReady;
        public event Action<IReadOnlyList<WinnerRow>> WinnersUpdated;
        public event Action<bool> CanAdvanceChanged;
        public event Action<bool> CanPickWinnerChanged;
        public event Action<bool> CanOfferBuybackChanged;

        // Finals gating
        public event Action<bool> CanStartFinalsChanged;
        private bool _finalsPending;
        public bool IsFinalsPending => _finalsPending;

        // ── Event: tournament complete ───────────────────────────────────────
        public class RaceSummary
        {
            public string EventName { get; set; }
            public string Bracket { get; set; }   // e.g., "Finals (Pro Ladder)"
            public Driver Winner { get; set; }
            public Driver RunnerUp { get; set; }
            public int TotalDrivers { get; set; }
            public int TotalMatches { get; set; }
            public DateTime CompletedAt { get; set; }
        }

        public event Action<RaceSummary> TournamentCompleted;
        private bool _tournamentClosed;   // prevent double-firing

        // Snapshots so we can still show RR after engine swaps
        private List<EngineMatch> _rrMatchesSnapshot;
        private List<string> _rrRoundOrderSnapshot;

        // ────────────────────  CTOR  ────────────────────
        public RaceController(RaceSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        // ──────────────────  PUBLIC API  ──────────────────
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

        public void SubmitWinner(int matchId, bool firstOption)
        {
            EnsureReady();

            var match = _engine.GetMatches().FirstOrDefault(m => m.MatchId == matchId);
            if (match == null)
            {
                Logger.Log($"[WINNER] Reject — match {matchId} not found.");
                return;
            }

            if (_engine.HasWinner(matchId))
            {
                Logger.Log($"[WINNER] Reject — match {matchId} already has a winner.");
                return;
            }

            var winner = firstOption ? match.Driver1 : match.Driver2;
            var loser = firstOption ? match.Driver2 : match.Driver1;

            // Universal block — no BYE as winner
            if (winner == null || string.Equals(winner.Name?.Trim(), "BYE", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"[WINNER] Reject — cannot select BYE as winner for M{matchId}.");
                return;
            }

            Logger.Log($"[WINNER] M{matchId} {match.RoundLabel}: {winner.Name} over {(loser?.Name ?? "BYE")}");

            _engine.SetWinner(matchId, winner);
            _matchResult.SetWinner(matchId, winner, loser);

            _winners.Add(new WinnerRow
            {
                MatchId = matchId,
                RoundLabel = match.RoundLabel,
                Winner = winner.Name,
                Loser = loser?.Name ?? "BYE"
            });

            WinnersUpdated?.Invoke(_winners);

            // advance UI/state first
            PushNextMatch();
            PushAdvanceState();

            // Per-round RR scoring once a full RR round is resolved
            if (_engine is RoundRobinEngineAdapter rr)
                TryLogCompletedRound(rr);
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

        public void Reset()
        {
            _engine = null;
            _losersEngine = null;

            _inLosersPhase = false;
            _finalsPending = false;
            _tournamentClosed = false;

            _revealedRounds.Clear();
            _winners.Clear();

            if (_session != null) _session.RaceType = string.Empty;

            BracketRedrawn?.Invoke(Array.Empty<PairingRow>());
            WinnersUpdated?.Invoke(Array.Empty<WinnerRow>());
            NextMatchReady?.Invoke(null);
            CanAdvanceChanged?.Invoke(false);
            CanPickWinnerChanged?.Invoke(false);

            Logger.Log("[RESET] Controller cleared — ready for new class.");
        }

        public void SaveSession()
        {
            try
            {
                if (_session == null)
                {
                    Logger.Log("[SAVE] No active session — skipping save.");
                    return;
                }

                var mainMatches = _engine?.GetMatches() ?? Enumerable.Empty<EngineMatch>();
                var lbMatches = _losersEngine?.GetMatches() ?? Enumerable.Empty<EngineMatch>();
                var allMatches = mainMatches.Concat(lbMatches);

                var list = new List<RCDragManagerProd.Domain.MatchResultSave>();
                foreach (var m in allMatches)
                {
                    var w = _matchResult.GetWinner(m.MatchId);
                    var l = _matchResult.GetLoser(m.MatchId);

                    if (m.HasResult || w != null || l != null)
                    {
                        list.Add(new RCDragManagerProd.Domain.MatchResultSave
                        {
                            MatchId = m.MatchId,
                            WinnerDriverId = w?.Id ?? -1,
                            LoserDriverId = l?.Id ?? -1
                        });
                    }
                }

                _session.SavedResults = list;
                _session.SavedRevealedRounds = _revealedRounds?.ToList() ?? new List<string>();

                if (string.IsNullOrWhiteSpace(_session.RaceType) && _engine != null)
                {
                    _session.RaceType =
                        _engine is ProLadderEngineAdapter ? "Finals" :
                        _engine is RoundRobinEngineAdapter ? "Round Robin" :
                        _engine is RandomEngineAdapter ? "Losers Bracket" :
                        _engine.GetType().Name;
                }

                if (_session.Drivers == null && _drivers != null)
                    _session.Drivers = new List<Driver>(_drivers);

                Logger.Log($"[SAVE] results={list.Count}, rounds={_session.SavedRevealedRounds.Count}, type='{_session.RaceType}'");
            }
            catch (Exception ex)
            {
                Logger.Log($"[SAVE][ERROR] {ex}");
            }
        }

        // ────────────────  INTERNAL HELPERS  ────────────────
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

        public Driver GetWinner(int matchId) => _results.GetWinner(matchId);
        public Driver GetLoser(int matchId) => _results.GetLoser(matchId);

        public List<Driver> GetEligibleBuybackDrivers()
        {
            Logger.Log("📥 Starting Round Robin buyback eligibility check...");

            if (_engine is not RoundRobinEngineAdapter rr)
            {
                Logger.Log("❌ Engine is not RoundRobinEngineAdapter — buyback not available.");
                return new List<Driver>();
            }

            var rrMatches = rr.GetMatches() ?? new List<EngineMatch>();
            var allDrivers = rrMatches
                .SelectMany(m => new[] { m.Driver1, m.Driver2 })
                .Where(d => d != null)
                .GroupBy(d => d.Id)
                .Select(g => g.First())
                .ToList();

            if (allDrivers.Count == 0 && _session?.Drivers != null)
                allDrivers = _session.Drivers.ToList();

            Logger.Log($"📊 RR roster from matches: {allDrivers.Count} → [{string.Join(", ", allDrivers.Select(d => d.Name))}]");

            var top3 = (_rrTop3 != null && _rrTop3.Count == 3) ? _rrTop3 : rr.GetTopRankedDrivers(3);
            Logger.Log($"🥇 Top-3: [{string.Join(", ", top3.Select(d => d.Name))}]");

            var top3Ids = new HashSet<int>(top3.Select(d => d.Id));

            var eligible = allDrivers.Where(d => !top3Ids.Contains(d.Id)).ToList();
            Logger.Log($"✅ Buyback-eligible count: {eligible.Count} → [{string.Join(", ", eligible.Select(d => d.Name))}]");

            if (eligible.Count < 2)
                Logger.Log("⚠️ Only 1 or 0 eligible drivers — Losers Bracket cannot be created.");

            return eligible;
        }

        // New, minimal wrapper: persist selection to session and funnel into StartLosersBracket
        public void GenerateLosersBracket(List<Driver> selectedDrivers)
        {
            Logger.Log("📦 GenerateLosersBracket wrapper called…");

            if (selectedDrivers == null || selectedDrivers.Count < 2)
            {
                Logger.Log("⚠️  Cannot generate LB — <2 drivers selected");
                return;
            }

            _session.BuybackDrivers = new List<Driver>(selectedDrivers);
            var names = string.Join(", ", _session.BuybackDrivers.Select(d => d.Name));
            Logger.Log($"🔒 Stored {_session.BuybackDrivers.Count} selected LB drivers → [{names}]");

            StartLosersBracket();
        }

        // Called by Form1 to rebuild the ListView each time the bracket changes.
        public IReadOnlyList<PairingRow> BuildCurrentBracketRows()
        {
            var rows = new List<PairingRow>();

            Logger.Log(
                $"[ROWS] BUILD v2 — snapshotMatches={_rrMatchesSnapshot?.Count.ToString() ?? "null"}, " +
                $"snapshotRounds={_rrRoundOrderSnapshot?.Count.ToString() ?? "null"}, " +
                $"engine={_engine?.GetType().Name ?? "null"}, losersEngine={_losersEngine?.GetType().Name ?? "null"}, " +
                $"revealed=[{string.Join(",", _revealedRounds)}]");

            void AppendFrom(IEnumerable<EngineMatch> matches, IEnumerable<string> roundOrder, string tag, bool filterByRevealed)
            {
                if (matches == null || roundOrder == null)
                {
                    Logger.Log($"[ROWS] AppendFrom({tag}) skipped (matches/roundOrder null)");
                    return;
                }

                var before = rows.Count;
                var mList = matches.ToList();
                var rList = roundOrder.ToList();

                foreach (var round in rList)
                {
                    if (filterByRevealed && !_revealedRounds.Contains(round)) continue;

                    rows.Add(new PairingRow { IsHeader = true, RoundLabel = round });

                    foreach (var m in mList.Where(x => x.RoundLabel == round))
                    {
                        rows.Add(new PairingRow
                        {
                            MatchNumber = null,
                            MatchId = m.MatchId,
                            Driver1 = m.Driver1?.Name ?? "BYE",
                            Driver2 = m.Driver2?.Name ?? "BYE",
                            RoundLabel = m.RoundLabel
                        });
                    }
                }

                Logger.Log($"[ROWS] AppendFrom({tag}) → added {rows.Count - before} items. Total={rows.Count}");
            }

            // 1) Round Robin — show ALL rounds if we have a snapshot; otherwise only revealed
            if (_rrMatchesSnapshot != null && _rrRoundOrderSnapshot != null)
            {
                AppendFrom(_rrMatchesSnapshot, _rrRoundOrderSnapshot, "RR-snapshot", filterByRevealed: false);
            }
            else if (_engine is RoundRobinEngineAdapter rrLive)
            {
                AppendFrom(rrLive.GetMatches(), rrLive.GetRoundOrder(), "RR-live", filterByRevealed: true);
            }
            // 2) Randomized — show only revealed rounds
            else if (_engine is RandomEngineAdapter rndLive)
            {
                AppendFrom(rndLive.GetMatches(), rndLive.GetRoundOrder(), "Random-live", filterByRevealed: true);
            }

            // 3) Losers Bracket — during Finals show all LB rounds; otherwise only revealed
            if (_losersEngine != null)
            {
                bool filterLb = !string.Equals(_session?.RaceType, "Finals", StringComparison.OrdinalIgnoreCase);
                AppendFrom(_losersEngine.GetMatches(), _losersEngine.GetRoundOrder(), "Losers", filterByRevealed: filterLb);
            }

            // 4) Finals (Pro Ladder) — only revealed (SF, then F)
            if (_engine is ProLadderEngineAdapter pro)
            {
                AppendFrom(pro.GetMatches(), pro.GetRoundOrder(), "Finals(Pro)", filterByRevealed: true);
            }

            int displayNo = 1;
            foreach (var r in rows)
                if (!r.IsHeader) r.MatchNumber = $"M{displayNo++}";

            Logger.Log($"[ROWS] BuiltCurrentBracketRows → items={rows.Count}, matches(numbered)={displayNo - 1}");
            return rows;
        }

        public void InjectFinal4Bracket()
        {
            Logger.Log("🏁 Injecting Final-4 Pro Ladder bracket…");

            if (_rrTop3 == null || _rrTop3.Count != 3)
            {
                Logger.Log("❌ Cannot inject finals — Top-3 snapshot missing or incomplete");
                return;
            }

            Driver lbChampion = null;

            if (_buybackChampionOverride != null)
            {
                lbChampion = _buybackChampionOverride;
                Logger.Log($"[FINALS] Using auto wildcard (no LB): {lbChampion.Name}");
                _buybackChampionOverride = null;
            }
            else if (_losersEngine is RandomEngineAdapter adapter)
            {
                lbChampion = adapter.GetWinner();
                if (lbChampion == null)
                {
                    Logger.Log("❌ Cannot inject finals — Losers bracket champion not found");
                    return;
                }
            }
            else
            {
                Logger.Log("❌ Cannot inject finals — no losers bracket and no wildcard override set");
                return;
            }

            _session.RaceType = "Finals";
            _inLosersPhase = false;
            Logger.Log("[FINALS] Session race type set to 'Finals' (LB phase cleared).");

            var finalists = new List<Driver>(4);
            finalists.AddRange(_rrTop3);
            finalists.Add(lbChampion);
            Logger.Log($"[PRO] Final-4 = {string.Join(", ", finalists.Select(d => d.Name))}");

            var proAdapter = new ProLadderEngineAdapter();
            proAdapter.LoadDrivers(finalists);
            proAdapter.GenerateBracket();
            _engine = proAdapter;

            var finalMatches = proAdapter.GetMatches();
            Logger.Log($"[PRO] Matches generated: {finalMatches.Count}");
            foreach (var match in finalMatches)
            {
                Logger.Log($"[PRO] Match {match.MatchId}: Round={match.RoundLabel}, Driver1={(match.Driver1?.Name ?? "BYE")}, Driver2={(match.Driver2?.Name ?? "BYE")}");
            }

            var preserveLb = _revealedRounds
                .Where(r => r.StartsWith("Losers Bracket", StringComparison.OrdinalIgnoreCase))
                .ToList();

            _revealedRounds.Clear();
            foreach (var r in preserveLb) _revealedRounds.Add(r);
            _revealedRounds.Add("SF");

            Logger.Log($"🎯 Final-4 revealedRounds set to: {string.Join(",", _revealedRounds)} (Final will be revealed on Next Round)");

            var rows = BuildCurrentBracketRows();
            BracketRedrawn?.Invoke(rows);
            Logger.Log($"🖼️  Final-4 bracket redrawn with {rows.Count} rows");

            PushNextMatch();
            Logger.Log("🔔 Final-4 first match pushed to UI");

            PushAdvanceState();
            Logger.Log("[FINALS] Advance state evaluated after SF reveal (F gated).");
        }

        // DEBUG helper – prints one-line snapshot of key state
        private void LogEngineSnapshot(string context)
        {
            Logger.Log($"[SNAP] {context}  |  _engine={_engine?.GetType().Name ?? "null"}  |  _losersEngine={_losersEngine?.GetType().Name ?? "null"}  |  revealedRounds={string.Join(",", _revealedRounds)}");
        }

        public void SetBuybackDrivers(List<Driver> drivers)
        {
            if (drivers == null || drivers.Count < 2)
            {
                Logger.Log($"[CTRL] SetBuybackDrivers: invalid list — count = {drivers?.Count ?? 0}");
                return;
            }

            _session.BuybackDrivers = new List<Driver>(drivers);
            _inLosersPhase = true;

            Logger.Log($"[CTRL] Buy-back drivers stored: {_session.BuybackDrivers.Count} → {string.Join(", ", _session.BuybackDrivers.Select(d => d.Name))}");
        }

        public void StartLosersBracket()
        {
            if (_session.BuybackDrivers == null || _session.BuybackDrivers.Count < 2)
            {
                Logger.Log("⚠️ Cannot start Losers Bracket — no drivers stored.");
                return;
            }

            var buyNames = string.Join(", ", _session.BuybackDrivers.Select(d => d.Name));
            Logger.Log($"📦 Starting Losers Bracket… drivers={_session.BuybackDrivers.Count} [{buyNames}]");
            Logger.Log($"[STATE] Before LB start → engine={_engine?.GetType().Name ?? "null"}, losersEngine={_losersEngine?.GetType().Name ?? "null"}, rrSnapshotMatches={_rrMatchesSnapshot?.Count.ToString() ?? "null"}, rrSnapshotRounds={_rrRoundOrderSnapshot?.Count.ToString() ?? "null"}");

            _session.PairingHistory ??= new HashSet<(int, int)>();

            if ((_rrMatchesSnapshot == null || _rrRoundOrderSnapshot == null) && _engine is RoundRobinEngineAdapter rrSnap)
            {
                _rrMatchesSnapshot = rrSnap.GetMatches().ToList();
                _rrRoundOrderSnapshot = rrSnap.GetRoundOrder().ToList();
                Logger.Log($"[RR] Fallback snapshot saved in StartLosersBracket: matches={_rrMatchesSnapshot.Count}, rounds={_rrRoundOrderSnapshot.Count}");
            }

            var lbMatches = LosersBracketBuilder.Build(
                _session.BuybackDrivers,
                _session.PairingHistory,
                1000);

            Logger.Log($"📊 LB matches generated: {lbMatches.Count}");

            if (lbMatches == null || lbMatches.Count == 0)
            {
                Logger.Log("⚠️ No LB matches generated — aborting Losers Bracket start.");
                return;
            }

            var lbEngine = new RandomEngineAdapter();
            lbEngine.LoadDrivers(_session.BuybackDrivers);
            lbEngine.InjectMatches(lbMatches);
            Logger.Log($"🛠️ Injected LB matches into RandomEngineAdapter (drivers={_session.BuybackDrivers.Count})");

            _losersEngine = lbEngine;
            _engine = lbEngine;
            _inLosersPhase = true;                 // key flag for finals injection
            _session.RaceType = "Losers Bracket";
            Logger.Log("[LB] Engine swapped to LB adapter; LB phase entered; session type='Losers Bracket'.");

            _revealedRounds.Clear();
            _revealedRounds.Add("Losers Bracket R1");
            Logger.Log("🎬 Revealed: Losers Bracket R1");

            var rows = BuildCurrentBracketRows();
            BracketRedrawn?.Invoke(rows);
            Logger.Log($"🖼️ BracketRedrawn (LB R1) with {rows.Count} rows.");

            PushNextMatch();
            Logger.Log("🔔 First LB match pushed to UI");

            PushAdvanceState();
            Logger.Log("[LB] Advance state evaluated after LB R1 reveal.");
        }

        public void StartFinals()
        {
            if (!_finalsPending)
            {
                Logger.Log("[CTRL] StartFinals called but finals are not pending.");
                return;
            }

            if (_rrTop3 == null || _rrTop3.Count < 3)
            {
                Logger.Log("❌ Finals cannot start — RR Top-3 snapshot missing. Keeping Finals pending.");
                CanStartFinalsChanged?.Invoke(true);
                return;
            }

            Logger.Log($"[FINALS] Start request accepted. rrTop3=[{string.Join(", ", _rrTop3.Select(d => d.Name))}] losersEngine={_losersEngine?.GetType().Name ?? "null"} engine={_engine?.GetType().Name ?? "null"} revealed=[{string.Join(",", _revealedRounds)}]");

            Logger.Log("🏁 Starting Finals — injecting Final-4 Pro Ladder bracket…");
            InjectFinal4Bracket();

            _finalsPending = false;
            CanStartFinalsChanged?.Invoke(false);
            Logger.Log("[FINALS] Finals gate lowered (button disabled).");
        }

        // Log per-completed-round standings while in RR
        private void TryLogCompletedRound(RoundRobinEngineAdapter rr)
        {
            var rounds = rr.GetRoundOrder().ToList();
            var matches = rr.GetMatches().ToList();

            foreach (var r in rounds)
            {
                if (_rrLoggedRounds.Contains(r)) continue;

                bool allResolved = matches
                    .Where(m => m.RoundLabel == r)
                    .All(m => _matchResult.HasResult(m.MatchId));

                if (!allResolved) break;

                Logger.Log($"[RR-SCORE] === {r} complete — standings so far ===");
                RoundRobinScorecardLogger.Log(rr, _matchResult);
                _rrLoggedRounds.Add(r);
            }
        }

        public void StartFinalsTop3NoBuyback()
        {
            Logger.Log("[FINALS][NOBUYBACK] Starting Finals with Top-3 only (no buyback entries).");

            if (_rrTop3 == null || _rrTop3.Count != 3)
            {
                Logger.Log("❌ [FINALS][NOBUYBACK] Top-3 snapshot missing or incomplete — aborting.");
                return;
            }

            _inLosersPhase = false;
            _session.RaceType = "Finals";
            _finalsPending = false;
            CanStartFinalsChanged?.Invoke(false);
            Logger.Log("[FINALS][NOBUYBACK] Session race type set to 'Finals'. LB phase cleared. Finals gate lowered.");

            var finalists = new List<Driver>(_rrTop3);
            Logger.Log($"[PRO] Final-3 = {string.Join(", ", finalists.Select(d => d.Name))}");

            var proAdapter = new ProLadderEngineAdapter();
            proAdapter.LoadDrivers(finalists);
            proAdapter.GenerateBracket();
            _engine = proAdapter;

            var finalMatches = proAdapter.GetMatches();
            Logger.Log($"[PRO] Matches generated: {finalMatches.Count}");
            foreach (var match in finalMatches)
            {
                Logger.Log($"[PRO] Match {match.MatchId}: Round={match.RoundLabel}, Driver1={(match.Driver1?.Name ?? "BYE")}, Driver2={(match.Driver2?.Name ?? "BYE")}");
            }

            _revealedRounds.Clear();
            _revealedRounds.Add("SF");
            Logger.Log($"🎯 [FINALS][NOBUYBACK] Revealed rounds set to: {string.Join(",", _revealedRounds)}");

            var rows = BuildCurrentBracketRows();
            BracketRedrawn?.Invoke(rows);
            Logger.Log($"🖼️  [FINALS][NOBUYBACK] Bracket redrawn with {rows.Count} rows");

            PushNextMatch();
            Logger.Log("🔔 [FINALS][NOBUYBACK] First Finals match pushed to UI");

            PushAdvanceState();
            Logger.Log("[FINALS][NOBUYBACK] Advance state evaluated.");
        }

        // Returns the next unresolved matches in revealed rounds (current first).
        public IReadOnlyList<EngineMatch> PeekUpcomingMatches(int count = 3)
        {
            try
            {
                if (_engine == null || count <= 0) return Array.Empty<EngineMatch>();

                var list = _engine.GetMatches()
                                  .Where(m => _revealedRounds.Contains(m.RoundLabel) && !m.HasResult)
                                  .OrderBy(m => m.MatchId)
                                  .Take(count)
                                  .ToList();

                Logger.Log($"[CTRL][PEEK] Upcoming count={list.Count}, take={count} → [{string.Join(", ", list.Select(m => $"M{m.MatchId}:{m.Driver1?.Name ?? "BYE"} vs {m.Driver2?.Name ?? "BYE"}"))}]");

                return list;
            }
            catch (Exception ex)
            {
                Logger.Log($"[CTRL][PEEK][ERROR] {ex}");
                return Array.Empty<EngineMatch>();
            }
        }

        public string GetActiveRoundLabel()
        {
            EnsureReady();
            string active = null;
            foreach (var r in _engine.GetRoundOrder())
                if (_revealedRounds.Contains(r))
                    active = r;   // last revealed
            Logger.Log($"[CTRL][EDIT] Active round = '{active ?? "null"}'");
            return active;
        }

        public bool IsMatchInActiveRound(int matchId)
        {
            var m = GetMatch(matchId);
            var active = GetActiveRoundLabel();
            bool ok = m != null && !string.IsNullOrEmpty(active) &&
                      string.Equals(m.RoundLabel, active, StringComparison.OrdinalIgnoreCase);
            Logger.Log($"[CTRL][EDIT] IsMatchInActiveRound(M{matchId}) → {ok}");
            return ok;
        }

        public bool EditWinnerInActiveRound(int matchId, bool firstOption)
        {
            EnsureReady();

            var match = _engine.GetMatches().FirstOrDefault(m => m.MatchId == matchId);
            if (match == null)
            {
                Logger.Log($"[CTRL][EDIT] M{matchId} not found.");
                return false;
            }

            var active = GetActiveRoundLabel();
            if (string.IsNullOrEmpty(active) ||
                !string.Equals(match.RoundLabel, active, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"[CTRL][EDIT] Reject edit — M{matchId} is in '{match.RoundLabel}', active='{active}'.");
                return false;
            }

            var newWinner = firstOption ? match.Driver1 : match.Driver2;
            var newLoser = firstOption ? match.Driver2 : match.Driver1;

            if (newWinner == null || string.Equals(newWinner.Name?.Trim(), "BYE", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"[CTRL][EDIT] Reject edit — cannot set BYE as winner (M{matchId}).");
                return false;
            }

            _engine.SetWinner(matchId, newWinner);
            _matchResult.SetWinner(matchId, newWinner, newLoser);

            var row = _winners.FirstOrDefault(w => w.MatchId == matchId);
            if (row != null)
            {
                row.Winner = newWinner.Name;
                row.Loser = newLoser?.Name ?? "BYE";
            }
            else
            {
                _winners.Add(new WinnerRow
                {
                    MatchId = matchId,
                    RoundLabel = match.RoundLabel,
                    Winner = newWinner.Name,
                    Loser = newLoser?.Name ?? "BYE"
                });
            }

            Logger.Log($"[CTRL][EDIT] Override: M{matchId} ({match.RoundLabel}) → {newWinner.Name} over {(newLoser?.Name ?? "BYE")}.");

            WinnersUpdated?.Invoke(_winners);
            PushNextMatch();
            PushAdvanceState();
            return true;
        }
    }
}
