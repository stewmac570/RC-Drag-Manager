using System;
using System.Windows.Forms;
using RCDragManagerProd.Controllers;


namespace RCDragManagerProd
{
    public partial class LandingForm : Form
    {
        private DriverRepository repository;
        private RaceSessionRepository sessionRepository;
        private string dbPath = "race_data.db";

        public LandingForm()
        {
            InitializeComponent();

            // Initialize repositories ONCE for entire app
            repository = new DriverRepository(dbPath);
            sessionRepository = new RaceSessionRepository(dbPath);
        }

        private void btnNewEvent_Click(object sender, EventArgs e)
        {
            // Quick mode (legacy, direct to empty Form1)
            var controller = new RaceController(new RaceSession());   // empty quick session
            Form1 mainForm = new Form1(controller);
            mainForm.Show();

        }

        private void btnCreateSession_Click(object sender, EventArgs e)
        {
            SessionSetupForm sessionForm = new SessionSetupForm(repository);
            if (sessionForm.ShowDialog() == DialogResult.OK)
            {
                var controller = new RaceController(sessionForm.RaceSessionResult);   // or “loaded”
                Form1 mainForm = new Form1(controller);

                mainForm.Show();
            }
        }

        private void btnLoadEvent_Click(object sender, EventArgs e)
        {
            LoadSessionForm loadForm = new LoadSessionForm("race_data.db");
            if (loadForm.ShowDialog() == DialogResult.OK)
            {
                RaceSession loaded = loadForm.LoadedSession;
                var controller = new RaceController(loaded);
                Form1 mainForm = new Form1(controller);


                mainForm.Show();
            }
        }


        private void btnDriverLists_Click(object sender, EventArgs e)
        {
            DriverManagerForm driverManager = new DriverManagerForm();
            driverManager.ShowDialog();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Settings feature not implemented yet.");
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
