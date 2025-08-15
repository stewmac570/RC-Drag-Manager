using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RCDragManagerProd
{
    public partial class LoadSessionForm : Form
    {
        private readonly RaceSessionRepository _sessionRepository;
        private List<RaceSessionSummary> _sessions;

        public RaceSession LoadedSession { get; private set; }

        public LoadSessionForm(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            Logger.Log("[UI][LoadSession] ctor");
            InitializeComponent();
            ConfigureListView();

            _sessionRepository = new RaceSessionRepository(connectionString);
            Logger.Log("[UI][LoadSession] Repository ready");

            LoadSessions();
        }

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
                Logger.Log("[UI][LoadSession] Loading sessions…");

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
                Logger.Log($"[UI][LoadSession] Loaded {_sessions.Count} sessions");
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][LoadSession][ERROR] Failed to load sessions: {ex}");
                MessageBox.Show("Failed to load sessions. Check the log for details.", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
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

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][LoadSession][ERROR] Load click failed: {ex}");
                MessageBox.Show("Failed to load session. Check the log for details.", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Logger.Log("[UI][LoadSession] Cancel");
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
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

        private void lvSessions_DoubleClick(object sender, EventArgs e)
        {
            if (lvSessions.SelectedItems.Count > 0)
                btnLoad_Click(sender, e);
        }
    }
}
