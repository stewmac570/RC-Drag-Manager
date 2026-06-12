using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private readonly RaceSessionRepository _sessionRepo;
        private readonly RaceSession _session;
        private readonly MultiClassEvent _multiEvent;
        private readonly MultiClassEventRepository _multiRepo;
        private List<Driver> _drivers = new List<Driver>();
        private MatchButtons? _currentButtons;
        private bool _finalsPopupShown;

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
            _raceConsole = new RaceConsoleService(_controller, this, new DriverRepository(connectionString));
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

            _controller.BracketRedrawn += OnBracketRedrawn;
            _controller.NextMatchReady += OnNextMatchReady;
            _controller.WinnersUpdated += OnWinnersUpdated;
            _controller.CanAdvanceChanged += OnCanAdvanceChanged;
            _controller.CanOfferBuybackChanged += OnCanOfferBuybackChanged;
            _controller.CanStartFinalsChanged += OnCanStartFinalsChanged;
            _controller.TournamentCompleted += OnTournamentCompleted;

            _controller.StartDialInPolling();
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
                _controller.CanOfferBuybackChanged -= OnCanOfferBuybackChanged;
                _controller.CanStartFinalsChanged -= OnCanStartFinalsChanged;
                _controller.TournamentCompleted -= OnTournamentCompleted;
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
                LblNowRacing.Text = "No active match";
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
        });

        private void OnCanAdvanceChanged(bool can) => Run(() =>
        {
            BtnNextRound.IsEnabled = can;
            if (can) _controller.UnlockDialIn();
            UpdatePrimaryButtons();
        });

        private void OnCanOfferBuybackChanged(bool enabled) => Run(() =>
        {
            BtnBuybacks.IsEnabled = enabled;
            BtnStandings.IsEnabled = enabled;
            if (enabled && !IsHostedMode)
                MessageBox.Show("Round-Robin complete.\nClick 'Open buybacks' to add drivers to the Losers Bracket.",
                    "Buyback phase ready", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        private void OnCanStartFinalsChanged(bool enabled) => Run(() =>
        {
            BtnGenerateBracket.IsEnabled = enabled;
            if (enabled)
            {
                BtnGenerateBracket.Content = "Start finals";
                if (!_finalsPopupShown && !IsHostedMode)
                {
                    _finalsPopupShown = true;
                    MessageBox.Show("Losers Bracket complete.\nClick 'Start finals' to run the Finals.",
                        "Finals ready", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else _finalsPopupShown = false;
            UpdatePrimaryButtons();
        });

        private void OnTournamentCompleted(RaceController.RaceSummary summary) => Run(() =>
        {
            // In hosted mode the multi-class window records stats and shows the popup.
            if (IsHostedMode) return;

            var winner = summary.Winner?.Name ?? "N/A";
            var runnerUp = summary.RunnerUp?.Name ?? "N/A";
            MessageBox.Show(
                $"Event: {summary.EventName}\nWinner: {winner}\nRunner-up: {runnerUp}\nMatches: {summary.TotalMatches}",
                "Event complete", MessageBoxButton.OK, MessageBoxImage.Information);
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
                .Select(d => new ConsoleDriverRow
                {
                    DriverId = d.Id,
                    Name = d.Name,
                    QualText = d.QualTime.HasValue ? d.QualTime.Value.ToString("0.000") : "—",
                    DialInText = FormatDialInPlain(_controller.GetDriverDialIn(d.Id))
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

        private void BtnAddDriver_Click(object sender, RoutedEventArgs e)
        {
            if (_controller.HasBracketStarted)
            {
                MessageBox.Show("This race has already started — drivers can't be added to the active race.",
                    "Race in progress", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string name = (TxtName.Text ?? "").Trim();
            string timeText = (TxtTime.Text ?? "").Trim();
            string error = _rosterService.Validate(name, timeText);
            if (error != null)
            {
                MessageBox.Show(error, "Add driver", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double? qual = _rosterService.ParseQualTime(timeText);
            var existing = _drivers.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (qual.HasValue) existing.QualTime = qual.Value;
            }
            else
            {
                _drivers.Add(_rosterService.BuildNewDriver(name, qual, _drivers));
            }

            RefreshDriverGrid();
            TxtName.Clear();
            TxtTime.Clear();
            BtnGenerateBracket.IsEnabled = _drivers.Count >= 2;
            UpdatePrimaryButtons();
        }

        private void BtnEditDriver_Click(object sender, RoutedEventArgs e)
        {
            if (_controller.HasBracketStarted)
            {
                MessageBox.Show("This race has already started — driver identity is fixed.",
                    "Race in progress", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var row = DgDrivers.SelectedItem as ConsoleDriverRow;
            if (row == null) return;
            var driver = _drivers.FirstOrDefault(d => d.Id == row.DriverId);
            if (driver == null) return;

            var dlg = new AddEditDriverDialog(driver.Name, "") { Owner = Host };
            if (dlg.ShowDialog() == true)
            {
                driver.Name = dlg.DriverName;
                RefreshDriverGrid();
            }
        }

        private void BtnSetQual_Click(object sender, RoutedEventArgs e)
        {
            var row = DgDrivers.SelectedItem as ConsoleDriverRow;
            if (row == null) { MessageBox.Show("Select a driver.", "Set qual time"); return; }
            var driver = _drivers.FirstOrDefault(d => d.Id == row.DriverId);
            if (driver == null) return;

            var dlg = new SetQualTimeDialog(driver.Name, driver.QualTime) { Owner = Host };
            if (dlg.ShowDialog() == true)
            {
                driver.QualTime = dlg.QualTime;
                RefreshDriverGrid();
            }
        }

        private void BtnSetDialIn_Click(object sender, RoutedEventArgs e)
        {
            var row = DgDrivers.SelectedItem as ConsoleDriverRow;
            if (row == null) { MessageBox.Show("Select a driver.", "Set dial-in"); return; }
            EditDialIn(row.DriverId, row.Name);
        }

        private void DgDrivers_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgDrivers.SelectedItem is ConsoleDriverRow row) EditDialIn(row.DriverId, row.Name);
        }

        private void EditDialIn(int driverId, string name)
        {
            if (_controller.DialInLocked)
            {
                var proceed = MessageBox.Show(
                    $"This round is in progress.\n\nEdit {name}'s dial-in anyway? It won't affect pairs that already raced.",
                    "Round in progress", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (proceed != MessageBoxResult.Yes) return;
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

            // In hosted mode the multi-class window records win/loss stats on completion.
            if (IsHostedMode) return;

            var winner = _controller.GetWinner(matchId);
            var loser = _controller.GetLoser(matchId);
            if (winner != null && loser != null && !IsBye(winner.Name) && !IsBye(loser.Name))
                PersistStats(winner, loser, matchId);
        }

        private void PersistStats(Driver winner, Driver loser, int matchId)
        {
            var match = _controller.GetMatch(matchId);
            _controller.PersistMatchStats(winner, loser, App.ConnectionString);
            var round = match?.RoundLabel ?? "";
            if (string.Equals(round, "F", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(round, "Final", StringComparison.OrdinalIgnoreCase))
                _controller.PersistEventWon(winner, App.ConnectionString);
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
            var raceType = _controller.Session?.RaceType ?? _session?.RaceType;
            var action = _raceConsole.ExecutePrimaryAction(_drivers, raceType);
            BtnGenerateBracket.IsEnabled = false;
            if (action == RaceConsolePrimaryAction.StartLosersBracket)
                BtnBuybacks.IsEnabled = false;
            UpdatePrimaryButtons();
        }

        private void BtnNextRound_Click(object sender, RoutedEventArgs e)
        {
            if (!BtnNextRound.IsEnabled) return;
            BtnNextRound.IsEnabled = false;
            try { _raceConsole.AdvanceRound(); }
            catch (Exception ex)
            {
                Logger.Log($"[WPF][CONSOLE] AdvanceRound failed: {ex}");
                MessageBox.Show("Failed to advance the round. Check the log.", "Advance round",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (_controller.HasBracketStarted)
            {
                var confirmed = MessageBox.Show(
                    "Reset this active race? This clears bracket progress, winners and round state. This cannot be undone.",
                    "Reset active race", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirmed != MessageBoxResult.Yes) return;
                if (_session != null) { try { _raceConsole.SaveProgress(); } catch { } }
            }

            _controller.Reset();
            IcPairings.ItemsSource = null;
            IcWinners.ItemsSource = null;
            UpdateQueue();
            BtnGenerateBracket.IsEnabled = _drivers.Count >= 2;
            BtnGenerateBracket.Content = "Generate bracket";
            BtnNextRound.IsEnabled = false;
            BtnStandings.IsEnabled = false;
            BtnBuybacks.IsEnabled = false;
            UpdatePrimaryButtons();
        }

        private void BtnStandings_Click(object sender, RoutedEventArgs e)
        {
            if (!_raceConsole.TryShowStandings())
                MessageBox.Show("Standings aren't available yet — they appear after Round Robin completes.",
                    "Standings not ready", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnBuybacks_Click(object sender, RoutedEventArgs e)
        {
            var eligible = _raceConsole.GetEligibleBuybacks();
            if (eligible == null || eligible.Count < 2)
            {
                MessageBox.Show("Not enough eligible drivers for a Losers Bracket.", "No entries",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new BuybackDialog(eligible.ToList()) { Owner = Host };
            if (dlg.ShowDialog() != true) return;

            switch (_raceConsole.ApplyBuybackSelection(dlg.SelectedDrivers))
            {
                case BuybackSelectionOutcome.Invalid:
                    MessageBox.Show("At least one driver must be selected.", "Invalid selection",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var selectable = (IcWinners.ItemsSource as IEnumerable<WinnerDisplayRow>)
                ?.Where(r => !r.IsHeader && r.MatchId > 0).ToList();
            if (selectable == null || selectable.Count == 0)
            {
                MessageBox.Show("No results to edit yet.", "Edit result",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var pick = new EditResultPickWindow(selectable) { Owner = Host };
            if (pick.ShowDialog() != true || pick.SelectedMatchId <= 0) return;
            int matchId = pick.SelectedMatchId;

            switch (_raceConsole.ValidateEditable(matchId))
            {
                case EditResultStatus.MatchNotFound:
                    MessageBox.Show("Match not found.", "Edit result"); return;
                case EditResultStatus.NoResultYet:
                    MessageBox.Show("That match has not run yet.", "Edit result"); return;
                case EditResultStatus.NotInActiveRound:
                    MessageBox.Show("You can only change results for the active round.", "Edit result"); return;
            }

            var match = _controller.GetMatch(matchId);
            var d1 = match.Driver1?.Name ?? "BYE";
            var d2 = match.Driver2?.Name ?? "BYE";
            var dlg = new EditResultDialog(matchId, match.RoundLabel, d1, d2,
                                           IsBye(d1), IsBye(d2)) { Owner = Host };
            if (dlg.ShowDialog() != true || dlg.Choice == 0) return;

            bool setFirst = dlg.Choice == 1;
            if (!_raceConsole.ApplyEditResult(matchId, setFirst))
                MessageBox.Show("Edit rejected. Only active-round matches can change and BYE can't win.",
                    "Edit result", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Save / close ──────────────────────────────────────────────────────

        private void BtnSaveProgress_Click(object sender, RoutedEventArgs e)
        {
            if (_session == null)
            {
                MessageBox.Show("Quick session has no saved file to update.", "Nothing to save");
                return;
            }
            _raceConsole.SaveProgress();
            MessageBox.Show("Race progress saved. You can resume this race later.", "Progress saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCloseRace_Click(object sender, RoutedEventArgs e)
        {
            var confirmed = MessageBox.Show(
                "Close this race? Make sure progress is saved if you want to resume it later.",
                "Close race", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirmed != MessageBoxResult.Yes) return;

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
