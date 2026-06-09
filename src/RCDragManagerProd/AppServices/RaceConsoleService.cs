using System;
using System.Collections.Generic;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;

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

        public RaceConsoleService(RaceController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <summary>Current console state snapshot.</summary>
        public RaceConsoleViewModel GetState() => RaceConsoleViewModelBuilder.Build(_controller);

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
