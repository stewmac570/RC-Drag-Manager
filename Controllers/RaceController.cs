using RCDragManagerProd;                     // Driver, RaceSession
using RCDragManagerProd.RaceEngines;        // IRaceEngine, EngineMatch, factory
using RCDragManagerProd.ViewModels;         // PairingRow, WinnerRow
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;



namespace RCDragManagerProd.Controllers
{
    public sealed class RaceController

    {
        // ────────────────────  STATE  ────────────────────
        private readonly RaceSession _session;  

        private IRaceEngine _engine;
        private IRaceEngine _losersEngine;
        private List<RandomMatch> _losersMatches;   
        private bool _inLosersPhase;          

        private List<Driver> _drivers;               
        private readonly HashSet<string> _revealedRounds = new();  
        private readonly List<WinnerRow> _winners = new();       

        public RaceSession Session => _session;             
        private readonly MatchResult _matchResult = new(); 
        private MatchResult _results => _matchResult;

        private List<Driver> _selectedDrivers;

        // ── round-robin snapshot ────────────────────────────────────
        private List<Driver> _rrTop3;

        // ────────────────────  EVENTS  ────────────────────
        public event Action<IReadOnlyList<PairingRow>> BracketRedrawn;   
        public event Action<PairingRow> NextMatchReady;   
        public event Action<IReadOnlyList<WinnerRow>> WinnersUpdated; 
        public event Action<bool> CanAdvanceChanged;  
        public event Action<bool> CanPickWinnerChanged;
        public event Action CanOfferBuybackChanged;
        

        // ────────────────────  CTOR  ────────────────────
        public RaceController(RaceSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }


        // ──────────────────  PUBLIC API  ──────────────────
        public void GenerateBracket(string raceType, List<Driver> drivers)
        {
            if (drivers == null || drivers.Count < 2)
                throw new InvalidOperationException("At least two drivers are required.");

            _drivers = drivers;

            _engine = RaceEngineFactory.Create(raceType);

            // ✅✅✅ Add this line: shows exactly what you get
            Console.WriteLine($"[DEBUG] Bracket using: {_engine.GetType().Name} for race type \"{raceType}\"");

            _engine.LoadDrivers(_drivers);
            _engine.GenerateBracket();

            _revealedRounds.Clear();
            _revealedRounds.Add(_engine.GetRoundOrder().First());

            _winners.Clear();
            PushFullRefresh();
        }


        public void SubmitWinner(int matchId, bool firstOption)
        {
            EnsureReady();

            EngineMatch match = _engine.GetMatches()
                .FirstOrDefault(m => m.MatchId == matchId);

            if (match == null)
                throw new ArgumentException($"Match {matchId} not found.", nameof(matchId));

            if (_engine.HasWinner(matchId))
                throw new InvalidOperationException("Winner already recorded.");

            Driver winner = firstOption ? match.Driver1 : match.Driver2;
            Driver loser = firstOption ? match.Driver2 : match.Driver1;

            // ✅ Universal block — no BYE as winner
            if (winner == null || string.Equals(winner.Name?.Trim(), "BYE", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot select BYE as winner.");

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

            PushNextMatch();
            PushAdvanceState();
        }

        public void AdvanceRound()
        {
            EnsureReady();
            LogEngineSnapshot("AdvanceRound-entry");

            // ── 🔍 Detect Round Robin complete and promote ──────────────
            if (_engine is RoundRobinEngineAdapter rr && rr.IsTournamentComplete())
            {
                Logger.Log("[RR] All round robin matches complete. Calculating rankings...");

                var topDrivers = rr.GetTopRankedDrivers(4);  // 🔧 Adjust number promoted here
                Logger.Log("[RR] Top drivers advancing to ladder: " +
                           string.Join(", ", topDrivers.Select(d => d.Name)));

                _engine = RaceEngineFactory.Create("pro ladder");
                _engine.LoadDrivers(topDrivers);
                _engine.GenerateBracket();

                _revealedRounds.Clear();
                _revealedRounds.Add("R1");

                Logger.Log("[RR] New Pro Ladder bracket created.");
                PushFullRefresh();
                return;
            }

            // ── 🏁 Losers Bracket complete — inject Final-4 ──────────────
            if (_session.RaceType == "Losers Bracket")
            {
                bool allResolved = _engine.GetMatches().All(m => m.HasResult);
                bool noMoreRounds = !_engine.GetRoundOrder().Any(r => !_revealedRounds.Contains(r));

                if (allResolved && noMoreRounds)
                {
                    Logger.Log("🚦 Losers Bracket complete — injecting Final-4...");
                    InjectFinal4Bracket();
                    return;
                }
            }

            // ── 🚦 Normal next round logic ───────────────────────────────
            string nextRound = _engine.GetRoundOrder()
                                      .FirstOrDefault(r => !_revealedRounds.Contains(r));

            if (nextRound == null)
            {
                Logger.Log("[ROUND] No next round to reveal.");
                return;
            }

            _revealedRounds.Add(nextRound);
            Logger.Log($"[ROUND] Revealing round: {nextRound}");

            PushFullRefresh();
        }



        public void Reset()
        {
            _engine?.Reset();
            _revealedRounds.Clear();
            _winners.Clear();

            BracketRedrawn?.Invoke(Array.Empty<PairingRow>());
            WinnersUpdated?.Invoke(Array.Empty<WinnerRow>());
            NextMatchReady?.Invoke(null);
            CanAdvanceChanged?.Invoke(false);
            CanPickWinnerChanged?.Invoke(false);
        }

        public void SaveSession()
        {
            if (_session == null) return;

            _session.SavedResults = _engine.GetMatches()
                .Where(m => _matchResult.HasResult(m.MatchId))
                .Select(m => new MatchResultSave
                {
                    MatchId = m.MatchId,
                    WinnerDriverId = _matchResult.GetWinner(m.MatchId)?.Id ?? -1,
                    LoserDriverId = _matchResult.GetLoser(m.MatchId)?.Id ?? -1
                })
                .ToList();

            _session.SavedRevealedRounds = _revealedRounds.ToList();
        }



        // ────────────────  INTERNAL HELPERS  ────────────────
        private void PushFullRefresh()
        {
            BracketRedrawn?.Invoke(BuildPairingRows());
            WinnersUpdated?.Invoke(_winners);
            PushNextMatch();
            PushAdvanceState();
            CanPickWinnerChanged?.Invoke(true);
        }

        public void PushNextMatch()
        {
            EnsureReady();

            // Look for the next unresolved match in revealed rounds, BYEs included
            var next = _engine.GetMatches()
                              .Where(m => _revealedRounds.Contains(m.RoundLabel) &&
                                          !m.HasResult)
                              .OrderBy(m => m.MatchId)
                              .FirstOrDefault();

            if (next == null)
            {
                CanPickWinnerChanged?.Invoke(false);
                NextMatchReady?.Invoke(null);

                // ✅ Log final standings if it's Round Robin
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

            Logger.Log($"[DEBUG] PushAdvanceState: " +
                       $"visible={visibleMatches.Count}, " +
                       $"resolved={visibleMatches.Count(m => m.HasResult)}, " +
                       $"moreRoundsExist={moreRoundsExist}, " +
                       $"canAdvance={canAdvance}");

            CanAdvanceChanged?.Invoke(canAdvance);

            // ── 🏁 Round Robin Buyback Trigger ─────────────────────────
            if (_engine is RoundRobinEngineAdapter)
            {
                bool allRRResolved = _engine.GetRoundOrder()
                                            .All(r => _revealedRounds.Contains(r)) &&
                                     _engine.GetMatches().All(m => m.HasResult);

                Logger.Log($"[DEBUG] PushAdvanceState (RoundRobin): allRRResolved={allRRResolved}");

                if (allRRResolved)
                {
                    Logger.Log("🏁 Round Robin complete — offering buyback phase.");
                    CanOfferBuybackChanged?.Invoke();
                }
            }

            // ── 🏁 Losers Bracket Final resolved — trigger Final-4 ───────
            if (_session.RaceType == "Losers Bracket" &&
                _revealedRounds.Contains("Losers Bracket Final"))
            {
                var finalMatch = _engine.GetMatches().LastOrDefault();
                if (finalMatch != null && finalMatch.HasResult)
                {
                    Logger.Log("🧩 LB Final match resolved — injecting Final-4 bracket...");
                    InjectFinal4Bracket();
                }
            }
        }


        private List<PairingRow> BuildPairingRows()
        {
            var rows = new List<PairingRow>();

            foreach (string round in _engine.GetRoundOrder()
                                            .Where(r => _revealedRounds.Contains(r)))
            {
                rows.Add(new PairingRow
                {
                    MatchId = -1,
                    RoundLabel = round,
                    IsHeader = true
                });

                rows.AddRange(_engine.GetMatches()
                                     .Where(m => m.RoundLabel == round)
                                     .OrderBy(m => m.MatchId)
                                     .Select(ToPairingRow));
            }

            return rows;
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

        private int ResolveDriverIdByName(string name)
        {
            return _drivers.FirstOrDefault(d =>
                string.Equals(d.Name?.Trim(), name?.Trim(), StringComparison.OrdinalIgnoreCase)
            )?.Id ?? -1;
        }

        public EngineMatch GetMatch(int matchId)
        {
            return _engine.GetMatches().FirstOrDefault(m => m.MatchId == matchId);
        }

        public Driver GetWinner(int matchId)
        {
            return _results.GetWinner(matchId);
        }

        public Driver GetLoser(int matchId)
        {
            return _results.GetLoser(matchId);
        }
        public string GetNextHiddenRound()
        {
            EnsureReady();

            foreach (var round in _engine.GetRoundOrder())
            {
                if (!_revealedRounds.Contains(round))
                    return round;
            }

            return null;
        }

        public List<Driver> GetEligibleBuybackDrivers()
        {
            Logger.Log("📥 Starting Round Robin buyback eligibility check...");

            // 1️⃣  Must be running the Round-Robin engine
            if (_engine is not RoundRobinEngineAdapter rr)
            {
                Logger.Log("❌ Engine is not RoundRobinEngineAdapter — buyback not available.");
                return new List<Driver>();
            }

            // 2️⃣  Grab standings
            Logger.Log("🔍 Retrieving ranked standings from RoundRobinEngineAdapter...");
            var top3 = rr.GetTopRankedDrivers(3).Select(d => d.Id).ToHashSet();

            var allDrivers = rr.GetStandings()
                   .Select(s => s.Driver)
                   .ToList();
            Logger.Log($"📊 Total drivers in session: {allDrivers.Count}");

            // 3️⃣  Everyone outside the top-3 can buy back
            var buybackEligible = allDrivers
                .Where(d => !top3.Contains(d.Id))
                .ToList();

            Logger.Log($"✅ Buyback eligible drivers: {string.Join(", ", buybackEligible.Select(d => d.Name))}");
            return buybackEligible;
        }


        public void GenerateLosersBracket(List<Driver> selectedDrivers)
        {
            Logger.Log("📦 Starting Losers Bracket generation…");

            // ── sanity check ──────────────────────────────────────────────
            if (selectedDrivers == null || selectedDrivers.Count < 2)
            {
                Logger.Log("⚠️  Cannot generate LB — <2 drivers selected");
                return;
            }

            // Persist selection so other methods (e.g. champion calc) can reuse it
            _selectedDrivers = selectedDrivers;
            Logger.Log($"🔒 Stored {_selectedDrivers.Count} selected LB drivers");

            // make sure the pairing-history set exists
            _session.PairingHistory ??= new HashSet<(int, int)>();

            // ── 1. build the bracket (RandomMatch list) ──────────────────
            var lbMatches = LosersBracketBuilder.Build(
                _selectedDrivers,
                _session.PairingHistory,
                1000); // startMatchId

            Logger.Log($"📊 LB matches generated: {lbMatches.Count}");

            // ── 2. capture Top-3 from RR before engine swap ──────────────
            LogEngineSnapshot("LB-pre-swap");

            if (_engine is RoundRobinEngineAdapter rr)
            {
                _rrTop3 = rr.GetTopRankedDrivers(3);
                var names = string.Join(", ", _rrTop3.Select(d => d.Name));
                Logger.Log($"[RR] Top-3 snapshot taken: {names}");
            }
            else
            {
                Logger.Log("⚠️  _engine was not RR adapter at LB-capture");
            }

            // ── 3. spin up new engine for losers bracket ─────────────────
            var adapter = new RandomEngineAdapter(new RandomMatchEngine());
            adapter.InjectMatches(lbMatches);
            Logger.Log($"🛠️  Injected {lbMatches.Count} LB matches into adapter");

            _losersEngine = adapter;   // preserve reference for Finals
            _engine = adapter;         // becomes active engine

            LogEngineSnapshot("LB-post-swap");

            // update session meta
            _session.RaceType = "Losers Bracket";

            // ── 4. reset/reveal UI state for round-1 ─────────────────────
            _revealedRounds.Clear();
            _revealedRounds.Add("Losers Bracket R1");

            // ── 5. push rows to the form via event ───────────────────────
            var rows = BuildCurrentBracketRows();
            BracketRedrawn?.Invoke(rows);
            Logger.Log($"🖼️  BracketRedrawn fired with {rows.Count} rows");

            // 🔔 Make the first LB pairing available to the UI
            PushNextMatch();
            Logger.Log("🔔 First losers-bracket match pushed to UI");

            // give the rest of the controller a chance to enable buttons
            PushAdvanceState();
        }





        public Driver RunLosersBracketChampion()
        {
            Logger.Log("🏁 Running Losers Bracket Engine to determine final buyback winner...");

            if (_session.RaceType != "Losers Bracket")
            {
                Logger.Log("⛔ Cannot resolve Losers Bracket — current session type is not 'Losers Bracket'");
                return null;
            }

            // Your LosersBracketBuilder.MatchIdOffset default is 1000
            var lbMatches = _session.Matches.OfType<RandomMatch>()
                                             .Where(m => m.MatchId >= 1000)
                                             .ToList();

            if (!lbMatches.Any())
            {
                Logger.Log("⚠️ No Losers Bracket matches found to resolve.");
                return null;
            }

            var winner = LosersBracketEngine.RunBracket(
                _selectedDrivers,               // a List<Driver>
                (driverA, driverB) =>          // callback that returns the winner
                {
                    //  ⬇️  decide who wins this pairing
                    //  REAL code could pop a UI dialog, rely on ET, etc.
                    //  For now: faster QualTime wins (nulls == BYE)
                    if (driverA == null) return driverB;
                    if (driverB == null) return driverA;

                    var winnerDriver = (driverA.QualTime ?? double.MaxValue)
                                     <= (driverB.QualTime ?? double.MaxValue)
                                     ? driverA : driverB;

                    Logger.Log($"🏁 LB match: {driverA.Name} vs {driverB.Name} ➜ {winnerDriver.Name}");
                    return winnerDriver;
                });



            Logger.Log($"🏆 Losers Bracket Champion: {winner?.Name ?? "null"}");

            return winner;
        }
        // ────────────────────  PUBLIC  ────────────────────
        // Called by Form1 to rebuild the ListView each time the bracket changes.
        public IReadOnlyList<PairingRow> BuildCurrentBracketRows()
        {
            var rows = new List<PairingRow>();

            // No engine? → return empty list
            if (_engine == null)
            {
                Logger.Log("[DEBUG] BuildCurrentBracketRows(): _engine is null");
                return rows;
            }

            // We rely on IRaceEngine.GetMatches() which every engine implements
            foreach (var m in _engine.GetMatches())
            {
                // Only show rounds that the UI has already revealed
                if (!_revealedRounds.Contains(m.RoundLabel))
                    continue;

                // Add a header row at the start of each round
                if (!rows.Any(r => r.RoundLabel == m.RoundLabel && r.IsHeader))
                {
                    rows.Add(new PairingRow
                    {
                        RoundLabel = m.RoundLabel,
                        IsHeader = true
                    });
                }

                // Normal pairing row
                rows.Add(new PairingRow
                {
                    MatchId = m.MatchId,
                    RoundLabel = m.RoundLabel,
                    Driver1 = m.Driver1?.Name ?? "BYE",
                    Driver2 = m.Driver2?.Name ?? "BYE",
                    IsHeader = false
                });

            }

            Logger.Log($"[DEBUG] BuildCurrentBracketRows(): built {rows.Count} rows");
            return rows;
        }
        /// <summary>
        /// Very first-cut winner picker: ALWAYS returns driver A.
        /// Replace with real UI / logic later.
        /// </summary>
        private Driver PickWinnerCallback(Driver a, Driver b)
        {
            // TODO: hook this to UI; for now just return a
            Logger.Log($"[DEBUG] Auto-picking winner between {a?.Name} and {b?.Name} – returning {a?.Name}");
            return a;
        }
        public void InjectFinal4Bracket()
        {
            Logger.Log("🏁 Injecting Final-4 Pro Ladder bracket…");

            // ── sanity check ──────────────────────────────────────────────
            if (_rrTop3 == null || _rrTop3.Count != 3)
            {
                Logger.Log("❌ Cannot inject finals — Top-3 snapshot missing or incomplete");
                return;
            }

            if (_losersEngine is not RandomEngineAdapter adapter)
            {
                Logger.Log("❌ Cannot inject finals — Losers engine is not a RandomEngineAdapter");
                return;
            }

            var lbChampion = adapter.GetWinner();
            if (lbChampion == null)
            {
                Logger.Log("❌ Cannot inject finals — Losers bracket champion not found");
                return;
            }

            // ── combine into final-4 ──────────────────────────────────────
            var finalists = _rrTop3.Concat(new List<Driver> { lbChampion }).ToList();
            Logger.Log($"[PRO] Final-4 = {string.Join(", ", finalists.Select(d => d.Name))}");

            // ── spin up fresh pro ladder engine ───────────────────────────
            var proAdapter = new ProLadderEngineAdapter();
            proAdapter.LoadDrivers(finalists);
            proAdapter.GenerateBracket();
            _engine = proAdapter;

            _revealedRounds.Clear();
            _revealedRounds.Add("Semi-Finals");

            var rows = BuildCurrentBracketRows();
            BracketRedrawn?.Invoke(rows);
            Logger.Log($"🖼️  Final-4 bracket redrawn with {rows.Count} rows");

            PushNextMatch();
            Logger.Log("🔔 Final-4 first match pushed to UI");

            PushAdvanceState();
        }


        // ──────────────────────────────────────────────────────────────
        // DEBUG helper – prints one-line snapshot of key state
        // Call with: LogEngineSnapshot("any label you like")
        private void LogEngineSnapshot(string context)
        {
            Logger.Log($"[SNAP] {context}  |  _engine={_engine?.GetType().Name ?? "null"}  |  " +
                       $"_losersEngine={_losersEngine?.GetType().Name ?? "null"}  |  " +
                       $"revealedRounds={string.Join(",", _revealedRounds)}");
        }

    }

}
