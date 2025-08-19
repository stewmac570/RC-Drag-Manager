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
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    public partial class Form1 : Form
    {
        private List<Driver> drivers = new List<Driver>();
        private RaceSession currentSession; // optional for Quick Session
        private RaceSessionRepository sessionRepository = new RaceSessionRepository(Program.ConnectionString);
        private readonly RaceController _controller;

        // one-time popup gate for finals
        private bool _finalsPopupShown;

        public Form1(RaceController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            InitializeComponent();

            // designer button hooks
            btnEditResult.Click += btnEditResult_Click;

            currentSession = _controller.Session;

            lblEventTitle.Text = currentSession != null
                ? $"Event: {currentSession.EventName}"
                : "Quick Session";

            // Hydrate drivers from session (if present)
            if (currentSession?.DriverEntries != null && currentSession.DriverEntries.Count > 0)
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

            // Disabled until controller says we can advance
            btnNextRound.Enabled = false;

            // ── Controller event hooks ───────────────────────────────────────────────
            _controller.BracketRedrawn += RedrawFullBracket;
            _controller.NextMatchReady += OnNextMatchReady;
            _controller.WinnersUpdated += OnWinnersUpdated;
            _controller.CanAdvanceChanged += OnCanAdvanceChanged;
            _controller.CanOfferBuybackChanged += OnCanOfferBuybackChanged;
            _controller.CanStartFinalsChanged += OnCanStartFinalsChanged;
            _controller.TournamentCompleted += OnTournamentCompleted;
        }

        // ========= Controller Event Handlers =========

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
            Logger.Log($"UI: Generate Losers Bracket button {(enabled ? "enabled" : "disabled")}.");

            if (enabled)
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
            {
                _finalsPopupShown = false;
            }
        }

        private void OnTournamentCompleted(RaceController.RaceSummary summary)
        {
            if (InvokeRequired) { BeginInvoke(new Action<RaceController.RaceSummary>(OnTournamentCompleted), summary); return; }

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

            // Stats bump
            try
            {
                var repo = new DriverRepository(Program.ConnectionString);

                // Everyone who raced gets +1 EventsEntered (if they exist in DB)
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

                // Winner gets +1 EventsWon
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
        }

        // ========= Helper: ordering for Winners panel =========

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
                    if (last.Length >= 2 && (last[0] == 'R' || last[0] == 'r') &&
                        int.TryParse(last.Substring(1), out int n))
                    {
                        return 200 + n;
                    }
                }

                Logger.Log($"[UI:Winners] LB round label not recognized: '{roundLabel}' — defaulting to 290");
                return 290;
            }

            if (roundLabel.Length >= 2 && (roundLabel[0] == 'R' || roundLabel[0] == 'r') &&
                int.TryParse(roundLabel.Substring(1), out int n1))
            {
                return 100 + n1;
            }

            if (roundLabel.StartsWith("Semi", StringComparison.OrdinalIgnoreCase)) return 990;
            if (roundLabel.StartsWith("Final", StringComparison.OrdinalIgnoreCase)) return 1000;

            Logger.Log($"[UI:Winners] Unrecognized round label for ordering: '{roundLabel}' — defaulting to 800");
            return 800;
        }

        // ========= UI Button Handlers =========

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

        private void btnWinner1_Click(object sender, EventArgs e) => HandleWinnerClick(true, btnWinner1.Tag);
        private void btnWinner2_Click(object sender, EventArgs e) => HandleWinnerClick(false, btnWinner2.Tag);

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

            // Recompute EventsWon from all saved sessions, then persist
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
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[STATS][ERROR] Recompute EventsWon failed: {ex}");
            }

            MessageBox.Show("Race session saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        // ========= UI update helpers =========

        private void UpdateDriverList()
        {
            lvDrivers.BeginUpdate();
            lvDrivers.Items.Clear();

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

        // BYE stays visible (grey) without fake Driver objects
        private void RedrawFullBracket(IReadOnlyList<PairingRow> rows)
        {
            if (InvokeRequired) { BeginInvoke(new Action<IReadOnlyList<PairingRow>>(RedrawFullBracket), rows); return; }
            if (rows == null) { Logger.Log("[UI] RedrawFullBracket called with rows=null"); return; }

            try
            {
                Logger.Log($"[UI] RedrawFullBracket: incoming rows={rows.Count}");
                lvPairings.BeginUpdate();
                lvPairings.Items.Clear();

                // prevent duplicate headers/rows in a single redraw
                string lastHeader = null;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                int added = 0;
                foreach (var row in rows)
                {
                    if (row == null) continue;

                    if (row.IsHeader)
                    {
                        string label = GetFullRoundLabel(row.RoundLabel);

                        // suppress consecutive duplicate headers
                        if (string.Equals(label, lastHeader, StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Log($"[UI] Header suppressed (duplicate): {label}");
                            continue;
                        }
                        lastHeader = label;

                        var header = new ListViewItem(string.Empty);
                        header.SubItems.Add(label);
                        header.SubItems.Add(string.Empty);
                        header.BackColor = Color.LightGray;
                        header.Font = new Font(lvPairings.Font, FontStyle.Italic);
                        lvPairings.Items.Add(header);
                        Logger.Log($"[UI] Header added: {label}");
                        continue;
                    }

                    // stable key to suppress duplicate match rows
                    string matchLabel = !string.IsNullOrEmpty(row.MatchNumber) ? row.MatchNumber : (row.MatchId > 0 ? $"M{row.MatchId}" : "-");
                    string key = row.MatchId > 0
                        ? $"{row.RoundLabel}|{row.MatchId}"
                        : $"{row.RoundLabel}|{row.Driver1}|{row.Driver2}";
                    if (!seen.Add(key))
                    {
                        Logger.Log($"[UI] Row suppressed (duplicate): key={key}");
                        continue;
                    }

                    bool bye1 = string.IsNullOrWhiteSpace(row.Driver1);
                    bool bye2 = string.IsNullOrWhiteSpace(row.Driver2);

                    string d1 = bye1 ? "BYE" : row.Driver1;
                    string d2 = bye2 ? "BYE" : row.Driver2;

                    var item = new ListViewItem(matchLabel);
                    item.SubItems.Add(d1);
                    item.SubItems.Add(d2);
                    item.UseItemStyleForSubItems = false;

                    // grey + italic only the BYE side (keep active driver normal)
                    if (bye1 ^ bye2)
                    {
                        int byeIdx = bye1 ? 1 : 2; // 0=Match, 1=D1, 2=D2
                        var byeSub = item.SubItems[byeIdx];
                        byeSub.ForeColor = SystemColors.GrayText;
                        byeSub.Font = new Font(lvPairings.Font, FontStyle.Italic);
                        Logger.Log($"[UI] BYE styled in {matchLabel} → {(bye1 ? "D1" : "D2")} is BYE");
                    }
                    else if (bye1 && bye2)
                    {
                        // sanity: both missing — grey whole row
                        item.ForeColor = SystemColors.GrayText;
                        item.Font = new Font(lvPairings.Font, FontStyle.Italic);
                        Logger.Log($"[UI] Both sides BYE in {matchLabel} (unexpected)");
                    }

                    lvPairings.Items.Add(item);
                    added++;
                    Logger.Log($"[UI] Row added: {matchLabel}  {d1} vs {d2}  [Round={row.RoundLabel}, MatchId={row.MatchId}]");
                }

                lvPairings.EndUpdate();
                Logger.Log($"[UI] Redraw complete: headers+rows={lvPairings.Items.Count}, matches added={added}");
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI] RedrawFullBracket() exception: {ex.GetType().Name}: {ex.Message}\n{ex}");
            }
        }

        // Buttons show CURRENT; label shows NEXT TWO (names only) + logging.
        private void OnNextMatchReady(PairingRow row)
        {
            try
            {
                if (InvokeRequired) { BeginInvoke(new Action<PairingRow>(OnNextMatchReady), row); return; }

                if (row == null)
                {
                    lblNext.AutoSize = false;
                    lblNext.TextAlign = ContentAlignment.MiddleCenter;
                    lblNext.Text = "No match ready";
                    btnWinner1.Enabled = false;
                    btnWinner2.Enabled = false;
                    Logger.Log("[UI][NEXT] No current match.");
                    return;
                }

                // Buttons = current matchup
                btnWinner1.Text = string.IsNullOrWhiteSpace(row.Driver1) ? "BYE" : row.Driver1;
                btnWinner2.Text = string.IsNullOrWhiteSpace(row.Driver2) ? "BYE" : row.Driver2;
                btnWinner1.Tag = row.MatchId;
                btnWinner2.Tag = row.MatchId;

                // BYE guard for buttons
                btnWinner1.Enabled = !IsByeName(btnWinner1.Text);
                btnWinner2.Enabled = !IsByeName(btnWinner2.Text);

                // Label = next two matchups (names only)
                var upcoming = _controller.PeekUpcomingMatches(3)
                                          .Where(m => m.MatchId != row.MatchId)
                                          .Take(2)
                                          .ToList();

                lblNext.AutoSize = false;
                lblNext.TextAlign = ContentAlignment.MiddleCenter;

                string text;
                if (upcoming.Count == 0)
                {
                    text = $"{btnWinner1.Text} vs {btnWinner2.Text}";
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

                Logger.Log($"[UI][NEXT] Current=M{row.MatchId}:{btnWinner1.Text} vs {btnWinner2.Text} | Label='{text.Replace(Environment.NewLine, " / ")}'");
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI] OnNextMatchReady() exception: {ex.GetType().Name}: {ex.Message}\n{ex}");
            }
        }

        private static string FormatMatchForNext(EngineMatch m)
        {
            string n1 = m.Driver1?.Name ?? "BYE";
            string n2 = m.Driver2?.Name ?? "BYE";
            return $"M{m.MatchId}: {n1} vs {n2}";
        }

        private void HandleWinnerClick(bool firstOption, object tag)
        {
            if (tag is not int matchId) return;

            var beforeWinner = _controller.GetWinner(matchId);

            _controller.SubmitWinner(matchId, firstOption); // commit

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

            // only bump if the winner actually changed
            if (beforeWinner == null || beforeWinner.Id != winner.Id)
            {
                UpdateDriverStats(winner, loser);

                // If this is a Final, bump event-won too
                if (string.Equals(round, "F", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(round, "Final", StringComparison.OrdinalIgnoreCase))
                {
                    BumpEventWon(winner);
                }
            }
            else
            {
                Logger.Log($"[STATS] No change (same winner) for M{matchId} ({round}).");
            }

            Logger.Log($"[RESULT] Match {matchId} ({round}): {wName} defeated {lName}");
            _controller.PushNextMatch();
        }

        // Rebuild the Winners list (grouped by round with headers).
        private void OnWinnersUpdated(IReadOnlyList<WinnerRow> rows)
        {
            if (InvokeRequired) { BeginInvoke(new Action<IReadOnlyList<WinnerRow>>(OnWinnersUpdated), rows); return; }

            try
            {
                if (lvWinners.Columns.Count == 0)
                {
                    lvWinners.View = View.Details;
                    lvWinners.Columns.Add("M#", 45, HorizontalAlignment.Left);
                    lvWinners.Columns.Add("Loser", 170, HorizontalAlignment.Left);
                    lvWinners.Columns.Add("Winner", 170, HorizontalAlignment.Left);
                }

                lvWinners.BeginUpdate();
                lvWinners.Items.Clear();

                if (rows == null || rows.Count == 0)
                {
                    lvWinners.EndUpdate();
                    return;
                }

                var ordered = rows
                    .OrderBy(w => GetGlobalRoundOrder(w.RoundLabel ?? string.Empty))
                    .ThenBy(w => w.MatchId)
                    .ToList();

                string currentHeader = null;
                int displayNo = 1;

                foreach (var w in ordered)
                {
                    if (!string.Equals(currentHeader, w.RoundLabel, StringComparison.OrdinalIgnoreCase))
                    {
                        currentHeader = w.RoundLabel ?? string.Empty;

                        var hdr = new ListViewItem(string.Empty);
                        hdr.SubItems.Add(GetFullRoundLabel(currentHeader));
                        hdr.SubItems.Add(string.Empty);
                        hdr.Tag = null; // header row
                        hdr.BackColor = Color.LightGray;
                        hdr.Font = new Font(lvWinners.Font, FontStyle.Italic);
                        lvWinners.Items.Add(hdr);

                        Logger.Log($"[UI:Winners] Header added: {currentHeader}");
                    }

                    var item = new ListViewItem($"M{displayNo++}");
                    item.SubItems.Add(w.Loser ?? string.Empty);
                    item.SubItems.Add(w.Winner ?? string.Empty);
                    item.Tag = w.MatchId; // store MatchId for edit
                    lvWinners.Items.Add(item);

                    Logger.Log($"[UI:Winners] Row added: {item.Text}  {w.Loser ?? ""} → {w.Winner ?? ""}  [Round={w.RoundLabel}, MatchId={w.MatchId}]");
                }

                Logger.Log($"[UI:Winners] Rebuilt: total rows={lvWinners.Items.Count}, matches(numbered)={displayNo - 1}");
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI:Winners][ERROR] {ex}");
            }
            finally
            {
                lvWinners.EndUpdate();
            }
        }

        private static bool IsByeName(string name)
            => string.Equals((name ?? "").Trim(), "BYE", StringComparison.OrdinalIgnoreCase);

        private string GetFullRoundLabel(string label)
        {
            return label switch
            {
                "R1" => "Round 1",
                "R2" => "Round 2",
                "R3" => "Round 3",
                "R4" => "Round 4",
                "QF" => "Quarterfinals",
                "SF" => "Semi-Finals",
                "F" => "Final",
                "LBF" => "Losers Bracket Final",
                _ => label
            };
        }

        private void BumpEventWon(Driver winner)
        {
            try
            {
                if (winner == null) return;
                var repo = new DriverRepository(Program.ConnectionString);
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

        // ========= Form lifecycle =========

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                _controller.BracketRedrawn -= RedrawFullBracket;
                _controller.NextMatchReady -= OnNextMatchReady;
                _controller.WinnersUpdated -= OnWinnersUpdated;
                _controller.CanAdvanceChanged -= OnCanAdvanceChanged;
                _controller.CanOfferBuybackChanged -= OnCanOfferBuybackChanged;
                _controller.CanStartFinalsChanged -= OnCanStartFinalsChanged;
                _controller.TournamentCompleted -= OnTournamentCompleted;
            }
            catch { /* ignore */ }

            base.OnFormClosed(e);
        }

        // ========= Designer stubs (leave empty) =========
        private void txtTime_TextChanged(object sender, EventArgs e) { }
        private void cmbRaceType_SelectedIndexChanged(object sender, EventArgs e) { }
        private void lblPairingsHeader_Click(object sender, EventArgs e) { }
        private void lblDriversHeader_Click(object sender, EventArgs e) { }
        private void lblWinnersHeader_Click(object sender, EventArgs e) { }
        private void lblEventTitle_Click(object sender, EventArgs e) { }
        private void lvDrivers_SelectedIndexChanged(object sender, EventArgs e) { }

        // ========= Buyback flow =========
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

                    // Enable finals/LB start
                    btnGenerateBracket.Enabled = true;

                    // Stay enabled to allow edits until LB is generated
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

        // ========= Edit Result =========
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
                dlg.Font = this.Font;
                dlg.KeyPreview = true;

                var lbl = new Label { Text = "Choose the correct winner:", AutoSize = true, Location = new Point(16, 16) };

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

                var btnCancel = new Button { Text = "Cancel", Location = new Point(344, 145), Size = new Size(80, 28), DialogResult = DialogResult.Cancel };

                dlg.KeyDown += (s, e) =>
                {
                    if ((e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1) && btn1.Enabled) { dlg.Tag = 1; dlg.DialogResult = DialogResult.OK; }
                    else if ((e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2) && btn2.Enabled) { dlg.Tag = 2; dlg.DialogResult = DialogResult.OK; }
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

        private void UpdateDriverStats(Driver winner, Driver loser)
        {
            try
            {
                if (winner == null || loser == null)
                {
                    Logger.Log("[STATS] Skip: winner/loser null.");
                    return;
                }

                if (string.Equals((winner.Name ?? "").Trim(), "BYE", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals((loser.Name ?? "").Trim(), "BYE", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log("[STATS] Skip: BYE in matchup.");
                    return;
                }

                var repo = new DriverRepository(Program.ConnectionString);
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
    }
}
