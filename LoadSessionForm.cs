using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RCDragManagerProd
{
    public partial class LoadSessionForm : Form
    {
        private RaceSessionRepository sessionRepository;
        private List<RaceSessionSummary> sessions;

        public RaceSession LoadedSession { get; private set; }

        public LoadSessionForm(string dbPath)
        {
            InitializeComponent();
            sessionRepository = new RaceSessionRepository(dbPath);
            LoadSessions();
        }

        private void LoadSessions()
        {
            // ✅ FULL COLUMN REBUILD
            lvSessions.Columns.Clear();
            lvSessions.Columns.Add("Event", 300);
            lvSessions.Columns.Add("Date", 150);
            lvSessions.Columns.Add("Class", 150);
            lvSessions.Columns.Add("Type", 150);

            sessions = sessionRepository.GetAllSessions();
            lvSessions.Items.Clear();

            foreach (var session in sessions)
            {
                var item = new ListViewItem(session.EventName);
                item.SubItems.Add(session.EventDate.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(session.ClassType);
                item.SubItems.Add(session.RaceType);
                item.Tag = session.Id;
                lvSessions.Items.Add(item);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (lvSessions.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a session to load.", "No Session Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = (int)lvSessions.SelectedItems[0].Tag;
            LoadedSession = sessionRepository.LoadSession(selectedId);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (lvSessions.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a session to delete.", "No Session Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show("Are you sure you want to permanently delete this session?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                int selectedId = (int)lvSessions.SelectedItems[0].Tag;
                sessionRepository.DeleteSession(selectedId);
                LoadSessions();
            }
        }
    }
}
