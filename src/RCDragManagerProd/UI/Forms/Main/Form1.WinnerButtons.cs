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
    public partial class Form1
    {
        private void HandleWinnerClick(bool firstOption, object tag)
        {
            int matchId;
            matchId = 0;

            if (tag == null)
            {
                Logger.Log("[UI][WINNER] HandleWinnerClick: tag is null");
                return;
            }

            if (tag is int)
            {
                matchId = (int)tag;
            }
            else if (tag is long)
            {
                matchId = (int)(long)tag;
            }
            else
            {
                string s;
                s = tag.ToString();

                if (!int.TryParse(s, out matchId))
                {
                    Logger.Log("[UI][WINNER] HandleWinnerClick: tag not int: '" + s + "'");
                    return;
                }
            }

            Logger.Log("[UI][WINNER] HandleWinnerClick start: firstOption(UI-left?)=" + firstOption + " matchId=" + matchId +
                       " btn1='" + (btnWinner1.Text ?? "") + "' btn2='" + (btnWinner2.Text ?? "") + "'");

            var beforeWinner = _controller.GetWinner(matchId);

            var match = _controller.GetMatch(matchId);
            if (match == null)
            {
                Logger.Log("[UI][WINNER] HandleWinnerClick: match not found for M" + matchId);
                return;
            }

            string round;
            round = match.RoundLabel ?? "Unknown";

            // ---- Source of truth: ENGINE drivers ----
            bool d1IsBye;
            bool d2IsBye;

            d1IsBye = (match.Driver1 == null) || IsByeName(match.Driver1.Name);
            d2IsBye = (match.Driver2 == null) || IsByeName(match.Driver2.Name);

            Logger.Log("[UI][WINNER] Engine snapshot: M" + matchId + " (" + round + ") " +
                       "D1=" + (match.Driver1 != null ? match.Driver1.Name : "NULL") + " bye=" + d1IsBye + " | " +
                       "D2=" + (match.Driver2 != null ? match.Driver2.Name : "NULL") + " bye=" + d2IsBye);

            bool engineFirstOption;

            // ---- BYE matches: force the real engine driver to win (ignores swap + button clicked) ----
            if (d1IsBye && !d2IsBye)
            {
                engineFirstOption = false; // Engine Driver2 must win
                Logger.Log("[UI][WINNER] Mapping: Engine D1 is BYE => force Engine option=2 (engineFirstOption=false)");
            }
            else if (d2IsBye && !d1IsBye)
            {
                engineFirstOption = true; // Engine Driver1 must win
                Logger.Log("[UI][WINNER] Mapping: Engine D2 is BYE => force Engine option=1 (engineFirstOption=true)");
            }
            else if (d1IsBye && d2IsBye)
            {
                Logger.Log("[UI][WINNER][ERROR] Both engine sides are BYE/NULL for M" + matchId + " (" + round + "). Cannot submit winner.");
                return;
            }
            else
            {
                // ---- Normal match: use lane swap mapping ----
                bool swapped;
                swapped = _controller.IsLaneSwapped(match.MatchId, match.RoundLabel, match.Driver1.Id, match.Driver2.Id);

                // firstOption == UI-left button. If swapped, UI-left corresponds to engine Driver2.
                engineFirstOption = swapped ? !firstOption : firstOption;

                Logger.Log("[UI][WINNER] Mapping: Normal match swapped=" + swapped +
                           " UI-firstOption(left)=" + firstOption +
                           " => engineFirstOption=" + engineFirstOption);
            }

            Logger.Log("[UI][WINNER] Calling SubmitWinner(M" + matchId + ", engineFirstOption=" + engineFirstOption + ")...");
            _controller.SubmitWinner(matchId, engineFirstOption);

            var winner = _controller.GetWinner(matchId);
            var loser = _controller.GetLoser(matchId);

            if (winner == null)
            {
                Logger.Log("[UI][WINNER][ERROR] After SubmitWinner, winner is still null for M" + matchId +
                           ". Submit likely rejected or engine did not store result. (Check [WINNER] Reject logs)");
                return;
            }

            string wName;
            wName = winner != null ? winner.Name : "BYE/Unknown";

            string lName;
            lName = loser != null ? loser.Name : "BYE/Unknown";

            // Stats only for real vs real
            if (loser == null || IsByeName(wName) || IsByeName(lName))
            {
                Logger.Log("[STATS] Skip: BYE/unresolved loser for M" + matchId + " (" + round + ").");
            }
            else
            {
                if (beforeWinner == null || beforeWinner.Id != winner.Id)
                {
                    UpdateDriverStats(winner, loser);

                    if (string.Equals(round, "F", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(round, "Final", StringComparison.OrdinalIgnoreCase))
                    {
                        BumpEventWon(winner);
                    }
                }
                else
                {
                    Logger.Log("[STATS] No change (same winner) for M" + matchId + " (" + round + ").");
                }
            }

            Logger.Log("[RESULT] Match " + matchId + " (" + round + "): " + wName + " defeated " + lName);
            Logger.Log("[UI][WINNER] HandleWinnerClick end: SubmitWinner handled advance via controller.");
        }

        private int ShowWinnerPicker(EngineMatch match)
        {
            bool swap;
            swap = _controller.IsLaneSwapped(match.MatchId, match.RoundLabel, match.Driver1.Id, match.Driver2.Id);

            Driver leftDriver;
            Driver rightDriver;

            bool leftIsFirstOption;

            if (!swap)
            {
                leftDriver = match.Driver1;
                rightDriver = match.Driver2;
                leftIsFirstOption = true;
            }
            else
            {
                leftDriver = match.Driver2;
                rightDriver = match.Driver1;
                leftIsFirstOption = false;
            }

            string n1 = leftDriver?.Name ?? "BYE";
            string n2 = rightDriver?.Name ?? "BYE";

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

                btn1.Click += (_, __) =>
                {
                    dlg.Tag = leftIsFirstOption ? 1 : 2;   // lane-left -> engine option
                    dlg.DialogResult = DialogResult.OK;
                };

                var btn2 = new Button
                {
                    Text = $"Set Winner: {n2}",
                    Location = new Point(16, 95),
                    Size = new Size(408, 40),
                    Enabled = !IsByeName(n2)
                };

                btn2.Click += (_, __) =>
                {
                    dlg.Tag = leftIsFirstOption ? 2 : 1;   // lane-right -> engine option
                    dlg.DialogResult = DialogResult.OK;
                };

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(344, 145),
                    Size = new Size(80, 28),
                    DialogResult = DialogResult.Cancel
                };

                dlg.KeyDown += (s, e) =>
                {
                    if ((e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1) && btn1.Enabled)
                    {
                        dlg.Tag = leftIsFirstOption ? 1 : 2;
                        dlg.DialogResult = DialogResult.OK;
                    }
                    else if ((e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2) && btn2.Enabled)
                    {
                        dlg.Tag = leftIsFirstOption ? 2 : 1;
                        dlg.DialogResult = DialogResult.OK;
                    }
                };

                dlg.Controls.AddRange(new Control[] { lbl, btn1, btn2, btnCancel });
                dlg.CancelButton = btnCancel;

                Logger.Log($"[UI][EDIT] Winner picker open: M{match.MatchId} ({match.RoundLabel}) swap={swap} leftFirstOption={leftIsFirstOption} — '{n1}' vs '{n2}'.");

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

                if (IsByeName(winner.Name) || IsByeName(loser.Name))
                {
                    Logger.Log("[STATS] Skip: BYE in matchup.");
                    return;
                }

                var repo = new DriverRepository(Program.ConnectionString);
                var wdb = repo.GetDriverById(winner.Id);
                var ldb = repo.GetDriverById(loser.Id);

                if (wdb == null || ldb == null)
                {
                    Logger.Log($"[STATS] Skip: DB lookup failed (winnerId={winner.Id}->{(wdb != null)}, loserId={loser.Id}->{(ldb != null)}).");
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
    }
}
