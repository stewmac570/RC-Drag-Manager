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
            if (tag is not int matchId) return;

            var beforeWinner = _controller.GetWinner(matchId);

            _controller.SubmitWinner(matchId, firstOption);

            var match = _controller.GetMatch(matchId);
            var winner = _controller.GetWinner(matchId);
            var loser = _controller.GetLoser(matchId);

            string round = match?.RoundLabel ?? "Unknown";
            string wName = winner?.Name ?? "BYE/Unknown";
            string lName = loser?.Name ?? "BYE/Unknown";

            if (winner == null || loser == null || IsByeName(wName) || IsByeName(lName))
            {
                Logger.Log($"[STATS] Skip: BYE/unresolved for M{matchId} ({round}).");
                Logger.Log($"[RESULT] Match {matchId} ({round}): {wName} defeated {lName}");
                _controller.PushNextMatch();
                return;
            }

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
                Logger.Log($"[STATS] No change (same winner) for M{matchId} ({round}).");
            }

            Logger.Log($"[RESULT] Match {matchId} ({round}): {wName} defeated {lName}");
            _controller.PushNextMatch();
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
