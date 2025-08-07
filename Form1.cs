using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.AccessControl;
using System.Windows.Forms;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.ViewModels;   // for PairingRow



namespace RCDragManagerProd
{
    public partial class Form1 : Form
    {
        private List<Driver> drivers = new List<Driver>();
        private RaceSession currentSession;             // (optional for Quick Session)
        private RaceSessionRepository sessionRepository = new RaceSessionRepository("race_data.db");  // (optional)
        private ComboBox cmbRaceType;                   // (optional for Quick Session)
        private Label lblRaceType;                      // (optional)
        private readonly RaceController _controller;

        private void UpdateNextUp() => _controller.PushNextMatch();
        private void UpdateWinnersList() => lvWinners.Items.Clear(); // or full refresh logic if you have it
        private void UpdateButtonStates() => btnNextRound.Enabled = false; // adjust to fit logic

        private RandomMatchEngine _losersEngine;

        public Form1(RaceController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            InitializeComponent();
            currentSession = _controller.Session;

            lblEventTitle.Text = currentSession != null
                ? $"Event: {currentSession.EventName}"
                : "Quick Session";

            // If you want the race type combo for Quick Session:
            // cmbRaceType.Visible = lblRaceType.Visible = (currentSession == null);
            // if (currentSession == null) cmbRaceType.SelectedIndex = 0;

            btnNextRound.Enabled = false;   // 🔒 ALWAYS disable on form load

            // Controller event hooks:
            _controller.BracketRedrawn += RedrawFullBracket;

            _controller.NextMatchReady += row =>
            {

                if (row == null)
                {
                    lblNext.Text = "No match ready";
                    btnWinner1.Enabled = false;
                    btnWinner2.Enabled = false;
                    return;
                }

                lblNext.Text = $"{row.Driver1} vs {row.Driver2}";
                btnWinner1.Text = row.Driver1;
                btnWinner2.Text = row.Driver2;
                btnWinner1.Tag = row.MatchId;
                btnWinner2.Tag = row.MatchId;

                // ✅ Bulletproof BYE disable — trims & ignores case
                btnWinner1.Enabled = !string.Equals(row.Driver1?.Trim(), "BYE", StringComparison.OrdinalIgnoreCase);
                btnWinner2.Enabled = !string.Equals(row.Driver2?.Trim(), "BYE", StringComparison.OrdinalIgnoreCase);
            };




            // This is in Form1.cs
            _controller.WinnersUpdated += rows =>
            {
                lvWinners.BeginUpdate();
                lvWinners.Items.Clear();

                // ✅ Use bracket sort — not alphabetical
                var grouped = rows.GroupBy(w => w.RoundLabel)
                                  .OrderBy(g => GetRoundOrder(g.Key));

                foreach (var group in grouped)
                {
                    var header = new ListViewItem("");
                    header.SubItems.Add($"Round {group.Key}");
                    header.BackColor = Color.LightGray;
                    header.Font = new Font(header.Font, FontStyle.Italic);
                    lvWinners.Items.Add(header);

                    foreach (var w in group)
                    {
                        var item = new ListViewItem($"M{w.MatchId}");
                        item.SubItems.Add(w.Loser ?? "");
                        item.SubItems.Add(w.Winner ?? "");
                        lvWinners.Items.Add(item);
                    }
                }

                lvWinners.EndUpdate();
            };

            Logger.Log("🔥 Logging system initialized");

            _controller.CanAdvanceChanged += canAdvance => btnNextRound.Enabled = canAdvance;

            // Enable the “Generate Losers Bracket” button once RR is complete
            _controller.CanOfferBuybackChanged += () =>
            {
                btnGenerateLosersBracket.Enabled = true;
                Logger.Log("UI: Generate Losers Bracket button enabled.");
            };

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

            try
            {
                string raceType = cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";
                Logger.Log($"[FORM1] GenerateBracket called with race type: {raceType}");
                _controller.GenerateBracket(raceType, drivers);
                btnGenerateBracket.Enabled = false; // controller drives UI state
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bracket generation failed:\n{ex.Message}");
            }
        }



        private void btnWinner1_Click(object sender, EventArgs e)
        {
            if (btnWinner1.Tag is int matchId)
            {
                _controller.SubmitWinner(matchId, firstOption: true);

                var match = _controller.GetMatch(matchId);
                var winner = _controller.GetWinner(matchId);
                var loser = _controller.GetLoser(matchId);
                string round = match?.RoundLabel ?? "Unknown";
                string winnerName = winner?.Name ?? "BYE/Unknown";
                string loserName = loser?.Name ?? "BYE/Unknown";

                Logger.Log($"[RESULT] Match {matchId} ({round}): {winnerName} defeated {loserName}");
            }
        }

        private void btnWinner2_Click(object sender, EventArgs e)
        {
            if (btnWinner2.Tag is int matchId)
            {
                _controller.SubmitWinner(matchId, firstOption: false);

                var match = _controller.GetMatch(matchId);
                var winner = _controller.GetWinner(matchId);
                var loser = _controller.GetLoser(matchId);
                string round = match?.RoundLabel ?? "Unknown";
                string winnerName = winner?.Name ?? "BYE/Unknown";
                string loserName = loser?.Name ?? "BYE/Unknown";

                Logger.Log($"[RESULT] Match {matchId} ({round}): {winnerName} defeated {loserName}");
            }
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



        private void btnNextRound_Click(object sender, EventArgs e)
        {
            try
            {
                var nextRound = _controller.GetNextHiddenRound();
                Logger.Log($"[FORM1] Generate Next Round clicked — revealing: {nextRound}");

                _controller.AdvanceRound();

                Logger.Log("[FORM1] AdvanceRound() completed");
            }
            catch (Exception ex)
            {
                Logger.Log($"[FORM1] AdvanceRound FAILED: {ex.Message}");
                MessageBox.Show($"Cannot advance round:\n{ex.Message}");
            }
        }


        private void RedrawFullBracket(IReadOnlyList<PairingRow> rows)
        {
            if (lvPairings.Columns.Count == 0)
            {
                lvPairings.View = View.Details;
                lvPairings.Columns.Add("M#", 45, HorizontalAlignment.Left);
                lvPairings.Columns.Add("Driver 1", 100, HorizontalAlignment.Left);
                lvPairings.Columns.Add("Driver 2", 100, HorizontalAlignment.Left);
            }

            lvPairings.Items.Clear();

            foreach (var row in rows)
            {
                if (row.IsHeader)
                {
                    var header = new ListViewItem("");
                    header.SubItems.Add($"Round {row.RoundLabel}");
                    header.SubItems.Add("");
                    header.BackColor = Color.LightGray;
                    header.Font = new Font(header.Font, FontStyle.Italic);
                    lvPairings.Items.Add(header);
                }
                else
                {
                    var item = new ListViewItem($"M{row.MatchId}");
                    item.SubItems.Add(row.Driver1 ?? "BYE");
                    item.SubItems.Add(row.Driver2 ?? "BYE");
                    lvPairings.Items.Add(item);
                }
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            _controller.Reset();

            lvPairings.Items.Clear();
            lvWinners.Items.Clear();
            lblNext.Text = "";

            btnGenerateBracket.Enabled = true;
            btnNextRound.Enabled = false; // ✅ Always disabled on reset
            //btnGenerateLosersBracket.Enabled = false; // ✅ If you have LB mode

            // Optional: reset race type selector for Quick Session
            if (currentSession != null && !string.IsNullOrEmpty(currentSession.RaceType))
            {
                cmbRaceType.SelectedItem = currentSession.RaceType;
            }
        }


        private void btnSaveAndClose_Click(object sender, EventArgs e)
        {
            if (currentSession == null)
            {
                MessageBox.Show("Quick Session completed. No session file saved.");
                this.Close();
                return;
            }

            // Save driver entries
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

            // Save bracket results and revealed rounds — the controller owns these
            _controller.SaveSession();   // <- call the controller’s version, if you wire it up

            // If you don’t have the SaveSession logic yet:
            // You could expose _controller.Winners or similar and copy them here.

            sessionRepository.SaveSession(currentSession);

            MessageBox.Show("Race session saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private int GetRoundOrder(string roundLabel)
        {
            switch (roundLabel)
            {
                case "R1": return 1;
                case "R2": return 2;
                case "R3": return 3;
                case "R4": return 4;   // If you use bigger ladders
                case "SF": return 98;  // Semi-Final high
                case "F": return 99;  // Final always last
                default: return 100;
            }
        }

        private void btnGenerateLosersBracket_Click(object sender, EventArgs e)
        {
            btnGenerateLosersBracket.Enabled = false;   // prevent second click

            Logger.Log("🔁 User triggered Generate Losers Bracket");

            var eligible = _controller.GetEligibleBuybackDrivers();

            if (eligible == null || eligible.Count == 0)
            {
                MessageBox.Show("No eligible buyback drivers found.", "No Entries", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logger.Log("⚠️ No eligible buybacks found.");
                return;
            }

            using (var dlg = new BuybackDriverSelectionForm(eligible))

            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;                                // user cancelled

                // ✅ grab the selection from the property, not a method
                var selectedDrivers = dlg.SelectedDrivers;

                if (selectedDrivers == null || selectedDrivers.Count == 0)
                {
                    MessageBox.Show("No drivers selected.");
                    return;
                }

                Logger.Log($"[LB] Drivers chosen: {string.Join(", ", selectedDrivers.Select(d => d.Name))}");

                // hand off to the controller – it will generate and run the bracket
                Logger.Log("[LB] Hand-off to controller.GenerateLosersBracket()");
                _controller.GenerateLosersBracket(selectedDrivers);

            }

        }

        /// <summary>
        /// Convenience overload so legacy call-sites that invoked
        /// <c>RedrawFullBracket()</c> with no parameters still compile.
        /// It fetches the latest bracket rows from the controller,
        /// emits a DEBUG log, and delegates to the real method.
        /// </summary>
        private void RedrawFullBracket()
        {
            if (_controller == null)
            {
                Logger.Log("⚠️  RedrawFullBracket(): _controller is null — aborting redraw");
                return;
            }

            IReadOnlyList<PairingRow> rows = _controller.BuildCurrentBracketRows();

            Logger.Log($"🔄 UI bracket redraw triggered — rows={rows.Count}");
            RedrawFullBracket(rows);              // ← existing, parameterised overload
        }


        private void UpdateUIAfterBracketChange()
        {
            RedrawFullBracket();      // now compiles — calls the wrapper above
            UpdateNextUp();
            UpdateWinnersList();
            UpdateButtonStates();
            // NB: Any additional per-round logic can be added here later.
        }




    }
}