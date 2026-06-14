using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.WPF.Dialogs;
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

            if (Tabs.Items.Count > 0) Tabs.SelectedIndex = 0;
            UpdateAllTabStates();
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
            _activeIndex = Tabs.SelectedIndex;
            LblStatus.Text = "";
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
                MessageDialog.Info(this,
                    "All classes have completed Round Robin.\nThe buyback phase is now open for every class.\n\n" +
                    "Switch to each class tab to run buybacks.",
                    "Buyback phase ready — all classes");
        }

        // ── Completion ─────────────────────────────────────────────────────────

        private void OnClassCompleted(int classIndex, RaceController.RaceSummary summary)
        {
            _summaries[classIndex] = summary;

            try { _service.RecordClassCompletion(summary); }
            catch (Exception ex) { Logger.Log($"[WPF][MultiClass][STATS] {ex}"); }

            var className = _multiEvent.ClassSessions[classIndex].ClassType;
            Logger.Log($"[RESULT][CLASS] '{className}' complete — winner={summary.Winner?.Name ?? "N/A"}, runner-up={summary.RunnerUp?.Name ?? "N/A"}");
            MessageDialog.Info(this,
                $"Class: {className}\nWinner: {summary.Winner?.Name ?? "N/A"}\nRunner-up: {summary.RunnerUp?.Name ?? "N/A"}",
                "Class complete");

            _completed.Add(classIndex);
            UpdateAllTabStates();

            if (_completed.Count == _controllers.Count)
            {
                Logger.Log($"[RESULT][EVENT] '{_multiEvent.EventName}' complete — all {_controllers.Count} classes finished");
                ShowCombinedSummary();
            }
        }

        private void ShowCombinedSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════");
            sb.AppendLine($"  {_multiEvent.EventName} — {_multiEvent.EventDate:yyyy-MM-dd}");
            sb.AppendLine("  FINAL RESULTS");
            sb.AppendLine("═══════════════════════════════");
            sb.AppendLine();

            for (int i = 0; i < _controllers.Count; i++)
            {
                var className = _multiEvent.ClassSessions[i].ClassType;
                sb.AppendLine($"  {className}");
                sb.AppendLine($"  {new string('─', Math.Max(1, className?.Length ?? 1))}");
                if (_summaries.TryGetValue(i, out var s))
                {
                    sb.AppendLine($"  Champion:   {s.Winner?.Name ?? "N/A"}");
                    sb.AppendLine($"  Runner-up:  {s.RunnerUp?.Name ?? "N/A"}");
                }
                else sb.AppendLine("  (no result recorded)");
                sb.AppendLine();
            }
            sb.AppendLine("═══════════════════════════════");

            new TextSummaryWindow("Event complete — final results", sb.ToString()) { Owner = this }.ShowDialog();
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
        private static readonly Brush StartedBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x6E, 0xA5));
        private static readonly Brush IdleBrush = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40));

        // ── Title bar ─────────────────────────────────────────────────────────

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
