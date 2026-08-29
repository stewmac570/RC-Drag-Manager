using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// Builds a <see cref="RaceConsoleViewModel"/> snapshot from a
    /// <see cref="RaceController"/> (issue #284). All console-state derivation lives here
    /// rather than in the form: the title composition, the current/on-deck/in-the-hole
    /// decomposition, and the primary Build/Start action decision. The builder reads only
    /// the controller's public/internal API and references no WinForms types, so its
    /// output can be asserted headlessly and reused by a future WPF view.
    /// </summary>
    public static class RaceConsoleViewModelBuilder
    {
        private const string BuildBracketLabel = "Build Bracket";
        private const string StartLosersBracketLabel = "Start Losers Bracket";
        private const string StartFinalsLabel = "Start Finals";
        private const string NoCurrentMatch = "No current match.";

        // Shown when a class is being run without a saved event behind it.
        internal const string UnsavedClassTitle = "Unsaved class";

        public static RaceConsoleViewModel Build(RaceController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            bool hasBracketStarted = controller.HasBracketStarted;

            // GetActiveRoundLabel() throws before a bracket exists (EnsureReady), so it is
            // only safe to query once the bracket has started.
            string activeRoundLabel = hasBracketStarted
                ? (controller.GetActiveRoundLabel() ?? string.Empty)
                : string.Empty;

            var pairingRows = hasBracketStarted
                ? controller.BuildCurrentBracketRows()
                : (IReadOnlyList<PairingRow>)Array.Empty<PairingRow>();

            ResolveUpcoming(controller, out string current, out string onDeck, out string inTheHole);

            var primaryAction = ResolvePrimaryAction(controller.IsFinalsPending, controller.IsInLosersBracketPhase);

            return new RaceConsoleViewModel(
                eventTitle: BuildEventTitle(controller),
                hasBracketStarted: hasBracketStarted,
                isInLosersBracketPhase: controller.IsInLosersBracketPhase,
                isFinalsPending: controller.IsFinalsPending,
                dialInLocked: controller.DialInLocked,
                activeRoundLabel: activeRoundLabel,
                pairingRows: pairingRows,
                currentMatchText: current,
                onDeckText: onDeck,
                inTheHoleText: inTheHole,
                primaryAction: primaryAction,
                primaryActionLabel: LabelFor(primaryAction));
        }

        /// <summary>
        /// The primary button's meaning by phase — the same precedence the console's
        /// Build/Start handler uses: Finals pending wins, then Losers Bracket pending,
        /// otherwise build the initial bracket.
        /// </summary>
        public static RaceConsolePrimaryAction ResolvePrimaryAction(bool isFinalsPending, bool isInLosersBracketPhase)
        {
            if (isFinalsPending) return RaceConsolePrimaryAction.StartFinals;
            if (isInLosersBracketPhase) return RaceConsolePrimaryAction.StartLosersBracket;
            return RaceConsolePrimaryAction.BuildBracket;
        }

        public static string LabelFor(RaceConsolePrimaryAction action)
        {
            switch (action)
            {
                case RaceConsolePrimaryAction.StartFinals: return StartFinalsLabel;
                case RaceConsolePrimaryAction.StartLosersBracket: return StartLosersBracketLabel;
                default: return BuildBracketLabel;
            }
        }

        private static string BuildEventTitle(RaceController controller)
        {
            var session = controller.Session;
            return session != null ? $"Event: {session.EventName}" : UnsavedClassTitle;
        }

        // Mirrors RaceController.BuildNextUpLabelText: the next up to three unresolved
        // matches in the active round are the current / on-deck / in-the-hole pairs, shown
        // with lane-adjusted names so the text matches the bracket and winner buttons.
        private static void ResolveUpcoming(RaceController controller, out string current, out string onDeck, out string inTheHole)
        {
            var list = controller.PeekUpcomingMatches(3).ToList();

            current = list.Count > 0 ? PairText(controller, list[0]) : NoCurrentMatch;
            onDeck = list.Count > 1 ? PairText(controller, list[1]) : string.Empty;
            inTheHole = list.Count > 2 ? PairText(controller, list[2]) : string.Empty;
        }

        private static string PairText(RaceController controller, EngineMatch match)
        {
            controller.GetLaneAdjustedNames(match, out string left, out string right);
            return $"{left} vs {right}";
        }
    }
}
