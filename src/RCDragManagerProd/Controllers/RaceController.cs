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

        private Driver _buybackChampionOverride;

        // Active round for RR: gates winner input to only the current pace-gated round.
        // null = non-RR mode (or pre-bracket). Set to first label on RR bracket generation,
        // advanced by AdvanceRound(), cleared when transitioning to Finals.
        private string _activeRound = null;

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
