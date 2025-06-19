using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.AccessControl;
using System.Windows.Forms;

namespace RCDragManagerProd
{
    public partial class Form1 : Form
    {
        private List<Driver> drivers = new List<Driver>();
        private MatchEngine engine = new MatchEngine();
        private RandomMatchEngine randomEngine;
        private RaceSession currentSession;
        private List<string> revealedRounds = new List<string>();
        private RaceSessionRepository sessionRepository = new RaceSessionRepository("race_data.db");
        private ComboBox cmbRaceType;
        private Label lblRaceType;

        private bool IsRandomMode(string raceType)
        {
            return raceType?.IndexOf("random", StringComparison.OrdinalIgnoreCase) >= 0;
        }

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

            // Determine race type once
            string selectedRaceType = currentSession?.RaceType ?? cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";
            bool isRandom = IsRandomMode(selectedRaceType);

            revealedRounds.Clear();

            // ──────────────────────────────────────────────────────────────
            // RANDOM DRAW  (blind draw, round-by-round)
            // ──────────────────────────────────────────────────────────────
            if (isRandom)
            {
                // Shuffle → assign seeds
                var shuffled = drivers.OrderBy(_ => Guid.NewGuid()).ToList();
                for (int i = 0; i < shuffled.Count; i++) shuffled[i].Seed = i + 1;
                drivers = shuffled;

                // Build first round
                var matches = RandomBracket.GenerateFirstRound(drivers);
                randomEngine = new RandomMatchEngine();
                randomEngine.LoadMatches(matches);

                // Auto-resolve BYEs
                foreach (var m in matches)
                {
                    var (d1, d2) = randomEngine.ResolveDrivers(m);
                    if (d1 != null && d2 == null) randomEngine.SetWinner(m.MatchId, d1);
                    else if (d2 != null && d1 == null) randomEngine.SetWinner(m.MatchId, d2);
                }

                revealedRounds.Add("R1");   // random brackets always start at R1
            }
            // ──────────────────────────────────────────────────────────────
            // PRO LADDER  (NHRA fixed ladder)
            // ──────────────────────────────────────────────────────────────
            else
            {
                // Seed by qualifying time
                drivers = drivers.OrderBy(d => d.QualTime).ToList();
                for (int i = 0; i < drivers.Count; i++) drivers[i].Seed = i + 1;

                engine = new MatchEngine();             // fresh instance
                engine.Initialize(drivers);             // build ladder

                // Determine the first round label in this ladder (R1, SF, etc.)
                var firstRoundLabel = engine.GetBracketMatches()
                                            .Select(m => m.RoundLabel)
                                            .OrderBy(r => GetRoundOrder(r))
                                            .FirstOrDefault();

                if (!string.IsNullOrEmpty(firstRoundLabel))
                    revealedRounds.Add(firstRoundLabel);
            }

            // ──────────────────────────────────────────────────────────────
            // UI refresh
            // ──────────────────────────────────────────────────────────────
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
            string raceType = currentSession?.RaceType ?? cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";

            // ──────────────────────────────────────────────────────────────
            // PRO LADDER  (fixed bracket)
            // ──────────────────────────────────────────────────────────────
            if (raceType == "Pro Ladder")
            {
                string nextRound = GetNextHiddenRound();
                if (nextRound == null) return;      // no more rounds

                revealedRounds.Add(nextRound);      // simply reveal and refresh
                RedrawFullBracket();
                UpdateNextUp();
                UpdateWinnersList();
                UpdateButtonStates();
                return;
            }

            // ──────────────────────────────────────────────────────────────
            // RANDOMIZED  (blind-draw, round-by-round)
            // ──────────────────────────────────────────────────────────────
            // 1️⃣ Collect winners from the *most recently revealed* round
            string currentRound = revealedRounds.Last();           // e.g. "R1"
            var lastRoundMatches = randomEngine.GetMatches()
                                               .Where(m => m.RoundLabel == currentRound)
                                               .Where(m => randomEngine.HasWinner(m.MatchId))
                                               .ToList();

            var advancingDrivers = lastRoundMatches
                                    .Select(m => randomEngine.GetWinner(m.MatchId))
                                    .Where(d => d != null)
                                    .Distinct()
                                    .ToList();

            // If only one driver remains, tournament is complete
            if (advancingDrivers.Count < 2) return;

            // 2️⃣ Build complete pairing history (avoid rematches)
            var history = new HashSet<(int, int)>();
            foreach (var m in randomEngine.GetMatches().Where(m => randomEngine.HasWinner(m.MatchId)))
            {
                var w = randomEngine.GetWinner(m.MatchId);
                var l = randomEngine.GetLoser(m.MatchId);
                if (w != null && l != null)
                    history.Add(w.Id < l.Id ? (w.Id, l.Id) : (l.Id, w.Id));
            }

            // 3️⃣ Generate the next round label and matches
            string nextRoundLabel = $"R{revealedRounds.Count + 1}";

            var nextMatches = RandomBracket.GenerateNextRound(advancingDrivers, history);

            int nextMatchId = randomEngine.GetMatches().Max(m => m.MatchId) + 1;
            foreach (var m in nextMatches)
            {
                m.MatchId = nextMatchId++;
                m.RoundLabel = nextRoundLabel;
            }

            // 4️⃣ Store matches, reveal round
            var updated = randomEngine.GetMatches().ToList();
            updated.AddRange(nextMatches);
            randomEngine.LoadMatches(updated);
            revealedRounds.Add(nextRoundLabel);

            // 5️⃣ Auto-resolve BYEs in the new round
            foreach (var m in nextMatches)
            {
                var (d1, d2) = randomEngine.ResolveDrivers(m);
                if (d1 != null && d2 == null) randomEngine.SetWinner(m.MatchId, d1);
                else if (d2 != null && d1 == null) randomEngine.SetWinner(m.MatchId, d2);
            }

            // 6️⃣ Refresh UI
            RedrawFullBracket();
            UpdateNextUp();
            UpdateWinnersList();
            UpdateButtonStates();
        }






        private string NormalizePair(int a, int b)
        {
            return (a < b) ? $"{a}-{b}" : $"{b}-{a}";
        }


        private string GetNextHiddenRound()
        {
            string raceType = currentSession?.RaceType ?? cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";

            IEnumerable<string> allRounds;

            if (raceType == "Pro Ladder")
            {
                allRounds = engine.GetBracketMatches().Select(m => m.RoundLabel);
            }
            else // Randomized
            {
                // safety: if randomEngine isn’t ready yet
                if (randomEngine == null) return null;

                allRounds = randomEngine.GetMatches().Select(m => m.RoundLabel);
            }

            foreach (var round in allRounds.Distinct().OrderBy(r => GetRoundOrder(r)))
            {
                if (!revealedRounds.Contains(round))
                    return round;
            }
            return null;
        }


        private void RedrawFullBracket()
        {
            // Ensure the ListView is in Details mode and has its 3 columns.
            // (Quick-Session Form loads without columns.)
            if (lvPairings.Columns.Count == 0)
            {
                lvPairings.View = View.Details;
                lvPairings.Columns.Add("M#", 45, HorizontalAlignment.Left);
                lvPairings.Columns.Add("Driver 1", 100, HorizontalAlignment.Left);
                lvPairings.Columns.Add("Driver 2", 100, HorizontalAlignment.Left);
            }

            lvPairings.Items.Clear();

            string raceType = currentSession?.RaceType ?? cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";
            bool isRandom = IsRandomMode(raceType);

            // ──────────────────────────────────────────────────────────────
            // PRO LADDER
            // ──────────────────────────────────────────────────────────────
            if (!isRandom)
            {
                var groups = engine.GetBracketMatches()
                                   .GroupBy(m => m.RoundLabel);

                foreach (var roundGroup in groups.OrderBy(g => GetRoundOrder(g.Key)))
                {
                    if (!revealedRounds.Contains(roundGroup.Key)) continue;

                    var header = new ListViewItem("");                       // empty M#
                    header.SubItems.Add($"Round {GetRoundName(roundGroup.Key)}");
                    header.SubItems.Add("");
                    header.BackColor = Color.LightGray;
                    header.Font = new Font(header.Font, FontStyle.Italic);
                    lvPairings.Items.Add(header);

                    foreach (var match in roundGroup)
                    {
                        var (d1, d2) = engine.ResolveDriversForMatch(match);
                        string n1 = d1?.Name ?? "BYE";
                        string n2 = d2?.Name ?? "BYE";

                        var item = new ListViewItem($"M{match.MatchId}");
                        item.SubItems.Add(n1);
                        item.SubItems.Add(n2);
                        lvPairings.Items.Add(item);
                    }
                }

                return;   // done
            }

            // ──────────────────────────────────────────────────────────────
            // RANDOMIZED
            // ──────────────────────────────────────────────────────────────
            if (randomEngine == null) return;   // first launch before Generate Bracket

            var rndGroups = randomEngine.GetMatches()
                                        .GroupBy(m => m.RoundLabel);

            foreach (var roundGroup in rndGroups.OrderBy(g => GetRoundOrder(g.Key)))
            {
                if (!revealedRounds.Contains(roundGroup.Key)) continue;

                var header = new ListViewItem("");
                header.SubItems.Add($"Round {roundGroup.Key.Replace("R", "")}");
                header.SubItems.Add("");
                header.BackColor = Color.LightGray;
                header.Font = new Font(header.Font, FontStyle.Italic);
                lvPairings.Items.Add(header);

                foreach (var match in roundGroup)
                {
                    var (d1, d2) = randomEngine.ResolveDrivers(match);
                    string n1 = d1?.Name ?? "BYE";
                    string n2 = d2?.Name ?? "BYE";

                    var item = new ListViewItem($"M{match.MatchId}");
                    item.SubItems.Add(n1);
                    item.SubItems.Add(n2);
                    lvPairings.Items.Add(item);
                }
            }
        }





        private void UpdateNextUp()
        {
            string raceType = currentSession?.RaceType ?? cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";
            bool isRandom = IsRandomMode(raceType);

            // ───────────────────────────────────────────
            // PRO LADDER
            // ───────────────────────────────────────────
            if (!isRandom)
            {
                var match = GetNextUnresolvedMatch();
                if (match != null)
                {
                    var (d1, d2) = engine.ResolveDriversForMatch(match);

                    btnWinner1.Text = d1?.Name ?? "BYE";
                    btnWinner2.Text = d2?.Name ?? "BYE";
                    btnWinner1.Enabled = d1?.Name != "BYE";
                    btnWinner2.Enabled = d2?.Name != "BYE";

                    lblNext.Text = $"Next: {d1?.Name ?? "BYE"} vs {d2?.Name ?? "BYE"}";
                }
                else
                {
                    lblNext.Text = "All matches resolved.";
                    btnWinner1.Enabled = btnWinner2.Enabled = false;
                }

                return;
            }

            // ───────────────────────────────────────────
            // RANDOM DRAW
            // ───────────────────────────────────────────
            if (randomEngine == null)              // before Generate Bracket or after Reset
            {
                btnWinner1.Enabled = btnWinner2.Enabled = false;
                lblNext.Text = "Up Next: --";
                return;
            }

            var rndMatch = randomEngine.GetMatches()
                                       .Where(m => revealedRounds.Contains(m.RoundLabel))
                                       .FirstOrDefault(m => !randomEngine.HasWinner(m.MatchId));

            if (rndMatch != null)
            {
                var (d1, d2) = randomEngine.ResolveDrivers(rndMatch);

                btnWinner1.Text = d1?.Name ?? "BYE";
                btnWinner2.Text = d2?.Name ?? "BYE";
                btnWinner1.Enabled = d1?.Name != "BYE";
                btnWinner2.Enabled = d2?.Name != "BYE";

                lblNext.Text = $"Next: {d1?.Name ?? "BYE"} vs {d2?.Name ?? "BYE"}";
            }
            else
            {
                lblNext.Text = "All matches resolved.";
                btnWinner1.Enabled = btnWinner2.Enabled = false;
            }
        }






        private void UpdateButtonStates()
        {
            string raceType = currentSession?.RaceType ?? cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";
            bool isRandom = IsRandomMode(raceType);

            bool anyUnresolved;
            bool moreRounds;

            // ───────────────────────────────────────────
            // PRO LADDER
            // ───────────────────────────────────────────
            if (!isRandom)
            {
                anyUnresolved = engine.GetBracketMatches()
                                      .Where(m => revealedRounds.Contains(m.RoundLabel))
                                      .Any(m => !engine.Results.IsMatchResolved(m.MatchId));

                moreRounds = GetNextHiddenRound() != null;
            }
            // ───────────────────────────────────────────
            // RANDOMIZED
            // ───────────────────────────────────────────
            else
            {
                // After “Reset Race” randomEngine is null OR no rounds revealed yet.
                if (randomEngine == null || revealedRounds.Count == 0)
                {
                    btnNextRound.Enabled = false;
                    return;
                }

                anyUnresolved = randomEngine.GetMatches()
                                            .Where(m => revealedRounds.Contains(m.RoundLabel))
                                            .Any(m => !randomEngine.HasWinner(m.MatchId));

                // Count winners in the latest revealed round only
                string currentRound = revealedRounds.Last();      // safe now
                int alive = randomEngine.GetMatches()
                                        .Where(m => m.RoundLabel == currentRound)
                                        .Where(m => randomEngine.HasWinner(m.MatchId))
                                        .Select(m => randomEngine.GetWinner(m.MatchId))
                                        .Distinct()
                                        .Count();

                moreRounds = alive > 1;   // need >1 driver to create another round
            }

            btnNextRound.Enabled = (!anyUnresolved && moreRounds);
        }




        private void UpdateWinnersList()
        {
            lvWinners.Items.Clear();

            string raceType = currentSession?.RaceType ?? cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";

            if (raceType == "Pro Ladder")
            {
                var groups = engine.GetBracketMatches()
                                   .Where(m => engine.Results.IsMatchResolved(m.MatchId))
                                   .GroupBy(m => m.RoundLabel);

                foreach (var roundGroup in groups.OrderBy(g => GetRoundOrder(g.Key)))
                {
                    var header = new ListViewItem("");
                    header.SubItems.Add($"Round {GetRoundName(roundGroup.Key)}");
                    header.SubItems.Add("");
                    header.BackColor = Color.LightGray;
                    header.Font = new Font(header.Font, FontStyle.Italic);
                    lvWinners.Items.Add(header);

                    foreach (var match in roundGroup)
                    {
                        var winner = engine.Results.GetWinner(match.MatchId);
                        var loser = engine.Results.GetLoser(match.MatchId);

                        var item = new ListViewItem($"M{match.MatchId}");
                        item.SubItems.Add(loser?.Name ?? "BYE");
                        item.SubItems.Add(winner?.Name ?? "");
                        lvWinners.Items.Add(item);
                    }
                }
            }
            else   // ───────── RANDOMIZED ─────────
            {
                if (randomEngine == null) return;

                var groups = randomEngine.GetMatches()
                                         .Where(m => randomEngine.HasWinner(m.MatchId))
                                         .GroupBy(m => m.RoundLabel);

                foreach (var roundGroup in groups.OrderBy(g => GetRoundOrder(g.Key)))
                {
                    var header = new ListViewItem("");
                    header.SubItems.Add($"Round {roundGroup.Key.Replace("R", "")}");
                    header.SubItems.Add("");
                    header.BackColor = Color.LightGray;
                    header.Font = new Font(header.Font, FontStyle.Italic);
                    lvWinners.Items.Add(header);

                    foreach (var match in roundGroup)
                    {
                        var winner = randomEngine.GetWinner(match.MatchId);
                        var loser = randomEngine.GetLoser(match.MatchId);

                        var item = new ListViewItem($"M{match.MatchId}");
                        item.SubItems.Add(loser?.Name ?? "BYE");
                        item.SubItems.Add(winner?.Name ?? "");
                        lvWinners.Items.Add(item);
                    }
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
                case "R4": return 4;   // 🔹 added – for 17-32 car fields
                case "R5": return 5;   // 🔹 added – 33-64 if you ever expand
                case "SF": return 98;  // keep Semi-final high
                case "F": return 99;  // keep Final highest
                default: return 100; // anything unknown
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
            randomEngine = null;              // reset random-draw state
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
            string raceType = currentSession?.RaceType ?? cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";
            bool isRandom = IsRandomMode(raceType);

            // ───────────────────────────────────────────
            // PRO LADDER
            // ───────────────────────────────────────────
            if (!isRandom)
            {
                var nextMatch = GetNextUnresolvedMatch();
                if (nextMatch == null) return;

                var (d1, d2) = engine.ResolveDriversForMatch(nextMatch);
                var winner = winner1 ? d1 : d2;
                var loser = winner1 ? d2 : d1;

                engine.SetWinner(nextMatch.MatchId, winner, loser);

                if (loser != null && loser.Name != "BYE")        // skip BYE stats
                    UpdateDriverStats(winner, loser);

                if (engine.IsTournamentComplete())
                    UpdateEventWinnerStats();
            }
            // ───────────────────────────────────────────
            // RANDOM DRAW
            // ───────────────────────────────────────────
            else
            {
                if (randomEngine == null) return;

                var nextMatch = randomEngine.GetMatches()
                                            .Where(m => revealedRounds.Contains(m.RoundLabel))
                                            .FirstOrDefault(m => !randomEngine.HasWinner(m.MatchId));
                if (nextMatch == null) return;

                var (d1, d2) = randomEngine.ResolveDrivers(nextMatch);
                var winner = winner1 ? d1 : d2;
                var loser = winner1 ? d2 : d1;

                randomEngine.SetWinner(nextMatch.MatchId, winner);

                if (loser != null && loser.Name != "BYE")
                    UpdateDriverStats(winner, loser);

                if (randomEngine.IsTournamentComplete())
                    UpdateEventWinnerStats();
            }

            // ───────────────────────────────────────────
            // Refresh UI
            // ───────────────────────────────────────────
            RedrawFullBracket();
            UpdateNextUp();
            UpdateWinnersList();
            UpdateButtonStates();
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