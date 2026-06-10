// UI/Forms/Main/MultiClassRaceForm.cs
// Phase 5 — Multi-Class Race Console
//
// Architecture: Option B (Hosted Form1 instances).
// Each class tab embeds a Form1 instance configured with IsHostedMode = true.
// MultiClassRaceForm handles the LB gate, stats writing, and combined summary.
// Form1's buyback MessageBox and TournamentCompleted stats/popup are suppressed
// in hosted mode so MultiClassRaceForm can coordinate across all classes.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    public partial class MultiClassRaceForm : Form
    {
        private readonly MultiClassEvent _multiEvent;
        private readonly MultiClassRaceService _service;
        private readonly MultiClassEventRepository _multiClassRepo;
        private readonly List<RaceController> _controllers;
        private readonly List<Form1> _classRaceForms;
        private readonly HashSet<int> _rrCompleteClassIndexes = new HashSet<int>();
        private readonly HashSet<int> _completedClassIndexes = new HashSet<int>();
        private readonly Dictionary<int, RaceController.RaceSummary> _raceSummaries =
            new Dictionary<int, RaceController.RaceSummary>();
        private readonly Dictionary<int, Color> _tabColors = new Dictionary<int, Color>();

        // ── Construction ──────────────────────────────────────────────────────

        public MultiClassRaceForm(MultiClassEvent multiEvent, string connectionString)
            : this(multiEvent,
                   new MultiClassRaceService(new DriverRepository(connectionString)),
                   new MultiClassEventRepository(connectionString))
        {
        }

        internal MultiClassRaceForm(MultiClassEvent multiEvent, MultiClassRaceService service,
                                    MultiClassEventRepository multiClassRepo)
        {
            InitializeComponent();

            _multiEvent = multiEvent ?? throw new ArgumentNullException(nameof(multiEvent));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _multiClassRepo = multiClassRepo;
            _controllers = new List<RaceController>();
            _classRaceForms = new List<Form1>();

            Text = $"Multi-Class Event: {multiEvent.EventName}";

            foreach (var session in multiEvent.ClassSessions)
            {
                var controller = new RaceController(session);
                _controllers.Add(controller);
                SubscribeToController(controller, _controllers.Count - 1);
            }

            tabControl.DrawItem += TabControl_DrawItem;

            BuildTabs();
        }

        private bool _resumeApplied;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Replay any saved bracket state once the tabs (and their Form1 event
            // subscriptions) exist, so an interrupted event resumes where it left off.
            // No-ops for fresh events (no resume snapshot).
            if (_resumeApplied) return;
            _resumeApplied = true;

            foreach (var controller in _controllers)
            {
                try { controller.RestoreFromSave(); }
                catch (Exception ex) { Logger.Log($"[RESUME][ERROR] {ex}"); }
            }

            UpdateAllTabStates();
        }

        private void SubscribeToController(RaceController controller, int classIndex)
        {
            controller.CanOfferBuybackChanged += (enabled) =>
            {
                if (enabled)
                    BeginInvokeIfNeeded(CheckAndReleaseRrGate);
            };

            controller.TournamentCompleted += (summary) =>
                BeginInvokeIfNeeded(() => OnClassTournamentCompleted(classIndex, summary));

            controller.CanAdvanceChanged += (_) => BeginInvokeIfNeeded(UpdateAllTabStates);
            controller.WinnersUpdated += (_) => BeginInvokeIfNeeded(UpdateAllTabStates);
            controller.BracketRedrawn += (_) => BeginInvokeIfNeeded(UpdateAllTabStates);
        }

        private void BeginInvokeIfNeeded(Action action)
        {
            if (!IsHandleCreated) return;
            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }

        private void BuildTabs()
        {
            tabControl.TabPages.Clear();
            _classRaceForms.Clear();

            for (int i = 0; i < _controllers.Count; i++)
            {
                var session = _multiEvent.ClassSessions[i];
                var tab = new TabPage(session.ClassType);

                var form1 = new Form1(_controllers[i]);
                form1.IsHostedMode = true;
                form1._multiClassEvent = _multiEvent;
                form1._multiClassEventRepo = _multiClassRepo;
                form1.TopLevel = false;
                form1.FormBorderStyle = FormBorderStyle.None;
                form1.Dock = DockStyle.Fill;

                tab.Controls.Add(form1);
                form1.HostedSaveAndCloseCompleted += OnHostedSaveAndCloseCompleted;
                form1.Show();

                tabControl.TabPages.Add(tab);
                _classRaceForms.Add(form1);
            }

            UpdateAllTabStates();
        }

        private void OnHostedSaveAndCloseCompleted(object sender, EventArgs e)
        {
            this.Close();
        }

        // ── Tab state ─────────────────────────────────────────────────────────

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            _tabColors.TryGetValue(e.Index, out Color backColor);
            if (backColor == Color.Empty) backColor = SystemColors.Control;

            using (var brush = new SolidBrush(backColor))
                e.Graphics.FillRectangle(brush, e.Bounds);

            var tab = tabControl.TabPages[e.Index];
            TextRenderer.DrawText(
                e.Graphics,
                tab.Text,
                e.Font,
                e.Bounds,
                Color.Black,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void UpdateAllTabStates()
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
                UpdateTabState(i);
        }

        private void UpdateTabState(int classIndex)
        {
            if (classIndex < 0 || classIndex >= tabControl.TabPages.Count) return;

            var controller = _controllers[classIndex];

            int nextActiveIndex = -1;
            for (int i = 0; i < _controllers.Count; i++)
            {
                if (_completedClassIndexes.Contains(i)) continue;
                if (_rrCompleteClassIndexes.Contains(i)) continue;
                if (_controllers[i].HasBracketStarted && _controllers[i].HasPendingMatchesInCurrentRound())
                {
                    nextActiveIndex = i;
                    break;
                }
            }

            Color color;

            if (_completedClassIndexes.Contains(classIndex))
                color = Color.LightGray;
            else if (_rrCompleteClassIndexes.Contains(classIndex))
                color = Color.Orange;
            else if (classIndex == nextActiveIndex)
                color = Color.LightGreen;
            else if (controller.HasBracketStarted && !controller.HasPendingMatchesInCurrentRound())
                color = Color.SteelBlue;
            else
                color = SystemColors.Control;

            _tabColors[classIndex] = color;
            tabControl.Invalidate();
        }

        // ── Tab switching enforcement ──────────────────────────────────────────

        private void tabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPageIndex == tabControl.SelectedIndex) return;

            var activeController = _controllers[tabControl.SelectedIndex];
            if (activeController.HasBracketStarted && activeController.HasPendingMatchesInCurrentRound())
            {
                e.Cancel = true;
                lblStatus.Text = "Complete all matches before switching class.";
            }
            else
            {
                lblStatus.Text = string.Empty;
            }
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblStatus.Text = string.Empty;
            UpdateAllTabStates();
        }

        // ── LB gate ───────────────────────────────────────────────────────────

        private void CheckAndReleaseRrGate()
        {
            _rrCompleteClassIndexes.Clear();
            for (int i = 0; i < _controllers.Count; i++)
            {
                if (_controllers[i].IsRrComplete())
                    _rrCompleteClassIndexes.Add(i);
            }

            UpdateAllTabStates();

            Logger.Log($"[MultiClass] RR gate check: {_rrCompleteClassIndexes.Count}/{_controllers.Count} complete");

            if (_controllers.Count > 0 && _rrCompleteClassIndexes.Count == _controllers.Count)
            {
                Logger.Log("[MultiClass] All classes completed RR — releasing LB gate");
                MessageBox.Show(
                    "All classes have completed Round Robin.\n" +
                    "The Buyback phase is now open for all classes.\n\n" +
                    "Switch to each class tab to run buybacks.",
                    "Buyback Phase Ready — All Classes",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        // ── Tournament completion ──────────────────────────────────────────────

        private void OnClassTournamentCompleted(int classIndex, RaceController.RaceSummary summary)
        {
            _raceSummaries[classIndex] = summary;

            // Write win/loss stats
            try
            {
                _service.RecordClassCompletion(summary);
            }
            catch (Exception ex)
            {
                Logger.Log($"[MultiClass][STATS][ERROR] {ex}");
            }

            // Per-class completion popup
            var className = _multiEvent.ClassSessions[classIndex].ClassType;
            var winnerName = summary.Winner?.Name ?? "N/A";
            var runnerUpName = summary.RunnerUp?.Name ?? "N/A";
            Logger.Log($"[MultiClass] Class '{className}' complete — winner={winnerName}, runner-up={runnerUpName}");

            MessageBox.Show(
                $"Class: {className}\nWinner: {winnerName}\nRunner-up: {runnerUpName}",
                "Class Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            _completedClassIndexes.Add(classIndex);
            UpdateTabState(classIndex);

            if (_completedClassIndexes.Count == _controllers.Count)
                ShowCombinedEventSummary();
        }

        private void ShowCombinedEventSummary()
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
                sb.AppendLine($"  {new string('─', className.Length)}");

                if (_raceSummaries.TryGetValue(i, out var s))
                {
                    sb.AppendLine($"  Champion:   {s.Winner?.Name ?? "N/A"}");
                    sb.AppendLine($"  Runner-Up:  {s.RunnerUp?.Name ?? "N/A"}");
                }
                else
                {
                    sb.AppendLine("  (no result recorded)");
                }

                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════");

            Logger.Log("[MultiClass] Showing combined event summary");
            ScrollableTextDialog.Show("Event Complete — Final Results", sb.ToString());
        }

    }
}
