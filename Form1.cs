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
        private RoundRobinEngine roundRobinEngine;
        private int currentMatchIndex = 0;
        private Button btnGenerateLosersBracket;
        private bool inLosersPhase = false;


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

        // -----------------------------------------------------------------------------
        // Generates the initial bracket / pairing list for the selected race type
        // -----------------------------------------------------------------------------
        private void btnGenerateBracket_Click(object sender, EventArgs e)
        {
            if (drivers.Count < 2)
            {
                MessageBox.Show("Not enough drivers to generate bracket.");
                return;
            }

            // Determine race type
            string selectedRaceType = currentSession?.RaceType
                                       ?? cmbRaceType.SelectedItem?.ToString()
                                       ?? "Pro Ladder";
            bool isRandom = IsRandomMode(selectedRaceType);

            // Reset engines / tracking
            engine = null;
            randomEngine = null;
            roundRobinEngine = null;
            revealedRounds.Clear();

            // ──────────────────────────────────────────────────────────────
            // RANDOM DRAW
            // ──────────────────────────────────────────────────────────────
            if (isRandom)
            {
                // Shuffle drivers and assign seeds
                var shuffled = drivers.OrderBy(_ => Guid.NewGuid()).ToList();
                for (int i = 0; i < shuffled.Count; i++) shuffled[i].Seed = i + 1;
                drivers = shuffled;

                // First-round pairings
                var matches = RandomBracket.GenerateFirstRound(drivers);

                randomEngine = new RandomMatchEngine();
                randomEngine.LoadMatches(matches);

                // Auto-resolve BYE wins
                foreach (var m in matches)
                {
                    var (d1, d2) = randomEngine.ResolveDrivers(m);
                    if (d1 != null && d2 == null) randomEngine.SetWinner(m.MatchId, d1);
                    else if (d2 != null && d1 == null) randomEngine.SetWinner(m.MatchId, d2);
                }

                revealedRounds.Add("R1");
            }
            // ──────────────────────────────────────────────────────────────
            // ROUND ROBIN
            // ──────────────────────────────────────────────────────────────
            else if (selectedRaceType == "Round Robin")
            {
                roundRobinEngine = new RoundRobinEngine();
                roundRobinEngine.LoadDrivers(drivers);
                roundRobinEngine.GenerateMatches();

                revealedRounds.Add("R1");
            }
            // ──────────────────────────────────────────────────────────────
            // PRO LADDER
            // ──────────────────────────────────────────────────────────────
            else
            {
                // Sort by qualifying time and seed
                drivers = drivers.OrderBy(d => d.QualTime).ToList();
                for (int i = 0; i < drivers.Count; i++) drivers[i].Seed = i + 1;

                engine = new MatchEngine();
                engine.Initialize(drivers);

                var firstRoundLabel = engine.GetBracketMatches()
                                            .Select(m => m.RoundLabel)
                                            .OrderBy(r => GetRoundOrder(r))
                                            .FirstOrDefault();

                if (!string.IsNullOrEmpty(firstRoundLabel))
                    revealedRounds.Add(firstRoundLabel);
            }

            // ──────────────────────────────────────────────────────────────
            // COMMON UI REFRESH
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
            if (engine == null) return null;

            return engine.GetBracketMatches()
                         .Where(m => revealedRounds.Contains(m.RoundLabel))
                         .FirstOrDefault(m => !engine.Results.IsMatchResolved(m.MatchId));
        }


        private void btnNextRound_Click(object sender, EventArgs e)
        {
            // Prevent null crash from Quick Session
            if (currentSession == null && cmbRaceType.SelectedItem == null)
            {
                MessageBox.Show("No race session or race type selected.");
                return;
            }

            string raceType = currentSession?.RaceType
                              ?? cmbRaceType.SelectedItem?.ToString()
                              ?? "Pro Ladder";
            var history = currentSession?.PairingHistory
                          ?? new HashSet<(int, int)>();

            // ──────────────────────────────────────────────────────────────
            // LOSERS BRACKET PHASE
            // ──────────────────────────────────────────────────────────────
            if (inLosersPhase)
            {
                if (randomEngine == null)
                    return;

                bool anyUnresolved = randomEngine.GetMatches()
                    .Where(m => revealedRounds.Contains(m.RoundLabel))
                    .Any(m => !randomEngine.HasWinner(m.MatchId));
                if (anyUnresolved)
                    return;

                var lbOrder = randomEngine.GetMatches()
                                 .Select(m => m.RoundLabel)
                                 .Distinct()
                                 .OrderBy(r => GetRoundOrder(r))
                                 .ToList();

                var lbRevealed = revealedRounds
                                  .Where(r => r.StartsWith("Losers Bracket"))
                                  .ToList();

                if (lbRevealed.Count < lbOrder.Count)
                {
                    string lbNextRound = lbOrder[lbRevealed.Count];

                    var lbNextMatches = randomEngine.GetMatches()
                                          .Where(m => m.RoundLabel == lbNextRound)
                                          .ToList();

                    foreach (var m in lbNextMatches)
                    {
                        var (d1, d2) = randomEngine.ResolveDrivers(m);

                        if (d1 != null && d2 == null)
                        {
                            currentSession.SavedResults.Add(new MatchResultSave
                            {
                                MatchId = m.MatchId,
                                WinnerDriverId = d1.Id,
                                LoserDriverId = -1
                            });
                            randomEngine.SetWinner(m.MatchId, d1);
                        }
                        else if (d2 != null && d1 == null)
                        {
                            currentSession.SavedResults.Add(new MatchResultSave
                            {
                                MatchId = m.MatchId,
                                WinnerDriverId = d2.Id,
                                LoserDriverId = -1
                            });
                            randomEngine.SetWinner(m.MatchId, d2);
                        }
                    }

                    revealedRounds.Add(lbNextRound);
                    RedrawFullBracket();
                    UpdateNextUp();
                    UpdateWinnersList();
                    UpdateButtonStates();
                    return;
                }

                // ── ALL LB ROUNDS COMPLETE: inject your 4-driver final ──
                var finalMatch = randomEngine.GetMatches()
                                  .Single(m => m.RoundLabel == lbOrder.Last());
                var lbWinner = randomEngine.GetWinner(finalMatch.MatchId);

                var rrResults = roundRobinEngine.GetResults();
                var standings = new RoundRobinRanker().Rank(rrResults, drivers);
                var top3 = standings
                                  .Where(r => r.Rank <= 3)
                                  .Select(r => drivers.First(d => d.Id == r.DriverId))
                                  .ToList();

                string msg = "Final 4 drivers advancing:\n\n";
                for (int i = 0; i < top3.Count; i++)
                    msg += $"{i + 1}. {top3[i].Name}\n";
                msg += $"4. {lbWinner.Name} (Buyback Winner)\n";

                MessageBox.Show(msg, "Buyback Complete");

                var finalists = new List<Driver>(top3) { lbWinner };

                engine = new MatchEngine();
                finalists = finalists.OrderBy(d => d.QualTime).ToList();
                for (int i = 0; i < finalists.Count; i++)
                    finalists[i].Seed = i + 1;
                engine.Initialize(finalists);

                revealedRounds.Clear();
                var firstRound = engine.GetBracketMatches()
                                       .Select(m => m.RoundLabel)
                                       .OrderBy(r => GetRoundOrder(r))
                                       .First();
                revealedRounds.Add(firstRound);

                currentSession.RaceType = "Pro Ladder";
                inLosersPhase = false;

                RedrawFullBracket();
                UpdateNextUp();
                UpdateWinnersList();
                UpdateButtonStates();
                return;
            }

            // ──────────────────────────────────────────────────────────────
            // PRO LADDER
            // ──────────────────────────────────────────────────────────────
            if (raceType == "Pro Ladder")
            {
                string nextRound = GetNextHiddenRound();
                if (nextRound == null) return;

                revealedRounds.Add(nextRound);
                RedrawFullBracket();
                UpdateNextUp();
                UpdateWinnersList();
                UpdateButtonStates();
                return;
            }

            // ──────────────────────────────────────────────────────────────
            // ROUND ROBIN
            // ──────────────────────────────────────────────────────────────
            if (raceType == "Round Robin")
            {
                string nextRound = GetNextHiddenRound();
                if (nextRound == null) return;

                revealedRounds.Add(nextRound);

                if (nextRound == "R3")
                {
                    var r3Matches = roundRobinEngine.GetMatches()
                                      .Where(m => m.RoundLabel == "R3")
                                      .Select(m => m.MatchId)
                                      .ToList();
                    bool allR3Done = r3Matches.All(id => roundRobinEngine.HasWinner(id));
                    btnGenerateLosersBracket.Enabled = allR3Done;

                    if (allR3Done)
                    {
                        var rrResults = roundRobinEngine.GetResults();
                        var ranked = new RoundRobinRanker().Rank(rrResults, drivers);
                        var top3 = ranked
                                    .Where(r => r.Rank <= 3)
                                    .Select(r => drivers.First(d => d.Id == r.DriverId))
                                    .ToList();

                        string msg = "Top 3 drivers advancing to finals:\n\n";
                        for (int i = 0; i < top3.Count; i++)
                            msg += $"{i + 1}. {top3[i].Name}\n";

                        msg += "\nSelect 'Generate Losers Bracket' to add buybacks.";

                        MessageBox.Show(msg, "Round Robin Complete");
                    }
                }

                RedrawFullBracket();
                UpdateNextUp();
                UpdateWinnersList();
                UpdateButtonStates();
                return;
            }

            // ──────────────────────────────────────────────────────────────
            // RANDOMIZED
            // ──────────────────────────────────────────────────────────────
            if (randomEngine == null || revealedRounds.Count == 0) return;

            string currentRound = revealedRounds.Last();
            var lastRoundMatches = randomEngine.GetMatches()
                                               .Where(m => m.RoundLabel == currentRound)
                                               .Where(m => randomEngine.HasWinner(m.MatchId))
                                               .ToList();

            var advancingDrivers = lastRoundMatches
                                    .Select(m => randomEngine.GetWinner(m.MatchId))
                                    .Where(d => d != null)
                                    .Distinct()
                                    .ToList();

            if (advancingDrivers.Count < 2) return;

            foreach (var m in randomEngine.GetMatches().Where(m => randomEngine.HasWinner(m.MatchId)))
            {
                var w = randomEngine.GetWinner(m.MatchId);
                var l = randomEngine.GetLoser(m.MatchId);
                if (w != null && l != null)
                    history.Add(w.Id < l.Id ? (w.Id, l.Id) : (l.Id, w.Id));
            }

            string nextRoundLabel = $"R{revealedRounds.Count + 1}";
            var nextMatches = RandomBracket.GenerateNextRound(advancingDrivers, history);

            int nextMatchId = randomEngine.GetMatches().Max(m => m.MatchId) + 1;
            foreach (var m in nextMatches)
            {
                m.MatchId = nextMatchId++;
                m.RoundLabel = nextRoundLabel;
            }

            var updated = randomEngine.GetMatches().ToList();
            updated.AddRange(nextMatches);
            randomEngine.LoadMatches(updated);
            revealedRounds.Add(nextRoundLabel);

            foreach (var m in nextMatches)
            {
                var (d1, d2) = randomEngine.ResolveDrivers(m);
                if (d1 != null && d2 == null) randomEngine.SetWinner(m.MatchId, d1);
                else if (d2 != null && d1 == null) randomEngine.SetWinner(m.MatchId, d2);
            }

            RedrawFullBracket();
            UpdateNextUp();
            UpdateWinnersList();
            UpdateButtonStates();
        }









        private string GetNextHiddenRound()
        {
            string raceType = currentSession?.RaceType
                              ?? cmbRaceType.SelectedItem?.ToString()
                              ?? "Pro Ladder";

            IEnumerable<string> allRounds = Enumerable.Empty<string>();

            if (raceType == "Pro Ladder")
            {
                if (engine == null) return null;
                allRounds = engine.GetBracketMatches().Select(m => m.RoundLabel);
            }
            else if (IsRandomMode(raceType))   // “Randomized”
            {
                if (randomEngine == null) return null;
                allRounds = randomEngine.GetMatches().Select(m => m.RoundLabel);
            }
            else                               // “Round Robin”
            {
                if (roundRobinEngine == null) return null;
                allRounds = roundRobinEngine.GetMatches().Select(m => m.RoundLabel);
            }

            return allRounds
                   .Distinct()
                   .OrderBy(r => GetRoundOrder(r))
                   .FirstOrDefault(r => !revealedRounds.Contains(r));
        }



        private void RedrawFullBracket()
        {
            // Ensure the ListView is in Details mode and has its 3 columns.
            if (lvPairings.Columns.Count == 0)
            {
                lvPairings.View = View.Details;
                lvPairings.Columns.Add("M#", 45, HorizontalAlignment.Left);
                lvPairings.Columns.Add("Driver 1", 100, HorizontalAlignment.Left);
                lvPairings.Columns.Add("Driver 2", 100, HorizontalAlignment.Left);
            }

            lvPairings.Items.Clear();

            // ▶ LOSERS-BRACKET PHASE: draw RR rounds then LB rounds when active
            if (inLosersPhase)
            {
                // 1️⃣ Round Robin portion
                if (roundRobinEngine != null)
                {
                    var rrGroups = roundRobinEngine.GetMatches()
                                                   .Where(m => revealedRounds.Contains(m.RoundLabel))
                                                   .GroupBy(m => m.RoundLabel);

                    foreach (var roundGroup in rrGroups.OrderBy(g => GetRoundOrder(g.Key)))
                    {
                        var header = new ListViewItem("");
                        header.SubItems.Add($"Round {roundGroup.Key.Replace("R", "")}");
                        header.SubItems.Add("");
                        header.BackColor = Color.LightGray;
                        header.Font = new Font(header.Font, FontStyle.Italic);
                        lvPairings.Items.Add(header);

                        foreach (var (matchId, d1, d2, round) in roundGroup)
                        {
                            string n1 = d1?.Name ?? "BYE";
                            string n2 = d2?.Name ?? "BYE";

                            var item = new ListViewItem($"M{matchId}");
                            item.SubItems.Add(n1);
                            item.SubItems.Add(n2);
                            lvPairings.Items.Add(item);
                        }
                    }
                }

                // 2️⃣ Losers Bracket portion
                if (randomEngine != null)
                {
                    var lbGroups = randomEngine.GetMatches()
                                               .Where(m => revealedRounds.Contains(m.RoundLabel))
                                               .GroupBy(m => m.RoundLabel);

                    foreach (var roundGroup in lbGroups.OrderBy(g => GetRoundOrder(g.Key)))
                    {
                        var header = new ListViewItem("");
                        header.SubItems.Add(roundGroup.Key);
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

                return;
            }

            string raceType = currentSession?.RaceType
                              ?? cmbRaceType.SelectedItem?.ToString()
                              ?? "Pro Ladder";

            // ───────────────────────────────────────────
            // PRO LADDER
            // ───────────────────────────────────────────
            if (raceType == "Pro Ladder")
            {
                var groups = engine.GetBracketMatches()
                                   .GroupBy(m => m.RoundLabel);

                foreach (var roundGroup in groups.OrderBy(g => GetRoundOrder(g.Key)))
                {
                    if (!revealedRounds.Contains(roundGroup.Key)) continue;

                    var header = new ListViewItem("");
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

                return;
            }

            // ───────────────────────────────────────────
            // ROUND ROBIN
            // ───────────────────────────────────────────
            if (raceType == "Round Robin")
            {
                if (roundRobinEngine == null) return;

                var rrGroups = roundRobinEngine.GetMatches()
                                               .Where(m => revealedRounds.Contains(m.RoundLabel))
                                               .GroupBy(m => m.RoundLabel);

                foreach (var roundGroup in rrGroups.OrderBy(g => GetRoundOrder(g.Key)))
                {
                    var header = new ListViewItem("");
                    header.SubItems.Add($"Round {roundGroup.Key.Replace("R", "")}");
                    header.SubItems.Add("");
                    header.BackColor = Color.LightGray;
                    header.Font = new Font(header.Font, FontStyle.Italic);
                    lvPairings.Items.Add(header);

                    foreach (var (matchId, d1, d2, round) in roundGroup)
                    {
                        string n1 = d1?.Name ?? "BYE";
                        string n2 = d2?.Name ?? "BYE";

                        var item = new ListViewItem($"M{matchId}");
                        item.SubItems.Add(n1);
                        item.SubItems.Add(n2);
                        lvPairings.Items.Add(item);
                    }
                }

                return;
            }

            // ───────────────────────────────────────────
            // RANDOMIZED
            // ───────────────────────────────────────────
            if (randomEngine == null) return;

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
            // ───────────────────────────────────────────
            // LOSERS BRACKET PHASE
            // ───────────────────────────────────────────
            if (inLosersPhase && randomEngine != null)
            {
                // find first unresolved LB match
                var nextLB = randomEngine.GetMatches()
                                 .Where(m => revealedRounds.Contains(m.RoundLabel))
                                 .FirstOrDefault(m => !randomEngine.HasWinner(m.MatchId));

                // ▶ only proceed if we actually found a match
                if (nextLB != null)
                {
                    var (d1, d2) = randomEngine.ResolveDrivers(nextLB);
                    btnWinner1.Text = d1?.Name ?? "BYE";
                    btnWinner2.Text = d2?.Name ?? "BYE";
                    btnWinner1.Enabled = d1 != null && d1.Name != "BYE";
                    btnWinner2.Enabled = d2 != null && d2.Name != "BYE";
                    lblNext.Text = $"Next: {btnWinner1.Text} vs {btnWinner2.Text}";
                    return;
                }
                goto AllResolved;
            }

            string raceType = currentSession?.RaceType ?? cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";
            bool isRandom = IsRandomMode(raceType);

            // ───────────────────────────────────────────
            // ROUND ROBIN
            // ───────────────────────────────────────────
            if (raceType == "Round Robin")
            {
                if (roundRobinEngine == null) return;

                var nextMatch = roundRobinEngine.GetMatches()
                                                .Where(m => revealedRounds.Contains(m.RoundLabel))
                                                .FirstOrDefault(m => !roundRobinEngine.HasWinner(m.MatchId));

                if (nextMatch.MatchId == 0) goto AllResolved;

                var (id, d1, d2, lbl) = nextMatch;

                btnWinner1.Text = d1?.Name ?? "BYE";
                btnWinner2.Text = d2?.Name ?? "BYE";
                btnWinner1.Enabled = d1 != null && d1.Name != "BYE";
                btnWinner2.Enabled = d2 != null && d2.Name != "BYE";

                lblNext.Text = $"Next: {btnWinner1.Text} vs {btnWinner2.Text}";
                return;
            }

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
                    btnWinner1.Enabled = d1 != null && d1.Name != "BYE";
                    btnWinner2.Enabled = d2 != null && d2.Name != "BYE";

                    lblNext.Text = $"Next: {btnWinner1.Text} vs {btnWinner2.Text}";
                    return;
                }
                goto AllResolved;
            }

            // ───────────────────────────────────────────
            // RANDOMIZED
            // ───────────────────────────────────────────
            if (randomEngine == null)
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
                btnWinner1.Enabled = d1 != null && d1.Name != "BYE";
                btnWinner2.Enabled = d2 != null && d2.Name != "BYE";

                lblNext.Text = $"Next: {btnWinner1.Text} vs {btnWinner2.Text}";
                return;
            }

        AllResolved:
            lblNext.Text = "All matches resolved.";
            btnWinner1.Enabled = btnWinner2.Enabled = false;
        }






        private void UpdateButtonStates()
        {
            // ───────────────────────────────────────────
            // LOSERS BRACKET PHASE
            // ───────────────────────────────────────────
            if (inLosersPhase)
            {
                // 🛡️ If we've reset the race, randomEngine can be null → exit LB phase
                if (randomEngine == null)
                {
                    inLosersPhase = false;
                }
                // ▶ only run LB logic when we actually have an engine
                else
                {
                    var lbLabels = new[]
                    {
                "Losers Bracket R1",
                "Losers Bracket R2",
                "Losers Bracket Final"
            };

                    // any unresolved in current LB rounds?
                    bool lbUnresolved = randomEngine.GetMatches()
                        .Where(m => revealedRounds.Contains(m.RoundLabel)
                                 && lbLabels.Contains(m.RoundLabel))
                        .Any(m => !randomEngine.HasWinner(m.MatchId));

                    // how many LB rounds have we revealed?
                    int revealedLbCount = revealedRounds.Count(r => lbLabels.Contains(r));

                    // is there another LB round to show?
                    bool lbHasNext = revealedLbCount < lbLabels.Length;

                    // enable Next Round only when current LB round done AND there's a next one
                    btnNextRound.Enabled = !lbUnresolved && lbHasNext;
                    return;
                }
            }

            // ───────────────────────────────────────────
            // (the rest of your existing button‐state logic is unchanged)
            // ───────────────────────────────────────────
            string raceType = currentSession?.RaceType
                              ?? cmbRaceType.SelectedItem?.ToString()
                              ?? "Pro Ladder";

            bool anyUnresolved = false;
            bool moreRounds = false;

            // PRO LADDER
            if (raceType == "Pro Ladder")
            {
                if (engine != null)
                {
                    anyUnresolved = engine.GetBracketMatches()
                                          .Where(m => revealedRounds.Contains(m.RoundLabel))
                                          .Any(m => !engine.Results.IsMatchResolved(m.MatchId));
                    moreRounds = GetNextHiddenRound() != null;
                }
            }
            // RANDOMIZED
            else if (IsRandomMode(raceType))
            {
                if (randomEngine != null && revealedRounds.Count > 0)
                {
                    anyUnresolved = randomEngine.GetMatches()
                                                .Where(m => revealedRounds.Contains(m.RoundLabel))
                                                .Any(m => !randomEngine.HasWinner(m.MatchId));

                    string currentRound = revealedRounds.Last();
                    int resolvedWinners = randomEngine.GetMatches()
                                          .Where(m => m.RoundLabel == currentRound
                                                   && randomEngine.HasWinner(m.MatchId))
                                          .Select(m => randomEngine.GetWinner(m.MatchId))
                                          .Where(d => d != null)
                                          .Distinct()
                                          .Count();

                    moreRounds = resolvedWinners > 1;
                }
            }
            // ROUND ROBIN
            else
            {
                if (roundRobinEngine != null && revealedRounds.Count > 0)
                {
                    anyUnresolved = roundRobinEngine.GetMatches()
                                                    .Where(m => revealedRounds.Contains(m.RoundLabel))
                                                    .Any(m => !roundRobinEngine.HasWinner(m.MatchId));
                    moreRounds = GetNextHiddenRound() != null;
                }
            }

            btnNextRound.Enabled = !anyUnresolved && moreRounds;

            // ▶ BUYBACK ENABLE (RR only)
            if (raceType == "Round Robin"
                && roundRobinEngine != null
                && GetNextHiddenRound() == null)
            {
                var r3Matches = roundRobinEngine
                                    .GetMatches()
                                    .Where(m => m.RoundLabel == "R3")
                                    .ToList();

                if (r3Matches.Any() &&
                    r3Matches.All(m => roundRobinEngine.HasWinner(m.MatchId)))
                {
                    btnGenerateLosersBracket.Enabled = true;
                }
            }
        }






        private void UpdateWinnersList()
        {
            lvWinners.Items.Clear();

            string raceType = currentSession?.RaceType ?? cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";

            // ──────────────────────────────────────────────────────────────
            // ROUND ROBIN
            // ──────────────────────────────────────────────────────────────
            if (raceType == "Round Robin")
            {
                if (roundRobinEngine == null) return;

                // 🟩 Always show standings
                var results = roundRobinEngine.GetResults();
                var standings = new RoundRobinRanker().Rank(results, drivers);

                var header = new ListViewItem("");
                header.SubItems.Add("---- Round Robin Standings ----");
                header.SubItems.Add("");
                header.BackColor = Color.LightGray;
                header.Font = new Font(header.Font, FontStyle.Italic);
                lvWinners.Items.Add(header);

                foreach (var rank in standings)
                {
                    var driver = drivers.FirstOrDefault(d => d.Id == rank.DriverId);
                    if (driver != null)
                    {
                        var item = new ListViewItem($"{rank.Rank}");
                        item.SubItems.Add(driver.Name);
                        item.SubItems.Add($"{rank.Points:0.0} pts");
                        lvWinners.Items.Add(item);
                    }
                }

                // 🟦 Then show match results (after any round)
                var completedMatches = roundRobinEngine.GetMatches()
                                                       .Where(m => roundRobinEngine.HasWinner(m.MatchId))
                                                       .GroupBy(m => m.RoundLabel);

                foreach (var roundGroup in completedMatches.OrderBy(g => GetRoundOrder(g.Key)))
                {
                    var subheader = new ListViewItem("");
                    subheader.SubItems.Add($"Round {roundGroup.Key.Replace("R", "")}");
                    subheader.SubItems.Add("");
                    subheader.BackColor = Color.LightGray;
                    subheader.Font = new Font(subheader.Font, FontStyle.Italic);
                    lvWinners.Items.Add(subheader);

                    foreach (var match in roundGroup)
                    {
                        var winner = roundRobinEngine.GetWinner(match.MatchId);
                        var loser = roundRobinEngine.GetLoser(match.MatchId);

                        var item = new ListViewItem($"M{match.MatchId}");
                        item.SubItems.Add(loser?.Name ?? "BYE");
                        item.SubItems.Add(winner?.Name ?? "");
                        lvWinners.Items.Add(item);
                    }
                }

                return;
            }

            // ──────────────────────────────────────────────────────────────
            // PRO LADDER
            // ──────────────────────────────────────────────────────────────
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

                return;
            }

            // ──────────────────────────────────────────────────────────────
            // RANDOMIZED
            // ──────────────────────────────────────────────────────────────
            if (randomEngine == null) return;

            var groupsRnd = randomEngine.GetMatches()
                                        .Where(m => randomEngine.HasWinner(m.MatchId))
                                        .GroupBy(m => m.RoundLabel);

            foreach (var roundGroup in groupsRnd.OrderBy(g => GetRoundOrder(g.Key)))
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
            // 1️⃣ core engines back to scratch
            engine = new MatchEngine();
            randomEngine = null;           // drop any LB/random engine
            inLosersPhase = false;         // clear losers-bracket flag

            // 2️⃣ clear all round-tracking
            revealedRounds.Clear();
            currentSession?.PairingHistory.Clear();

            // 3️⃣ clear both UI lists and “Up Next”
            lvPairings.Items.Clear();
            lvWinners.Items.Clear();
            lblNext.Text = "";

            // 4️⃣ restore buttons to initial state
            btnGenerateBracket.Enabled = true;
            btnGenerateLosersBracket.Enabled = false;
            UpdateButtonStates();

            // 5️⃣ restore selected race type in dropdown (do not re-trigger bracket)
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

            // ──────────────────────────────────────────────────────────────
            // Persist current results & revealed rounds into currentSession
            // ──────────────────────────────────────────────────────────────
            // ──────────────────────────────────────────────────────────────
            // Persist match results for whichever engine is active
            // ──────────────────────────────────────────────────────────────
            currentSession.SavedResults.Clear();

            string raceType = currentSession?.RaceType
                              ?? cmbRaceType.SelectedItem?.ToString()
                              ?? "Pro Ladder";

            // ---------- PRO LADDER ----------
            if (raceType == "Pro Ladder" && engine != null)
            {
                foreach (var match in engine.GetBracketMatches())
                {
                    if (engine.Results.IsMatchResolved(match.MatchId))
                    {
                        var winner = engine.Results.GetWinner(match.MatchId);
                        var loser = engine.Results.GetLoser(match.MatchId);

                        currentSession.SavedResults.Add(new MatchResultSave
                        {
                            MatchId = match.MatchId,
                            WinnerDriverId = winner?.Id ?? 0,
                            LoserDriverId = loser?.Id ?? 0
                        });
                    }
                }
            }
            // ---------- RANDOM DRAW ----------
            else if (IsRandomMode(raceType) && randomEngine != null)
            {
                foreach (var match in randomEngine.GetMatches())
                {
                    if (randomEngine.HasWinner(match.MatchId))
                    {
                        var winner = randomEngine.GetWinner(match.MatchId);
                        var loser = randomEngine.GetLoser(match.MatchId);

                        currentSession.SavedResults.Add(new MatchResultSave
                        {
                            MatchId = match.MatchId,
                            WinnerDriverId = winner?.Id ?? 0,
                            LoserDriverId = loser?.Id ?? 0
                        });
                    }
                }
            }
            // ---------- ROUND ROBIN ----------
            else if (raceType == "Round Robin" && roundRobinEngine != null)
            {
                foreach (var (matchId, _, _, _) in roundRobinEngine.GetMatches())
                {
                    if (roundRobinEngine.HasWinner(matchId))
                    {
                        var winner = roundRobinEngine.GetWinner(matchId);
                        var loser = roundRobinEngine.GetLoser(matchId);

                        currentSession.SavedResults.Add(new MatchResultSave
                        {
                            MatchId = matchId,
                            WinnerDriverId = winner?.Id ?? 0,
                            LoserDriverId = loser?.Id ?? 0
                        });
                    }
                }
            }

            // Save which rounds have been revealed
            currentSession.SavedRevealedRounds = new List<string>(revealedRounds);


            // Save which rounds the user has already viewed
            currentSession.SavedRevealedRounds = new List<string>(revealedRounds);


            currentSession.SavedRevealedRounds = new List<string>(revealedRounds);

            // build and store pairing-history if this session used random draw
            if (IsRandomMode(currentSession.RaceType) && randomEngine != null)
            {
                var hist = new HashSet<(int, int)>();
                foreach (var m in randomEngine.GetMatches().Where(m => randomEngine.HasWinner(m.MatchId)))
                {
                    var w = randomEngine.GetWinner(m.MatchId);
                    var l = randomEngine.GetLoser(m.MatchId);
                    if (w != null && l != null)
                        hist.Add(w.Id < l.Id ? (w.Id, l.Id) : (l.Id, w.Id));
                }
                currentSession.PairingHistory = hist;
            }

            // existing line — leave as-is
            sessionRepository.SaveSession(currentSession);

            sessionRepository.SaveSession(currentSession);

            MessageBox.Show("Race session saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }



        // -----------------------------------------------------------------------------
        // Records the winner for the next unresolved match, whatever the race type.
        // -----------------------------------------------------------------------------
        private void ProcessMatchWinner(bool winner1)
        {
            // ───────────────────────────────────────────
            // LOSERS BRACKET PHASE
            // ───────────────────────────────────────────
            if (inLosersPhase && randomEngine != null)
            {
                // find the next unresolved Losers-Bracket match
                var lbMatch = randomEngine.GetMatches()
                                 .Where(m => revealedRounds.Contains(m.RoundLabel))
                                 .FirstOrDefault(m => !randomEngine.HasWinner(m.MatchId));
                if (lbMatch == null) return;

                // determine winner/loser
                var (d1, d2) = randomEngine.ResolveDrivers(lbMatch);
                var winner = winner1 ? d1 : d2;
                var loser = winner1 ? d2 : d1;

                // 🔧 fix corrupted match that falsely reports loser as BYE/null
                if (loser == null || loser.Name == "BYE")
                {
                    if (d1 != null && d1.Id != winner.Id)
                        loser = d1;
                    else if (d2 != null && d2.Id != winner.Id)
                        loser = d2;
                }


                // record result
                randomEngine.SetWinner(lbMatch.MatchId, winner);

                // update stats
                if (loser != null && loser.Name != "BYE")
                    UpdateDriverStats(winner, loser);


                // ─── AUTO-REVEAL NEXT LB ROUND IF THIS ONE IS FULLY DONE ───
                       // build ordered LB labels from your engine
                var lbOrder = randomEngine.GetMatches()
                                        .Select(m => m.RoundLabel)
                                        .Distinct()
                                        .OrderBy(r => GetRoundOrder(r))
                                        .ToList();
                
                       // count how many rounds are already visible
                var lbRevealed = revealedRounds
                                         .Where(r => r.StartsWith("Losers Bracket"))
                                         .ToList();
                
                       // if *all* matches in the current round are now resolved, and there’s still another LB round…
                bool roundDone = !randomEngine.GetMatches()
                                           .Where(m => revealedRounds.Contains(m.RoundLabel))
                                           .Any(m => !randomEngine.HasWinner(m.MatchId));
                if (roundDone && lbRevealed.Count < lbOrder.Count)
                {
                    // User must trigger next LB round manually
                    btnNextRound.Enabled = true;
                    return;
                }


                RedrawFullBracket();
                UpdateNextUp();
                UpdateWinnersList();

                // 🔒 Disable winner buttons until next round is manually triggered
                if (roundDone && lbRevealed.Count < lbOrder.Count)
                {
                    btnWinner1.Enabled = false;
                    btnWinner2.Enabled = false;
                    btnNextRound.Enabled = true;
                }
                else
                {
                    UpdateButtonStates();
                }

                return;

            }

            string raceType = currentSession?.RaceType
                              ?? cmbRaceType.SelectedItem?.ToString()
                              ?? "Pro Ladder";

            // ───────────────────────────────────────────
            // PRO LADDER
            // ───────────────────────────────────────────
            if (raceType == "Pro Ladder")
            {
                var nextMatch = GetNextUnresolvedMatch();
                if (nextMatch != null)
                {
                    var (d1, d2) = engine.ResolveDriversForMatch(nextMatch);
                    var winner = winner1 ? d1 : d2;
                    var loser = winner1 ? d2 : d1;

                    engine.SetWinner(nextMatch.MatchId, winner, loser);

                    if (loser != null && loser.Name != "BYE")
                        UpdateDriverStats(winner, loser);

                    // ← Removed UpdateEventWinnerStats() here
                }
            }
            // ───────────────────────────────────────────
            // ROUND ROBIN
            // ───────────────────────────────────────────
            else if (raceType == "Round Robin")
            {
                if (roundRobinEngine == null) return;

                var nextMatch = roundRobinEngine.GetMatches()
                                                .Where(m => revealedRounds.Contains(m.RoundLabel))
                                                .FirstOrDefault(m => !roundRobinEngine.HasWinner(m.MatchId));
                if (nextMatch.MatchId == 0) return;

                var (matchId, d1, d2, _) = nextMatch;
                var winner = winner1 ? d1 : d2;
                var loser = winner1 ? d2 : d1;

                roundRobinEngine.SetWinner(matchId, winner, loser);

                // 🏁 Check if all RR matches are now resolved
                bool allDone = roundRobinEngine.GetMatches()
                    .Where(m => revealedRounds.Contains(m.RoundLabel))
                    .All(m => roundRobinEngine.HasWinner(m.MatchId));

                bool hasR3 = revealedRounds.Contains("R3");

                if (allDone && hasR3)
                {
                    var rrResults = roundRobinEngine.GetResults();
                    var ranked = new RoundRobinRanker().Rank(rrResults, drivers);
                    var top3 = ranked
                        .Where(r => r.Rank <= 3)
                        .Select(r => drivers.First(d => d.Id == r.DriverId))
                        .ToList();

                    string msg = "Top 3 drivers advancing to finals:\n\n";
                    for (int i = 0; i < top3.Count; i++)
                        msg += $"{i + 1}. {top3[i].Name}\n";

                    msg += "\nSelect 'Generate Losers Bracket' to add buybacks.";

                    MessageBox.Show(msg, "Round Robin Complete");
                }


                if (loser != null && loser.Name != "BYE")
                    UpdateDriverStats(winner, loser);

                // ← Removed UpdateEventWinnerStats() here
            }
            // ───────────────────────────────────────────
            // RANDOMIZED
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

                // ← Removed UpdateEventWinnerStats() here
            }

            // If all current LB matches resolved, force disable winner buttons
            if (inLosersPhase)
            {
                var lbOrder = randomEngine.GetMatches()
                                          .Select(m => m.RoundLabel)
                                          .Distinct()
                                          .OrderBy(r => GetRoundOrder(r))
                                          .ToList();

                var lbRevealed = revealedRounds
                                 .Where(r => r.StartsWith("Losers Bracket"))
                                 .ToList();

                bool roundDone = !randomEngine.GetMatches()
                                      .Where(m => revealedRounds.Contains(m.RoundLabel))
                                      .Any(m => !randomEngine.HasWinner(m.MatchId));

                if (roundDone && lbRevealed.Count < lbOrder.Count)
                {
                    btnWinner1.Enabled = false;
                    btnWinner2.Enabled = false;
                    btnNextRound.Enabled = true;
                    return;
                }
            }

            // ───────────────────────────────────────────
            // UI refresh
            // ───────────────────────────────────────────
            RedrawFullBracket();
            UpdateNextUp();
            UpdateWinnersList();
            UpdateButtonStates();

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

        private List<DriverRankResult> GetRoundRobinStandings()
        {
            if (roundRobinEngine == null) return new List<DriverRankResult>();

            var results = roundRobinEngine.GetResults();
            return new RoundRobinRanker().Rank(results, drivers);
        }

        private void btnGenerateLosersBracket_Click(object sender, EventArgs e)
        {
            // 1️⃣ Round-Robin standings
            var rrResults = roundRobinEngine.GetResults();
            var standings = new RoundRobinRanker().Rank(rrResults, drivers);

            // build the pool of drivers ranked 4+
            var buybackPool = standings
                .Where(r => r.Rank > 3)
                .Select(r => drivers.First(d => d.Id == r.DriverId))
                .ToList();

            // 2️⃣ Show the selector (with a “No Buyback” button returning DialogResult.No)
            using var sel = new BuybackDriverSelectionForm(buybackPool);
            var dlg = sel.ShowDialog();

            // ——— Skip Losers Bracket entirely — take exactly the 4th-place driver ———
            if (dlg == DialogResult.No)
            {
                // pick the driver with Rank==4
                var fourth = drivers.First(d =>
                    d.Id == standings.Single(r => r.Rank == 4).DriverId);

                // collect Top-3
                var top3 = standings
                    .Where(r => r.Rank <= 3)
                    .Select(r => drivers.First(d => d.Id == r.DriverId))
                    .ToList();

                // finalists: 3 + #4
                var finalists = new List<Driver>(top3) { fourth };

                // reinitialize the Pro-Ladder engine for exactly these 4 drivers
                engine = new MatchEngine();
                // sort by QualTime & assign seeds 1..4
                finalists = finalists.OrderBy(d => d.QualTime).ToList();
                for (int i = 0; i < finalists.Count; i++)
                    finalists[i].Seed = i + 1;

                engine.Initialize(finalists);

                // clear out any prior rounds & reveal the first Pro-Ladder round
                revealedRounds.Clear();
                var firstRound = engine.GetBracketMatches()
                                       .Select(m => m.RoundLabel)
                                       .OrderBy(r => GetRoundOrder(r))
                                       .First();
                revealedRounds.Add(firstRound);

                // redraw and let the director run Semi-finals & Final
                RedrawFullBracket();
                UpdateNextUp();
                UpdateWinnersList();
                UpdateButtonStates();
                return;
            }

            // ——— Otherwise: user picked 1+ buybacks — run the Losers Bracket flow ———
            if (dlg != DialogResult.OK)
                return;

            var entrants = sel.SelectedDrivers;

            // 3️⃣ Gather all prior RR pairings to prevent rematches
            var allHistory = new HashSet<(int, int)>(
                rrResults.Select(r =>
                {
                    var p = (r.Driver1Id, r.Driver2Id);
                    return p.Item1 < p.Item2 ? p
                                             : (p.Item2, p.Item1);
                })
            );

            // 4️⃣ Build and load the single-elim Losers Bracket
            int offset = roundRobinEngine.GetMatches()
                             .Max(m => m.MatchId) + 1;

            var lbMatches = LosersBracketBuilder
                                .Build(entrants, allHistory, offset);

            randomEngine = new RandomMatchEngine();
            randomEngine.LoadMatches(lbMatches);

            // mark the first LB round visible
            // do NOT reveal the first LB round yet — wait for user to click "Next Round"
            // revealedRounds.Add("Losers Bracket R1");
            btnNextRound.Enabled = true;

            currentSession.PairingHistory.UnionWith(allHistory);

            // ▶ ENTER LOSERS-BRACKET PHASE
            inLosersPhase = true;

            // ▶ Auto-resolve any BYE in the first LB round
            var lbLabels = randomEngine.GetMatches()
                                       .Select(m => m.RoundLabel)
                                       .Distinct()
                                       .OrderBy(r => GetRoundOrder(r))
                                       .ToList();
            var firstLbLabel = lbLabels.First();
            foreach (var m in randomEngine.GetMatches().Where(m => m.RoundLabel == firstLbLabel))
            {
                var (d1, d2) = randomEngine.ResolveDrivers(m);
                if (d1 != null && d2 == null)
                {
                    randomEngine.SetWinner(m.MatchId, d1);
                }
                else if (d2 != null && d1 == null)
                {
                    randomEngine.SetWinner(m.MatchId, d2);
                }
            }

            // 5️⃣ Refresh UI for manual LB results
            RedrawFullBracket();
            UpdateNextUp();
            UpdateWinnersList();
            UpdateButtonStates();
        }






    }
}