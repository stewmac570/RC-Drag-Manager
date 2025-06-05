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
        private string currentRound = "R1";

        public Form1(RaceSession session = null)
        {
            InitializeComponent();
            currentSession = session;

            if (currentSession != null)
            {
                lblEventTitle.Text = $"Event: {currentSession.EventName}";
                InitializeDriversFromSession();
            }
            else
            {
                lblEventTitle.Text = "Quick Session";
            }

            UpdateDriverList();
            UpdateButtonStates();
        }

        private void InitializeDriversFromSession()
        {
            drivers.Clear();

            foreach (var entry in currentSession.DriverEntries)
            {
                Driver driver = new Driver
                {
                    Id = entry.DriverID,
                    Name = entry.DriverName,
                    QualTime = entry.QualifyingTime ?? 0.0
                };
                drivers.Add(driver);
            }
        }

        private void btnAddDriver_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();

            if (!double.TryParse(txtTime.Text.Trim(), out double qualTime) || name == "")
            {
                MessageBox.Show("Enter a valid name and qualifying time.");
                return;
            }

            var existingDriver = drivers.FirstOrDefault(d => d.Name == name);
            if (existingDriver != null)
            {
                existingDriver.QualTime = qualTime;
            }
            else
            {
                drivers.Add(new Driver { Name = name, QualTime = qualTime });
            }

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
                    txtTime.Text = (driver.QualTime ?? 0.0).ToString("0.000");
                }
            }
        }

        private void UpdateDriverList()
        {
            lvDrivers.Items.Clear();
            foreach (var d in drivers.OrderBy(d => d.QualTime))
            {
                var item = new ListViewItem(d.Name);
                item.SubItems.Add((d.QualTime ?? 0.0).ToString("0.000"));
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

            drivers = drivers.OrderBy(d => d.QualTime).ToList();
            for (int i = 0; i < drivers.Count; i++)
            {
                drivers[i].Seed = i + 1;
            }

            engine.Initialize(drivers);
            currentRound = "R1";
            UpdateFullPairingList();
            UpdateNextUp();
            UpdateWinnersList();
            UpdateNextRoundButtonState();

            btnGenerateBracket.Enabled = false;
        }

        private void btnWinner1_Click(object sender, EventArgs e)
        {
            ProcessMatchWinner(true);
        }

        private void btnWinner2_Click(object sender, EventArgs e)
        {
            ProcessMatchWinner(false);
        }

        private void ProcessMatchWinner(bool winner1)
        {
            var matches = engine.GetBracketMatches().Where(m => m.RoundLabel == currentRound).ToList();
            var match = matches.FirstOrDefault(m => !engine.Results.IsMatchResolved(m.MatchId));

            if (match != null)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                engine.SetWinner(match.MatchId, winner1 ? driver1 : driver2);

                UpdateFullPairingList();
                UpdateNextUp();
                UpdateWinnersList();
                UpdateNextRoundButtonState();
            }
        }

        private void btnNextRound_Click(object sender, EventArgs e)
        {
            if (!IsCurrentRoundComplete())
            {
                MessageBox.Show("Current round not complete yet.");
                return;
            }

            currentRound = GetNextRoundLabel(currentRound);
            UpdateFullPairingList();
            UpdateNextUp();
            UpdateWinnersList();
            UpdateNextRoundButtonState();
        }

        private string GetNextRoundLabel(string round)
        {
            switch (round)
            {
                case "R1": return "SF";
                case "SF": return "F";
                default: return "COMPLETE";
            }
        }

        private void UpdateFullPairingList()
        {
            lstFullPairings.Items.Clear();

            var matches = engine.GetBracketMatches().Where(m => m.RoundLabel == currentRound).ToList();
            foreach (var match in matches)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                lstFullPairings.Items.Add($"{match.RoundLabel}: {driver1.Name} vs {driver2.Name}");
            }
        }

        private void UpdateNextUp()
        {
            var matches = engine.GetBracketMatches().Where(m => m.RoundLabel == currentRound).ToList();
            var match = matches.FirstOrDefault(m => !engine.Results.IsMatchResolved(m.MatchId));

            if (match != null)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(match);

                lblNext.Text = $"{driver1.Name} vs {driver2.Name}";

                btnWinner1.Text = driver1.Name;
                btnWinner2.Text = driver2.Name;

                btnWinner1.Enabled = (driver1.Name != "BYE" && driver1.Name != "TBD");
                btnWinner2.Enabled = (driver2.Name != "BYE" && driver2.Name != "TBD");
            }
            else
            {
                lblNext.Text = "Waiting...";
                btnWinner1.Text = "";
                btnWinner2.Text = "";
                btnWinner1.Enabled = false;
                btnWinner2.Enabled = false;
            }
        }

        private void UpdateNextRoundButtonState()
        {
            btnNextRound.Enabled = IsCurrentRoundComplete();
        }

        private bool IsCurrentRoundComplete()
        {
            var matches = engine.GetBracketMatches().Where(m => m.RoundLabel == currentRound).ToList();
            return matches.All(m => engine.Results.IsMatchResolved(m.MatchId));
        }

        private void UpdateWinnersList()
        {
            lstWinners.Items.Clear();

            var allMatches = engine.GetBracketMatches();
            foreach (var match in allMatches)
            {
                if (engine.Results.IsMatchResolved(match.MatchId))
                {
                    var winner = engine.Results.GetWinner(match.MatchId);
                    lstWinners.Items.Add($"{match.RoundLabel}: {winner.Name}");
                }
            }
        }

        private void btnEditResult_Click(object sender, EventArgs e)
        {
            var matches = engine.GetBracketMatches().Where(m => m.RoundLabel == currentRound).ToList();
            var match = matches.FirstOrDefault(m => !engine.Results.IsMatchResolved(m.MatchId));

            if (match != null)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                var editDialog = new EditWinnerDialog(driver1, driver2);
                if (editDialog.ShowDialog() == DialogResult.OK)
                {
                    engine.SetWinner(match.MatchId, editDialog.SelectedWinner);
                    UpdateFullPairingList();
                    UpdateNextUp();
                    UpdateWinnersList();
                    UpdateNextRoundButtonState();
                }
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            engine = new MatchEngine();
            currentRound = "R1";
            lstFullPairings.Items.Clear();
            lstWinners.Items.Clear();
            lblNext.Text = "";

            btnGenerateBracket.Enabled = true;
            btnNextRound.Enabled = false;
            btnWinner1.Enabled = false;
            btnWinner2.Enabled = false;
        }

        private void UpdateButtonStates()
        {
            btnGenerateBracket.Enabled = true;
            btnNextRound.Enabled = false;
            btnWinner1.Enabled = false;
            btnWinner2.Enabled = false;
        }
    }
}
