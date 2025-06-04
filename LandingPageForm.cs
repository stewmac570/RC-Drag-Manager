using System;
using System.Windows.Forms;

namespace RCDragManager
{
    public partial class LandingForm : Form
    {
        private DriverRepository repository;

        public LandingForm()
        {
            InitializeComponent();

            // Initialize repository ONCE here for entire app
            string dbPath = "race_data.db";
            repository = new DriverRepository(dbPath);
        }

        private void btnNewEvent_Click(object sender, EventArgs e)
        {
            // Quick mode (legacy, direct to empty Form1)
            Form1 mainForm = new Form1(null);  // Pass null for legacy mode
            mainForm.Show();
        }

        private void btnCreateSession_Click(object sender, EventArgs e)
        {
            SessionSetupForm sessionForm = new SessionSetupForm(repository);
            if (sessionForm.ShowDialog() == DialogResult.OK)
            {
                // Session built — pass RaceSession directly into Form1
                Form1 mainForm = new Form1(sessionForm.RaceSessionResult);
                mainForm.Show();
            }
        }

        private void btnLoadEvent_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Load Event feature not implemented yet.");
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
