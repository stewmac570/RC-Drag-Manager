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

            if (winner == null)
                throw new InvalidOperationException("Cannot select a BYE as winner.");

            _engine.SetWinner(matchId, winner);

            _winners.Add(new WinnerRow
            {
                MatchId = matchId,
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

        /// <summary>
        /// Placeholder – persistence layer not wired yet.
        /// </summary>
        public void SaveSession()
        {
            // Intentionally left blank until repository / DB layer is defined.
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
            EngineMatch next = _engine.GetMatches()
                                      .Where(m => !m.HasResult &&
                                                  _revealedRounds.Contains(m.RoundLabel))
                                      .OrderBy(m => m.MatchId)
                                      .FirstOrDefault();

            if (next == null)
            {
                CanPickWinnerChanged?.Invoke(false);
                NextMatchReady?.Invoke(null);
                return;
            }

            NextMatchReady?.Invoke(ToPairingRow(next));
            CanPickWinnerChanged?.Invoke(true);
        }

        private void PushAdvanceState()
        {
            bool allVisibleResolved = _engine.GetMatches()
                                             .Where(m => _revealedRounds.Contains(m.RoundLabel))
                                             .All(m => m.HasResult);

            bool moreRoundsExist = _engine.GetRoundOrder()
                                          .Any(r => !_revealedRounds.Contains(r));

            CanAdvanceChanged?.Invoke(allVisibleResolved && moreRoundsExist);
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
    }
}
