using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RCDragManager
{
    public partial class Form1 : Form
    {
        private List<Driver> drivers = new List<Driver>();
        private MatchEngine engine = new MatchEngine();
        private RaceSession currentSession;

        public Form1(RaceSession session)
        {
            InitializeComponent();
            currentSession = session;

            if (currentSession != null)
            {
                LoadDriversFromSession();
            }
        }

        private void LoadDriversFromSession()
        {
            drivers.Clear();
            foreach (var entry in currentSession.DriverEntries)
            {
                drivers.Add(new Driver
                {
                    Id = entry.DriverID,
                    Name = entry.DriverName,
                    QualTime = entry.QualifyingTime ?? 0.0  // Default 0 if not entered yet
                });
            }
            UpdateDriverList();
        }

        private void btnAddDriver_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            if (!double.TryParse(txtTime.Text.Trim(), out double qualTime) || name == "")
            {
                MessageBox.Show("Enter a valid name and qualifying time.");
                return;
            }

            drivers.Add(new Driver { Name = name, QualTime = qualTime });
            UpdateDriverList();
            txtName.Text = "";
            txtTime.Text = "";
        }

        private void btnEditDriver_Click(object sender, EventArgs e)
        {
            if (lvDrivers.SelectedItems.Count > 0)
            {
                string selectedName = lvDrivers.SelectedItems[0].Text;
                var driver = drivers.FirstOrDefault(d => d.Name == selectedName);
                if (driver != null)
                {
                    txtName.Text = driver.Name;
                    txtTime.Text = driver.QualTime.ToString();
                    drivers.Remove(driver);
                    UpdateDriverList();
                }
            }
        }

        private void UpdateDriverList()
        {
            lvDrivers.Items.Clear();
            foreach (var d in drivers.OrderBy(d => d.QualTime))
            {
                var item = new ListViewItem(d.Name);
                item.SubItems.Add(d.QualTime.HasValue ? d.QualTime.Value.ToString("0.00") : "");
                lvDrivers.Items.Add(item);
            }
        }

        private void btnGenerateBracket_Click(object sender, EventArgs e)
        {
            if (drivers.Count < 2)
            {
                MessageBox.Show("Not enough drivers to generate bracket.");
                return;
            }

            engine.Initialize(drivers);
            UpdateFullPairingList();
            UpdateNextUp();
        }

        private void btnWinner1_Click(object sender, EventArgs e)
        {
            var match = engine.GetCurrentRoundMatches().FirstOrDefault(m => !engine.Results.IsMatchResolved(m.MatchId));
            if (match != null)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                engine.SetWinner(match.MatchId, driver1);
                UpdateFullPairingList();
                UpdateNextUp();
            }
        }

        private void btnWinner2_Click(object sender, EventArgs e)
        {
            var match = engine.GetCurrentRoundMatches().FirstOrDefault(m => !engine.Results.IsMatchResolved(m.MatchId));
            if (match != null)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                engine.SetWinner(match.MatchId, driver2);
                UpdateFullPairingList();
                UpdateNextUp();
            }
        }

        private void btnNextRound_Click(object sender, EventArgs e)
        {
            if (!engine.IsCurrentRoundComplete())
            {
                MessageBox.Show("Current round not complete yet.");
                return;
            }

            engine.AdvanceToNextRound();
            UpdateFullPairingList();
            UpdateNextUp();
        }

        private void UpdateFullPairingList()
        {
            lstFullPairings.Items.Clear();

            var matches = engine.GetBracketMatches();
            foreach (var match in matches)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                lstFullPairings.Items.Add($"{match.RoundLabel}: {driver1.Name} vs {driver2.Name}");
            }
        }

        private void UpdateNextUp()
        {
            var match = engine.GetCurrentRoundMatches().FirstOrDefault(m => !engine.Results.IsMatchResolved(m.MatchId));
            if (match != null)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                lblNext.Text = $"{driver1.Name} vs {driver2.Name}";
            }
            else
            {
                lblNext.Text = "Waiting...";
            }
        }

        private void btnEditResult_Click(object sender, EventArgs e)
        {
            var match = engine.GetCurrentRoundMatches().FirstOrDefault(m => !engine.Results.IsMatchResolved(m.MatchId));
            if (match != null)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                var editDialog = new EditWinnerDialog(driver1, driver2);
                if (editDialog.ShowDialog() == DialogResult.OK)
                {
                    engine.SetWinner(match.MatchId, editDialog.SelectedWinner);
                    UpdateFullPairingList();
                    UpdateNextUp();
                }
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            drivers.Clear();
            engine = new MatchEngine();
            lvDrivers.Items.Clear();
            lstFullPairings.Items.Clear();
            lblNext.Text = "";
        }
        this.Text = "RC Drag Manager - Randomizer Test Branch";

    }
}
