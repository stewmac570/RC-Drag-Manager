using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Integration;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.WPF.Dialogs;
using RCDragManagerProd.WPF.ViewModels;
using RCDragManagerProd.WPF.Views;

namespace RCDragManagerProd.WPF.Windows
{
    /// <summary>
    /// Hosts one <see cref="RaceConsoleView"/> per class as tabs and coordinates the
    /// round-robin gate, buyback phase, per-class completion stats/popups and the
    /// combined event summary — the WPF port of MultiClassRaceForm.
    /// </summary>
    public partial class MultiClassRaceWindow : Window
    {
        private readonly MultiClassEvent _multiEvent;
        private readonly MultiClassRaceService _service;
        private readonly MultiClassEventRepository _multiRepo;
        private readonly string _connectionString;

        private readonly List<RaceController> _controllers = new List<RaceController>();
        private readonly List<RaceConsoleView> _views = new List<RaceConsoleView>();
        private readonly List<Ellipse> _dots = new List<Ellipse>();
        private readonly HashSet<int> _rrComplete = new HashSet<int>();
        private readonly HashSet<int> _completed = new HashSet<int>();
        private readonly Dictionary<int, RaceController.RaceSummary> _summaries =
            new Dictionary<int, RaceController.RaceSummary>();

        private EventSettingsView _settingsView;
        private int _activeIndex;
        private bool _resumeApplied;

        public MultiClassRaceWindow(MultiClassEvent multiEvent, string connectionString)
        {
            _multiEvent = multiEvent ?? throw new ArgumentNullException(nameof(multiEvent));
            _connectionString = connectionString;
            InitializeComponent();
            WindowSizing.FitToScreen(this);

            _service = new MultiClassRaceService(new DriverRepository(connectionString));
            _multiRepo = new MultiClassEventRepository(connectionString);

            LblTitle.Text = $"Multi-class event: {_multiEvent.EventName}";

            for (int i = 0; i < _multiEvent.ClassSessions.Count; i++)
            {
                var controller = new RaceController(_multiEvent.ClassSessions[i], new WpfStandingsDialogService());
                _controllers.Add(controller);
                SubscribeToController(controller, i);
            }

            BuildTabs();

            Loaded += OnLoadedRestore;
            Closed += OnClosedTeardown;
        }

        private void BuildTabs()
        {
            BuildSettingsTab();

            for (int i = 0; i < _controllers.Count; i++)
            {
                var session = _multiEvent.ClassSessions[i];
                var view = new RaceConsoleView(_controllers[i], _connectionString,
                                               hosted: true, evt: _multiEvent, multiRepo: _multiRepo);
                view.CloseRaceCompleted += (_, __) => Close();
                _views.Add(view);

                var dot = new Ellipse { Width = 8, Height = 8, Margin = new Thickness(0, 0, 8, 0),
                                        VerticalAlignment = VerticalAlignment.Center, Fill = IdleBrush };
                _dots.Add(dot);

                var header = new StackPanel { Orientation = Orientation.Horizontal };
                header.Children.Add(dot);
                header.Children.Add(new TextBlock { Text = session.ClassType ?? $"Class {i + 1}",
                                                    VerticalAlignment = VerticalAlignment.Center });

                Tabs.Items.Add(new TabItem { Header = header, Content = view });
            }

            // Settings sits first positionally, but the operator lands on the first
            // class — opening an event is nearly always about racing it.
            if (_controllers.Count > 0) Tabs.SelectedIndex = FirstClassTabIndex;
            else if (Tabs.Items.Count > 0) Tabs.SelectedIndex = 0;
            UpdateAllTabStates();
        }

        /// <summary>Tab index of class 0 — the settings tab occupies index 0.</summary>
        private const int FirstClassTabIndex = 1;

        // ── Event settings tab (#415) ─────────────────────────────────────────

        private void BuildSettingsTab()
        {
            _settingsView = new EventSettingsView
            {
                ResetClass = ResetClassAt,
                SetBuybacks = SetBuybacksAt,
                RequestRefresh = RefreshSettingsTab
            };

            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock
            {
                Text = "",                     // Segoe MDL2 settings gear
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 12,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            header.Children.Add(new TextBlock
            {
                Text = "Event settings",
                VerticalAlignment = VerticalAlignment.Center
            });

            Tabs.Items.Add(new TabItem { Header = header, Content = _settingsView });
            RefreshSettingsTab();
        }

        private void RefreshSettingsTab()
        {
            if (_settingsView == null) return;

            _settingsView.SetHeader(
                _multiEvent.EventName,
                $"{_multiEvent.EventDate:ddd d MMM yyyy} · " +
                $"{_controllers.Count} {(_controllers.Count == 1 ? "class" : "classes")}");

            var rows = new List<EventSettingsRow>();
            for (int i = 0; i < _controllers.Count; i++)
                rows.Add(EventSettingsRowBuilder.Build(
                    i, _multiEvent.ClassSessions[i], $"Class {i + 1}",
                    complete: _completed.Contains(i),
                    roundRobinComplete: _rrComplete.Contains(i),
                    bracketStarted: _controllers[i].HasBracketStarted));

            _settingsView.SetClasses(rows);
        }

        private string ResetClassAt(int index)
        {
            if (index < 0 || index >= _controllers.Count) return "That class no longer exists.";

            var session = _multiEvent.ClassSessions[index];
            var check = EventSettingsService.CanResetClass(_completed.Contains(index));
            if (!check.IsAllowed) return check.Reason;

            _views[index].ResetClass();

            // The class is back to "not started", so drop the state the window tracks
            // for it or its tab dot and the RR gate would still count it as done.
            _completed.Remove(index);
            _rrComplete.Remove(index);
            _summaries.Remove(index);

            PersistEvent($"reset class '{session.ClassType}'");
            UpdateAllTabStates();
            LblStatus.Text = $"'{session.ClassType}' was reset.";
            return null;
        }

        private string SetBuybacksAt(int index, bool enabled)
        {
            if (index < 0 || index >= _controllers.Count) return "That class no longer exists.";

            var session = _multiEvent.ClassSessions[index];
            bool currentlyOn = EventSettingsService.BuybacksEnabledIn(session.RoundRobinVariant);
            if (currentlyOn == enabled) return null;

            var check = EventSettingsService.CanChangeBuybacks(
                session.RaceType,
                _completed.Contains(index),
                _rrComplete.Contains(index),
                (session.BuybackDrivers?.Count ?? 0) > 0,
                turningOff: !enabled,
                roundsToRun: session.RoundsToRun);
            if (!check.IsAllowed) return check.Reason;

            session.RoundRobinVariant = EventSettingsService.VariantFor(enabled);
            PersistEvent($"buybacks {(enabled ? "on" : "off")} for class '{session.ClassType}'");
            LblStatus.Text = $"Buybacks {(enabled ? "enabled" : "disabled")} for '{session.ClassType}'.";
            return null;
        }

        private void PersistEvent(string what)
        {
            try
            {
                _multiRepo.SaveEvent(_multiEvent);
                Logger.Log($"[WPF][SETTINGS] Saved event after {what}.");
            }
            catch (Exception ex)
            {
                // The change still holds in memory for this sitting; surfacing a modal
                // here would interrupt the operator mid-event.
                Logger.Log($"[WPF][SETTINGS] Save failed after {what}: {ex}");
            }
        }

        private void OnLoadedRestore(object sender, RoutedEventArgs e)
        {
            if (_resumeApplied) return;
            _resumeApplied = true;

            foreach (var c in _controllers)
            {
                try { c.RestoreFromSave(); }
                catch (Exception ex) { Logger.Log($"[WPF][RESUME] {ex}"); }
            }

            // Announce every class to the live site as soon as the event opens, so all
            // classes are visible immediately — classes without a bracket yet show as
            // "waiting" rather than being absent until their first round is generated.
            foreach (var c in _controllers)
            {
                try { c.BroadcastLiveSnapshot("EventStarted"); }
                catch (Exception ex) { Logger.Log($"[WPF][LIVE][INIT] {ex}"); }
            }

            UpdateAllTabStates();
        }

        private void OnClosedTeardown(object sender, EventArgs e)
        {
            foreach (var v in _views) v.Teardown();
        }

        // ── Controller subscriptions (gating + completion) ─────────────────────

        private void SubscribeToController(RaceController controller, int classIndex)
        {
            controller.CanOfferBuybackChanged += enabled =>
            {
                if (enabled) Dispatcher.Invoke(CheckAndReleaseRrGate);
            };
            controller.TournamentCompleted += summary =>
                Dispatcher.Invoke(() => OnClassCompleted(classIndex, summary));
            controller.CanAdvanceChanged += _ => Dispatcher.Invoke(UpdateAllTabStates);
            controller.WinnersUpdated += _ => Dispatcher.Invoke(UpdateAllTabStates);
            controller.BracketRedrawn += _ => Dispatcher.Invoke(UpdateAllTabStates);
        }

        // ── Tab switching enforcement ──────────────────────────────────────────

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ignore selection changes bubbling up from inner controls (e.g. DataGrids).
            if (e.OriginalSource != Tabs) return;

            // Free navigation — the operator can switch to any class tab at any time.
            // Index 0 is the settings tab, so class indices are offset by one.
            _activeIndex = Tabs.SelectedIndex - FirstClassTabIndex;
            LblStatus.Text = "";

            // Class state moves while the operator is racing, so rebuild the settings
            // rows each time they come back to the tab.
            if (Tabs.SelectedIndex == 0) RefreshSettingsTab();

            UpdateAllTabStates();
        }

        // ── RR gate ────────────────────────────────────────────────────────────

        private void CheckAndReleaseRrGate()
        {
            _rrComplete.Clear();
            for (int i = 0; i < _controllers.Count; i++)
                if (_controllers[i].IsRrComplete())
                    _rrComplete.Add(i);

            UpdateAllTabStates();
            Logger.Log($"[WPF][MultiClass] RR gate: {_rrComplete.Count}/{_controllers.Count} complete");

            if (_controllers.Count > 0 && _rrComplete.Count == _controllers.Count)
                Logger.Log("[WPF][MultiClass] All classes completed Round Robin; buybacks are available.");
        }

        // ── Completion ─────────────────────────────────────────────────────────

        private void OnClassCompleted(int classIndex, RaceController.RaceSummary summary)
        {
            _summaries[classIndex] = summary;

            try { _service.RecordClassCompletion(summary); }
            catch (Exception ex) { Logger.Log($"[WPF][MultiClass][STATS] {ex}"); }

            var className = _multiEvent.ClassSessions[classIndex].ClassType;
            Logger.Log($"[RESULT][CLASS] '{className}' complete — winner={summary.Winner?.Name ?? "N/A"}, runner-up={summary.RunnerUp?.Name ?? "N/A"}");
            new ClassCompletionWindow(_multiEvent.ClassSessions[classIndex]) { Owner = this }.ShowDialog();

            _completed.Add(classIndex);
            UpdateAllTabStates();

            if (_completed.Count == _controllers.Count)
            {
                Logger.Log($"[RESULT][EVENT] '{_multiEvent.EventName}' complete — all {_controllers.Count} classes finished");
                ClearFromLiveSite();
                ShowCombinedSummary();
            }
        }

        /// <summary>Every class has finished, so the event is over. Take it off the
        /// live site now instead of leaving it up until the server's two-hour
        /// expiry, where it sits above the event people are actually racing.</summary>
        private void ClearFromLiveSite()
        {
            try
            {
                _ = new LiveApiClient().ResetAsync(string.Empty, null, _multiEvent.EventName);
                Logger.Log($"[LIVE][RESET] event '{_multiEvent.EventName}' finished — cleared from live site");
            }
            catch (Exception ex)
            {
                Logger.Log($"[LIVE][RESET] failed to clear '{_multiEvent.EventName}': {ex.Message}");
            }
        }

        /// <summary>
        /// The end-of-event board, once every class has finished. A single-class event
        /// skips it — ClassCompletionWindow has just shown that same champion, and
        /// stacking a second popup on top of it was pure noise.
        /// </summary>
        private void ShowCombinedSummary()
        {
            if (_controllers.Count <= 1)
            {
                Logger.Log("[WPF][MultiClass] Single-class event — class completion board already shown.");
                return;
            }

            new EventCompletionWindow(_multiEvent) { Owner = this }.ShowDialog();
        }

        // ── Tab state colouring ────────────────────────────────────────────────

        private void UpdateAllTabStates()
        {
            int nextActive = -1;
            for (int i = 0; i < _controllers.Count; i++)
            {
                if (_completed.Contains(i) || _rrComplete.Contains(i)) continue;
                if (_controllers[i].HasBracketStarted && _controllers[i].HasPendingMatchesInCurrentRound())
                {
                    nextActive = i;
                    break;
                }
            }

            for (int i = 0; i < _dots.Count; i++)
            {
                Brush fill;
                if (_completed.Contains(i)) fill = CompletedBrush;
                else if (_rrComplete.Contains(i)) fill = RrCompleteBrush;
                else if (i == nextActive) fill = NextActiveBrush;
                else if (_controllers[i].HasBracketStarted && !_controllers[i].HasPendingMatchesInCurrentRound())
                    fill = StartedBrush;
                else fill = IdleBrush;
                _dots[i].Fill = fill;
            }
        }

        private Brush Res(string key) => (Brush)FindResource(key);
        private Brush CompletedBrush => Res("Brush.TextHint");
        private Brush RrCompleteBrush => Res("Brush.Primary");
        private Brush NextActiveBrush => Res("Brush.Success");
        private Brush StartedBrush => Res("Brush.Info");
        private Brush IdleBrush => Res("Brush.TextGhost");

        // ── Title bar ─────────────────────────────────────────────────────────

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
