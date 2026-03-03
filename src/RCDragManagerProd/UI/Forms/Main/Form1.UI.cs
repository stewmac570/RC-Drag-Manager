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
                lvDrivers.Items.Add(item);
            }

            lvDrivers.EndUpdate();

            bool canGenerate = drivers.Count >= 2 && !_controller.HasBracketStarted;
            btnGenerateBracket.Enabled = canGenerate;
            Logger.Log($"[UI] Driver list updated ({drivers.Count}); Generate Bracket {(canGenerate ? "ENABLED" : "disabled")}.");
        }

        private int GetGlobalRoundOrder(string roundLabel)
        {
            return RoundLabels.Compare(roundLabel, "R1");
        }


        private string GetFullRoundLabel(string label)
        {
            return RoundLabels.Normalize(label);
        }

        private static bool IsByeName(string name)
            => string.Equals((name ?? "").Trim(), "BYE", StringComparison.OrdinalIgnoreCase);

        private static string FormatMatchForNext(EngineMatch m)
        {
            string n1 = m.Driver1?.Name ?? "BYE";
            string n2 = m.Driver2?.Name ?? "BYE";
            return $"M{m.MatchId}: {n1} vs {n2}";
        }
    }
}
