using System;
using System.Collections.Generic;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// UI-independent command + state seam for the race console (issue #284). The console
    /// form calls these commands and renders the returned <see cref="RaceConsoleViewModel"/>
    /// instead of owning the workflow decisions itself. Holds no WinForms types, so the same
    /// service can drive a future WPF view and the command logic can be asserted headlessly.
    /// </summary>
    public sealed class RaceConsoleService
    {
        private readonly RaceController _controller;
        private readonly IRaceSessionStore _store;
        private readonly DriverRepository _driverRepo;

        public RaceConsoleService(RaceController controller, IRaceSessionStore store = null,
                                  DriverRepository driverRepo = null)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _store = store;
            _driverRepo = driverRepo;
        }

        /// <summary>Current console state snapshot.</summary>
        public RaceConsoleViewModel GetState() => RaceConsoleViewModelBuilder.Build(_controller);

        /// <summary>
        /// Lane-correct winner-button info for a match: the left/right names (after lane swap),
        /// their dial-ins, and whether each side is a BYE. Returns null when the match is unknown.
        /// Lets a view (WinForms or WPF) label the two winner buttons without owning the internal
        /// lane-adjustment logic; pass the resulting left/right click straight to
        /// <see cref="SubmitWinnerFromButton"/> as uiFirstOption (true = left).
        /// </summary>
        public MatchButtons? GetMatchButtons(int matchId)
        {
            var m = _controller.GetMatch(matchId);
            if (m == null) return null;

            _controller.GetLaneAdjustedNames(m, out string left, out string right);

            bool swapped = left != (m.Driver1?.Name ?? "BYE");
            int leftId  = (swapped ? m.Driver2 : m.Driver1)?.Id ?? 0;
            int rightId = (swapped ? m.Driver1 : m.Driver2)?.Id ?? 0;

            return new MatchButtons(
                matchId,
                left, right,
                leftId  > 0 ? _controller.GetDriverDialIn(leftId)  : null,
                rightId > 0 ? _controller.GetDriverDialIn(rightId) : null,
                leftId, rightId,
                IsByeName(left), IsByeName(right));
        }

        private static bool IsByeName(string name) =>
            string.Equals((name ?? "").Trim(), "BYE", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Runs the phase-appropriate primary Build/Start action and reports which one ran.
        /// This is the decision that used to live inline in the console's Build/Start button
        /// handler: Finals pending → start Finals; Losers Bracket pending → start it;
        /// otherwise build the initial bracket from <paramref name="drivers"/>.
        /// </summary>
        /// <param name="drivers">Roster used only when building the initial bracket.</param>
        /// <param name="raceType">Engine key used only when building the initial bracket.</param>
        public RaceConsolePrimaryAction ExecutePrimaryAction(List<Driver> drivers, string raceType)
        {
            var action = RaceConsoleViewModelBuilder.ResolvePrimaryAction(
                _controller.IsFinalsPending, _controller.IsInLosersBracketPhase);

            switch (action)
            {
                case RaceConsolePrimaryAction.StartFinals:
                    _controller.StartFinals();
                    break;
                case RaceConsolePrimaryAction.StartLosersBracket:
                    _controller.StartLosersBracket();
                    break;
                default:
                    _controller.GenerateBracket(raceType, drivers);
                    break;
            }

            return action;
        }

        /// <summary>
        /// Advances to the next round. Locks the dial-ins first (a round is now committed)
        /// then advances — the lock/advance pairing the console's "Generate Next Round" button
        /// used to do inline. The form still refreshes its dial-in button state afterward.
        /// </summary>
        public void AdvanceRound()
        {
            _controller.LockDialIn();
            _controller.AdvanceRound();
        }

        /// <summary>
        /// Shows the Round Robin standings if available; returns false when they are not ready
        /// yet (so the form can explain that). Backs the "Standings" button.
        /// </summary>
        public bool TryShowStandings() => _controller.TryShowRoundRobinStandings();

        /// <summary>Drivers currently eligible for a buyback into the Losers Bracket.</summary>
        public IReadOnlyList<Driver> GetEligibleBuybacks() => _controller.GetEligibleBuybackDrivers();

        /// <summary>
        /// Applies the operator's buyback selection and reports what happened: a single pick
        /// skips the Losers Bracket and goes straight to a Finals slot; two or more are stored
        /// for a Losers Bracket; an empty selection is rejected. This is the dispatch that used
        /// to live inline in the buyback button handler (the selection dialog stays in the form).
        /// </summary>
        public BuybackSelectionOutcome ApplyBuybackSelection(List<Driver> selected)
        {
            if (selected == null || selected.Count == 0)
                return BuybackSelectionOutcome.Invalid;

            if (selected.Count == 1)
            {
                _controller.GenerateLosersBracket(selected);
                return BuybackSelectionOutcome.SingleToFinals;
            }

            _controller.SetBuybackDrivers(selected);
            return BuybackSelectionOutcome.Stored;
        }

        /// <summary>
        /// Submits the winner of a match from a winner-button click. Maps the UI side that was
        /// clicked to the engine's winner option, honoring BYEs (the real driver is forced to
        /// win) and lane swap (a swapped pairing flips left/right). Returns whether the submit
        /// was accepted (rejected only when both sides are BYE/unresolved). This is the mapping
        /// that used to live inline in the console's winner-button handler; the form keeps the
        /// stats bookkeeping that runs after a result is recorded.
        /// </summary>
        /// <param name="matchId">Engine match id.</param>
        /// <param name="uiFirstOption">True when the left winner button was clicked.</param>
        public WinnerSubmission SubmitWinnerFromButton(int matchId, bool uiFirstOption)
        {
            var match = _controller.GetMatch(matchId);
            if (match == null)
                return WinnerSubmission.Rejected;

            bool d1IsBye = ByePolicy.IsBye(match.Driver1);
            bool d2IsBye = ByePolicy.IsBye(match.Driver2);

            bool engineFirstOption;
            if (d1IsBye && !d2IsBye)
                engineFirstOption = false;            // engine Driver2 (the real driver) wins
            else if (d2IsBye && !d1IsBye)
                engineFirstOption = true;             // engine Driver1 (the real driver) wins
            else if (d1IsBye && d2IsBye)
                return WinnerSubmission.Rejected;      // nothing real to advance
            else
            {
                bool swapped = _controller.IsLaneSwapped(match.MatchId, match.RoundLabel, match.Driver1.Id, match.Driver2.Id);
                engineFirstOption = swapped ? !uiFirstOption : uiFirstOption;
            }

            _controller.SubmitWinner(matchId, engineFirstOption);
            return WinnerSubmission.Accept(engineFirstOption);
        }

        /// <summary>
        /// Pushes the current (next-up) race to the back of its round — used when a racer
        /// needs more time. No-op when fewer than two races remain in the round. The order
        /// is live-session only and is not persisted with the session.
        /// </summary>
        public void PushCurrentMatchToEndOfRound() => _controller.PushCurrentMatchToEndOfRound();

        /// <summary>
        /// Whether a match's result can be edited: it must exist, already have a result, and be
        /// in the active round. Backs the validation the console's "Edit Match Result" handler
        /// did before opening the winner picker.
        /// </summary>
        public EditResultStatus ValidateEditable(int matchId)
        {
            var match = _controller.GetMatch(matchId);
            if (match == null) return EditResultStatus.MatchNotFound;
            if (!match.HasResult) return EditResultStatus.NoResultYet;
            if (!_controller.IsMatchInActiveRound(matchId)) return EditResultStatus.NotInActiveRound;
            return EditResultStatus.Editable;
        }

        /// <summary>
        /// Applies an edited winner for an active-round match (engine option as chosen in the
        /// picker) and refreshes the on-deck match. Returns false if the controller rejects it.
        /// </summary>
        public bool ApplyEditResult(int matchId, bool engineFirstOption)
        {
            var ok = _controller.EditWinnerInActiveRound(matchId, engineFirstOption);
            if (ok) _controller.PushNextMatch();
            return ok;
        }

        /// <summary>
        /// Captures a resumable checkpoint and writes it through the session store: the race
        /// stays open and can be reopened later. Backs both "Save Progress" and the recovery
        /// snapshot taken before an active-race reset.
        /// </summary>
        public void SaveProgress()
        {
            RequireStore();
            _controller.SaveProgress();
            _store.Persist();
        }

        /// <summary>
        /// Finalises and closes the event: captures final state into the session, marks it
        /// finished, and persists. Stat recomputation and leaving the console stay with the
        /// caller (they are UI/lifecycle concerns).
        /// </summary>
        public void CloseRace()
        {
            RequireStore();
            _controller.SaveSession();
            _controller.CloseRace();
            _store.Persist();
        }

        /// <summary>
        /// Persists win/loss tallies, events-won, and events-entered stats for a completed
        /// single-class tournament. Safe to call with a null summary or when no
        /// <see cref="DriverRepository"/> was supplied (no-ops). Backs
        /// <c>Form1.OnTournamentCompleted</c>.
        /// </summary>
        public void RecordTournamentCompletion(RaceController.RaceSummary summary, IEnumerable<Driver> participants)
        {
            if (_driverRepo == null || summary == null) return;

            if (participants != null)
                foreach (var d in participants)
                    if (d?.Id > 0)
                    {
                        _driverRepo.IncrementEventsEntered(d.Id);
                        Logger.Log($"[STATS] +EventsEntered → #{d.Id} {d.Name}");
                    }

            if (summary.MatchResults != null)
                foreach (var (wId, lId) in summary.MatchResults)
                {
                    _driverRepo.IncrementWinsAndLosses(wId, lId);
                    Logger.Log($"[STATS] +Wins/Losses → winner={wId}, loser={lId}");
                }

            var winnerId = summary.Winner?.Id ?? 0;
            if (winnerId > 0)
            {
                _driverRepo.IncrementEventsWon(winnerId);
                Logger.Log($"[STATS] +EventsWon → #{winnerId} {summary.Winner?.Name}");
            }
        }

        /// <summary>
        /// Recomputes each participant's events-won tally from saved session history.
        /// Safe to call when no <see cref="DriverRepository"/> was supplied (no-ops).
        /// Backs the <c>Form1.btnCloseRace_Click</c> recompute after close.
        /// </summary>
        public void RecomputeEventsWon(IEnumerable<Driver> participants)
        {
            if (_driverRepo == null || participants == null) return;

            foreach (var d in participants)
            {
                if (d?.Id <= 0) continue;
                var db = _driverRepo.GetDriverById(d.Id);
                if (db == null) continue;
                db.EventsWon = _driverRepo.ComputeEventsWonFromSavedSessions(d.Id);
                _driverRepo.UpdateDriver(db);
                Logger.Log($"[STATS] Recompute EventsWon → #{db.Id} {db.Name}: {db.EventsWon}");
            }
        }

        private void RequireStore()
        {
            if (_store == null)
                throw new InvalidOperationException(
                    "This RaceConsoleService was created without an IRaceSessionStore; persistence commands are unavailable.");
        }
    }

    /// <summary>
    /// Persistence seam for the race console: writes the current session (and parent
    /// multi-class event, when hosted) through the repositories. The console form implements
    /// this over its repositories so save/close orchestration can live in
    /// <see cref="RaceConsoleService"/> and be tested with a fake store (issue #284/#283).
    /// </summary>
    public interface IRaceSessionStore
    {
        /// <summary>Writes the current race session through the repositories.</summary>
        void Persist();
    }

    /// <summary>Result of <see cref="RaceConsoleService.SubmitWinnerFromButton"/>.</summary>
    public readonly struct WinnerSubmission
    {
        private WinnerSubmission(bool accepted, bool engineFirstOption)
        {
            Accepted = accepted;
            EngineFirstOption = engineFirstOption;
        }

        /// <summary>False only when the match could not be resolved (both sides BYE / not found).</summary>
        public bool Accepted { get; }

        /// <summary>The engine winner option that was submitted (meaningful when accepted).</summary>
        public bool EngineFirstOption { get; }

        public static readonly WinnerSubmission Rejected = new WinnerSubmission(false, false);

        public static WinnerSubmission Accept(bool engineFirstOption) => new WinnerSubmission(true, engineFirstOption);
    }

    /// <summary>Lane-correct winner-button info from <see cref="RaceConsoleService.GetMatchButtons"/>.</summary>
    public readonly struct MatchButtons
    {
        public MatchButtons(int matchId, string leftName, string rightName,
                            double? leftDialIn, double? rightDialIn,
                            int leftDriverId, int rightDriverId,
                            bool leftIsBye, bool rightIsBye)
        {
            MatchId = matchId;
            LeftName = leftName;
            RightName = rightName;
            LeftDialIn = leftDialIn;
            RightDialIn = rightDialIn;
            LeftDriverId = leftDriverId;
            RightDriverId = rightDriverId;
            LeftIsBye = leftIsBye;
            RightIsBye = rightIsBye;
        }

        public int MatchId { get; }
        public string LeftName { get; }
        public string RightName { get; }
        public double? LeftDialIn { get; }
        public double? RightDialIn { get; }
        public int LeftDriverId { get; }
        public int RightDriverId { get; }
        public bool LeftIsBye { get; }
        public bool RightIsBye { get; }
    }

    /// <summary>Result of <see cref="RaceConsoleService.ValidateEditable"/>.</summary>
    public enum EditResultStatus
    {
        /// <summary>No match with that id.</summary>
        MatchNotFound,

        /// <summary>The match has not run yet — nothing to edit.</summary>
        NoResultYet,

        /// <summary>Only active-round results can be changed.</summary>
        NotInActiveRound,

        /// <summary>The result can be edited.</summary>
        Editable
    }

    /// <summary>Result of <see cref="RaceConsoleService.ApplyBuybackSelection"/>.</summary>
    public enum BuybackSelectionOutcome
    {
        /// <summary>No driver was selected — nothing applied.</summary>
        Invalid,

        /// <summary>One driver selected — Losers Bracket skipped, Finals slot promoted.</summary>
        SingleToFinals,

        /// <summary>Two or more selected — stored for the Losers Bracket.</summary>
        Stored
    }
}
