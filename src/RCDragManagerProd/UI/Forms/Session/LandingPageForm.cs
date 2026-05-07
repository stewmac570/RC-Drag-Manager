using System;
using System.Windows.Forms;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    public partial class LandingForm : Form
    {
        private readonly string _connStr;
        private DriverRepository _driverRepo;
        private RaceSessionRepository _sessionRepo;

        // ✅ primary runtime ctor
        public LandingForm(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            _connStr = connectionString;

            InitializeComponent();

            // Init repos once
            _driverRepo = new DriverRepository(Program.ConnectionString);
            _sessionRepo = new RaceSessionRepository(Program.ConnectionString);


            Logger.Log("[UI][Landing] repositories wired");
        }

        // ✅ keep designer convenience
        public LandingForm() : this(Program.ConnectionString) { }

        private void btnCreateSession_Click(object sender, EventArgs e)
        {
            Logger.Log("[CREATE] Opening Create Session setup dialog…");
            var setup = new SessionSetupForm(_driverRepo);
            if (setup.ShowDialog() == DialogResult.OK)
            {
                var rs = setup.RaceSessionResult;
                int count = rs?.DriverEntries?.Count ?? 0;
                Logger.Log($"[CREATE] Session created: '{rs?.EventName ?? "(unnamed)"}' | raceType='{rs?.RaceType ?? "n/a"}' | entries={count}");

                var controller = new RaceController(rs);
                var mainForm = new Form1(controller);
                mainForm.Show();
            }
            else
            {
                Logger.Log("[CREATE] Session creation cancelled.");
            }
        }

        private void btnLoadEvent_Click(object sender, EventArgs e)
        {
            using (var load = new LoadSessionForm(_connStr))
            {
                if (load.ShowDialog() == DialogResult.OK)
                {
                    var loaded = load.LoadedSession;
                    var controller = new RaceController(loaded);
                    var mainForm = new Form1(controller);
                    mainForm.Show();
                }
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


        private void btnNewMultiClassEvent_Click(object sender, EventArgs e)
        {
            Logger.Log("[MULTI] Opening Multi-Class Event setup…");
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
                Logger.Log("[MULTI] Multi-class setup cancelled.");
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
