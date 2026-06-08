using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.RaceEngines;
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
            if (canAdvance)
                _controller.UnlockDialIn();
            UpdateDialInButtonEnabled();
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

            _controller.PersistTournamentStats(summary, drivers, Program.ConnectionString);
        }

        // === UI Button Handlers ===

        private void btnAddDriver_Click(object sender, EventArgs e)
        {
            // Late entries are a setup-only action. Once a bracket is live the active
            // race must not gain or lose drivers (issue #254).
            if (_controller.HasBracketStarted)
            {
                MessageBox.Show(
                    "This race has already started. New drivers can be added to the driver list, but not to the active race.",
                    "Race In Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
            // Driver identity is fixed once the bracket is live — editing it on the
            // active console can desync from saved/bracket/report state (issue #254).
            if (_controller.HasBracketStarted)
            {
                MessageBox.Show(
                    "This race has already started. New drivers can be added to the driver list, but not to the active race.",
                    "Race In Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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

        private void btnSetDialIn_Click(object sender, EventArgs e)
        {
            if (lvDrivers.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a driver to edit their dial-in time.",
                    "Set Dial-In", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedName = lvDrivers.SelectedItems[0].Text;
            var driver = drivers.FirstOrDefault(d => d.Name == selectedName);
            if (driver == null)
            {
                Logger.Log($"[UI][DIALIN] Selected driver '{selectedName}' not found in driver list.");
                return;
            }

            // Round in progress: keep the lock as a guard rail but offer a safe override
            // so the director never has to close (and lose) the race to honor a late
            // dial-in change (issue #306).
            if (_controller.DialInLocked)
            {
                var choice = MessageBox.Show(
                    $"This round is in progress.\n\nEdit {driver.Name}'s dial-in anyway?\n\n" +
                    "This won't affect pairs that have already raced.",
                    "Round In Progress", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (choice != DialogResult.Yes)
                {
                    Logger.Log($"[UI][DIALIN] Locked-round override declined for '{driver.Name}'.");
                    return;
                }
                Logger.Log($"[UI][DIALIN] Locked-round override accepted for '{driver.Name}'.");
            }

            double? current = _controller.GetDriverDialIn(driver.Id);
            ShowEditDialInDialog(driver.Id, driver.Name, current);
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

            var selectedType = _controller.Session?.RaceType ?? currentSession.RaceType;
            Logger.Log($"[FORM1] GenerateBracket called with race type: {selectedType} and {drivers.Count} drivers");
            _controller.GenerateBracket(selectedType, drivers);
            btnGenerateBracket.Enabled = false;
            UpdateDriverEntryVisibility();
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
                _controller.LockDialIn();
                UpdateDialInButtonEnabled();
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
            UpdateDriverEntryVisibility();
        }

        // Save Progress and Close Race are distinct operator actions (issue #255).
        // Save Progress persists a resumable checkpoint and KEEPS the console open;
        // Close Race finalises the event and leaves the console. They share the
        // persistence path below so both write through the same repositories.

        private void btnSaveProgress_Click(object sender, EventArgs e)
        {
            if (currentSession == null)
            {
                MessageBox.Show("Quick Session has no saved file to update.", "Nothing to Save",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _controller.SaveProgress();   // captures a resumable checkpoint, keeps race open
            PersistSession();

            MessageBox.Show("Race progress saved. You can resume this race later.", "Progress Saved",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnCloseRace_Click(object sender, EventArgs e)
        {
            if (currentSession == null)
            {
                MessageBox.Show("Quick Session completed. No session file saved.");
                Close();
                return;
            }

            var confirm = MessageBox.Show(
                "Close this race? Make sure progress has been saved if you need to resume it later.",
                "Close Race", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
                return;

            _controller.SaveSession();   // capture final state into the session
            _controller.CloseRace();     // mark the event finished
            PersistSession();
            _controller.RecomputeEventsWon(drivers, Program.ConnectionString);

            if (IsHostedMode)
                HostedSaveAndCloseCompleted?.Invoke(this, EventArgs.Empty);
            else
                Close();
        }

        // Writes the current race session (and the parent multi-class event, when
        // hosted) through the repositories. Callers capture controller state into the
        // session first; this performs the single repository write per operator action.
        private void PersistSession()
        {
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
        }

        // Designer stubs
        private void txtTime_TextChanged(object sender, EventArgs e) { }
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

                    if (selectedDrivers == null || selectedDrivers.Count == 0)
                    {
                        MessageBox.Show("At least one driver must be selected.", "Invalid Selection",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Logger.Log("⚠️ [LB] Invalid buyback selection — no drivers selected.");
                        return;
                    }

                    Logger.Log($"📥 [LB] Buybacks selected: {selectedDrivers.Count} drivers → {string.Join(", ", selectedDrivers.Select(d => d.Name))}");

                    if (selectedDrivers.Count == 1)
                    {
                        // Single buyback: skip LB entirely, promote direct to Finals.
                        // GenerateLosersBracket sets _buybackChampionOverride + fires CanStartFinalsChanged(true),
                        // which enables btnGenerateBracket via OnCanStartFinalsChanged.
                        _controller.GenerateLosersBracket(selectedDrivers);
                        Logger.Log("[UI] Single buyback driver — LB skipped, Finals gate raised.");
                    }
                    else
                    {
                        _controller.SetBuybackDrivers(selectedDrivers);

                        btnGenerateBracket.Enabled = true;

                        btnGenerateLosersBracket.Enabled = true;
                        btnGenerateLosersBracket.Text = "Edit Buybacks";

                        Logger.Log("[UI] Buybacks stored. 'Generate Bracket' enabled; 'Buy Back' stays enabled for edits until LB is generated.");
                    }
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
        private void ShowEditDialInForButton(bool isLeft)
        {
            if (_currentWinnerButtonContext == null) return;
            string name    = isLeft ? _currentWinnerButtonContext.LeftName  : _currentWinnerButtonContext.RightName;
            int driverId   = isLeft ? _currentWinnerButtonContext.LeftDriverId : _currentWinnerButtonContext.RightDriverId;
            if (IsByeName(name) || driverId <= 0) return;

            double? current = _controller.GetDriverDialIn(driverId);
            ShowEditDialInDialog(driverId, name, current);
        }

        private void ShowEditDialInDialog(int driverId, string driverName, double? currentDialIn)
        {
            using (var dlg = new Form())
            {
                dlg.Text = $"Edit Dial-In — {driverName}";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(320, 120);
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;

                var lbl = new Label
                {
                    Text = "Dial-in (seconds):",
                    AutoSize = true,
                    Location = new Point(16, 18)
                };

                var txt = new TextBox
                {
                    Text = currentDialIn?.ToString("F3") ?? string.Empty,
                    Location = new Point(16, 40),
                    Width = 140
                };
                txt.SelectAll();

                var btnOk = new Button
                {
                    Text = "OK",
                    Location = new Point(16, 78),
                    Size = new Size(70, 26),
                    DialogResult = DialogResult.OK
                };

                var btnClear = new Button
                {
                    Text = "Clear",
                    Location = new Point(92, 78),
                    Size = new Size(64, 26)
                };
                btnClear.Click += (_, __) => txt.Clear();

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(224, 78),
                    Size = new Size(80, 26),
                    DialogResult = DialogResult.Cancel
                };

                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;
                dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnClear, btnCancel });

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                double? newDialIn = null;
                string val = txt.Text.Trim();
                if (!string.IsNullOrEmpty(val))
                {
                    if (!double.TryParse(val, System.Globalization.NumberStyles.Any,
                                         System.Globalization.CultureInfo.InvariantCulture, out double parsed))
                    {
                        MessageBox.Show("Invalid value — enter a number or leave blank to clear.",
                            "Invalid Dial-In", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    newDialIn = parsed;
                }

                _controller.UpdateDriverDialIn(driverId, newDialIn);

                // Refresh the winner button text
                if (_currentWinnerButtonContext != null)
                {
                    double? ld = _controller.GetDriverDialIn(_currentWinnerButtonContext.LeftDriverId);
                    double? rd = _controller.GetDriverDialIn(_currentWinnerButtonContext.RightDriverId);
                    btnWinner1.Text = _currentWinnerButtonContext.LeftName  + FormatDialIn(ld);
                    btnWinner2.Text = _currentWinnerButtonContext.RightName + FormatDialIn(rd);
                }

                RefreshDialInColumn();
            }
        }

        private void RefreshDialInColumn()
        {
            if (lvDrivers == null || lvDrivers.Columns.Count < 3) return;

            lvDrivers.BeginUpdate();
            try
            {
                foreach (ListViewItem item in lvDrivers.Items)
                {
                    var driver = drivers.FirstOrDefault(d => d.Name == item.Text);
                    if (driver == null) continue;

                    double? dialIn = _controller.GetDriverDialIn(driver.Id);
                    string text = dialIn.HasValue ? dialIn.Value.ToString("0.000") : "—";

                    while (item.SubItems.Count < 3) item.SubItems.Add(string.Empty);
                    item.SubItems[2].Text = text;
                }
            }
            finally
            {
                lvDrivers.EndUpdate();
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

        private void btnShowQRCode_Click(object sender, EventArgs e)
        {
            using (var dialog = new QRCodeDialog())
            {
                dialog.ShowDialog(this);
            }
        }

    }
}
