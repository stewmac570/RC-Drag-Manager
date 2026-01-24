// SessionSetupForm.UI.cs
using RCDragManagerProd.Domain;        // RaceSession, RaceSessionDriverEntry
using RCDragManagerProd.Logging;
using RCDragManagerProd.RandomMode;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.RoundRobinMode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DCar = RCDragManagerProd.Domain.Car;
// Aliases to avoid any namespace confusion in this file
using DDriver = RCDragManagerProd.Domain.Driver;

namespace RCDragManagerProd.UI.Forms
{
    public partial class SessionSetupForm : Form
    {
        private void CreateFilterControls()
        {
            var host = lvEventRoster.Parent ?? this;

            if (pnlFilters != null && !pnlFilters.IsDisposed)
            {
                try { host.Controls.Remove(pnlFilters); } catch { }
                pnlFilters.Dispose();
                pnlFilters = null;
            }

            int comboW = 120;
            int labelToBox = 4;
            int groupGap = 12;

            int y = (btnAddNewDriver != null && !btnAddNewDriver.IsDisposed)
                        ? btnAddNewDriver.Top
                        : Math.Max(0, lvEventRoster.Top - 28);

            int x = (btnAddNewDriver != null && !btnAddNewDriver.IsDisposed)
                        ? btnAddNewDriver.Right + 8
                        : lvEventRoster.Left;

            int carLblW = TextRenderer.MeasureText("Car:", this.Font).Width;
            int classLblW = TextRenderer.MeasureText("Class:", this.Font).Width;
            int stateLblW = TextRenderer.MeasureText("State:", this.Font).Width;

            var lblCar = new Label { Text = "Car:", AutoSize = true, Left = x, Top = y + 6 };
            host.Controls.Add(lblCar); lblCar.BringToFront();

            cmbFilterCar = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Left = lblCar.Left + carLblW + labelToBox, Top = y + 2, Width = comboW };
            host.Controls.Add(cmbFilterCar); cmbFilterCar.BringToFront();

            int nextX = cmbFilterCar.Left + comboW + groupGap;

            var lblClass = new Label { Text = "Class:", AutoSize = true, Left = nextX, Top = y + 6 };
            host.Controls.Add(lblClass); lblClass.BringToFront();

            cmbFilterClass = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Left = lblClass.Left + classLblW + labelToBox, Top = y + 2, Width = comboW };
            host.Controls.Add(cmbFilterClass); cmbFilterClass.BringToFront();

            nextX = cmbFilterClass.Left + comboW + groupGap;

            var lblState = new Label { Text = "State:", AutoSize = true, Left = nextX, Top = y + 6 };
            host.Controls.Add(lblState); lblState.BringToFront();

            cmbFilterState = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Left = lblState.Left + stateLblW + labelToBox, Top = y + 2, Width = comboW };
            host.Controls.Add(cmbFilterState); cmbFilterState.BringToFront();

            cmbFilterCar.SelectedIndexChanged += FilterChanged;
            cmbFilterClass.SelectedIndexChanged += FilterChanged;
            cmbFilterState.SelectedIndexChanged += FilterChanged;

            if (lvEventRoster.Columns.Count == 4)
                lvEventRoster.Columns.Add("State", 70, HorizontalAlignment.Left);

            Logger.Log($"[CREATE:FILTER] Row at Y={y}, X={x} (comboW={comboW}, gap={groupGap}).");
        }

        private void FillFilterCombos()
        {
            try
            {
                // coalesce via IEnumerable to dodge generic-type mismatches
                List<DDriver> allDrivers = (repository.GetAllDrivers() ?? Enumerable.Empty<DDriver>()).ToList();

                var carNames = allDrivers
                    .SelectMany(d => d.Cars ?? new List<DCar>())
                    .Select(c => c.CarName)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .ToList();

                cmbFilterCar.Items.Clear();
                cmbFilterCar.Items.Add("(All)");
                foreach (var n in carNames) cmbFilterCar.Items.Add(n);
                cmbFilterCar.SelectedIndex = 0;

                cmbFilterClass.Items.Clear();
                cmbFilterClass.Items.AddRange(new object[] { "(All)", "Heads Up", "Bracket", "Dial In" });
                cmbFilterClass.SelectedIndex = 0;

                var states = allDrivers
                    .Select(d => d.State)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .ToList();

                cmbFilterState.Items.Clear();
                cmbFilterState.Items.Add("(All)");
                foreach (var s in states) cmbFilterState.Items.Add(s);
                cmbFilterState.SelectedIndex = 0;

                Logger.Log("[CREATE:FILTER] combos populated.");
            }
            catch (Exception ex)
            {
                Logger.Log($"[CREATE:FILTER][ERROR] {ex}");
            }
        }

        private void RefreshDriverList()
        {
            allEligibleDrivers ??= new List<(DDriver, DCar)>();
            eventRoster ??= new List<(DDriver, DCar)>();

            string carFilter = (cmbFilterCar?.SelectedItem as string) ?? "(All)";
            string classFilter = (cmbFilterClass?.SelectedItem as string) ?? "(All)";
            string stateFilter = (cmbFilterState?.SelectedItem as string) ?? "(All)";
            bool useRadioClass = (classFilter == "(All)");

            Logger.Log($"[FILTER] Rebuild → Car='{carFilter}', Class='{classFilter}'(useRadio={useRadioClass}), State='{stateFilter}'");

            _suppressRosterEvents = true;
            lvEventRoster.BeginUpdate();
            try
            {
                lvEventRoster.Items.Clear();
                allEligibleDrivers.Clear();

                List<DDriver> drivers = repository.GetAllDrivers() ?? new List<DDriver>();

                foreach (DDriver driver in drivers)
                {
                    if (stateFilter != "(All)" &&
                        !string.Equals(driver.State ?? "", stateFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (DCar car in (driver.Cars ?? new List<DCar>()))
                    {
                        bool eligible = false;

                        if (useRadioClass)
                        {
                            if (rbHeadsUp.Checked && car.ClassType == "Heads Up") eligible = true;
                            if (rbBracket.Checked && car.ClassType == "Bracket") eligible = true;
                            if (rbDialIn.Checked && car.ClassType == "Dial In") eligible = true;
                        }
                        else
                        {
                            if (string.Equals(car.ClassType, classFilter, StringComparison.OrdinalIgnoreCase))
                                eligible = true;
                        }

                        if (carFilter != "(All)" &&
                            !string.Equals(car.CarName, carFilter, StringComparison.OrdinalIgnoreCase))
                            eligible = false;

                        if (!eligible) continue;

                        allEligibleDrivers.Add((driver, car));

                        var item = new ListViewItem(new[]
                        {
                            driver.Name,
                            car.CarName,
                            car.ClassType,
                            car.DefaultDialIn?.ToString("0.000") ?? "-",
                            driver.State ?? ""
                        })
                        { Tag = (driver, car) };

                        bool wasChecked = eventRoster.Any(x => x.driver.Id == driver.Id && x.car.CarID == car.CarID);
                        item.Checked = wasChecked;

                        lvEventRoster.Items.Add(item);
                    }
                }

                Logger.Log($"[FILTER] List rebuilt: visible={lvEventRoster.Items.Count}, rosterPersisted={eventRoster.Count}");
            }
            finally
            {
                lvEventRoster.EndUpdate();
                _suppressRosterEvents = false;
            }
        }
    }
}
