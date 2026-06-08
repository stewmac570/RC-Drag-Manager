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
    public partial class Form1 : Form
    {
        private List<Driver> drivers = new List<Driver>();
        private RaceSession currentSession;
        private RaceSessionRepository sessionRepository = new RaceSessionRepository(Program.ConnectionString);
        private readonly RaceController _controller;
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
            InitializeComponent();

            btnEditResult.Click += btnEditResult_Click;

            btnWinner1.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) ShowEditDialInForButton(isLeft: true); };
            btnWinner2.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) ShowEditDialInForButton(isLeft: false); };

            lvPairings.SizeChanged += (s, e) => ResizePairingsColumns();
            lvWinners.SizeChanged += (s, e) => ResizeWinnersColumns();

            currentSession = _controller.Session;

            lblEventTitle.Text = currentSession != null
                ? $"Event: {currentSession.EventName}"
                : "Quick Session";

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
