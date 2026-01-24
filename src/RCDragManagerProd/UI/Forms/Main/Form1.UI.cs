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
