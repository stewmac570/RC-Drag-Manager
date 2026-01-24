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
        private void RedrawFullBracket(IReadOnlyList<PairingRow> rows)
        {
            if (InvokeRequired) { BeginInvoke(new Action<IReadOnlyList<PairingRow>>(RedrawFullBracket), rows); return; }
            if (rows == null) { Logger.Log("[UI] RedrawFullBracket called with rows=null"); return; }

            try
            {
                Logger.Log($"[UI] RedrawFullBracket: incoming rows={rows.Count}");
                lvPairings.BeginUpdate();
                lvPairings.Items.Clear();

                string lastHeader = null;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                int added = 0;
                foreach (var row in rows)
                {
                    if (row == null) continue;

                    if (row.IsHeader)
                    {
                        string label = GetFullRoundLabel(row.RoundLabel);

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

                    if (bye1 ^ bye2)
                    {
                        int byeIdx = bye1 ? 1 : 2;
                        var byeSub = item.SubItems[byeIdx];
                        byeSub.ForeColor = SystemColors.GrayText;
                        byeSub.Font = new Font(lvPairings.Font, FontStyle.Italic);
                        Logger.Log($"[UI] BYE styled in {matchLabel} → {(bye1 ? "D1" : "D2")} is BYE");
                    }
                    else if (bye1 && bye2)
                    {
                        item.ForeColor = SystemColors.GrayText;
                        item.Font = new Font(lvPairings.Font, FontStyle.Italic);
                        Logger.Log($"[UI] Both sides BYE in {matchLabel} (unexpected)");
                    }

                    lvPairings.Items.Add(item);
                    added++;
                    Logger.Log($"[UI] Row added: {matchLabel}  {d1} vs {d2}  [Round={row.RoundLabel}, MatchId={row.MatchId}]");
                }

                lvPairings.EndUpdate();
                Logger.Log($"[UI] Redrawn complete: items={lvPairings.Items.Count}, matches added={added}");
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI] RedrawFullBracket() exception: {ex}");
            }
        }

        private string FormatMatchForNextLaneAdjusted(EngineMatch m)
        {
            if (m == null) return "M?: BYE vs BYE";

            string left;
            string right;

            // This must exist on RaceController as internal/public
            _controller.GetLaneAdjustedNames(m, out left, out right);

            return "M" + m.MatchId + ": " + left + " vs " + right;
        }

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

                    btnWinner1.Text = "—";
                    btnWinner2.Text = "—";
                    btnWinner1.Tag = null;
                    btnWinner2.Tag = null;

                    btnWinner1.Enabled = false;
                    btnWinner2.Enabled = false;

                    Logger.Log("[UI][NEXT] No current match.");
                    return;
                }

                // ALWAYS take the truth from the EngineMatch + lane adjustment
                var current = _controller.GetMatch(row.MatchId);

                string currentLeft;
                string currentRight;

                if (current != null)
                {
                    _controller.GetLaneAdjustedNames(current, out currentLeft, out currentRight);
                }
                else
                {
                    // fallback: use the row as-is
                    currentLeft = string.IsNullOrWhiteSpace(row.Driver1) ? "BYE" : row.Driver1;
                    currentRight = string.IsNullOrWhiteSpace(row.Driver2) ? "BYE" : row.Driver2;
                }

                btnWinner1.Text = currentLeft;
                btnWinner2.Text = currentRight;

                btnWinner1.Tag = row.MatchId;
                btnWinner2.Tag = row.MatchId;

                // Grey out / disable BYE buttons (so clicking does nothing because you can't click it)
                bool leftIsBye;
                bool rightIsBye;

                leftIsBye = IsByeName(btnWinner1.Text);
                rightIsBye = IsByeName(btnWinner2.Text);

                ApplyByeButtonStyle(btnWinner1, leftIsBye);
                ApplyByeButtonStyle(btnWinner2, rightIsBye);

                Logger.Log("[UI][NEXT] Buttons: leftIsBye=" + leftIsBye + " rightIsBye=" + rightIsBye +
                           " leftEnabled=" + btnWinner1.Enabled + " rightEnabled=" + btnWinner2.Enabled);


                var upcoming = _controller.PeekUpcomingMatches(3)
                                          .Where(m => m.MatchId != row.MatchId)
                                          .Take(2)
                                          .ToList();

                lblNext.AutoSize = false;
                lblNext.TextAlign = ContentAlignment.MiddleCenter;

                string text;
                if (upcoming.Count == 0)
                {
                    text = currentLeft + " vs " + currentRight;
                }
                else if (upcoming.Count == 1)
                {
                    text = "On Deck — " + FormatMatchForNextLaneAdjusted(upcoming[0]);
                }
                else
                {
                    text = "On Deck — " + FormatMatchForNextLaneAdjusted(upcoming[0]) +
                           Environment.NewLine +
                           "In The Hole — " + FormatMatchForNextLaneAdjusted(upcoming[1]);
                }

                lblNext.Text = text;

                Logger.Log(
                    "[UI][NEXT] Current=M" + row.MatchId + ":" + currentLeft + " vs " + currentRight +
                    " | Label='" + text.Replace(Environment.NewLine, " / ") + "'"
                );
            }
            catch (Exception ex)
            {
                Logger.Log("[UI] OnNextMatchReady() exception: " + ex);
            }
        }


        private void ApplyByeButtonStyle(Button btn, bool isBye)
        {
            if (btn == null) return;

            if (isBye)
            {
                btn.Enabled = false;
                btn.ForeColor = SystemColors.GrayText;
                btn.BackColor = SystemColors.Control;
                btn.FlatStyle = FlatStyle.Standard;
                btn.Text = "BYE";
            }
            else
            {
                btn.Enabled = true;
                btn.ForeColor = SystemColors.ControlText;
                btn.UseVisualStyleBackColor = true;
                btn.FlatStyle = FlatStyle.Standard;
            }
        }

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
                        hdr.Tag = null;
                        hdr.BackColor = Color.LightGray;
                        hdr.Font = new Font(lvWinners.Font, FontStyle.Italic);
                        lvWinners.Items.Add(hdr);

                        Logger.Log($"[UI:Winners] Header added: {currentHeader}");
                    }

                    var item = new ListViewItem($"M{displayNo++}");
                    item.SubItems.Add(w.Loser ?? string.Empty);
                    item.SubItems.Add(w.Winner ?? string.Empty);
                    item.Tag = w.MatchId;
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
    }
}
