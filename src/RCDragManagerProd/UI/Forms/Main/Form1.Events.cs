using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RCDragManagerProd.UI.Forms
{
    public partial class Form1
    {
        // === Controller Events ===

        private void OnCanAdvanceChanged(bool canAdvance)
        {
            if (InvokeRequired) { BeginInvoke(new Action<bool>(OnCanAdvanceChanged), canAdvance); return; }
            btnNextRound.Enabled = canAdvance;
            Logger.Log($"UI: Generate Next Round button {(canAdvance ? "enabled" : "disabled")}.");
        }

        private void OnCanOfferBuybackChanged(bool enabled)
        {
            if (InvokeRequired) { BeginInvoke(new Action<bool>(OnCanOfferBuybackChanged), enabled); return; }
            btnGenerateLosersBracket.Enabled = enabled;
            btnStandings.Enabled = enabled;

            Logger.Log($"UI: Generate Losers Bracket button {(enabled ? "enabled" : "disabled")}.");

            if (enabled && !IsHostedMode)
            {
                MessageBox.Show(
                    "Round-Robin complete.\nClick 'Buy Back' to add drivers to the Losers Bracket.",
                    "Buy-Back Phase Ready",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void OnCanStartFinalsChanged(bool enabled)
        {
            if (InvokeRequired) { BeginInvoke(new Action<bool>(OnCanStartFinalsChanged), enabled); return; }
            btnGenerateBracket.Enabled = enabled;
            Logger.Log($"UI: Finals pending — Generate Bracket {(enabled ? "enabled" : "disabled")}.");

            if (enabled && !_finalsPopupShown)
            {
                _finalsPopupShown = true;
                MessageBox.Show(
                    "Losers Bracket complete.\nWinner will be added to the Finals.\n\nClick 'Generate Bracket' to start the Finals.",
                    "Finals Ready",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            if (!enabled)
                _finalsPopupShown = false;
        }

        private void OnTournamentCompleted(RaceController.RaceSummary summary)
        {
            if (InvokeRequired) { BeginInvoke(new Action<RaceController.RaceSummary>(OnTournamentCompleted), summary); return; }

            // In hosted mode MultiClassRaceForm handles stats and the completion popup.
            if (IsHostedMode) return;

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

            try
            {
                var repo = new DriverRepository(Program.ConnectionString);

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
                            else
                            {
                                Logger.Log($"[STATS] +EventsEntered skipped — driver Id={d.Id} ('{d.Name}') not found in DB (quick/local session)");
                            }
                        }
                    }
                }

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
                        Logger.Log($"[STATS] +EventsWon skipped — winner Id={winnerId} ('{summary.Winner?.Name}') not found in DB (quick/local session)");
                    }
                }

                if (summary.MatchResults != null)
                {
                    foreach (var (wId, lId) in summary.MatchResults)
                    {
                        repo.IncrementWinsAndLosses(wId, lId);
                        Logger.Log($"[STATS] +TotalWins/TotalLosses → winner={wId}, loser={lId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[STATS][ERROR] Failed to bump event stats: {ex}");
            }
        }

        // === UI Button Handlers ===

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

                var existingDriver = drivers.FirstOrDefault(d =>
                    string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));

                if (existingDriver != null)
                {
                    if (qualTime.HasValue)
                    {
                        existingDriver.QualTime = qualTime.Value;
                        Logger.Log($"[UI][ADD] Updated time for '{name}' → {qualTime.Value:0.000} (Id={existingDriver.Id}).");
                    }
                }
                else
                {
                    int newId = (drivers.Count == 0) ? 1 : drivers.Max(d => d.Id) + 1;

                    var newDriver = new Driver
                    {
                        Id = newId,
                        Name = name,
                        QualTime = qualTime
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

        private void btnGenerateBracket_Click(object sender, EventArgs e)
        {
            if (_controller.IsFinalsPending)
            {
                Logger.Log("[FORM1] Generate Bracket pressed — starting Finals…");
                _controller.StartFinals();
                btnGenerateBracket.Enabled = false;
                return;
            }

            if (_controller.IsInLosersBracketPhase)
            {
                Logger.Log("[FORM1] Starting Losers Bracket from stored buybacks...");
                _controller.StartLosersBracket();
                btnGenerateBracket.Enabled = false;
                btnGenerateLosersBracket.Enabled = false;
                return;
            }

            var selectedType = cmbRaceType.SelectedItem?.ToString();
            Logger.Log($"[FORM1] GenerateBracket called with race type: {selectedType} and {drivers.Count} drivers");
            _controller.GenerateBracket(selectedType, drivers);
            btnGenerateBracket.Enabled = false;
        }

        private void btnWinner1_Click(object sender, EventArgs e)
        {
            try
            {
                Logger.Log("[UI][CLICK] Winner1 clicked. Text='" + (btnWinner1.Text ?? "") + "' Tag=" + (btnWinner1.Tag != null ? btnWinner1.Tag.ToString() : "null") + " Enabled=" + btnWinner1.Enabled);
                HandleWinnerClick(true, btnWinner1.Tag);
            }
            catch (Exception ex)
            {
                Logger.Log("[UI][CLICK][ERROR] btnWinner1_Click failed: " + ex);
                MessageBox.Show("Winner1 click failed. Check log.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnWinner2_Click(object sender, EventArgs e)
        {
            try
            {
                Logger.Log("[UI][CLICK] Winner2 clicked. Text='" + (btnWinner2.Text ?? "") + "' Tag=" + (btnWinner2.Tag != null ? btnWinner2.Tag.ToString() : "null") + " Enabled=" + btnWinner2.Enabled);
                HandleWinnerClick(false, btnWinner2.Tag);
            }
            catch (Exception ex)
            {
                Logger.Log("[UI][CLICK][ERROR] btnWinner2_Click failed: " + ex);
                MessageBox.Show("Winner2 click failed. Check log.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void btnReset_Click(object sender, EventArgs e)
        {
            _controller.Reset();

            lvPairings.Items.Clear();
            lvWinners.Items.Clear();
            UpdateRaceQueuePanel(null);

            btnGenerateBracket.Enabled = true;
            btnNextRound.Enabled = false;
            btnStandings.Enabled = false;


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
                Close();
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

            if (_multiClassEventRepo != null && _multiClassEvent != null)
            {
                try
                {
                    _multiClassEventRepo.SaveEvent(_multiClassEvent);
                    Logger.Log("[SAVE] Multi-class event record saved.");
                }
                catch (Exception ex)
                {
                    Logger.Log($"[SAVE][ERROR] Multi-class event save failed: {ex}");
                }
            }

            try
            {
                var repo = new DriverRepository(Program.ConnectionString);
                if (drivers != null)
                {
                    foreach (var d in drivers)
                    {
                        if (d?.Id > 0)
                        {
                            var db = repo.GetDriverById(d.Id);
                            if (db != null)
                            {
                                db.EventsWon = repo.ComputeEventsWonFromSavedSessions(d.Id);
                                repo.UpdateDriver(db);
                                Logger.Log($"[STATS] Recompute EventsWon → #{db.Id} {db.Name}: {db.EventsWon}");
                            }
                            else
                            {
                                Logger.Log($"[STATS] Recompute EventsWon skipped — driver Id={d.Id} ('{d.Name}') not found in DB (quick/local session)");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[STATS][ERROR] Recompute EventsWon failed: {ex}");
            }

            MessageBox.Show("Race session saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (IsHostedMode)
                HostedSaveAndCloseCompleted?.Invoke(this, EventArgs.Empty);
            else
                Close();
        }

        // Designer stubs
        private void txtTime_TextChanged(object sender, EventArgs e) { }
        private void cmbRaceType_SelectedIndexChanged(object sender, EventArgs e) { }
        private void lblPairingsHeader_Click(object sender, EventArgs e) { }
        private void lblDriversHeader_Click(object sender, EventArgs e) { }
        private void lblWinnersHeader_Click(object sender, EventArgs e) { }
        private void lblEventTitle_Click(object sender, EventArgs e) { }
        private void lvDrivers_SelectedIndexChanged(object sender, EventArgs e) { }

        // Buybacks
        private void btnGenerateLosersBracket_Click(object sender, EventArgs e)
        {
            Logger.Log("🔁 [UI] Buybacks button clicked");

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
                        Logger.Log("🔕[LB] Buyback dialog cancelled by user.");
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
            catch (Exception ex)
            {
                Logger.Log($"[LB][ERROR] {ex}");
            }
        }

        // Edit results
        private void btnEditResult_Click(object sender, EventArgs e)
        {
            try
            {
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

                int choice = ShowWinnerPicker(match);
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

                _controller.PushNextMatch();
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][EDIT][ERROR] {ex}");
                MessageBox.Show("Failed to edit match result. See log for details.",
                    "Edit Match Result", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnStandings_Click(object sender, EventArgs e)
        {
            try
            {
                Logger.Log("[UI][CLICK] Standings clicked.");

                var shown = _controller.TryShowRoundRobinStandings();
                Logger.Log("[UI][STANDINGS] TryShowRoundRobinStandings() -> " + (shown ? "SHOWN" : "NOT AVAILABLE"));

                if (!shown)
                {
                    MessageBox.Show(
                        "Standings are not available yet.\n\nThey will be available after Round Robin is complete.",
                        "Standings Not Ready",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("[UI][STANDINGS][ERROR] " + ex);
                MessageBox.Show("Failed to show standings. Check log.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
