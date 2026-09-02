// RaceController.cs
using System;
using System.Collections.Generic;

using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;
using RCDragManagerProd.RaceEngines;

namespace RCDragManagerProd.Controllers
{
    public sealed partial class RaceController
    {
        // ────────────────────  STATE  ────────────────────
        private readonly RaceSession _session;
        private readonly IStandingsDialogService _standingsDialogService;

        private IRaceEngine _engine;
        private IRaceEngine _losersEngine;
        private bool _inLosersPhase;

        private List<Driver> _drivers;
        // List (not HashSet) so insertion order is preserved for display ordering.
        // Rounds are added in chronological race order: RR → LB → Finals.
        private readonly List<string> _revealedRounds = new();
        private readonly List<WinnerRow> _winners = new();

        public RaceSession Session => _session;
        public bool IsCompleted =>
            _session.IsClosed || _session.ResultsArchive?.CompletedAt != null;
        private readonly MatchResult _matchResult = new();
        private MatchResult _results => _matchResult;

        public bool IsInLosersBracketPhase =>
            _session != null && _session.BuybackDrivers != null && _session.BuybackDrivers.Count >= 2;

        public bool HasBracketStarted => _engine != null;

        /// <summary>True once at least one match result has been recorded. A generated
        /// bracket with no result is still safe to discard so the roster can be fixed.</summary>
        public bool HasRaceRun => _matchResult.GetAllResults().Count > 0;

        private Driver _buybackChampionOverride;

        // Active round for RR: gates winner input to only the current pace-gated round.
        // null = non-RR mode (or pre-bracket). Set to first label on RR bracket generation,
        // advanced by AdvanceRound(), cleared when transitioning to Finals.
        private string _activeRound = null;

        // Per-round RR logging guard
        private readonly HashSet<string> _rrLoggedRounds = new HashSet<string>();

        // Round-robin snapshot (captured at completion)
        private List<Driver> _rrTop3;
        private bool _rrCompletionAnnounced;

        // ────────────────────  EVENTS  ────────────────────
        public event Action<IReadOnlyList<PairingRow>> BracketRedrawn;
        public event Action<PairingRow> NextMatchReady;
        public event Action<IReadOnlyList<WinnerRow>> WinnersUpdated;
        public event Action<bool> CanAdvanceChanged;
        public event Action<bool> CanPickWinnerChanged;
        public event Action<bool> CanOfferBuybackChanged;
        public event Action RoundRobinCompleted;

        /// <summary>
        /// A message for the operator: (title, body). Raised so the view can show it in
        /// the app's own themed dialog — the controller owns no UI. May fire on a
        /// non-UI thread; subscribers marshal to their dispatcher.
        /// </summary>
        public event Action<string, string> OperatorNotice;

        // Finals gating
        public event Action<bool> CanStartFinalsChanged;
        private bool _finalsPending;
        public bool IsFinalsPending => _finalsPending;

        /// <summary>Why the Finals are waiting, so the console can explain the gate
        /// without knowing how the class got here. One of the FinalsReason* values.</summary>
        public const string FinalsReasonLosersBracketComplete = "LosersBracketComplete";
        public const string FinalsReasonRoundRobinAllAdvance = "RoundRobinAllAdvance";
        public const string FinalsReasonBuybackSkipped = "BuybackSkipped";

        public string FinalsPendingReason { get; private set; }

        /// <summary>The wildcard promoted when there were too few drivers for a buyback;
        /// null otherwise. Shown to the RD before they commit to the Finals.</summary>
        public string FinalsPendingWildcardName { get; private set; }

        /// <summary>
        /// Round Robin finishing order captured when the Finals gate went up on an
        /// all-advance (QMDRA) class. Held rather than injected so the RD starts the
        /// Finals themselves; recomputed from the engine if a resume cleared it.
        /// </summary>
        private List<Driver> _pendingFinalsRanking;

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
            public IReadOnlyList<(int WinnerId, int LoserId)> MatchResults { get; set; }
        }

        public event Action<RaceSummary> TournamentCompleted;
        private bool _tournamentClosed;   // prevent double-firing

        // Snapshots so we can still show RR after engine swaps
        private List<EngineMatch> _rrMatchesSnapshot;
        private List<string> _rrRoundOrderSnapshot;

        // ────────────────────  CTOR  ────────────────────
        public RaceController(RaceSession session, IStandingsDialogService standingsDialogService = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _standingsDialogService = standingsDialogService ?? new ScrollableStandingsDialogService();
        }
    }
}
