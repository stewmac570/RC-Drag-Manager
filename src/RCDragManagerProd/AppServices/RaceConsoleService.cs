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
    }
}
