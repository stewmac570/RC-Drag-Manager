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
        private readonly RaceController _controller;


        private bool IsRandomMode(string raceType)
        {
            return raceType?.IndexOf("random", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public Form1(RaceController controller)
        {
            // ── controller & session ───────────────────────────────
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            InitializeComponent();
            currentSession = _controller.Session;

            // ── header text ────────────────────────────────────────
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

            // ── race-type combo visibility ─────────────────────────
            cmbRaceType.Visible = lblRaceType.Visible = (currentSession == null);

            if (currentSession == null)
                cmbRaceType.SelectedIndex = 0;          // default “Pro Ladder”
            else
                cmbRaceType.SelectedItem = currentSession.RaceType;

            // ── initial UI refresh ────────────────────────────────
            UpdateDriverList();
            UpdateButtonStates();

            // ────────────────────  controller event hooks  ─────────────────────

            // 1) Full bracket redraw → rebuild ListView
            _controller.BracketRedrawn += _ => RedrawFullBracket();

            // 2) Next match ready → update “Next” label & winner buttons
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
                btnWinner1.Enabled = btnWinner2.Enabled = true;
            };

            // 3) Winners list updated → refresh winners ListView
            _controller.WinnersUpdated += rows =>
            {
                lvWinners.BeginUpdate();
                lvWinners.Items.Clear();

                foreach (var w in rows)
                    lvWinners.Items.Add($"{w.Winner} defeated {w.Loser}");

                lvWinners.EndUpdate();
            };

            // 4) “Advance Round” button enabled/disabled
            _controller.CanAdvanceChanged += canAdvance =>
            {
                btnNextRound.Enabled = canAdvance;
            };

            // 5) Winner-pick buttons enabled/disabled
            _controller.CanPickWinnerChanged += canPick =>
            {
                btnWinner1.Enabled = btnWinner2.Enabled = canPick;
            };
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

            // Race type: pull from combo-box (Quick Session) or from loaded session
            string raceType = currentSession?.RaceType
                              ?? cmbRaceType.SelectedItem?.ToString()
                              ?? "Pro Ladder";

            try
            {
                _controller.GenerateBracket(raceType, drivers);
                btnGenerateBracket.Enabled = false;   // controller will drive UI from now on
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bracket generation failed:\n{ex.Message}");
            }
        }


        private void btnWinner1_Click(object sender, EventArgs e)
        {
            if (btnWinner1.Tag is int matchId)          // Tag was set in NextMatchReady
                _controller.SubmitWinner(matchId, firstOption: true);
        }

        private void btnWinner2_Click(object sender, EventArgs e)
        {
            if (btnWinner2.Tag is int matchId)
                _controller.SubmitWinner(matchId, firstOption: false);
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
            try
            {
                _controller.AdvanceRound();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot advance round:\n{ex.Message}");
            }
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

            // ▶ LOSERS-BRACKET PHASE: draw only real RR rounds then LB rounds when active
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
                        // header
                        var header = new ListViewItem("");
                        header.SubItems.Add($"Round {roundGroup.Key.Replace("R", "")}");
                        header.SubItems.Add("");
                        header.BackColor = Color.LightGray;
                        header.Font = new Font(header.Font, FontStyle.Italic);
                        lvPairings.Items.Add(header);

                        // only show matches with two drivers
                        foreach (var (matchId, d1, d2, round) in roundGroup)
                        {
                            if (d1 == null || d2 == null) continue;

                            var item = new ListViewItem($"M{matchId}");
                            item.SubItems.Add(d1.Name);
                            item.SubItems.Add(d2.Name);
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
                        // header
                        var header = new ListViewItem("");
                        header.SubItems.Add(roundGroup.Key);
                        header.SubItems.Add("");
                        header.BackColor = Color.LightGray;
                        header.Font = new Font(header.Font, FontStyle.Italic);
                        lvPairings.Items.Add(header);

                        // only show matches with two drivers
                        foreach (var match in roundGroup)
                        {
                            var (d1, d2) = randomEngine.ResolveDrivers(match);
                            if (d1 == null || d2 == null) continue;

                            var item = new ListViewItem($"M{match.MatchId}");
                            item.SubItems.Add(d1.Name);
                            item.SubItems.Add(d2.Name);
                            lvPairings.Items.Add(item);
                        }
                    }
                }

                return;
            }

            // ───────────────────────────────────────────
            // PRO LADDER
            // ───────────────────────────────────────────
            string raceType = currentSession?.RaceType
                              ?? cmbRaceType.SelectedItem?.ToString()
                              ?? "Pro Ladder";
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

                    // only show matches with two drivers
                    foreach (var match in roundGroup)
                    {
                        var (d1, d2) = engine.ResolveDriversForMatch(match);
                        if (d1 == null || d2 == null) continue;

                        var item = new ListViewItem($"M{match.MatchId}");
                        item.SubItems.Add(d1.Name);
                        item.SubItems.Add(d2.Name);
                        lvPairings.Items.Add(item);
                    }
                }

                return;
            }

            // ───────────────────────────────────────────
            // ROUND ROBIN
            // ───────────────────────────────────────────
            if (raceType == "Round Robin" && roundRobinEngine != null)
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
                        if (d1 == null || d2 == null) continue;

                        var item = new ListViewItem($"M{matchId}");
                        item.SubItems.Add(d1.Name);
                        item.SubItems.Add(d2.Name);
                        lvPairings.Items.Add(item);
                    }
                }

                return;
            }

            // ───────────────────────────────────────────
            // RANDOMIZED
            // ───────────────────────────────────────────
            if (randomEngine != null)
            {
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
                        if (d1 == null || d2 == null) continue;

                        var item = new ListViewItem($"M{match.MatchId}");
                        item.SubItems.Add(d1.Name);
                        item.SubItems.Add(d2.Name);
                        lvPairings.Items.Add(item);
                    }
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
                // ▶ Build ordered list of all pending LB matches
                var pending = randomEngine.GetMatches()
                    .Where(m => revealedRounds.Contains(m.RoundLabel) && !randomEngine.HasWinner(m.MatchId))
                    .OrderBy(m => GetRoundOrder(m.RoundLabel))
                    .ThenBy(m => m.MatchId)
                    .ToList();

                // ▶ DEBUG: show exactly which matches are pending
                MessageBox.Show(
                    pending.Count > 0
                        ? "Pending LB matches:\n" + string.Join("\n", pending.Select(m => $"M{m.MatchId} ({m.RoundLabel})"))
                        : "No pending LB matches",
                    "DEBUG: UpdateNextUp");

                // ▶ Pick the first pending LB match
                var nextLB = pending.FirstOrDefault();
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

                // ▶ All LB matches done
                lblNext.Text = "All LB matches resolved.";
                btnWinner1.Enabled = btnWinner2.Enabled = false;
                return;
            }

            // ───────────────────────────────────────────
            // ROUND ROBIN PHASE
            // ───────────────────────────────────────────
            string raceType = currentSession?.RaceType
                              ?? cmbRaceType.SelectedItem?.ToString()
                              ?? "Pro Ladder";
            bool isRandom = IsRandomMode(raceType);

            if (raceType == "Round Robin" && roundRobinEngine != null)
            {
                var nextRR = roundRobinEngine.GetMatches()
                               .Where(m => revealedRounds.Contains(m.RoundLabel))
                               .FirstOrDefault(m => !roundRobinEngine.HasWinner(m.MatchId));
                if (nextRR.MatchId > 0)
                {
                    var (id, d1, d2, lbl) = nextRR;
                    btnWinner1.Text = d1?.Name ?? "BYE";
                    btnWinner2.Text = d2?.Name ?? "BYE";
                    btnWinner1.Enabled = d1 != null && d1.Name != "BYE";
                    btnWinner2.Enabled = d2 != null && d2.Name != "BYE";
                    lblNext.Text = $"Next: {btnWinner1.Text} vs {btnWinner2.Text}";
                    return;
                }
                lblNext.Text = "All RR matches resolved.";
                btnWinner1.Enabled = btnWinner2.Enabled = false;
                return;
            }

            // ───────────────────────────────────────────
            // PRO LADDER PHASE
            // ───────────────────────────────────────────
            if (!isRandom)
            {
                var nextPL = GetNextUnresolvedMatch(); // your existing helper
                if (nextPL != null)
                {
                    var (d1, d2) = engine.ResolveDriversForMatch(nextPL);
                    btnWinner1.Text = d1?.Name ?? "BYE";
                    btnWinner2.Text = d2?.Name ?? "BYE";
                    btnWinner1.Enabled = d1 != null && d1.Name != "BYE";
                    btnWinner2.Enabled = d2 != null && d2.Name != "BYE";
                    lblNext.Text = $"Next: {btnWinner1.Text} vs {btnWinner2.Text}";
                    return;
                }
                lblNext.Text = "All Pro Ladder matches resolved.";
                btnWinner1.Enabled = btnWinner2.Enabled = false;
                return;
            }

            // ───────────────────────────────────────────
            // RANDOMIZED PHASE
            // ───────────────────────────────────────────
            if (randomEngine == null)
            {
                lblNext.Text = "Up Next: --";
                btnWinner1.Enabled = btnWinner2.Enabled = false;
                return;
            }

            var nextRnd = randomEngine.GetMatches()
                            .Where(m => revealedRounds.Contains(m.RoundLabel))
                            .FirstOrDefault(m => !randomEngine.HasWinner(m.MatchId));
            if (nextRnd != null && nextRnd.MatchId > 0)
            {
                var (d1, d2) = randomEngine.ResolveDrivers(nextRnd);
                btnWinner1.Text = d1?.Name ?? "BYE";
                btnWinner2.Text = d2?.Name ?? "BYE";
                btnWinner1.Enabled = d1 != null && d1.Name != "BYE";
                btnWinner2.Enabled = d2 != null && d2.Name != "BYE";
                lblNext.Text = $"Next: {btnWinner1.Text} vs {btnWinner2.Text}";
                return;
            }

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
                if (randomEngine == null)
                {
                    inLosersPhase = false;
                }
                else
                {
                    // build full ordered list of LB rounds
                    var lbOrder = randomEngine.GetMatches()
                                    .Select(m => m.RoundLabel)
                                    .Distinct()
                                    .Where(r => r.StartsWith("Losers Bracket"))
                                    .OrderBy(r => GetRoundOrder(r))
                                    .ToList();

                    // check if any revealed LB match is unresolved
                    bool lbUnresolved = randomEngine.GetMatches()
                        .Where(m => revealedRounds.Contains(m.RoundLabel))
                        .Any(m => !randomEngine.HasWinner(m.MatchId));

                    // count revealed LB rounds
                    int revealedLbCount = revealedRounds
                        .Count(r => r.StartsWith("Losers Bracket"));

                    // determine if another LB round exists
                    bool lbHasNext = revealedLbCount < lbOrder.Count;

                    // enable Next Round only when current LB fully resolved and more rounds exist
                    btnNextRound.Enabled = !lbUnresolved && lbHasNext;

                    // enable winner buttons if the first pending LB match exists
                    var nextMatch = randomEngine.GetMatches()
                                     .Where(m => revealedRounds.Contains(m.RoundLabel)
                                              && !randomEngine.HasWinner(m.MatchId))
                                     .OrderBy(m => GetRoundOrder(m.RoundLabel))
                                     .ThenBy(m => m.MatchId)
                                     .FirstOrDefault();
                    bool canPick = nextMatch != null;
                    btnWinner1.Enabled = canPick;
                    btnWinner2.Enabled = canPick;

                    return;
                }
            }

            // ───────────────────────────────────────────
            // OTHER RACE TYPES (unchanged)
            // ───────────────────────────────────────────
            string raceType = currentSession?.RaceType
                              ?? cmbRaceType.SelectedItem?.ToString()
                              ?? "Pro Ladder";

            bool anyUnresolved = false;
            bool moreRounds = false;

            // PRO LADDER
            if (raceType == "Pro Ladder" && engine != null)
            {
                anyUnresolved = engine.GetBracketMatches()
                                      .Where(m => revealedRounds.Contains(m.RoundLabel))
                                      .Any(m => !engine.Results.IsMatchResolved(m.MatchId));
                moreRounds = GetNextHiddenRound() != null;
            }
            // RANDOMIZED
            else if (IsRandomMode(raceType) && randomEngine != null && revealedRounds.Count > 0)
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
            // ROUND ROBIN
            else if (roundRobinEngine != null && revealedRounds.Count > 0)
            {
                anyUnresolved = roundRobinEngine.GetMatches()
                                                .Where(m => revealedRounds.Contains(m.RoundLabel))
                                                .Any(m => !roundRobinEngine.HasWinner(m.MatchId));
                moreRounds = GetNextHiddenRound() != null;
            }

            btnNextRound.Enabled = !anyUnresolved && moreRounds;

            // ▶ BUYBACK ENABLE (RR only)
            if (raceType == "Round Robin"
                && roundRobinEngine != null
                && GetNextHiddenRound() == null)
            {
                var r3Matches = roundRobinEngine.GetMatches()
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
                var currentRound = randomEngine.GetMatches()
                    .Where(m => revealedRounds.Contains(m.RoundLabel))
                    .Select(m => m.RoundLabel)
                    .Last();

                bool roundDone = randomEngine.GetMatches()
                    .Where(m => m.RoundLabel == currentRound)
                    .All(m => randomEngine.HasWinner(m.MatchId));

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
                if (roundDone)
                {
                    DisableWinnerButtons();
                    btnNextRound.Enabled = lbRevealed.Count < lbOrder.Count;
                    UpdateNextUp(); // 🩹 force reevaluation in case last match just completed
                    UpdateButtonStates();
                }
                else
                {
                    UpdateNextUp();
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

            // 2️⃣ Build the buyback pool: drivers ranked 4th or lower
            var buybackPool = standings
                .Where(r => r.Rank > 3)
                .Select(r => drivers.First(d => d.Id == r.DriverId))
                .ToList();

            // 3️⃣ Show selector
            using var sel = new BuybackDriverSelectionForm(buybackPool);
            if (sel.ShowDialog() != DialogResult.OK)
                return;

            // 4️⃣ Entrants = exactly what the user picked
            var entrants = sel.SelectedDrivers.ToList();

            // 5️⃣ Generate first-round random bracket just like standalone Random mode
            var firstRound = RandomBracket.GenerateFirstRound(entrants);

            // 6️⃣ Load into engine
            randomEngine = new RandomMatchEngine();
            randomEngine.LoadMatches(firstRound);

            // 7️⃣ Record pairings so no rematches
            currentSession.PairingHistory.UnionWith(
                firstRound
                    .Where(m => m.Seed2 != null)
                    .Select(m =>
                    {
                        int a = m.Seed1.Id, b = m.Seed2.Id;
                        return a < b ? (a, b) : (b, a);
                    })
            );

            // 8️⃣ Switch into LB mode
            inLosersPhase = true;
            revealedRounds.Clear();
            // firstRound items all share the same RoundLabel (e.g. "R1")
            revealedRounds.Add(firstRound.First().RoundLabel);

            btnNextRound.Enabled = true;

            // 9️⃣ Draw UI
            RedrawFullBracket();
            UpdateNextUp();
            UpdateWinnersList();
            UpdateButtonStates();
        }



        private void DisableWinnerButtons()
        {
            btnWinner1.Enabled = false;
            btnWinner2.Enabled = false;
        }




    }
}