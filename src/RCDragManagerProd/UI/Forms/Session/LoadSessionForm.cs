using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.UI.Forms
{
    public partial class LoadSessionForm : Form
    {
        private readonly string _connectionString;
        private readonly RaceSessionRepository _sessionRepository;
        private readonly MultiClassEventRepository _multiClassRepo;

        private List<RaceSessionSummary> _sessions;
        private List<MultiClassEventSummary> _multiEvents;

        /// <summary>Set when a single-class session is loaded (DialogResult.OK).</summary>
        public RaceSession LoadedSession { get; private set; }

        public LoadSessionForm(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            Logger.Log("[UI][LoadSession] ctor");
            _connectionString = connectionString;

            InitializeComponent();
            ConfigureListView();
            ConfigureMultiClassListView();

            _sessionRepository = new RaceSessionRepository(connectionString);
            _multiClassRepo = new MultiClassEventRepository(connectionString);
            Logger.Log("[UI][LoadSession] Repositories ready");

            LoadSessions();
            LoadMultiClassEvents();
        }

        // ── Single-class list ─────────────────────────────────────────────────

        private void ConfigureListView()
        {
            if (lvSessions == null) return;

            lvSessions.BeginUpdate();
            lvSessions.View = View.Details;
            lvSessions.FullRowSelect = true;
            lvSessions.MultiSelect = false;
            lvSessions.HideSelection = false;
            lvSessions.Columns.Clear();
            lvSessions.Columns.Add("Event", 300);
            lvSessions.Columns.Add("Date", 150);
            lvSessions.Columns.Add("Class", 150);
            lvSessions.Columns.Add("Type", 150);
            lvSessions.EndUpdate();
        }

        private void LoadSessions()
        {
            try
            {
                Logger.Log("[UI][LoadSession] Loading single-class sessions…");
                _sessions = _sessionRepository.GetAllSessions() ?? new List<RaceSessionSummary>();

                lvSessions.BeginUpdate();
                lvSessions.Items.Clear();

                foreach (var s in _sessions)
                {
                    var item = new ListViewItem(s.EventName);
                    item.SubItems.Add(s.EventDate.ToString("yyyy-MM-dd HH:mm"));
                    item.SubItems.Add(s.ClassType);
                    item.SubItems.Add(s.RaceType);
                    item.Tag = s.Id;
                    lvSessions.Items.Add(item);
                }

                lvSessions.EndUpdate();
                Logger.Log($"[UI][LoadSession] Loaded {_sessions.Count} single-class sessions");
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][LoadSession][ERROR] Failed to load sessions: {ex}");
                MessageBox.Show("Failed to load sessions. Check the log for details.", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Multi-class list ──────────────────────────────────────────────────

        private void ConfigureMultiClassListView()
        {
            if (lvMultiClass == null) return;

            lvMultiClass.BeginUpdate();
            lvMultiClass.View = View.Details;
            lvMultiClass.FullRowSelect = true;
            lvMultiClass.MultiSelect = false;
            lvMultiClass.HideSelection = false;
            lvMultiClass.Columns.Clear();
            lvMultiClass.Columns.Add("Event Name", 340);
            lvMultiClass.Columns.Add("Date", 150);
            lvMultiClass.Columns.Add("Classes", 100);
            lvMultiClass.EndUpdate();
        }

        private void LoadMultiClassEvents()
        {
            try
            {
                Logger.Log("[UI][LoadSession] Loading multi-class events…");
                _multiEvents = _multiClassRepo.GetAllEvents() ?? new List<MultiClassEventSummary>();

                lvMultiClass.BeginUpdate();
                lvMultiClass.Items.Clear();

                foreach (var evt in _multiEvents)
                {
                    var item = new ListViewItem(evt.EventName);
                    item.SubItems.Add(evt.EventDate.ToString("yyyy-MM-dd"));
                    item.SubItems.Add(evt.ClassCount.ToString());
                    item.Tag = evt.Id;
                    lvMultiClass.Items.Add(item);
                }

                lvMultiClass.EndUpdate();
                Logger.Log($"[UI][LoadSession] Loaded {_multiEvents.Count} multi-class events");
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][LoadSession][ERROR] Failed to load multi-class events: {ex}");
            }
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex == 1)
            {
                LoadSelectedMultiClassEvent();
                return;
            }

            // Single-class load — wrap into a one-class MultiClassEvent and route through MultiClassRaceForm
            try
            {
                if (lvSessions.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select a session to load.", "No Session Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int selectedId = (int)lvSessions.SelectedItems[0].Tag;
                Logger.Log($"[UI][LoadSession] Loading session id={selectedId}");

                LoadedSession = _sessionRepository.LoadSession(selectedId);
                if (LoadedSession == null)
                {
                    Logger.Log("[UI][LoadSession][WARN] Repository returned null session");
                    MessageBox.Show("Unable to load the selected session.", "Load Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var wrapped = new MultiClassEvent
                {
                    EventName = LoadedSession.EventName,
                    EventDate = LoadedSession.EventDate != default ? LoadedSession.EventDate : DateTime.Now,
                    ClassSessions = new List<RaceSession> { LoadedSession }
                };
                Logger.Log($"[UI][LoadSession] Wrapping single-class session '{wrapped.EventName}' (raceType='{LoadedSession.RaceType}') into MultiClassRaceForm");

                var form = new MultiClassRaceForm(wrapped, _connectionString);
                form.Show();
                Close();
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][LoadSession][ERROR] Load click failed: {ex}");
                MessageBox.Show("Failed to load session. Check the log for details.", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSelectedMultiClassEvent()
        {
            try
            {
                if (lvMultiClass.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select an event to load.", "No Event Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int selectedId = (int)lvMultiClass.SelectedItems[0].Tag;
                Logger.Log($"[UI][LoadSession] Loading multi-class event id={selectedId}");

                var evt = _multiClassRepo.LoadEvent(selectedId);
                if (evt == null)
                {
                    Logger.Log("[UI][LoadSession][WARN] MultiClassEventRepository returned null");
                    MessageBox.Show("Unable to load the selected event.", "Load Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var form = new MultiClassRaceForm(evt, _connectionString);
                form.Show();
                Close();
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][LoadSession][ERROR] Multi-class load failed: {ex}");
                MessageBox.Show("Failed to load event. Check the log for details.", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex == 1)
            {
                DeleteSelectedMultiClassEvent();
                return;
            }

            // Single-class delete
            try
            {
                if (lvSessions.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select a session to delete.", "No Session Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    "Are you sure you want to permanently delete this session?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes) return;

                int selectedId = (int)lvSessions.SelectedItems[0].Tag;
                Logger.Log($"[UI][LoadSession] Deleting session id={selectedId}");

                _sessionRepository.DeleteSession(selectedId);
                LoadSessions();
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][LoadSession][ERROR] Delete failed: {ex}");
                MessageBox.Show("Failed to delete session. Check the log for details.", "Delete Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteSelectedMultiClassEvent()
        {
            try
            {
                if (lvMultiClass.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select an event to delete.", "No Event Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    "Are you sure you want to permanently delete this multi-class event?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes) return;

                int selectedId = (int)lvMultiClass.SelectedItems[0].Tag;
                Logger.Log($"[UI][LoadSession] Deleting multi-class event id={selectedId}");

                _multiClassRepo.DeleteEvent(selectedId);
                LoadMultiClassEvents();
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][LoadSession][ERROR] Multi-class delete failed: {ex}");
                MessageBox.Show("Failed to delete event. Check the log for details.", "Delete Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Logger.Log("[UI][LoadSession] Cancel");
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void lvSessions_DoubleClick(object sender, EventArgs e)
        {
            if (lvSessions.SelectedItems.Count > 0)
                btnLoad_Click(sender, e);
        }

        private void lvMultiClass_DoubleClick(object sender, EventArgs e)
        {
            if (lvMultiClass.SelectedItems.Count > 0)
                LoadSelectedMultiClassEvent();
        }
    }
}
