using System;
using System.Windows.Forms;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.UI.Forms
{
    public partial class LandingForm : Form
    {
        private readonly string _connStr;

        // ✅ primary runtime ctor
        public LandingForm(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            _connStr = connectionString;

            InitializeComponent();

            Logger.Log("[UI][Landing] initialised");
        }

        // ✅ keep designer convenience
        public LandingForm() : this(Program.ConnectionString) { }

        private void btnLoadEvent_Click(object sender, EventArgs e)
        {
            using (var load = new LoadSessionForm(_connStr))
            {
                load.ShowDialog();
            }
        }

        private void btnDriverLists_Click(object sender, EventArgs e)
        {
            // If DriverManagerForm has an overload that takes conn string, use it.
            // Otherwise it can read Program.ConnectionString internally.
            var dlg = new DriverManagerForm();
            dlg.ShowDialog();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var dlg = new SettingsForm())
                dlg.ShowDialog(this);
        }


        private void btnCreateRaceSession_Click(object sender, EventArgs e)
        {
            Logger.Log("[MULTI] Opening Create Race Session setup…");
            var setup = new MultiClassSetupForm(_connStr);
            if (setup.ShowDialog() == DialogResult.OK)
            {
                var multiEvent = setup.MultiClassEventResult;
                Logger.Log($"[MULTI] Setup complete: '{multiEvent.EventName}', {multiEvent.ClassSessions.Count} class(es)");
                var form = new MultiClassRaceForm(multiEvent, _connStr);
                form.Show();
            }
            else
            {
                Logger.Log("[MULTI] Create Race Session setup cancelled.");
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
