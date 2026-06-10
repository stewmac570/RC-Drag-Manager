using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.UI.Forms
{
    public partial class LoadSessionForm : Form
    {
        private readonly LoadSessionService _service;
        private readonly string _connectionString;

        public LoadSessionForm(string connectionString)
            : this(
                new LoadSessionService(
                    new RaceSessionRepository(connectionString),
                    new MultiClassEventRepository(connectionString)),
                connectionString)
        {
        }

        internal LoadSessionForm(LoadSessionService service, string connectionString)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            Logger.Log("[UI][LoadSession] ctor");
            _service = service;
            _connectionString = connectionString;

            InitializeComponent();
            ConfigureListView();
            ConfigureMultiClassListView();

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
                var sessions = _service.ListSessions();

                lvSessions.BeginUpdate();
                lvSessions.Items.Clear();

                foreach (var s in sessions)
                {
                    var item = new ListViewItem(s.EventName);
                    item.SubItems.Add(s.EventDate.ToString("yyyy-MM-dd HH:mm"));
                    item.SubItems.Add(s.ClassType);
                    item.SubItems.Add(s.RaceType);
                    item.Tag = s.Id;
                    lvSessions.Items.Add(item);
                }

                lvSessions.EndUpdate();
                Logger.Log($"[UI][LoadSession] Loaded {sessions.Count} single-class sessions");
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
                var events = _service.ListMultiClassEvents();

                lvMultiClass.BeginUpdate();
                lvMultiClass.Items.Clear();

                foreach (var evt in events)
                {
                    var item = new ListViewItem(evt.EventName);
                    item.SubItems.Add(evt.EventDate.ToString("yyyy-MM-dd"));
                    item.SubItems.Add(evt.ClassCount.ToString());
                    item.Tag = evt.Id;
                    lvMultiClass.Items.Add(item);
                }

                lvMultiClass.EndUpdate();
                Logger.Log($"[UI][LoadSession] Loaded {events.Count} multi-class events");
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

            // Single-class load
            if (lvSessions.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a session to load.", "No Session Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = (int)lvSessions.SelectedItems[0].Tag;
            Logger.Log($"[UI][LoadSession] Loading session id={selectedId}");

            try
            {
                var result = _service.LoadSingleClassSession(selectedId);
                if (!result.Success)
                {
                    MessageBox.Show(result.ErrorMessage, "Load Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                OpenEventAndClose(result.Event);
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
            if (lvMultiClass.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select an event to load.", "No Event Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = (int)lvMultiClass.SelectedItems[0].Tag;
            Logger.Log($"[UI][LoadSession] Loading multi-class event id={selectedId}");

            try
            {
                var result = _service.LoadMultiClassEvent(selectedId);
                if (!result.Success)
                {
                    MessageBox.Show(result.ErrorMessage, "Load Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                OpenEventAndClose(result.Event);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][LoadSession][ERROR] Multi-class load failed: {ex}");
                MessageBox.Show("Failed to load event. Check the log for details.", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenEventAndClose(RCDragManagerProd.Domain.MultiClassEvent evt)
        {
            var form = new MultiClassRaceForm(evt, _connectionString);
            form.Show();
            Close();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex == 1)
            {
                DeleteSelectedMultiClassEvent();
                return;
            }

            // Single-class delete
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

            try
            {
                _service.DeleteSession(selectedId);
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

            try
            {
                _service.DeleteMultiClassEvent(selectedId);
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
