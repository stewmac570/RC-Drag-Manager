using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.ViewModels;
using RCDragManagerProd.WPF.Dialogs;
using RCDragManagerProd.WPF.ViewModels;

namespace RCDragManagerProd.WPF.Views
{
    /// <summary>
    /// One class's race console — drivers, bracket, winners, queue and winner
    /// buttons. Binds to a RaceController + RaceConsoleService (Form → Service →
    /// Engine). Hosted standalone in RaceConsoleWindow or one-per-tab inside
    /// MultiClassRaceWindow.
    /// </summary>
    public partial class RaceConsoleView : UserControl, IRaceSessionStore
    {
        private readonly RaceController _controller;
        private readonly RaceConsoleService _raceConsole;
        private readonly SessionRosterService _rosterService = new SessionRosterService();
        private readonly RaceRosterService _rosterEditService;
        private readonly RaceSessionRepository _sessionRepo;
        private readonly RaceSession _session;
        private readonly MultiClassEvent _multiEvent;
        private readonly MultiClassEventRepository _multiRepo;
        private List<Driver> _drivers = new List<Driver>();
        private MatchButtons? _currentButtons;
        private bool _finalsPopupShown;

        /// <summary>True while a dial-in cell editor is open, so background dial-in
        /// polls don't rebuild the grid out from under the operator (#416).</summary>
        private bool _dialInCellEditing;

        /// <summary>True when hosted in MultiClassRaceWindow; suppresses the buyback +
        /// completion popups so the parent coordinates them across all classes.</summary>
        public bool IsHostedMode { get; }

        /// <summary>Raised after the operator closes the race (hosted mode); the parent
        /// decides what to do (close the multi-class window).</summary>
        public event EventHandler CloseRaceCompleted;

        public RaceController Controller => _controller;

        public RaceConsoleView(RaceController controller, string connectionString,
                               bool hosted = false, MultiClassEvent evt = null,
                               MultiClassEventRepository multiRepo = null)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            IsHostedMode = hosted;
            _multiEvent = evt;
            _multiRepo = multiRepo;
            InitializeComponent();

            _sessionRepo = new RaceSessionRepository(connectionString);
            var driverRepo = new DriverRepository(connectionString);
            _rosterEditService = new RaceRosterService(driverRepo);
            _raceConsole = new RaceConsoleService(_controller, this, driverRepo);
            _session = _controller.Session;

            LblEventTitle.Text = _raceConsole.GetState().EventTitle;

            if (_session?.DriverEntries != null && _session.DriverEntries.Count > 0)
            {
                _drivers = _session.DriverEntries
                    .Select(e => new Driver { Id = e.DriverID, Name = e.DriverName, QualTime = e.QualifyingTime })
                    .ToList();
                RefreshDriverGrid();
                BtnGenerateBracket.IsEnabled = true;
            }
            UpdatePrimaryButtons();
            UpdateResultsButtons();
            ApplyCompletedRaceState();

            _controller.BracketRedrawn += OnBracketRedrawn;
            _controller.NextMatchReady += OnNextMatchReady;
            _controller.WinnersUpdated += OnWinnersUpdated;
            _controller.CanAdvanceChanged += OnCanAdvanceChanged;
            _controller.CanDeferChanged += OnCanDeferChanged;
            _controller.CanOfferBuybackChanged += OnCanOfferBuybackChanged;
            _controller.RoundRobinCompleted += OnRoundRobinCompleted;
            _controller.CanStartFinalsChanged += OnCanStartFinalsChanged;
            _controller.TournamentCompleted += OnTournamentCompleted;
            _controller.DialInsChanged += OnDialInsChanged;
            _controller.OperatorNotice += OnOperatorNotice;

            if (!_controller.IsCompleted)
                _controller.StartDialInPolling();
        }

        private void ApplyCompletedRaceState()
        {
            if (!_controller.IsCompleted) return;

            LblEventTitle.Text = $"{_raceConsole.GetState().EventTitle} — completed (results only)";
            BtnEditRoster.IsEnabled = false;
            DgDrivers.IsHitTestVisible = false;
            BtnEditResult.IsEnabled = false;
            BtnBuybacks.IsEnabled = false;
            BtnWinner1.IsEnabled = false;
            BtnWinner2.IsEnabled = false;
            BtnMoreTime.IsEnabled = false;
            BtnGenerateBracket.IsEnabled = false;
            BtnNextRound.IsEnabled = false;

            // Results and Standings are read-only, so a finished class keeps them —
            // that is how the RD gets the winner board back after the popup is gone.
            UpdateResultsButtons();
            UpdatePrimaryButtons();
            Logger.Log($"[WPF][CONSOLE] Completed race '{_session?.EventName}' opened results-only.");
        }

        /// <summary>
        /// Clears this class's bracket progress and returns the console to its
        /// pre-bracket state. Driven from the event Settings tab (#415), which is the
        /// only place reset lives now — it was removed from this console in #413
        /// after a one-click reset wiped a class at a meet.
        /// </summary>
        public void ResetClass()
        {
            if (_session != null) { try { _raceConsole.SaveProgress(); } catch { } }

            // Reset blanks RaceType, which would leave the class unable to generate a
            // bracket again — and mid-event RaceType has already mutated away from
            // what the class was configured with, so restore the captured original.
            var restoreType = _session == null
                ? null
                : EventSettingsService.RaceTypeToRestoreOnReset(
                    _session.OriginalRaceType, _session.RaceType);

            _controller.Reset();

            if (_session != null && restoreType != null)
                _session.RaceType = restoreType;

            IcPairings.ItemsSource = null;
            IcWinners.ItemsSource = null;
            UpdateQueue();
            RefreshDriverGrid();
            BtnGenerateBracket.IsEnabled = _drivers.Count >= 2;
            BtnGenerateBracket.Content = "Generate bracket";
            BtnNextRound.IsEnabled = false;
            BtnStandings.IsEnabled = false;
            BtnBuybacks.IsEnabled = false;
            UpdatePrimaryButtons();

            Logger.Log($"[WPF][SETTINGS] Class reset: '{_session?.ClassType ?? "(unsaved)"}'");
        }

        /// <summary>
        /// Persists progress without the "saved" confirmation, for host-driven saves
        /// such as a settings change (#415). No-op for a class with no saved file.
        /// </summary>
        public void SaveProgressQuiet()
        {
            if (_session == null) return;
            try { _raceConsole.SaveProgress(); }
            catch (Exception ex) { Logger.Log($"[WPF][SETTINGS] Quiet save failed: {ex}"); }
        }

        /// <summary>Explicit cleanup — called by the host window on close (not on tab
        /// switches, which would unload the control prematurely).</summary>
        public void Teardown()
        {
            try
            {
                _controller.BracketRedrawn -= OnBracketRedrawn;
                _controller.NextMatchReady -= OnNextMatchReady;
                _controller.WinnersUpdated -= OnWinnersUpdated;
                _controller.CanAdvanceChanged -= OnCanAdvanceChanged;
                _controller.CanDeferChanged -= OnCanDeferChanged;
                _controller.CanOfferBuybackChanged -= OnCanOfferBuybackChanged;
                _controller.RoundRobinCompleted -= OnRoundRobinCompleted;
                _controller.CanStartFinalsChanged -= OnCanStartFinalsChanged;
                _controller.TournamentCompleted -= OnTournamentCompleted;
                _controller.DialInsChanged -= OnDialInsChanged;
                _controller.OperatorNotice -= OnOperatorNotice;
            }
            catch { }
            _controller.StopDialInPolling();
        }

        void IRaceSessionStore.Persist()
        {
            if (_session != null) _sessionRepo.SaveSession(_session);
            if (_multiRepo != null && _multiEvent != null)
            {
                try { _multiRepo.SaveEvent(_multiEvent); }
                catch (Exception ex) { Logger.Log($"[WPF][SAVE] Multi-class event save failed: {ex}"); }
            }
        }

        private Window Host => Window.GetWindow(this);
        private void Run(Action a)
        {
            if (Dispatcher.CheckAccess()) a();
            else Dispatcher.Invoke(a);
        }

        // ── Controller events ─────────────────────────────────────────────────

        private void OnBracketRedrawn(IReadOnlyList<PairingRow> rows) => Run(() =>
        {
            var list = new List<PairingDisplayRow>();
            string lastHeader = null;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows ?? new List<PairingRow>())
            {
                if (row == null) continue;
                if (row.IsHeader)
                {
                    var label = RoundLabels.Normalize(row.RoundLabel);
                    if (string.Equals(label, lastHeader, StringComparison.OrdinalIgnoreCase)) continue;
                    lastHeader = label;
                    list.Add(new PairingDisplayRow { IsHeader = true, HeaderText = label });
                    continue;
                }

                string key = row.MatchId > 0
                    ? $"{row.RoundLabel}|{row.MatchId}"
                    : $"{row.RoundLabel}|{row.Driver1}|{row.Driver2}";
                if (!seen.Add(key)) continue;

                bool bye1 = IsBye(row.Driver1);
                bool bye2 = IsBye(row.Driver2);
                list.Add(new PairingDisplayRow
                {
                    MatchLabel = !string.IsNullOrEmpty(row.MatchNumber) ? row.MatchNumber
                               : (row.MatchId > 0 ? $"M{row.MatchId}" : "-"),
                    Driver1 = bye1 ? "BYE" : row.Driver1,
                    Driver2 = bye2 ? "BYE" : row.Driver2,
                    Bye1 = bye1, Bye2 = bye2
                });
            }

            IcPairings.ItemsSource = list;
        });

        private void OnNextMatchReady(PairingRow row) => Run(() =>
        {
            if (row == null)
            {
                _currentButtons = null;
                SetWinnerButton(BtnWinner1, "—", false);
                SetWinnerButton(BtnWinner2, "—", false);
                LblNowRacing.Text = "No active race";
                UpdateQueue();
                return;
            }

            var mb = _raceConsole.GetMatchButtons(row.MatchId);
            _currentButtons = mb;

            if (mb == null)
            {
                SetWinnerButton(BtnWinner1, "—", false);
                SetWinnerButton(BtnWinner2, "—", false);
                return;
            }

            var b = mb.Value;
            SetWinnerButton(BtnWinner1, b.LeftName + FormatDialIn(b.LeftDialIn), !b.LeftIsBye);
            SetWinnerButton(BtnWinner2, b.RightName + FormatDialIn(b.RightDialIn), !b.RightIsBye);
            if (b.LeftIsBye) BtnWinner1.Content = "BYE";
            if (b.RightIsBye) BtnWinner2.Content = "BYE";

            LblNowRacing.Text = $"Now racing — {RoundLabels.Normalize(row.RoundLabel)}";
            UpdateQueue();
        });

        private void OnWinnersUpdated(IReadOnlyList<WinnerRow> rows) => Run(() =>
        {
            var list = new List<WinnerDisplayRow>();
            string header = null;
            int n = 1;
            foreach (var w in rows ?? new List<WinnerRow>())
            {
                if (!string.Equals(header, w.RoundLabel, StringComparison.OrdinalIgnoreCase))
                {
                    header = w.RoundLabel ?? "";
                    list.Add(new WinnerDisplayRow { IsHeader = true, HeaderText = RoundLabels.Normalize(header) });
                }
                list.Add(new WinnerDisplayRow
                {
                    MatchLabel = $"M{n++}",
                    Winner = w.Winner ?? "",
                    Loser = w.Loser ?? "",
                    MatchId = w.MatchId
                });
            }
            IcWinners.ItemsSource = list;
            UpdateResultsButtons();
        });

        private void OnCanAdvanceChanged(bool can) => Run(() =>
        {
            BtnNextRound.IsEnabled = can && !_controller.IsCompleted;
            if (can) _controller.UnlockDialIn();
            UpdatePrimaryButtons();
        });

        private void OnCanDeferChanged(bool can) =>
            Run(() => BtnMoreTime.IsEnabled = can && !_controller.IsCompleted);

        private void OnCanOfferBuybackChanged(bool enabled) => Run(() =>
        {
            BtnBuybacks.IsEnabled = enabled && !_controller.IsCompleted;
            UpdateResultsButtons();
        });

        private void OnRoundRobinCompleted() => Run(() =>
        {
            UpdateResultsButtons();
            new RaceResultsWindow(_session) { Owner = Host }.ShowDialog();
        });

        private void OnCanStartFinalsChanged(bool enabled) => Run(() =>
        {
            BtnGenerateBracket.IsEnabled = enabled && !_controller.IsCompleted;
            if (enabled)
            {
                BtnGenerateBracket.Content = "Start finals";
                if (!_finalsPopupShown && !IsHostedMode)
                {
                    _finalsPopupShown = true;
                    MessageDialog.Info(Host, FinalsReadyMessage(), "Finals ready");
                }
            }
            else _finalsPopupShown = false;
            UpdatePrimaryButtons();
        });

        /// <summary>Explains why the Finals are waiting. Every route ends the same way —
        /// nothing starts until the RD clicks "Start finals".</summary>
        private string FinalsReadyMessage()
        {
            switch (_controller.FinalsPendingReason)
            {
                case RaceController.FinalsReasonRoundRobinAllAdvance:
                    return "Round Robin complete.\nEvery driver advances. Click 'Start finals' to run the Finals.";

                case RaceController.FinalsReasonBuybackSkipped:
                    var wildcard = _controller.FinalsPendingWildcardName;
                    return "Round Robin complete.\nNot enough drivers for a buyback" +
                           (string.IsNullOrWhiteSpace(wildcard) ? "" : $", so {wildcard} goes through as the wildcard") +
                           ".\nClick 'Start finals' to run the Finals.";

                default:
                    return "Losers Bracket complete.\nClick 'Start finals' to run the Finals.";
            }
        }

        // Fires for local edits and for changes the background live-site poll applies;
        // without this the grid and winner buttons showed stale dial-ins until the
        // operator touched them (#381).
        /// <summary>Shows a controller message in the app's own dialog, so nothing the
        /// engine has to say arrives as a grey Windows message box.</summary>
        private void OnOperatorNotice(string title, string message) =>
            Run(() => MessageDialog.Info(Host, message, title));

        private void OnDialInsChanged() => Run(() =>
        {
            // Rebuilding ItemsSource tears down an open cell editor, so a poll that
            // lands mid-typing would silently discard what the operator was entering
            // (#416). The edit's own commit refreshes the grid when it finishes.
            if (!_dialInCellEditing) RefreshDriverGrid();
            RefreshWinnerButtonDialIns();
        });

        private void OnTournamentCompleted(RaceController.RaceSummary summary) => Run(() =>
        {
            UpdateResultsButtons();
            ApplyCompletedRaceState();

            // In hosted mode the multi-class window records stats and shows the popup.
            if (IsHostedMode) return;

            var winner = summary.Winner?.Name ?? "N/A";
            var runnerUp = summary.RunnerUp?.Name ?? "N/A";
            Logger.Log($"[RESULT][EVENT] '{summary.EventName}' complete — winner={winner}, runner-up={runnerUp}, matches={summary.TotalMatches}");
            new ClassCompletionWindow(_session) { Owner = Host }.ShowDialog();
            _raceConsole.RecordTournamentCompletion(summary, _drivers);
        });

        // ── Rendering helpers ─────────────────────────────────────────────────

        private void UpdateQueue()
        {
            var upcoming = _controller.PeekUpcomingMatches(3)
                .Where(m => _currentButtons == null || m.MatchId != _currentButtons.Value.MatchId)
                .Take(2).ToList();

            LblOnDeck.Text = QueueText(upcoming.Count > 0 ? upcoming[0] : null);
            LblInHole.Text = QueueText(upcoming.Count > 1 ? upcoming[1] : null);
        }

        private string QueueText(EngineMatch m)
        {
            if (m == null) return "—";
            var mb = _raceConsole.GetMatchButtons(m.MatchId);
            if (mb == null) return "—";
            return $"{mb.Value.LeftName}  vs  {mb.Value.RightName}";
        }

        private void UpdatePrimaryButtons()
        {
            var primary = (Style)FindResource("Style.Button.Dialog.Primary");
            var secondary = (Style)FindResource("Style.Button.Dialog.Secondary");
            bool nextActive = BtnNextRound.IsEnabled;
            BtnNextRound.Style = nextActive ? primary : secondary;
            BtnGenerateBracket.Style = (!nextActive && BtnGenerateBracket.IsEnabled) ? primary : secondary;
        }

        private void RefreshDriverGrid()
        {
            DgDrivers.ItemsSource = _drivers
                .Select(d =>
                {
                    var dialIn = _controller.GetDriverDialIn(d.Id);
                    return new ConsoleDriverRow
                    {
                        DriverId = d.Id,
                        Name = d.Name,
                        QualText = d.QualTime.HasValue ? d.QualTime.Value.ToString("0.000") : "—",
                        DialInText = FormatDialInPlain(dialIn),
                        DialInEdit = dialIn.HasValue ? dialIn.Value.ToString("0.000") : ""
                    };
                })
                .ToList();
        }

        private void SetWinnerButton(Button btn, string text, bool enabled)
        {
            btn.Content = text;
            btn.IsEnabled = enabled;
        }

        private static bool IsBye(string name) =>
            string.IsNullOrWhiteSpace(name) || string.Equals(name.Trim(), "BYE", StringComparison.OrdinalIgnoreCase);

        private static string FormatDialIn(double? d) => d.HasValue ? $"  [{d.Value:0.000}]" : "";
        private static string FormatDialInPlain(double? d) => d.HasValue ? d.Value.ToString("0.000") : "—";

        // ── Driver actions ────────────────────────────────────────────────────

        private bool RejectCompletedRaceEdit()
        {
            if (!_controller.IsCompleted) return false;
            MessageDialog.Info(Host,
                "This class is complete and is available for viewing only. Use Results to view the saved winners and ladder.",
                "Class complete");
            return true;
        }

        /// <summary>Stops an unrun bracket and returns the console to roster setup.
        /// Once any winner is recorded, driver identity is permanently locked.</summary>
        private bool EnsureRosterCanBeEdited()
        {
            if (RejectCompletedRaceEdit()) return false;
            if (!_controller.HasBracketStarted) return true;

            if (_controller.HasRaceRun)
            {
                MessageDialog.Info(Host,
                    "A race result has already been recorded. The driver roster can no longer be changed.",
                    "Racing has started");
                return false;
            }

            if (!MessageDialog.Confirm(Host,
                    "The bracket has been generated, but no race has run.\n\nStop this bracket so you can change the drivers? You will need to generate a new bracket.",
                    "Edit race roster", destructive: true))
                return false;

            if (!_controller.TryDiscardUnrunBracket()) return false;

            IcPairings.ItemsSource = null;
            IcWinners.ItemsSource = null;
            _currentButtons = null;
            UpdateQueue();
            BtnGenerateBracket.IsEnabled = _drivers.Count >= 2;
            BtnGenerateBracket.Content = "Generate bracket";
            BtnNextRound.IsEnabled = false;
            BtnStandings.IsEnabled = false;
            BtnBuybacks.IsEnabled = false;
            UpdatePrimaryButtons();
            return true;
        }

        private void BtnEditRoster_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureRosterCanBeEdited()) return;
            var dlg = new RaceRosterDialog(_rosterEditService, _drivers, _session?.ClassType) { Owner = Host };
            if (dlg.ShowDialog() != true) return;

            _drivers = dlg.Drivers;
            SyncSessionRoster();
            RefreshDriverGrid();
            BtnGenerateBracket.IsEnabled = _drivers.Count >= 2;
            UpdatePrimaryButtons();
        }

        private void BtnSetQual_Click(object sender, RoutedEventArgs e)
        {
            if (RejectCompletedRaceEdit()) return;

            var row = DgDrivers.SelectedItem as ConsoleDriverRow;
            if (row == null) { MessageDialog.Info(Host, "Select a driver.", "Set qual time"); return; }
            var driver = _drivers.FirstOrDefault(d => d.Id == row.DriverId);
            if (driver == null) return;

            var dlg = new SetQualTimeDialog(driver.Name, driver.QualTime) { Owner = Host };
            if (dlg.ShowDialog() == true)
            {
                driver.QualTime = dlg.QualTime;
                SyncSessionRoster();
                RefreshDriverGrid();
            }
        }

        private void SyncSessionRoster()
        {
            if (_session == null) return;
            _rosterService.SyncSession(_session, _drivers);
        }

        private void BtnSetDialIn_Click(object sender, RoutedEventArgs e)
        {
            if (RejectCompletedRaceEdit()) return;

            var row = DgDrivers.SelectedItem as ConsoleDriverRow;
            if (row == null) { MessageDialog.Info(Host, "Select a driver.", "Set dial-in"); return; }
            EditDialIn(row.DriverId, row.Name);
        }

        private void DgDrivers_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RejectCompletedRaceEdit()) return;

            if (DgDrivers.SelectedItem is ConsoleDriverRow row) EditDialIn(row.DriverId, row.Name);
        }

        // ── Inline dial-in editing (#416) ────────────────────────────────────
        //
        // The dial-in changes constantly during a meet, so one click on the value
        // starts editing it. The "Dial-in" button and its dialog stay as the
        // keyboard/discoverable path; both commit through the same parser.

        /// <summary>First click into the Dial-in cell opens the editor, rather than only selecting it.</summary>
        private void DgDrivers_PreviewClick(object sender, MouseButtonEventArgs e)
        {
            var cell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null || cell.Column != ColDialIn || cell.IsEditing) return;

            var row = FindParent<DataGridRow>(cell);
            if (row?.Item == null) return;

            // Deliberately not handling the event: the DataGrid needs to run its own
            // click handling (selection, focus) first, or the editor opens without
            // the caret in it. Opening the editor is queued behind that.
            var item = row.Item;
            var column = cell.Column;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DgDrivers.CurrentCell = new DataGridCellInfo(item, column);
                DgDrivers.BeginEdit();
            }), DispatcherPriority.Input);
        }

        /// <summary>Puts the caret in the dial-in editor so the operator can just type.</summary>
        private void DgDrivers_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.Column != ColDialIn) return;

            _dialInCellEditing = true;

            var box = e.EditingElement as TextBox ?? FindChild<TextBox>(e.EditingElement);
            if (box == null) return;
            box.Focus();
            box.SelectAll();
        }

        private void DgDrivers_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Column != ColDialIn) { e.Cancel = true; return; }

            if (_controller.IsCompleted) { e.Cancel = true; RejectCompletedRaceEdit(); return; }

            var row = e.Row?.Item as ConsoleDriverRow;
            if (row == null) { e.Cancel = true; return; }

            // Same guard the dialog path uses: changing a dial-in mid-round is
            // allowed, but never silently.
            if (_controller.DialInLocked &&
                !MessageDialog.Confirm(Host,
                    $"This round is in progress.\n\nEdit {row.Name}'s dial-in anyway? It won't affect pairs that already raced.",
                    "Round in progress", destructive: true))
                e.Cancel = true;
        }

        private void DgDrivers_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column != ColDialIn) return;

            if (e.EditAction != DataGridEditAction.Commit || !(e.Row?.Item is ConsoleDriverRow row))
            {
                _dialInCellEditing = false;   // Escape / abandoned edit
                return;
            }

            // For a template column EditingElement is the ContentPresenter hosting the
            // editing template, not the TextBox — reading it directly yields "", which
            // parses as "clear the dial-in".
            var box = e.EditingElement as TextBox ?? FindChild<TextBox>(e.EditingElement);
            var parsed = RaceConsoleService.ParseDialIn(box?.Text ?? "");

            if (!parsed.Success)
            {
                // Keep the operator in the cell with what they typed so a typo can be
                // corrected, rather than discarding the entry. Escape still backs out.
                // The dialog is deferred so it doesn't run inside the edit-ending event.
                e.Cancel = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageDialog.Warn(Host, parsed.Error, "Dial-in");
                    box?.Focus();
                    box?.SelectAll();
                }), DispatcherPriority.Background);
                return;
            }

            _dialInCellEditing = false;
            _controller.UpdateDriverDialIn(row.DriverId, parsed.Cleared ? (double?)null : parsed.DialIn);

            // The controller raises DialInsChanged, but that fires before this edit
            // commits, so refresh once the DataGrid has finished with the cell.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshDriverGrid();
                RefreshWinnerButtonDialIns();
            }));
        }

        private static T FindParent<T>(DependencyObject d) where T : DependencyObject
        {
            while (d != null && !(d is T))
                d = VisualTreeHelper.GetParent(d);
            return d as T;
        }

        private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T hit) return hit;
                var nested = FindChild<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }

        private void EditDialIn(int driverId, string name)
        {
            if (RejectCompletedRaceEdit()) return;

            if (_controller.DialInLocked)
            {
                if (!MessageDialog.Confirm(Host,
                        $"This round is in progress.\n\nEdit {name}'s dial-in anyway? It won't affect pairs that already raced.",
                        "Round in progress", destructive: true))
                    return;
            }

            double? current = _controller.GetDriverDialIn(driverId);
            var dlg = new DialInDialog(name, current) { Owner = Host };
            if (dlg.ShowDialog() != true) return;

            _controller.UpdateDriverDialIn(driverId, dlg.Cleared ? (double?)null : dlg.DialIn);
            RefreshDriverGrid();
            RefreshWinnerButtonDialIns();
        }

        private void RefreshWinnerButtonDialIns()
        {
            if (_currentButtons == null) return;
            var mb = _raceConsole.GetMatchButtons(_currentButtons.Value.MatchId);
            if (mb == null) return;
            _currentButtons = mb;
            var b = mb.Value;
            if (!b.LeftIsBye) BtnWinner1.Content = b.LeftName + FormatDialIn(b.LeftDialIn);
            if (!b.RightIsBye) BtnWinner2.Content = b.RightName + FormatDialIn(b.RightDialIn);
        }

        // ── Winner buttons ────────────────────────────────────────────────────

        private void BtnWinner1_Click(object sender, RoutedEventArgs e) => SubmitWinner(true);
        private void BtnWinner2_Click(object sender, RoutedEventArgs e) => SubmitWinner(false);

        private void SubmitWinner(bool leftClicked)
        {
            if (_currentButtons == null) return;
            int matchId = _currentButtons.Value.MatchId;
            var submission = _raceConsole.SubmitWinnerFromButton(matchId, leftClicked);
            if (!submission.Accepted)
            {
                Logger.Log($"[WPF][WINNER] Submit not accepted for M{matchId}.");
                return;
            }

            var winner = _controller.GetWinner(matchId);
            var loser = _controller.GetLoser(matchId);
            var round = _controller.GetMatch(matchId)?.RoundLabel ?? "?";
            Logger.Log($"[RESULT] M{matchId} ({round}): {winner?.Name ?? "?"} defeated {loser?.Name ?? "BYE"}");

            // Stats are persisted once, at tournament completion (RecordTournamentCompletion,
            // or the multi-class window in hosted mode) — never per match, so result edits
            // before completion can't leave stale increments behind (#379).
        }

        private void BtnWinner1_RightClick(object sender, MouseButtonEventArgs e) => EditButtonDialIn(true);
        private void BtnWinner2_RightClick(object sender, MouseButtonEventArgs e) => EditButtonDialIn(false);

        private void EditButtonDialIn(bool left)
        {
            if (_currentButtons == null) return;
            var b = _currentButtons.Value;
            string name = left ? b.LeftName : b.RightName;
            int id = left ? b.LeftDriverId : b.RightDriverId;
            if (id <= 0 || IsBye(name)) return;
            EditDialIn(id, name);
        }

        // ── Primary actions ───────────────────────────────────────────────────

        private void BtnGenerateBracket_Click(object sender, RoutedEventArgs e)
        {
            if (_controller.IsCompleted)
            {
                MessageDialog.Info(Host, "This class is complete. Use Results to view the saved winners and ladder.",
                    "Class complete");
                return;
            }

            var raceType = _controller.Session?.RaceType ?? _session?.RaceType;
            var action = _raceConsole.ExecutePrimaryAction(_drivers, raceType);
            BtnGenerateBracket.IsEnabled = false;
            if (action == RaceConsolePrimaryAction.StartLosersBracket)
                BtnBuybacks.IsEnabled = false;
            UpdatePrimaryButtons();
        }

        private void BtnMoreTime_Click(object sender, RoutedEventArgs e)
        {
            try { _raceConsole.PushCurrentMatchToEndOfRound(); }
            catch (Exception ex)
            {
                Logger.Log($"[WPF][CONSOLE] PushCurrentMatchToEndOfRound failed: {ex}");
                MessageDialog.Error(Host, "Couldn't move that race. Check the log.", "More time");
            }
        }

        private void BtnNextRound_Click(object sender, RoutedEventArgs e)
        {
            if (!BtnNextRound.IsEnabled) return;
            BtnNextRound.IsEnabled = false;
            try { _raceConsole.AdvanceRound(); }
            catch (Exception ex)
            {
                Logger.Log($"[WPF][CONSOLE] AdvanceRound failed: {ex}");
                MessageDialog.Error(Host, "Failed to advance the round. Check the log.", "Advance round");
            }
        }

        private void BtnStandings_Click(object sender, RoutedEventArgs e)
        {
            // One standings table, not two. This used to open a monospace scorecard
            // popup that scored each round on a decaying scale (R1 4/1/2, R2
            // 3.5/0.75/1.5, R3 3/0.5/1) while RoundRobinRanker — the only thing that
            // actually decides rank and the Finals seeding — scores every round 4/1/2.
            // The two disagreed on both points and finishing order.
            if (RaceResultsPresentationBuilder.Build(_session).HasRoundRobinStandings)
            {
                new RaceResultsWindow(_session, ResultsTab.RoundRobinStandings) { Owner = Host }.ShowDialog();
                return;
            }

            MessageDialog.Info(Host, "Standings aren't available yet — they appear after Round Robin completes.",
                "Standings not ready");
        }

        private void BtnRaceResults_Click(object sender, RoutedEventArgs e)
        {
            var presentation = RaceResultsPresentationBuilder.Build(_session);
            if (!presentation.HasResults)
            {
                MessageDialog.Info(Host, "No race results have been recorded yet.", "Class results");
                return;
            }

            new RaceResultsWindow(_session) { Owner = Host }.ShowDialog();
        }

        /// <summary>
        /// Results and Standings are read-only views of the saved archive, so they stay
        /// available whenever there is something to show — including on a class that has
        /// finished. The winner board lives on the Results window's Winner tab, which is
        /// how a completed class gets its champion back after the popup is dismissed.
        /// </summary>
        private void UpdateResultsButtons()
        {
            var presentation = RaceResultsPresentationBuilder.Build(_session);
            if (BtnRaceResults != null) BtnRaceResults.IsEnabled = presentation.HasResults;
            if (BtnStandings != null) BtnStandings.IsEnabled = presentation.HasRoundRobinStandings;
        }

        private void BtnBuybacks_Click(object sender, RoutedEventArgs e)
        {
            if (RejectCompletedRaceEdit()) return;

            var eligible = _raceConsole.GetEligibleBuybacks();
            if (eligible == null || eligible.Count < 2)
            {
                MessageDialog.Info(Host, "Not enough eligible drivers for a Losers Bracket.", "No entries");
                return;
            }

            var dlg = new BuybackDialog(eligible.ToList()) { Owner = Host };
            if (dlg.ShowDialog() != true) return;

            switch (_raceConsole.ApplyBuybackSelection(dlg.SelectedDrivers))
            {
                case BuybackSelectionOutcome.Invalid:
                    MessageDialog.Warn(Host, "At least one driver must be selected.", "Invalid selection");
                    break;
                case BuybackSelectionOutcome.SingleToFinals:
                    break;
                case BuybackSelectionOutcome.Stored:
                    BtnGenerateBracket.IsEnabled = true;
                    BtnGenerateBracket.Content = "Start losers bracket";
                    BtnBuybacks.Content = "Edit buybacks";
                    UpdatePrimaryButtons();
                    break;
            }
        }

        private void BtnEditResult_Click(object sender, RoutedEventArgs e)
        {
            if (RejectCompletedRaceEdit()) return;

            var selectable = (IcWinners.ItemsSource as IEnumerable<WinnerDisplayRow>)
                ?.Where(r => !r.IsHeader && r.MatchId > 0).ToList();
            if (selectable == null || selectable.Count == 0)
            {
                MessageDialog.Info(Host, "No results to edit yet.", "Edit result");
                return;
            }

            var pick = new EditResultPickWindow(selectable) { Owner = Host };
            if (pick.ShowDialog() != true || pick.SelectedMatchId <= 0) return;
            int matchId = pick.SelectedMatchId;

            switch (_raceConsole.ValidateEditable(matchId))
            {
                case EditResultStatus.MatchNotFound:
                    MessageDialog.Info(Host, "Race not found.", "Edit result"); return;
                case EditResultStatus.NoResultYet:
                    MessageDialog.Info(Host, "That race hasn't run yet.", "Edit result"); return;
                case EditResultStatus.NotInActiveRound:
                    MessageDialog.Info(Host, "You can only change results for the active round.", "Edit result"); return;
            }

            var match = _controller.GetMatch(matchId);
            var d1 = match.Driver1?.Name ?? "BYE";
            var d2 = match.Driver2?.Name ?? "BYE";
            var dlg = new EditResultDialog(matchId, match.RoundLabel, d1, d2,
                                           IsBye(d1), IsBye(d2)) { Owner = Host };
            if (dlg.ShowDialog() != true || dlg.Choice == 0) return;

            bool setFirst = dlg.Choice == 1;
            if (!_raceConsole.ApplyEditResult(matchId, setFirst))
                MessageDialog.Info(Host, "Edit rejected. Only races in the active round can change, and a BYE can't win.",
                    "Edit result");
        }

        // ── Save / close ──────────────────────────────────────────────────────

        private void BtnSaveProgress_Click(object sender, RoutedEventArgs e)
        {
            if (RejectCompletedRaceEdit()) return;

            if (_session == null)
            {
                MessageDialog.Info(Host, "This class has no saved file to update.", "Nothing to save");
                return;
            }
            _raceConsole.SaveProgress();
            MessageDialog.Info(Host, "Class progress saved. You can resume this class later.", "Progress saved");
        }

        private void BtnCloseRace_Click(object sender, RoutedEventArgs e)
        {
            if (!MessageDialog.Confirm(Host,
                    "Close this class? Make sure progress is saved if you want to resume it later.",
                    "Close class", destructive: true))
                return;

            if (_session != null)
            {
                _raceConsole.CloseRace();
                _raceConsole.RecomputeEventsWon(_drivers);
            }

            if (IsHostedMode)
                CloseRaceCompleted?.Invoke(this, EventArgs.Empty);
            else
                Host?.Close();
        }
    }
}
