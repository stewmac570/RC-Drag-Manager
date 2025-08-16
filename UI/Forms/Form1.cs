using RCDragManagerProd.Controllers;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories; // Assuming DriverRepository is defined here

namespace RCDragManagerProd.UI.Forms
{
    public partial class Form1 : Form
    {
        private List<Driver> drivers = new List<Driver>();
        private RaceSession currentSession;             // (optional for Quick Session)
        private RaceSessionRepository sessionRepository = new RaceSessionRepository("race_data.db");  // (optional)
        private readonly RaceController _controller;

        public Form1(RaceController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            InitializeComponent(); // Designer owns all UI

            btnEditResult.Click += btnEditResult_Click;

            currentSession = _controller.Session;

            lblEventTitle.Text = currentSession != null
                ? $"Event: {currentSession.EventName}"
                : "Quick Session";

            if (currentSession != null && currentSession.DriverEntries != null && currentSession.DriverEntries.Count > 0)
            {
                drivers = currentSession.DriverEntries
                    .Select(e => new Driver
                    {
                        Id = e.DriverID,
                        Name = e.DriverName,
                        QualTime = e.QualifyingTime
                    })
                    .ToList();

                Logger.Log($"[CREATE] Hydrated {drivers.Count} drivers from RaceSession.DriverEntries.");

                if (!string.IsNullOrWhiteSpace(currentSession.RaceType) && cmbRaceType != null)
                {
                    try { cmbRaceType.SelectedItem = currentSession.RaceType; } catch { /* ignore */ }
                    Logger.Log($"[CREATE] Restored RaceType on UI: '{currentSession.RaceType}'");
                }

                UpdateDriverList();
                btnGenerateBracket.Enabled = true;
            }

            btnNextRound.Enabled = false;   // always disabled on load

            // Controller event hooks:
            _controller.BracketRedrawn += RedrawFullBracket;

            _controller.NextMatchReady += OnNextMatchReady;


            _controller.WinnersUpdated += rows =>
            {
                // Designer owns columns; just rebuild items
                lvWinners.BeginUpdate();
                lvWinners.Items.Clear();

                var ordered = rows
                    .OrderBy(w => GetGlobalRoundOrder(w.RoundLabel))
                    .ThenBy(w => w.MatchId)
                    .ToList();

                int displayNo = 1;
                string currentHeader = null;

                foreach (var w in ordered)
                {
                    if (!string.Equals(currentHeader, w.RoundLabel, StringComparison.OrdinalIgnoreCase))
                    {
                        currentHeader = w.RoundLabel;

                        var hdr = new ListViewItem("");
                        hdr.SubItems.Add(GetFullRoundLabel(currentHeader));
                        hdr.SubItems.Add("");
                        hdr.BackColor = Color.LightGray;
                        hdr.Font = new Font(hdr.Font, FontStyle.Italic);
                        hdr.Tag = null; // <<< header row (NOT a match)
                        lvWinners.Items.Add(hdr);

                        Logger.Log($"[UI:Winners] Header added: {currentHeader}");
                    }

                    var item = new ListViewItem($"M{displayNo++}");
                    item.SubItems.Add(w.Loser ?? "");
                    item.SubItems.Add(w.Winner ?? "");
                    item.Tag = w.MatchId; // <<< store real MatchId for edit
                    lvWinners.Items.Add(item);

                    Logger.Log($"[UI:Winners] Row added: {item.Text}  {w.Loser ?? ""} → {w.Winner ?? ""}  [Round={w.RoundLabel}, MatchId={w.MatchId}]");
                }

                Logger.Log($"[UI:Winners] Rebuilt: total rows={lvWinners.Items.Count}, matches(numbered)={displayNo - 1}");
                lvWinners.EndUpdate();
            };


            _controller.CanAdvanceChanged += canAdvance =>
            {
                btnNextRound.Enabled = canAdvance;
                Logger.Log($"UI: Generate Next Round button {(canAdvance ? "enabled" : "disabled")}.");
            };

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

            bool finalsPopupShown = false;
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

            // ── Tournament complete popup + stats bump ─────────────────────────────
            _controller.TournamentCompleted += summary =>
            {
                var winnerName = summary.Winner?.Name ?? "N/A";
                var runnerUp = summary.RunnerUp?.Name ?? "N/A";

                var msg =
                    $"Event: {summary.EventName}\n" +
                    $"Bracket: {summary.Bracket}\n" +
                    $"Winner: {winnerName}\n" +
                    $"Runner-up: {runnerUp}\n" +
                    $"Matches: {summary.TotalMatches}";

                Logger.Log($"[UI] TournamentCompleted → Winner={winnerName}, RunnerUp={runnerUp}");
                MessageBox.Show(msg, "Event Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logger.Log("[UI] Event Complete acknowledged (OK). Session left intact.");

                // ---- stats bump ----------------------------------------------------
                try
                {
                    var repo = new DriverRepository("race_data.db");

                    // 1) Everyone who raced gets +1 EventsEntered (if they exist in DB)
                    if (drivers != null)
                    {
                        foreach (var d in drivers)
                        {
                            if (d?.Id > 0)
                            {
                                var db = repo.GetDriverById(d.Id);
                                if (db != null)
                                {
                                    db.EventsEntered += 1;
                                    repo.UpdateDriver(db);
                                    Logger.Log($"[STATS] +EventsEntered → #{db.Id} {db.Name}: {db.EventsEntered}");
                                }
                            }
                        }
                    }

                    // 2) Winner gets +1 EventsWon
                    var winnerId = summary.Winner?.Id ?? 0;
                    if (winnerId > 0)
                    {
                        var wdb = repo.GetDriverById(winnerId);
                        if (wdb != null)
                        {
                            wdb.EventsWon += 1;
                            repo.UpdateDriver(wdb);
                            Logger.Log($"[STATS] +EventsWon → #{wdb.Id} {wdb.Name}: {wdb.EventsWon}");
                        }
                        else
                        {
                            Logger.Log($"[STATS][WARN] Winner id {winnerId} not found in DB; EventsWon not bumped.");
                        }
                    }
                    else
                    {
                        Logger.Log("[STATS][WARN] Summary had no winner id; EventsWon not bumped.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[STATS][ERROR] Failed to bump event stats: {ex}");
                }
            };
        }

        // Global order for results panel
        private int GetGlobalRoundOrder(string roundLabel)
        {
            if (string.IsNullOrWhiteSpace(roundLabel)) return 999;
            if (string.Equals(roundLabel, "F", StringComparison.OrdinalIgnoreCase)) return 1000;
            if (string.Equals(roundLabel, "SF", StringComparison.OrdinalIgnoreCase)) return 990;

            if (roundLabel.StartsWith("Losers Bracket", StringComparison.OrdinalIgnoreCase))
            {
                var label = roundLabel.Trim();
                if (label.EndsWith("Final", StringComparison.OrdinalIgnoreCase))
                    return 299;

                string[] parts = label.Split(' ');
                if (parts.Length >= 3)
                {
                    string last = parts[parts.Length - 1];
                    if (last.Length >= 2 && (last[0] == 'R' || last[0] == 'r'))
                    {
                        if (int.TryParse(last.Substring(1), out int n))
                            return 200 + n;
                    }
                }

                Logger.Log($"[UI:Winners] LB round label not recognized: '{roundLabel}' — defaulting to 290");
                return 290;
            }

            if (roundLabel.Length >= 2 && (roundLabel[0] == 'R' || roundLabel[0] == 'r'))
            {
                if (int.TryParse(roundLabel.Substring(1), out int n))
                    return 100 + n;
            }

            if (roundLabel.StartsWith("Semi", StringComparison.OrdinalIgnoreCase)) return 990;
            if (roundLabel.StartsWith("Final", StringComparison.OrdinalIgnoreCase)) return 1000;

            Logger.Log($"[UI:Winners] Unrecognized round label for ordering: '{roundLabel}' — defaulting to 800");
            return 800;
        }

        private void btnAddDriver_Click(object sender, EventArgs e)
        {
            try
            {
                string name = (txtName.Text ?? "").Trim();
                string timeText = (txtTime.Text ?? "").Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Enter a driver name.");
                    return;
                }

                if (drivers == null) drivers = new List<Driver>();

                // parse optional time
                double? qualTime = null;
                if (!string.IsNullOrWhiteSpace(timeText))
                {
                    if (double.TryParse(timeText, out var parsed))
                        qualTime = parsed;
                    else
                    {
                        MessageBox.Show("Qualifying time is invalid. Leave it blank or enter a number.");
                        return;
                    }
                }

                // Find existing by name (case-insensitive)
                var existingDriver = drivers.FirstOrDefault(d =>
                    string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));

                if (existingDriver != null)
                {
                    if (qualTime.HasValue)
                    {
                        existingDriver.QualTime = qualTime.Value;
                        Logger.Log($"[UI][ADD] Updated time for '{name}' → {qualTime.Value:0.000} (Id={existingDriver.Id}).");
                    }
                    else
                    {
                        Logger.Log($"[UI][ADD] Name '{name}' exists; no time provided — leaving time unchanged.");
                    }
                }
                else
                {
                    // Ensure a unique Id per session
                    int newId = (drivers.Count == 0) ? 1 : drivers.Max(d => d.Id) + 1;

                    var newDriver = new Driver
                    {
                        Id = newId,
                        Name = name,
                        QualTime = qualTime // may be null
                    };

                    drivers.Add(newDriver);
                    var tmsg = qualTime.HasValue ? qualTime.Value.ToString("0.000") : "—";
                    Logger.Log($"[UI][ADD] Added driver Id={newId}, Name='{name}', Qual={tmsg}.");
                }

                UpdateDriverList();
                txtName.Clear();
                txtTime.Clear();
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][ADD][ERROR] {ex}");
                MessageBox.Show("Failed to add driver. See log for details.");
            }
        }



        private void btnEditDriver_Click(object sender, EventArgs e)
        {
            if (lvDrivers.SelectedItems.Count > 0)
            {
                string selectedName = lvDrivers.SelectedItems[0].Text;
                var driver = drivers.FirstOrDefault(d => d.Name == selectedName);
                if (driver != null)
                {
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
            lvDrivers.BeginUpdate();
            lvDrivers.Items.Clear();

            // Show timed first (ascending), then no-time drivers at the bottom
            var ordered = drivers
                .OrderBy(d => d.QualTime.HasValue ? 0 : 1)
                .ThenBy(d => d.QualTime ?? double.MaxValue)
                .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var d in ordered)
            {
                var item = new ListViewItem(d.Name);
                string timeText = d.QualTime.HasValue ? d.QualTime.Value.ToString("0.000") : "—";
                item.SubItems.Add(timeText);
                lvDrivers.Items.Add(item);
            }

            lvDrivers.EndUpdate();

            bool canGenerate = drivers.Count >= 2 && !_controller.HasBracketStarted;
            btnGenerateBracket.Enabled = canGenerate;
            Logger.Log($"[UI] Driver list updated ({drivers.Count}); Generate Bracket {(canGenerate ? "ENABLED" : "disabled")}.");
        }


        private void btnGenerateBracket_Click(object sender, EventArgs e)
        {
            // Finals start
            if (_controller.IsFinalsPending)
            {
                Logger.Log("[FORM1] Generate Bracket pressed — starting Finals…");
                _controller.StartFinals();
                btnGenerateBracket.Enabled = false;
                return;
            }

            // Losers Bracket start
            if (_controller.IsInLosersBracketPhase)
            {
                Logger.Log("[FORM1] Starting Losers Bracket from stored buybacks...");
                _controller.StartLosersBracket();
                btnGenerateBracket.Enabled = false;
                btnGenerateLosersBracket.Enabled = false;
                return;
            }

            // Initial bracket build
            var selectedType = cmbRaceType.SelectedItem?.ToString();
            Logger.Log($"[FORM1] GenerateBracket called with race type: {selectedType} and {drivers.Count} drivers");
            _controller.GenerateBracket(selectedType, drivers);
            btnGenerateBracket.Enabled = false;
        }
        private void btnWinner1_Click(object sender, EventArgs e)
        {
            HandleWinnerClick(firstOption: true, btnWinner1.Tag);
        }

        private void btnWinner2_Click(object sender, EventArgs e)
        {
            HandleWinnerClick(firstOption: false, btnWinner2.Tag);
        }

        // --- helper (kept private inside Form1) ---
        private void HandleWinnerClick(bool firstOption, object tag)
        {
            if (tag is not int matchId) return;

            // capture winner BEFORE commit to avoid double-bumps
            var beforeWinner = _controller.GetWinner(matchId);

            _controller.SubmitWinner(matchId, firstOption);     // commit result

            var match = _controller.GetMatch(matchId);
            var winner = _controller.GetWinner(matchId);
            var loser = _controller.GetLoser(matchId);

            string round = match?.RoundLabel ?? "Unknown";
            string wName = winner?.Name ?? "BYE/Unknown";
            string lName = loser?.Name ?? "BYE/Unknown";

            // BYE or unresolved → no stat changes
            if (winner == null || loser == null || IsByeName(wName) || IsByeName(lName))
            {
                Logger.Log($"[STATS] Skip: BYE/unresolved for M{matchId} ({round}).");
                Logger.Log($"[RESULT] Match {matchId} ({round}): {wName} defeated {lName}");
                _controller.PushNextMatch();
                return;
            }

            // only bump if the winner actually changed (prevents repeat clicks / edits)
            if (beforeWinner == null || beforeWinner.Id != winner.Id)
            {
                try
                {
                    // Wins/Losses
                    var repo = new DriverRepository("race_data.db");
                    var wDb = repo.GetDriverById(winner.Id);
                    var lDb = repo.GetDriverById(loser.Id);

                    if (wDb != null)
                    {
                        wDb.TotalWins += 1;
                        repo.UpdateDriver(wDb);
                    }
                    if (lDb != null)
                    {
                        lDb.TotalLosses += 1;
                        repo.UpdateDriver(lDb);
                    }
                    Logger.Log($"[STATS] +Win {winner.Id} / +Loss {loser.Id}  (W={wDb?.TotalWins}, L={lDb?.TotalLosses})");

                    // Finals → EventsWon++
                    if (string.Equals(round, "F", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(round, "Final", StringComparison.OrdinalIgnoreCase))
                    {
                        if (wDb != null)
                        {
                            wDb.EventsWon += 1;
                            repo.UpdateDriver(wDb);
                            Logger.Log($"[STATS] +EventsWon (Final) → #{wDb.Id} {wDb.Name}: {wDb.EventsWon}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[STATS][ERROR] Persist failed for M{matchId}: {ex}");
                }
            }
            else
            {
                Logger.Log($"[STATS] No change (same winner) for M{matchId} ({round}).");
            }

            Logger.Log($"[RESULT] Match {matchId} ({round}): {wName} defeated {lName}");
            _controller.PushNextMatch();   // refresh current/next display
        }



        private void UpdateDriverStats(Driver winner, Driver loser)
        {
            try
            {
                if (winner == null || loser == null)
                {
                    Logger.Log("[STATS] Skip: winner/loser null.");
                    return;
                }

                // Skip BYE results (don’t count)
                if (string.Equals((winner.Name ?? "").Trim(), "BYE", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals((loser.Name ?? "").Trim(), "BYE", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log("[STATS] Skip: BYE in matchup.");
                    return;
                }

                // Must exist in DB (Quick Session drivers might not)
                var repo = new DriverRepository("race_data.db");
                var wdb = repo.GetDriverById(winner.Id);
                var ldb = repo.GetDriverById(loser.Id);

                if (wdb == null || ldb == null)
                {
                    Logger.Log($"[STATS] Skip: DB lookup failed (winnerId={winner.Id}→{(wdb != null)}, loserId={loser.Id}→{(ldb != null)}).");
                    return;
                }

                wdb.TotalWins += 1;
                ldb.TotalLosses += 1;
                repo.UpdateDriver(wdb);
                repo.UpdateDriver(ldb);

                Logger.Log($"[STATS] +Win {wdb.Name} / +Loss {ldb.Name}  (W={wdb.TotalWins}, L={ldb.TotalLosses})");
            }
            catch (Exception ex)
            {
                Logger.Log($"[STATS][ERROR] UpdateDriverStats failed: {ex}");
            }
        }


        private void btnNextRound_Click(object sender, EventArgs e)
        {
            if (!btnNextRound.Enabled) return;

            try
            {
                btnNextRound.Enabled = false;
                Logger.Log("[FORM1] Generate Next Round clicked");
                _controller.AdvanceRound();
            }
            catch (Exception ex)
            {
                Logger.Log($"[FORM1][ERROR] AdvanceRound failed: {ex.Message}");
                MessageBox.Show("Failed to advance the round. Check the log for details.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Logger.Log("[FORM1] AdvanceRound() completed");
            }
        }

        private void RedrawFullBracket(IReadOnlyList<PairingRow> rows)
        {
            if (rows == null)
            {
                Logger.Log("[UI] RedrawFullBracket called with rows=null");
                return;
            }
            Logger.Log($"[UI] RedrawFullBracket: incoming rows={rows.Count}");

            // Designer owns columns; only update items
            lvPairings.BeginUpdate();
            lvPairings.Items.Clear();

            int added = 0;
            foreach (var row in rows)
            {
                if (row == null) continue;

                if (row.IsHeader)
                {
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
            btnNextRound.Enabled = false;

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

            _controller.SaveSession();
            sessionRepository.SaveSession(currentSession);

            // --- Recompute EventsWon from all saved sessions, then persist ---
            try
            {
               
            }
            catch (Exception ex)
            {
                Logger.Log($"[STATS][ERROR] Recompute EventsWon failed: {ex}");
            }


            MessageBox.Show("Race session saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
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

        private void btnGenerateLosersBracket_Click(object sender, EventArgs e)
        {
            Logger.Log("🔁 [UI] Buybacks button clicked");
            btnGenerateLosersBracket.Enabled = false;

            try
            {
                var eligible = _controller.GetEligibleBuybackDrivers();

                if (eligible == null || eligible.Count < 2)
                {
                    MessageBox.Show("Not enough eligible drivers for a Losers Bracket.", "No Entries",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Logger.Log($"⚠️ [LB] Only {eligible?.Count ?? 0} eligible buyback drivers — bracket not created.");
                    return;
                }

                using (var dlg = new BuybackDriverSelectionForm(eligible))
                {
                    var dr = dlg.ShowDialog();

                    if (dr != DialogResult.OK)
                    {
                        Logger.Log("🔕 [LB] Buyback dialog cancelled by user.");
                        return;
                    }

                    var selectedDrivers = dlg.SelectedDrivers;

                    if (selectedDrivers == null || selectedDrivers.Count < 2)
                    {
                        MessageBox.Show("At least two drivers must be selected.", "Invalid Selection",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Logger.Log($"⚠️ [LB] Invalid buyback selection — {selectedDrivers?.Count ?? 0} drivers selected.");
                        return;
                    }

                    Logger.Log($"📥 [LB] Buybacks selected: {selectedDrivers.Count} drivers → {string.Join(", ", selectedDrivers.Select(d => d.Name))}");

                    _controller.SetBuybackDrivers(selectedDrivers);

                    btnGenerateBracket.Enabled = true;
                    btnGenerateLosersBracket.Enabled = true;
                    btnGenerateLosersBracket.Text = "Edit Buybacks";

                    Logger.Log("[UI] Buybacks stored. 'Generate Bracket' enabled; 'Buy Back' stays enabled for edits until LB is generated.");
                }
            }
            finally
            {
                if (!btnGenerateBracket.Enabled)
                    btnGenerateLosersBracket.Enabled = true;
            }
        }

        private void txtTime_TextChanged(object sender, EventArgs e)
        {

        }

        private void cmbRaceType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lblPairingsHeader_Click(object sender, EventArgs e)
        {

        }

        private void lblDriversHeader_Click(object sender, EventArgs e)
        {

        }

        private void lblWinnersHeader_Click(object sender, EventArgs e)
        {

        }

        private void lblEventTitle_Click(object sender, EventArgs e)
        {

        }

        private void lvDrivers_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        // Buttons show CURRENT; label shows NEXT TWO (names only) + logging.
        private void OnNextMatchReady(PairingRow row)
        {
            if (row == null)
            {
                lblNext.AutoSize = false;
                lblNext.TextAlign = ContentAlignment.MiddleCenter;
                //lblNext.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
                lblNext.Text = "No match ready";
                btnWinner1.Enabled = false;
                btnWinner2.Enabled = false;
                Logger.Log("[UI][NEXT] No current match.");
                return;
            }

            // Buttons = current matchup
            btnWinner1.Text = row.Driver1;
            btnWinner2.Text = row.Driver2;
            btnWinner1.Tag = row.MatchId;
            btnWinner2.Tag = row.MatchId;

            // BYE guard for buttons
            btnWinner1.Enabled = !string.Equals(row.Driver1?.Trim(), "BYE", StringComparison.OrdinalIgnoreCase);
            btnWinner2.Enabled = !string.Equals(row.Driver2?.Trim(), "BYE", StringComparison.OrdinalIgnoreCase);

            // Label = next two matchups (names only)
            var upcoming = _controller.PeekUpcomingMatches(3)
                                      .Where(m => m.MatchId != row.MatchId)
                                      .Take(2)
                                      .ToList();

            lblNext.AutoSize = false;
            lblNext.TextAlign = ContentAlignment.MiddleCenter;
            //lblNext.Font = new Font("Segoe UI", 10f, FontStyle.Regular);

            string text;
            if (upcoming.Count == 0)
            {
                text = $"{row.Driver1} vs {row.Driver2}";
            }
            else if (upcoming.Count == 1)
            {
                text = $"On Deck — {FormatMatchForNext(upcoming[0])}";
            }
            else
            {
                text = $"On Deck — {FormatMatchForNext(upcoming[0])}" +
                       Environment.NewLine +
                       $"In The Hole — {FormatMatchForNext(upcoming[1])}";
            }

            lblNext.Text = text;

            Logger.Log($"[UI][NEXT] Current=M{row.MatchId}:{row.Driver1} vs {row.Driver2} | Label='{text.Replace(Environment.NewLine, " / ")}'");
        }
        private static string FormatMatchForNext(EngineMatch m)
        {
            string n1 = m.Driver1?.Name ?? "BYE";
            string n2 = m.Driver2?.Name ?? "BYE";
            return $"M{m.MatchId}: {n1} vs {n2}";
        }
        private void btnEditResult_Click(object sender, EventArgs e)
        {
            try
            {
                // Must select a real match row (not a round header) in Winners list
                if (lvWinners.SelectedItems.Count == 0 || !(lvWinners.SelectedItems[0].Tag is int matchId))
                {
                    MessageBox.Show("Select a match in the Winners list to edit.", "Edit Match Result",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Logger.Log("[UI][EDIT] No match selected in Winners list (or header row selected).");
                    return;
                }

                var match = _controller.GetMatch(matchId);
                if (match == null)
                {
                    MessageBox.Show("Match not found.", "Edit Match Result",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Logger.Log($"[UI][EDIT] GetMatch({matchId}) returned null.");
                    return;
                }

                if (!match.HasResult)
                {
                    MessageBox.Show("That match has not run yet.", "Edit Match Result",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Logger.Log($"[UI][EDIT] Reject — M{matchId} has no result yet.");
                    return;
                }

                if (!_controller.IsMatchInActiveRound(matchId))
                {
                    MessageBox.Show("You can only change results for the ACTIVE round.", "Edit Match Result",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Logger.Log($"[UI][EDIT] Reject — M{matchId} not in active round.");
                    return;
                }

                // Show clear two-button chooser
                int choice = ShowWinnerPicker(match); // 1 = Driver1, 2 = Driver2, 0 = cancel
                if (choice == 0)
                {
                    Logger.Log($"[UI][EDIT] Winner picker cancelled for M{matchId}.");
                    return;
                }

                bool setFirst = (choice == 1);
                var d1 = match.Driver1?.Name ?? "BYE";
                var d2 = match.Driver2?.Name ?? "BYE";

                var ok = _controller.EditWinnerInActiveRound(matchId, setFirst);
                Logger.Log($"[UI][EDIT] Set winner {(setFirst ? d1 : d2)} for M{matchId} → {(ok ? "OK" : "REJECTED")}");

                if (!ok)
                {
                    MessageBox.Show("Edit rejected. Only active-round matches can be changed and BYE cannot be a winner.",
                        "Edit Match Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _controller.PushNextMatch(); // refresh current/next display
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][EDIT][ERROR] {ex}");
                MessageBox.Show("Failed to edit match result. See log for details.",
                    "Edit Match Result", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        // Rebuild winners list; every real match row stores its MatchId in ListViewItem.Tag
        private void OnWinnersUpdated(List<WinnerRow> rows)
        {
            // columns (safe to run once)
            if (lvWinners.Columns.Count == 0)
            {
                lvWinners.View = View.Details;
                lvWinners.Columns.Add("M#", 45, HorizontalAlignment.Left);
                lvWinners.Columns.Add("Loser", 170, HorizontalAlignment.Left);
                lvWinners.Columns.Add("Winner", 170, HorizontalAlignment.Left);
            }

            lvWinners.BeginUpdate();
            lvWinners.Items.Clear();

            var ordered = rows
                .OrderBy(w => w.RoundLabel)   // engine order is fine; we only need stable grouping
                .ThenBy(w => w.MatchId)
                .ToList();

            string currentHeader = null;
            int displayNo = 1;

            foreach (var w in ordered)
            {
                if (!string.Equals(currentHeader, w.RoundLabel, StringComparison.OrdinalIgnoreCase))
                {
                    currentHeader = w.RoundLabel;

                    var hdr = new ListViewItem("");     // header row (not a match)
                    hdr.SubItems.Add(currentHeader);
                    hdr.SubItems.Add("");
                    hdr.Tag = null;                     // <<< headers have NO Tag
                    hdr.BackColor = Color.LightGray;
                    hdr.Font = new Font(lvWinners.Font, FontStyle.Italic);
                    lvWinners.Items.Add(hdr);

                    Logger.Log($"[UI:Winners] Header: {currentHeader}");
                }

                var item = new ListViewItem($"M{displayNo++}");
                item.SubItems.Add(w.Loser ?? "");
                item.SubItems.Add(w.Winner ?? "");
                item.Tag = w.MatchId;                   // <<< critical: store real MatchId
                lvWinners.Items.Add(item);

                Logger.Log($"[UI:Winners] Row M{w.MatchId}: {w.Loser ?? ""} → {w.Winner ?? ""}");
            }

            lvWinners.EndUpdate();
            Logger.Log($"[UI:Winners] Rebuilt {lvWinners.Items.Count} items.");
        }
        private static bool IsByeName(string name)
    => string.Equals((name ?? "").Trim(), "BYE", StringComparison.OrdinalIgnoreCase);

        // Returns 1 (Driver1), 2 (Driver2), or 0 (Cancel)
        private int ShowWinnerPicker(EngineMatch match)
        {
            string n1 = match.Driver1?.Name ?? "BYE";
            string n2 = match.Driver2?.Name ?? "BYE";

            using (var dlg = new Form())
            {
                dlg.Text = $"Edit Result — M{match.MatchId} ({match.RoundLabel})";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(440, 190);
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.Font = this.Font; // keep designer font
                dlg.KeyPreview = true;

                var lbl = new Label
                {
                    Text = "Choose the correct winner:",
                    AutoSize = true,
                    Location = new Point(16, 16)
                };

                var btn1 = new Button
                {
                    Text = $"Set Winner: {n1}",
                    Location = new Point(16, 50),
                    Size = new Size(408, 40),
                    Enabled = !IsByeName(n1)
                };
                btn1.Click += (_, __) => { dlg.Tag = 1; dlg.DialogResult = DialogResult.OK; };

                var btn2 = new Button
                {
                    Text = $"Set Winner: {n2}",
                    Location = new Point(16, 95),
                    Size = new Size(408, 40),
                    Enabled = !IsByeName(n2)
                };
                btn2.Click += (_, __) => { dlg.Tag = 2; dlg.DialogResult = DialogResult.OK; };

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(344, 145),
                    Size = new Size(80, 28),
                    DialogResult = DialogResult.Cancel
                };

                // Keyboard shortcuts: 1 or 2
                dlg.KeyDown += (s, e) =>
                {
                    if ((e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1) && btn1.Enabled)
                    { dlg.Tag = 1; dlg.DialogResult = DialogResult.OK; }
                    else if ((e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2) && btn2.Enabled)
                    { dlg.Tag = 2; dlg.DialogResult = DialogResult.OK; }
                };

                dlg.Controls.AddRange(new Control[] { lbl, btn1, btn2, btnCancel });
                dlg.CancelButton = btnCancel;

                Logger.Log($"[UI][EDIT] Winner picker open: M{match.MatchId} — '{n1}' vs '{n2}'.");

                var dr = dlg.ShowDialog(this);
                int choice = (dr == DialogResult.OK && dlg.Tag is int c) ? c : 0;

                Logger.Log($"[UI][EDIT] Winner picker close: result={dr}, choice={choice}.");
                return choice;
            }
        }
        private void BumpEventWon(Driver winner)
        {
            try
            {
                if (winner == null) return;
                var repo = new DriverRepository("race_data.db");
                var db = repo.GetDriverById(winner.Id);
                if (db != null)
                {
                    db.EventsWon += 1;
                    repo.UpdateDriver(db);
                    Logger.Log($"[STATS] +EventsWon (Final) → #{db.Id} {db.Name}: {db.EventsWon}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[STATS][ERROR] BumpEventWon failed: {ex}");
            }
        }



    }
}
