// ==========================================================================
// RaceController.cs   —  repository-free build
// RC Drag Manager – pure in-memory race controller.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd;                     // Driver, RaceSession
using RCDragManagerProd.RaceEngines;        // IRaceEngine, EngineMatch, factory
using RCDragManagerProd.ViewModels;         // PairingRow, WinnerRow


namespace RCDragManagerProd.Controllers
{
    public sealed class RaceController

    {
        // ────────────────────  STATE  ────────────────────
        private readonly RaceSession _session;

        private IRaceEngine _engine;
        private List<Driver> _drivers;
        private readonly HashSet<string> _revealedRounds = new HashSet<string>();
        private readonly List<WinnerRow> _winners = new List<WinnerRow>();
        public RaceSession Session => _session;
        private readonly MatchResult _matchResult = new MatchResult();


        // ────────────────────  EVENTS  ────────────────────
        public event Action<IReadOnlyList<PairingRow>> BracketRedrawn;
        public event Action<PairingRow> NextMatchReady;
        public event Action<IReadOnlyList<WinnerRow>> WinnersUpdated;
        public event Action<bool> CanAdvanceChanged;
        public event Action<bool> CanPickWinnerChanged;

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

            string nextRound = _engine.GetRoundOrder()
                                      .FirstOrDefault(r => !_revealedRounds.Contains(r));

            if (nextRound == null) return;

            _revealedRounds.Add(nextRound);
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

        private void PushNextMatch()
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
                return;
            }

            NextMatchReady?.Invoke(ToPairingRow(next));

            // If this match is a BYE pairing, disable the BYE button automatically in Form1.
            // So here, you just say: "Picking winner is allowed"
            CanPickWinnerChanged?.Invoke(true);
        }




        private void PushAdvanceState()
        {
            // ✅ If nothing has been revealed, you can't advance yet
            if (_revealedRounds.Count == 0)
            {
                CanAdvanceChanged?.Invoke(false);
                return;
            }

            bool allVisibleResolved = _engine.GetMatches()
                                             .Where(m => _revealedRounds.Contains(m.RoundLabel))
                                             .All(m => m.HasResult);

            bool moreRoundsExist = _engine.GetRoundOrder()
                                          .Any(r => !_revealedRounds.Contains(r));

            bool canAdvance = allVisibleResolved && moreRoundsExist;

            Console.WriteLine($"[DEBUG] PushAdvanceState: allVisibleResolved={allVisibleResolved}, moreRoundsExist={moreRoundsExist}, canAdvance={canAdvance}");

            CanAdvanceChanged?.Invoke(canAdvance);
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


    }
}
