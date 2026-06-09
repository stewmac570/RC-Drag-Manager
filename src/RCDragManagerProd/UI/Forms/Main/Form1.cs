using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    public partial class Form1 : Form, IRaceSessionStore
    {
        private List<Driver> drivers = new List<Driver>();
        private RaceSession currentSession;
        private RaceSessionRepository sessionRepository = new RaceSessionRepository(Program.ConnectionString);
        private readonly RaceController _controller;
        private readonly RaceConsoleService _raceConsole;
        private bool _finalsPopupShown;
        private WinnerButtonContext _currentWinnerButtonContext;

        /// <summary>
        /// When true, Form1 is embedded inside MultiClassRaceForm.
        /// Suppresses the buyback MessageBox and TournamentCompleted stats/popup
        /// so MultiClassRaceForm can coordinate those across all classes.
        /// </summary>
        public bool IsHostedMode { get; set; }
        public event EventHandler HostedSaveAndCloseCompleted;

        /// <summary>
        /// Set by MultiClassRaceForm so that Save Progress / Close Race also persist
        /// the parent multi-class event record. Null in standalone (non-hosted) mode.
        /// </summary>
        internal MultiClassEvent _multiClassEvent;
        internal MultiClassEventRepository _multiClassEventRepo;

        public Form1(RaceController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _raceConsole = new RaceConsoleService(_controller, this);
            InitializeComponent();

            btnEditResult.Click += btnEditResult_Click;

            btnWinner1.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) ShowEditDialInForButton(isLeft: true); };
            btnWinner2.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) ShowEditDialInForButton(isLeft: false); };

            lvPairings.SizeChanged += (s, e) => ResizePairingsColumns();
            lvWinners.SizeChanged += (s, e) => ResizeWinnersColumns();

            // Double-clicking a driver row is a shortcut to the Set Dial-In flow
            // (issue #260). It routes through the same handler as the button so the
            // dial-in lock policy (issue #306) stays in one place. Guarded by HitTest
            // so a double-click on empty space does nothing.
            lvDrivers.DoubleClick += (s, e) =>
            {
                var hit = lvDrivers.HitTest(lvDrivers.PointToClient(Cursor.Position));
                if (hit.Item != null) btnSetDialIn_Click(s, e);
            };

            currentSession = _controller.Session;

            // Title comes from the console view model so the "Event: …" / "Quick Session"
            // composition lives in one UI-independent place (issue #284).
            lblEventTitle.Text = _raceConsole.GetState().EventTitle;

            if (currentSession?.DriverEntries != null && currentSession.DriverEntries.Count > 0)
            {
                drivers = currentSession.DriverEntries
                    .Select(e => new Driver
                    {
                        Id = e.DriverID,
                        Name = e.DriverName,
                        QualTime = e.QualifyingTime
                    })
                    .ToList();

                Logger.Log($"[CREATE] Hydrated {drivers.Count} drivers from RaceSession.DriverEntries.");

                Logger.Log($"[CREATE][RR] Session config at Form load → Variant='{currentSession.RoundRobinVariant}', N={currentSession.RoundsToRun}");

                UpdateDriverList();
                btnGenerateBracket.Enabled = true;
            }

            btnNextRound.Enabled = false;
            UpdateSetupPhaseUi();

            _controller.BracketRedrawn += RedrawFullBracket;
            _controller.NextMatchReady += OnNextMatchReady;
            _controller.WinnersUpdated += OnWinnersUpdated;
            _controller.CanAdvanceChanged += OnCanAdvanceChanged;
            _controller.CanOfferBuybackChanged += OnCanOfferBuybackChanged;
            _controller.CanStartFinalsChanged += OnCanStartFinalsChanged;
            _controller.TournamentCompleted += OnTournamentCompleted;

            _controller.StartDialInPolling();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                _controller.BracketRedrawn -= RedrawFullBracket;
                _controller.NextMatchReady -= OnNextMatchReady;
                _controller.WinnersUpdated -= OnWinnersUpdated;
                _controller.CanAdvanceChanged -= OnCanAdvanceChanged;
                _controller.CanOfferBuybackChanged -= OnCanOfferBuybackChanged;
                _controller.CanStartFinalsChanged -= OnCanStartFinalsChanged;
                _controller.TournamentCompleted -= OnTournamentCompleted;
            }
            catch { }

            _controller.StopDialInPolling();

            base.OnFormClosed(e);
        }

        // IRaceSessionStore: RaceConsoleService calls this to persist save/close orchestration
        // through the form's repositories (issue #284). Callers guard quick sessions
        // (currentSession == null) before invoking the persistence commands.
        void IRaceSessionStore.Persist() => PersistSession();
        private sealed class WinnerButtonContext
        {
            public int MatchId { get; set; }
            public string RoundLabel { get; set; }
            public string LeftName { get; set; }
            public string RightName { get; set; }
            public int LeftDriverId { get; set; }
            public int RightDriverId { get; set; }
        }

    }

}
