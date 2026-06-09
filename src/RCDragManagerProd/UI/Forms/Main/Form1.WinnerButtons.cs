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

namespace RCDragManagerProd.UI.Forms
{
    public partial class Form1
    {
        private void HandleWinnerClick(bool firstOption, object tag)
        {
            int matchId = 0;
            int taggedMatchId = 0;

            if (tag == null)
            {
                Logger.Log("[UI][WINNER] HandleWinnerClick: tag is null");
                return;
            }

            if (tag is int)
            {
                taggedMatchId = (int)tag;
            }
            else if (tag is long)
            {
                taggedMatchId = (int)(long)tag;
            }
            else
            {
                string s;
                s = tag.ToString();

                if (!int.TryParse(s, out taggedMatchId))
                {
                    Logger.Log("[UI][WINNER] HandleWinnerClick: tag not int: '" + s + "'");
                    return;
                }
            }

            matchId = taggedMatchId;
            if (_currentWinnerButtonContext != null)
            {
                if (_currentWinnerButtonContext.MatchId != taggedMatchId)
                {
                    Logger.Log("[UI][WINNER][MAP-WARN] Tag/context mismatch. " +
                               "TagM=" + taggedMatchId + " ContextM=" + _currentWinnerButtonContext.MatchId +
                               " ContextPairing='" + (_currentWinnerButtonContext.LeftName ?? "BYE") +
                               " vs " + (_currentWinnerButtonContext.RightName ?? "BYE") + "'. " +
                               "Using context MatchId.");
                }

                matchId = _currentWinnerButtonContext.MatchId;
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

            // BYE-forcing + lane-swap → engine winner option, and the submit itself, now live
            // in RaceConsoleService (issue #284). The form keeps the post-result stats below.
            var submission = _raceConsole.SubmitWinnerFromButton(matchId, firstOption);
            if (!submission.Accepted)
            {
                Logger.Log("[UI][WINNER] Submit not accepted (both sides BYE/unresolved) for M" + matchId + " (" + round + ").");
                return;
            }

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
            if (IsByeName(winner?.Name) || IsByeName(loser?.Name))
            {
                Logger.Log("[STATS] Skip: BYE in matchup.");
                return;
            }

            _controller.PersistMatchStats(winner, loser, Program.ConnectionString);
        }

        private void BumpEventWon(Driver winner)
        {
            _controller.PersistEventWon(winner, Program.ConnectionString);
        }
    }
}
