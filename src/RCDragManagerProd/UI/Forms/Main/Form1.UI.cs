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

                double? dialIn = _controller.GetDriverDialIn(d.Id);
                string dialInText = dialIn.HasValue ? dialIn.Value.ToString("0.000") : "—";
                item.SubItems.Add(dialInText);

                lvDrivers.Items.Add(item);
            }

            lvDrivers.EndUpdate();

            bool canGenerate = drivers.Count >= 2 && !_controller.HasBracketStarted;
            btnGenerateBracket.Enabled = canGenerate;

            UpdateDialInButtonEnabled();
            UpdateSetupPhaseUi();

            Logger.Log($"[UI] Driver list updated ({drivers.Count}); Generate Bracket {(canGenerate ? "ENABLED" : "disabled")}.");
        }

        // Setup-phase affordances that must not act like normal race-day controls once
        // a bracket is live:
        //   • the Add Driver section (name/time inputs + Add/Edit) is hidden (issue #254);
        //   • Reset is a plain "Reset Setup" before racing but becomes the protected
        //     "Reset Race" recovery action afterwards (issue #256).
        // All of these return to setup form after a Reset clears the bracket.
        private void UpdateSetupPhaseUi()
        {
            bool setupPhase = !_controller.HasBracketStarted;
            txtName.Visible = setupPhase;
            txtTime.Visible = setupPhase;
            tlpAddEdit.Visible = setupPhase;
            btnReset.Text = setupPhase ? "Reset Setup" : "Reset Race";

            // Back in setup (fresh load or after a Reset clears the bracket) the action
            // buttons return to their base operator labels (issue #257). Phase-specific
            // labels — "Start Losers Bracket", "Start Finals", "Edit Buybacks" — are
            // applied later by the controller phase events.
            if (setupPhase)
            {
                btnGenerateBracket.Text = "Build Bracket";
                btnGenerateLosersBracket.Text = "Open Buybacks";
            }
        }

        private void UpdateDialInButtonEnabled()
        {
            if (btnSetDialIn == null) return;
            // Stays enabled while a round is locked so the director can still open the
            // override-confirmation path (issue #306). The lock is enforced inside
            // btnSetDialIn_Click, not by disabling the button.
            btnSetDialIn.Enabled = drivers.Count > 0;
        }

        private string GetFullRoundLabel(string label)
        {
            return RoundLabels.Normalize(label);
        }

        private static bool IsByeName(string name)
            => string.Equals((name ?? "").Trim(), "BYE", StringComparison.OrdinalIgnoreCase);

        private void ResizePairingsColumns()
        {
            if (lvPairings.Columns.Count < 3) return;
            const int matchColWidth = 40;
            int available = lvPairings.ClientSize.Width - matchColWidth;
            if (available < 80) return;
            colMatch.Width = matchColWidth;
            colDriver1.Width = available / 2;
            colDriver2.Width = available - available / 2;
        }

        private void ResizeWinnersColumns()
        {
            if (lvWinners.Columns.Count < 3) return;
            const int matchColWidth = 40;
            int available = lvWinners.ClientSize.Width - matchColWidth;
            if (available < 80) return;
            lvWinners.Columns[0].Width = matchColWidth;
            lvWinners.Columns[1].Width = available / 2;
            lvWinners.Columns[2].Width = available - available / 2;
        }

    }
}
