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
            // Unified Results panel: RR → Losers → Finals, continuous M# and headers
            _controller.WinnersUpdated += rows =>
            {
                if (lvWinners.Columns.Count == 0)
                {
                    lvWinners.View = View.Details;
                    lvWinners.Columns.Add("M#", 45, HorizontalAlignment.Left);
                    lvWinners.Columns.Add("Loser", 120, HorizontalAlignment.Left);
                    lvWinners.Columns.Add("Winner", 120, HorizontalAlignment.Left);
                    Logger.Log("[UI:Winners] Columns initialised.");
                }

                lvWinners.BeginUpdate();
                lvWinners.Items.Clear();

                // Order across ALL stages with a single key:
                //  - R1..R9 (Round Robin) first
                //  - Losers Bracket R1..Rn
                //  - SF, then F
                var ordered = rows
                    .OrderBy(w => GetGlobalRoundOrder(w.RoundLabel))
                    .ThenBy(w => w.MatchId) // tie-breaker inside the same round
                    .ToList();

                int displayNo = 1;
                string currentHeader = null;

                foreach (var w in ordered)
                {
                    // Insert a header when round changes
                    if (!string.Equals(currentHeader, w.RoundLabel, StringComparison.OrdinalIgnoreCase))
                    {
                        currentHeader = w.RoundLabel;

                        var hdr = new ListViewItem("");
                        hdr.SubItems.Add(GetFullRoundLabel(currentHeader)); // same pretty label you use on the left pane
                        hdr.SubItems.Add("");
                        hdr.BackColor = Color.LightGray;
                        hdr.Font = new Font(hdr.Font, FontStyle.Italic);
                        lvWinners.Items.Add(hdr);

                        Logger.Log($"[UI:Winners] Header added: {currentHeader}");
                    }

                    // Continuous M# across the whole event
                    var item = new ListViewItem($"M{displayNo++}");
                    item.SubItems.Add(w.Loser ?? "");
                    item.SubItems.Add(w.Winner ?? "");
                    lvWinners.Items.Add(item);

                    Logger.Log($"[UI:Winners] Row added: {item.Text}  {w.Loser ?? ""} → {w.Winner ?? ""}  [Round={w.RoundLabel}, MatchId={w.MatchId}]");
                }

                Logger.Log($"[UI:Winners] Rebuilt: total rows={lvWinners.Items.Count}, matches(numbered)={displayNo - 1}");
                lvWinners.EndUpdate();
            };


            Logger.Log("🔥 Logging system initialized");

            // ── toggle “Generate Next Round” state — with logging ───────────────
            _controller.CanAdvanceChanged += canAdvance =>
            {
                btnNextRound.Enabled = canAdvance;
                Logger.Log($"UI: Generate Next Round button {(canAdvance ? "enabled" : "disabled")}.");
            };

            // ── toggle “Generate Losers Bracket” state and popup prompt ─────────
            _controller.CanOfferBuybackChanged += enabled =>
            {
                btnGenerateLosersBracket.Enabled = enabled;
                Logger.Log($"UI: Generate Losers Bracket button {(enabled ? "enabled" : "disabled")}.");

                if (enabled)
                {
                    MessageBox.Show(
                        "Round-Robin complete.\nClick 'Buy Back' to add drivers to the Losers Bracket.",
                        "Buy-Back Phase Ready",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            };
            // ── Finals gate: enable Generate Bracket and inform RD ─────────────
            bool finalsPopupShown = false; // prevent duplicate popups if event fires again
            _controller.CanStartFinalsChanged += enabled =>
            {
                btnGenerateBracket.Enabled = enabled;
                Logger.Log($"UI: Finals pending — Generate Bracket {(enabled ? "enabled" : "disabled")}.");

                if (enabled && !finalsPopupShown)
                {
                    finalsPopupShown = true;
                    MessageBox.Show(
                        "Losers Bracket complete.\nWinner will be added to the Finals.\n\nClick 'Generate Bracket' to start the Finals.",
                        "Finals Ready",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (!enabled)
                {
                    finalsPopupShown = false;
                }
            };
            // ── Tournament complete popup (OK only; no reset/close) ─────────────
            _controller.TournamentCompleted += summary =>
            {
                var winner = summary.Winner?.Name ?? "N/A";
                var runnerUp = summary.RunnerUp?.Name ?? "N/A";

                var msg =
                    $"Event: {summary.EventName}\n" +
                    $"Bracket: {summary.Bracket}\n" +
                    $"Winner: {winner}\n" +
                    $"Runner-up: {runnerUp}\n" +
                    $"Matches: {summary.TotalMatches}";

                Logger.Log($"[UI] TournamentCompleted → Winner={winner}, RunnerUp={runnerUp}");
                MessageBox.Show(msg, "Event Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // No automatic reset/close. Leave UI as-is; Reset Race stays available.
                Logger.Log("[UI] Event Complete acknowledged (OK). Session left intact.");
            };
        }

        // Sort key for ANY round label so Results panel is globally ordered.
        // Order: RR R1..Rn (100+x) -> LB R1..Rn (200+x) -> LB Final (299) -> SF (990) -> F (1000).
        private int GetGlobalRoundOrder(string roundLabel)
        {
            if (string.IsNullOrWhiteSpace(roundLabel)) return 999;

            // Finals last
            if (string.Equals(roundLabel, "F", StringComparison.OrdinalIgnoreCase)) return 1000;
            if (string.Equals(roundLabel, "SF", StringComparison.OrdinalIgnoreCase)) return 990;

            // Losers Bracket mapping
            if (roundLabel.StartsWith("Losers Bracket", StringComparison.OrdinalIgnoreCase))
            {
                var label = roundLabel.Trim();

                // Explicit: put LB Final after all LB Rounds
                if (label.EndsWith("Final", StringComparison.OrdinalIgnoreCase))
                    return 299;

                // "Losers Bracket R{n}"
                string[] parts = label.Split(' ');
                if (parts.Length >= 3)
                {
                    string last = parts[parts.Length - 1];
                    if (last.Length >= 2 && (last[0] == 'R' || last[0] == 'r'))
                    {
                        int n;
                        if (int.TryParse(last.Substring(1), out n))
                            return 200 + n;
                    }
                }

                Logger.Log($"[UI:Winners] LB round label not recognized: '{roundLabel}' — defaulting to 290");
                return 290; // still before SF/F
            }

            // Round Robin: "R1", "R2", ...
            if (roundLabel.Length >= 2 && (roundLabel[0] == 'R' || roundLabel[0] == 'r'))
            {
                int n;
                if (int.TryParse(roundLabel.Substring(1), out n))
                    return 100 + n;
            }

            // Safety: handle spelled-out headers if they ever slip through
            if (roundLabel.StartsWith("Semi", StringComparison.OrdinalIgnoreCase)) return 990;
            if (roundLabel.StartsWith("Final", StringComparison.OrdinalIgnoreCase)) return 1000;

            Logger.Log($"[UI:Winners] Unrecognized round label for ordering: '{roundLabel}' — defaulting to 800");
            return 800;
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
            // ── refresh list view ─────────────────────────────────────────────
            lvDrivers.Items.Clear();
            foreach (var d in drivers.OrderBy(d => d.QualTime))
            {
                var item = new ListViewItem(d.Name);
                item.SubItems.Add((d.QualTime ?? 0.0).ToString("0.000"));
                lvDrivers.Items.Add(item);
            }

            // ── NEW: toggle “Generate Bracket” availability ───────────────────
            bool canGenerate = drivers.Count >= 2 && !_controller.HasBracketStarted;
            btnGenerateBracket.Enabled = canGenerate;
            Logger.Log($"[UI] Generate Bracket {(canGenerate ? "ENABLED" : "disabled")} — drivers={drivers.Count}");
        }


        private void btnGenerateBracket_Click(object sender, EventArgs e)
        {
            // ── Finals start path (gated) ───────────────────────────────────
            if (_controller.IsFinalsPending)
            {
                Logger.Log("[FORM1] Generate Bracket pressed — starting Finals…");
                _controller.StartFinals();
                btnGenerateBracket.Enabled = false;
                return;
            }

            // ── Losers-Bracket path ─────────────────────────────────────────
            if (_controller.IsInLosersBracketPhase)
            {
                Logger.Log("[FORM1] Starting Losers Bracket from stored buybacks...");
                _controller.StartLosersBracket();
                btnGenerateBracket.Enabled = false;
                btnGenerateLosersBracket.Enabled = false;
                return;
            }

            // ── Initial bracket build ───────────────────────────────────────
            var selectedType = cmbRaceType.SelectedItem?.ToString();
            Logger.Log($"[FORM1] GenerateBracket called with race type: {selectedType} and {drivers.Count} drivers");
            _controller.GenerateBracket(selectedType, drivers);
            btnGenerateBracket.Enabled = false;
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
            // ── guard ────────────────────────────────────────────────────────
            if (rows == null)
            {
                Logger.Log("[UI] RedrawFullBracket called with rows=null");
                return;
            }
            Logger.Log($"[UI] RedrawFullBracket: incoming rows={rows.Count}");

            // ── one-time ListView setup ──────────────────────────────────────
            if (lvPairings.Columns.Count == 0)
            {
                lvPairings.View = View.Details;
                lvPairings.FullRowSelect = true;
                lvPairings.Columns.Add("M#", 45, HorizontalAlignment.Left);
                lvPairings.Columns.Add("Driver 1", 100, HorizontalAlignment.Left);
                lvPairings.Columns.Add("Driver 2", 100, HorizontalAlignment.Left);
                Logger.Log("[UI] lvPairings columns initialised (M#, Driver1, Driver2)");
            }

            lvPairings.BeginUpdate();
            lvPairings.Items.Clear();

            int added = 0;
            foreach (var row in rows)
            {
                if (row == null) continue;

                if (row.IsHeader)
                {
                    // round header row
                    string label = GetFullRoundLabel(row.RoundLabel);
                    var header = new ListViewItem(string.Empty);
                    header.SubItems.Add(label);
                    header.SubItems.Add(string.Empty);
                    header.BackColor = Color.LightGray;
                    header.Font = new Font(header.Font, FontStyle.Italic);
                    lvPairings.Items.Add(header);
                    Logger.Log($"[UI] Header added: {label}");
                    continue;
                }

                // normal match row
                string mLabel = !string.IsNullOrEmpty(row.MatchNumber) ? row.MatchNumber : $"M{row.MatchId}";
                string d1 = row.Driver1 ?? "BYE";
                string d2 = row.Driver2 ?? "BYE";

                var item = new ListViewItem(mLabel);
                item.SubItems.Add(d1);
                item.SubItems.Add(d2);
                lvPairings.Items.Add(item);
                added++;

                Logger.Log($"[UI] Row added: {mLabel}  {d1} vs {d2}  [Round={row.RoundLabel}, MatchId={row.MatchId}]");
            }

            lvPairings.EndUpdate();
            Logger.Log($"[UI] Redraw complete: headers+rows total={lvPairings.Items.Count}, matches added={added}");
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
            Logger.Log("🔁 [UI] Buybacks button clicked");

            btnGenerateLosersBracket.Enabled = false;   // prevent double-click

            var eligible = _controller.GetEligibleBuybackDrivers();

            if (eligible == null || eligible.Count < 2)
            {
                MessageBox.Show("Not enough eligible drivers for a Losers Bracket.", "No Entries", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logger.Log($"⚠️ [LB] Only {eligible?.Count ?? 0} eligible buyback drivers — bracket not created.");
                return;
            }

            using (var dlg = new BuybackDriverSelectionForm(eligible))
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    Logger.Log("🔕 [LB] Buyback dialog cancelled by user.");
                    return;
                }

                var selectedDrivers = dlg.SelectedDrivers;

                if (selectedDrivers == null || selectedDrivers.Count < 2)
                {
                    MessageBox.Show("At least two drivers must be selected.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Logger.Log($"⚠️ [LB] Invalid buyback selection — {selectedDrivers?.Count ?? 0} drivers selected.");
                    return;
                }

                Logger.Log($"📥 [LB] Buybacks selected: {selectedDrivers.Count} drivers → {string.Join(", ", selectedDrivers.Select(d => d.Name))}");

                // ✅ Store the drivers, but do not start the bracket yet
                _controller.SetBuybackDrivers(selectedDrivers);

                // ✅ Enable the "Generate Bracket" button so the race director can manually start
                btnGenerateBracket.Enabled = true;
                Logger.Log("[UI] Generate Bracket button enabled for Losers Bracket start.");
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
        private string GetFullRoundLabel(string label)
        {
            switch (label)
            {
                case "R1": return "Round 1";
                case "R2": return "Round 2";
                case "R3": return "Round 3";
                case "R4": return "Round 4";
                case "QF": return "Quarterfinals";
                case "SF": return "Semi-Finals";
                case "F": return "Final";
                case "LBF": return "Losers Bracket Final";
                default: return label;
            }
        }




    }
}