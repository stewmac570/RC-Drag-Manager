using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RCDragManagerProd
{
    public partial class Form1 : Form
    {
        private List<Driver> drivers = new List<Driver>();
        private MatchEngine engine = new MatchEngine();
        private RaceSession currentSession;
        private List<string> revealedRounds = new List<string>();
        private RaceSessionRepository sessionRepository = new RaceSessionRepository("race_data.db");
        private ComboBox cmbRaceType;
        private Label lblRaceType;


        public Form1(RaceSession session = null)
        {
            InitializeComponent();
            currentSession = session;
            

            if (currentSession != null)
            {
                lblEventTitle.Text = $"Event: {currentSession.EventName}";
                InitializeDriversFromSession();
                RestoreBracketState();
            }
            else
            {
                lblEventTitle.Text = "Quick Session";
            }
            cmbRaceType.Visible = (currentSession == null);
            lblRaceType.Visible = (currentSession == null);

            if (currentSession == null)
            {
                cmbRaceType.SelectedIndex = 0; // Default to Pro Ladder
            }
            else
            {
                // Set the combo to match the session's race type, if you want to display it anyway
                cmbRaceType.SelectedItem = currentSession.RaceType;
            }

            UpdateDriverList();
            UpdateButtonStates();
        }

        private void InitializeDriversFromSession()
        {
            drivers.Clear();

            foreach (var entry in currentSession.DriverEntries)
            {
                double timeToUse = 0.0;

                if (currentSession.ClassType == "Heads Up")
                    timeToUse = entry.QualifyingTime ?? 0.0;
                else
                    timeToUse = entry.DialIn ?? 0.0;

                Driver driver = new Driver
                {
                    Id = entry.DriverID,
                    Name = entry.DriverName,
                    QualTime = timeToUse
                };
                drivers.Add(driver);
            }
        }

        private void RestoreBracketState()
        {
            drivers = drivers.OrderBy(d => d.QualTime).ToList();
            for (int i = 0; i < drivers.Count; i++)
            {
                drivers[i].Seed = i + 1;
            }

            engine = new MatchEngine(); // don’t auto-initialize bracket

            revealedRounds = currentSession.SavedRevealedRounds ?? new List<string>();

            // Don’t auto-add R1 unless session was in progress
            if (revealedRounds.Count > 0)
            {
                engine.Initialize(drivers);
                foreach (var result in currentSession.SavedResults)
                {
                    var winner = drivers.FirstOrDefault(d => d.Id == result.WinnerDriverId);
                    if (winner != null)
                    {
                        engine.SetWinner(result.MatchId, winner);
                    }
                }

                RedrawFullBracket();
                UpdateNextUp();
                UpdateWinnersList();
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
                    // ✅ Final safe constructor call with 2 string parameters
                    var editDialog = new EditDriverDialog(driver.Name, "");
                    if (editDialog.ShowDialog() == DialogResult.OK)
                    {
                        driver.Name = editDialog.DriverName;
                        UpdateDriverList();
                    }
                }
            }
        }


        private void btnSetQualTime_Click(object sender, EventArgs e)
        {
            if (lvDrivers.SelectedItems.Count > 0)
            {
                string selectedName = lvDrivers.SelectedItems[0].Text;
                var driver = drivers.FirstOrDefault(d => d.Name == selectedName);
                if (driver != null)
                {
                    var qualDialog = new AddEditQualTimeDialog(driver.Name, driver.QualTime);
                    if (qualDialog.ShowDialog() == DialogResult.OK)
                    {
                        driver.QualTime = qualDialog.QualifyingTime;
                        UpdateDriverList();
                    }
                }
            }
            else
            {
                MessageBox.Show("Select a driver to edit qualifying time.");
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
            revealedRounds.Clear();
            revealedRounds.Add("R1");

            RedrawFullBracket();
            UpdateNextUp();
            UpdateWinnersList();
            UpdateButtonStates();

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

        private void UpdateDriverStats(Driver winner, Driver loser)
        {
            var repo = new DriverRepository("race_data.db");

            var winnerInDb = repo.GetDriverById(winner.Id);
            var loserInDb = repo.GetDriverById(loser.Id);

            if (winnerInDb != null)
            {
                winnerInDb.TotalWins += 1;
                repo.UpdateDriver(winnerInDb);
            }

            if (loserInDb != null)
            {
                loserInDb.TotalLosses += 1;
                repo.UpdateDriver(loserInDb);
            }
        }

        private ProLadder.LadderMatch GetNextUnresolvedMatch()
        {
            return engine.GetBracketMatches()
                .Where(m => revealedRounds.Contains(m.RoundLabel))
                .FirstOrDefault(m => !engine.Results.IsMatchResolved(m.MatchId));
        }

        private void btnNextRound_Click(object sender, EventArgs e)
        {
            var nextRound = GetNextHiddenRound();
            if (nextRound != null)
            {
                revealedRounds.Add(nextRound);
                RedrawFullBracket();
                UpdateNextUp();
                UpdateWinnersList();
                UpdateButtonStates();
            }
        }

        private string GetNextHiddenRound()
        {
            var allRounds = engine.GetBracketMatches().Select(m => m.RoundLabel).Distinct().OrderBy(r => GetRoundOrder(r)).ToList();
            foreach (var round in allRounds)
            {
                if (!revealedRounds.Contains(round))
                    return round;
            }
            return null;
        }

        private void RedrawFullBracket()
        {
            lvPairings.Items.Clear();

            var matchesGrouped = engine.GetBracketMatches().GroupBy(m => m.RoundLabel);
            foreach (var group in matchesGrouped.OrderBy(g => GetRoundOrder(g.Key)))
            {
                if (!revealedRounds.Contains(group.Key))
                    continue;

                // Round label
                var roundHeader = new ListViewItem(""); // M# column empty
                roundHeader.SubItems.Add($"Round {group.Key.Replace("R", "")}");
                roundHeader.SubItems.Add(""); // Driver 2 column
                roundHeader.BackColor = Color.LightGray;
                roundHeader.Font = new Font("Segoe UI", 9F, FontStyle.Italic); // or roundHeader.Font
               



                roundHeader.BackColor = Color.LightGray;
                lvPairings.Items.Add(roundHeader);

                foreach (var match in group)
                {
                    var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                    string name1 = driver1.Name == "TBD" ? "BYE" : driver1.Name;
                    string name2 = driver2.Name == "TBD" ? "BYE" : driver2.Name;

                    var item = new ListViewItem($"M{match.MatchId}");
                    item.SubItems.Add(name1);
                    item.SubItems.Add(name2);

                    item.SubItems.Add(driver1.Name);
                    item.SubItems.Add(driver2.Name);
                    lvPairings.Items.Add(item);
                }
            }
        }

        private void UpdateNextUp()
        {
            var match = GetNextUnresolvedMatch();

            if (match != null)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(match);

                string name1 = driver1.Name == "TBD" ? "BYE" : driver1.Name;
                string name2 = driver2.Name == "TBD" ? "BYE" : driver2.Name;

                lblNext.Text = $"{name1} vs {name2}";

                btnWinner1.Text = name1;
                btnWinner2.Text = name2;


                btnWinner1.Text = driver1.Name;
                btnWinner2.Text = driver2.Name;

                btnWinner1.Enabled = (name1 != "BYE");
                btnWinner2.Enabled = (name2 != "BYE");
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

        private void UpdateButtonStates()
        {
            bool anyUnresolved = engine.GetBracketMatches()
                .Where(m => revealedRounds.Contains(m.RoundLabel))
                .Any(m => !engine.Results.IsMatchResolved(m.MatchId));

            btnNextRound.Enabled = (!anyUnresolved && GetNextHiddenRound() != null);
        }


        private void UpdateWinnersList()
        {
            lvWinners.Items.Clear();

            var matchesGrouped = engine.GetBracketMatches()
                .Where(m => engine.Results.IsMatchResolved(m.MatchId))
                .GroupBy(m => m.RoundLabel);

            foreach (var group in matchesGrouped.OrderBy(g => GetRoundOrder(g.Key)))
            {
                var roundHeader = new ListViewItem("");
                roundHeader.SubItems.Add($"Round {GetRoundName(group.Key)}");
                roundHeader.SubItems.Add("");
                roundHeader.BackColor = Color.LightGray;
                roundHeader.Font = new Font(roundHeader.Font, FontStyle.Italic);
                lvWinners.Items.Add(roundHeader);

                foreach (var match in group)
                {
                    var winner = engine.Results.GetWinner(match.MatchId);
                    var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                    var loser = (winner.Id == driver1.Id) ? driver2 : driver1;

                    var item = new ListViewItem($"M{match.MatchId}");
                    item.SubItems.Add(loser.Name);
                    item.SubItems.Add(winner.Name);
                    lvWinners.Items.Add(item);
                }
            }
        }


        private int GetRoundOrder(string roundLabel)
        {
            switch (roundLabel)
            {
                case "R1": return 1;
                case "R2": return 2;
                case "R3": return 3;
                //case "QF": return 3; // Optional, if used for 8-car stage
                case "SF": return 4;
                case "F": return 5;
                default: return 99;
            }
        }

        private void btnEditResult_Click(object sender, EventArgs e)
        {
            var match = GetNextUnresolvedMatch();

            if (match != null)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                var editDialog = new EditWinnerDialog(driver1, driver2);
                if (editDialog.ShowDialog() == DialogResult.OK)
                {
                    engine.SetWinner(match.MatchId, editDialog.SelectedWinner);
                    RedrawFullBracket();
                    UpdateNextUp();
                    UpdateWinnersList();
                    UpdateButtonStates();
                }
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            engine = new MatchEngine();
            revealedRounds.Clear();
            lvPairings.Items.Clear();
            lvWinners.Items.Clear();
            lblNext.Text = "";
            btnGenerateBracket.Enabled = true;
            UpdateButtonStates();
        }

        private void btnSaveAndClose_Click(object sender, EventArgs e)
        {
            if (currentSession == null)
            {
                MessageBox.Show("Quick Session completed. No session file saved.");
                this.Close();
                return;
            }

            currentSession.DriverEntries.Clear();

            foreach (var d in drivers)
            {
                var entry = new RaceSessionDriverEntry
                {
                    DriverID = d.Id,
                    DriverName = d.Name,
                    QualifyingTime = d.QualTime
                };
                currentSession.DriverEntries.Add(entry);
            }

            currentSession.SavedResults.Clear();
            foreach (var match in engine.GetBracketMatches())
            {
                if (engine.Results.IsMatchResolved(match.MatchId))
                {
                    var winner = engine.Results.GetWinner(match.MatchId);
                    var loser = engine.Results.GetLoser(match.MatchId);

                    currentSession.SavedResults.Add(new MatchResultSave
                    {
                        MatchId = match.MatchId,
                        WinnerDriverId = winner.Id,
                        LoserDriverId = loser?.Id ?? 0
                    });
                }
            }

            currentSession.SavedRevealedRounds = new List<string>(revealedRounds);

            sessionRepository.SaveSession(currentSession);

            MessageBox.Show("Race session saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }



        private void ProcessMatchWinner(bool winner1)
        {
            var nextMatch = GetNextUnresolvedMatch();
            if (nextMatch != null)
            {
                var (driver1, driver2) = engine.ResolveDriversForMatch(nextMatch);
                var winner = winner1 ? driver1 : driver2;
                var loser = winner1 ? driver2 : driver1;

                engine.SetWinner(nextMatch.MatchId, winner, loser);


                UpdateDriverStats(winner, loser);

                if (engine.IsTournamentComplete())
                {
                    UpdateEventWinnerStats();
                }

                RedrawFullBracket();
                UpdateNextUp();
                UpdateWinnersList();
                UpdateButtonStates();
            }
        }

        private void UpdateEventWinnerStats()
        {
            var finalMatch = engine.GetBracketMatches().FirstOrDefault(m => m.RoundLabel == "F");
            if (finalMatch != null)
            {
                var tournamentWinner = engine.Results.GetWinner(finalMatch.MatchId);
                if (tournamentWinner != null)
                {
                    var repo = new DriverRepository("race_data.db");
                    var winnerInDb = repo.GetDriverById(tournamentWinner.Id);
                    if (winnerInDb != null)
                    {
                        winnerInDb.EventsWon += 1;
                        repo.UpdateDriver(winnerInDb);
                    }
                }
            }
        }
        private string GetRoundName(string code)
        {
            switch (code)
            {
                case "R1": return "1";
                case "R2": return "2";
                case "R3": return "3";
                case "SF": return "SF";
                case "F": return "F";
                default: return code;
            }
        }


    }
}
