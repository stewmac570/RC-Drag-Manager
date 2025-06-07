using System;
using System.Collections.Generic;
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

            engine.Initialize(drivers);

            revealedRounds = currentSession.SavedRevealedRounds ?? new List<string>();
            if (revealedRounds.Count == 0)
            {
                revealedRounds.Add("R1");
            }

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
            UpdateButtonStates();
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
            lstFullPairings.Items.Clear();

            var matchesGrouped = engine.GetBracketMatches().GroupBy(m => m.RoundLabel);
            foreach (var group in matchesGrouped.OrderBy(g => GetRoundOrder(g.Key)))
            {
                if (!revealedRounds.Contains(group.Key))
                    continue;

                lstFullPairings.Items.Add($"---- {group.Key} ----");

                foreach (var match in group)
                {
                    var (driver1, driver2) = engine.ResolveDriversForMatch(match);
                    lstFullPairings.Items.Add($"{driver1.Name} vs {driver2.Name}");
                }
            }
        }

        private void UpdateNextUp()
        {
            var match = GetNextUnresolvedMatch();

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

        private void UpdateButtonStates()
        {
            btnNextRound.Enabled = (GetNextHiddenRound() != null);
        }

        private void UpdateWinnersList()
        {
            lstWinners.Items.Clear();

            var matchesGrouped = engine.GetBracketMatches()
                .Where(m => engine.Results.IsMatchResolved(m.MatchId))
                .GroupBy(m => m.RoundLabel);

            foreach (var group in matchesGrouped.OrderBy(g => GetRoundOrder(g.Key)))
            {
                lstWinners.Items.Add($"---- {group.Key} ----");
                foreach (var match in group)
                {
                    var winner = engine.Results.GetWinner(match.MatchId);
                    lstWinners.Items.Add($"{winner.Name}");
                }
            }
        }

        private int GetRoundOrder(string roundLabel)
        {
            switch (roundLabel)
            {
                case "R1": return 1;
                case "R2": return 2;
                case "QF": return 3;
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
            lstFullPairings.Items.Clear();
            lstWinners.Items.Clear();
            lblNext.Text = "";
            btnGenerateBracket.Enabled = true;
            UpdateButtonStates();
        }

        private void btnSaveAndClose_Click(object sender, EventArgs e)
        {
            if (currentSession == null)
            {
                MessageBox.Show("No session data available to save.");
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
                    currentSession.SavedResults.Add(new MatchResultSave
                    {
                        MatchId = match.MatchId,
                        WinnerDriverId = winner.Id
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

                engine.SetWinner(nextMatch.MatchId, winner);

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
    }
}
