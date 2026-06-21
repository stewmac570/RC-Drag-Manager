using System.Collections.Generic;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// The primary "what does the Build/Start button do right now" decision for the
    /// race console. Resolved from the controller's phase, it replaces the three-way
    /// branch currently inline in <c>btnGenerateBracket_Click</c>.
    /// </summary>
    public enum RaceConsolePrimaryAction
    {
        /// <summary>Initial state — build the bracket from the entered drivers.</summary>
        BuildBracket,
        None,

        /// <summary>Round Robin done and buybacks stored — start the Losers Bracket.</summary>
        StartLosersBracket,

        /// <summary>Losers Bracket done — start the Finals.</summary>
        StartFinals
    }

    /// <summary>
    /// Immutable snapshot of the race-console screen state, derived from
    /// <see cref="RCDragManagerProd.Controllers.RaceController"/> by
    /// <see cref="RaceConsoleViewModelBuilder"/>. This is the UI-independent contract
    /// the race console renders from (issue #284): no WinForms types in or out, so the
    /// same state can back a future WPF view and can be asserted in headless tests.
    /// </summary>
    /// <remarks>
    /// This type is the data contract only. It performs no rendering and holds no
    /// engine-internal types — pairing rows are the existing UI-neutral
    /// <see cref="PairingRow"/> view models.
    /// </remarks>
    public sealed class RaceConsoleViewModel
    {
        public RaceConsoleViewModel(
            string eventTitle,
            bool hasBracketStarted,
            bool isInLosersBracketPhase,
            bool isFinalsPending,
            bool dialInLocked,
            string activeRoundLabel,
            IReadOnlyList<PairingRow> pairingRows,
            string currentMatchText,
            string onDeckText,
            string inTheHoleText,
            RaceConsolePrimaryAction primaryAction,
            string primaryActionLabel)
        {
            EventTitle = eventTitle;
            HasBracketStarted = hasBracketStarted;
            IsInLosersBracketPhase = isInLosersBracketPhase;
            IsFinalsPending = isFinalsPending;
            DialInLocked = dialInLocked;
            ActiveRoundLabel = activeRoundLabel;
            PairingRows = pairingRows ?? new List<PairingRow>();
            CurrentMatchText = currentMatchText;
            OnDeckText = onDeckText;
            InTheHoleText = inTheHoleText;
            PrimaryAction = primaryAction;
            PrimaryActionLabel = primaryActionLabel;
        }

        /// <summary>Header title, e.g. "Event: Spring Shootout" or "Quick Session".</summary>
        public string EventTitle { get; }

        /// <summary>True once a bracket has been generated for this session.</summary>
        public bool HasBracketStarted { get; }

        /// <summary>True when buybacks are stored and the Losers Bracket is pending.</summary>
        public bool IsInLosersBracketPhase { get; }

        /// <summary>True when the Losers Bracket is complete and the Finals are pending.</summary>
        public bool IsFinalsPending { get; }

        /// <summary>True while the active round's dial-ins are locked.</summary>
        public bool DialInLocked { get; }

        /// <summary>Active round label (e.g. "RR1", "SF"); empty before the bracket starts.</summary>
        public string ActiveRoundLabel { get; }

        /// <summary>The full revealed bracket as UI-neutral rows (headers + matches).</summary>
        public IReadOnlyList<PairingRow> PairingRows { get; }

        /// <summary>Current match as "Name vs Name", or "No current match." when none.</summary>
        public string CurrentMatchText { get; }

        /// <summary>On-deck match as "Name vs Name"; empty when there isn't one.</summary>
        public string OnDeckText { get; }

        /// <summary>In-the-hole match as "Name vs Name"; empty when there isn't one.</summary>
        public string InTheHoleText { get; }

        /// <summary>What the primary Build/Start button does in the current phase.</summary>
        public RaceConsolePrimaryAction PrimaryAction { get; }

        /// <summary>Caption for the primary button matching <see cref="PrimaryAction"/>.</summary>
        public string PrimaryActionLabel { get; }
    }
}
