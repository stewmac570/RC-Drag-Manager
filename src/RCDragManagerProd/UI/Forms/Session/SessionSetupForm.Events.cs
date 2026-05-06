// SessionSetupForm.Events.cs
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
        private void FilterChanged(object sender, EventArgs e)
        {
            var car = cmbFilterCar?.SelectedItem?.ToString() ?? "(null)";
            var cls = cmbFilterClass?.SelectedItem?.ToString() ?? "(null)";
            var state = cmbFilterState?.SelectedItem?.ToString() ?? "(null)";
            Logger.Log($"[FILTER] Change → Car='{car}', Class='{cls}', State='{state}'");
            RefreshDriverList();
        }

        private void LvEventRoster_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suppressRosterEvents) return;

            if (e.Item?.Tag is ValueTuple<DDriver, DCar> t)
            {
                var (driver, car) = t;

                if (e.Item.Checked)
                {
                    if (!eventRoster.Any(x => x.driver.Id == driver.Id && x.car.CarID == car.CarID))
                    {
                        eventRoster.Add((driver, car));
                        Logger.Log($"[ROSTER] + d#{driver.Id}:{driver.Name}  car#{car.CarID}:{car.CarName}");
                    }
                }
                else
                {
                    int removed = eventRoster.RemoveAll(x => x.driver.Id == driver.Id && x.car.CarID == car.CarID);
                    Logger.Log($"[ROSTER] - d#{driver.Id}:{driver.Name}  car#{car.CarID}:{car.CarName}  (removed={removed})");
                }
            }
        }

        private void ClassSelectionChanged(object sender, EventArgs e)
        {
            if (rbHeadsUp.Checked)
            {
                lblFixedDial.Visible = false;
                txtFixedDial.Visible = false;
            }
            else if (rbBracket.Checked)
            {
                lblFixedDial.Visible = true;
                txtFixedDial.Visible = true;
            }
            else if (rbDialIn.Checked)
            {
                lblFixedDial.Visible = false;
                txtFixedDial.Visible = false;
            }

            Logger.Log($"[CREATE] ClassSelectionChanged → HeadsUp={rbHeadsUp.Checked}, Bracket={rbBracket.Checked}, DialIn={rbDialIn.Checked}");
            RefreshDriverList();
        }

        private void BtnAddNewDriver_Click(object sender, EventArgs e)
        {
            using (var addDialog = new AddDriverAndCarDialog())
            {
                if (addDialog.ShowDialog() != DialogResult.OK) return;

                var newDriver = new DDriver
                {
                    Name = addDialog.DriverName,
                    Cars = new List<DCar>()
                };
                repository.AddDriver(newDriver);

                var insertedDriver = repository.GetAllDrivers().First(d => d.Name == newDriver.Name);

                var newCar = new DCar
                {
                    CarName = addDialog.CarName,
                    ClassType = addDialog.ClassType,
                    DefaultDialIn = addDialog.DialIn
                };
                repository.AddCar(insertedDriver.Id, newCar);

                Logger.Log($"[CREATE] Added driver '{insertedDriver.Name}' with car '{newCar.CarName}' ({newCar.ClassType}).");
            }

            FillFilterCombos();
            RefreshDriverList();
        }
        private void UpdateRoundRobinUiState()
        {
            var raceType = cmbRaceType?.SelectedItem?.ToString() ?? string.Empty;
            var isRR = string.Equals(raceType, RaceTypes.RoundRobin, StringComparison.OrdinalIgnoreCase);

            // show/hide RR controls
            if (lblRRVariant != null) lblRRVariant.Visible = isRR;
            if (cmbRRVariant != null) cmbRRVariant.Visible = isRR;
            if (lblRoundsToRun != null) lblRoundsToRun.Visible = isRR;
            if (nudRoundsToRun != null) nudRoundsToRun.Visible = isRR;

            if (!isRR)
            {
                if (cmbRRVariant != null) cmbRRVariant.SelectedItem = "Standard";
                if (nudRoundsToRun != null)
                {
                    nudRoundsToRun.Enabled = false;
                }

                Logger.Log("[CREATE][RR] UI state → not RR (controls hidden, variant reset to Standard)");
                return;
            }

            // RR mode: enable N only for QMDRA
            var variant = cmbRRVariant?.SelectedItem?.ToString() ?? "Standard";
            var isQmdra = string.Equals(variant, "QMDRA", StringComparison.OrdinalIgnoreCase);

            if (nudRoundsToRun != null)
            {
                nudRoundsToRun.Enabled = isQmdra;
                if (!isQmdra)
                {
                    // keep whatever default value you like; just disable it
                }
            }

            Logger.Log($"[CREATE][RR] UI state → RR Variant='{variant}', NEnabled={isQmdra}");
        }

        private void CmbRaceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateRoundRobinUiState();
        }

        private void CmbRRVariant_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateRoundRobinUiState();
        }

        private void BtnStartRace_Click(object sender, EventArgs e)
        {
            if (eventRoster.Count < 2)
            {
                MessageBox.Show("Please select at least 2 drivers.");
                return;
            }

            string classType = rbHeadsUp.Checked ? "Heads Up" :
                               rbBracket.Checked ? "Bracket Class" : "Dial-In";

            double? fixedDial = null;
            if (rbBracket.Checked && double.TryParse(txtFixedDial.Text, out double fd))
                fixedDial = fd;

            foreach (var (driver, _) in eventRoster)
            {
                var dbDriver = repository.GetDriverById(driver.Id);
                if (dbDriver != null)
                {
                    dbDriver.EventsEntered += 1;
                    repository.UpdateDriver(dbDriver);
                    Logger.Log($"[CREATE][STATS] +EventsEntered → #{dbDriver.Id} {dbDriver.Name}: {dbDriver.EventsEntered}");
                }
            }

            // Round Robin config (authoritative insertion point)
            string rrVariant = null;
            int? rrRoundsToRun = null;

            string selectedRaceType = cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder";
            if (string.Equals(selectedRaceType, RaceTypes.RoundRobin, StringComparison.OrdinalIgnoreCase))
            {
                rrVariant = cmbRRVariant?.SelectedItem?.ToString() ?? "Standard";

                if (string.Equals(rrVariant, "QMDRA", StringComparison.OrdinalIgnoreCase))
                {
                    int n = 0;
                    try { n = (int)nudRoundsToRun.Value; } catch { n = 0; }

                    if (n <= 0)
                    {
                        MessageBox.Show("Rounds To Run (N) is required for QMDRA and must be >= 1.");
                        Logger.Log("[CREATE][RR] BLOCKED start → QMDRA missing/invalid N.");
                        return;
                    }

                    rrRoundsToRun = n;
                }

                Logger.Log($"[CREATE][RR] Selected → Variant='{rrVariant}', RoundsToRun={(rrRoundsToRun.HasValue ? rrRoundsToRun.Value.ToString() : "null")}");
            }


            RaceSessionResult = new RaceSession
            {
                EventName = txtEventName.Text.Trim(),
                EventDate = dateRaceDate.Value.Date,
                RaceType = cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder",

                // RR config (null unless Round Robin)
                RoundRobinVariant = rrVariant,
                RoundsToRun = rrRoundsToRun,

                ClassType = classType,
                FixedDialIn = fixedDial,

                DriverEntries = eventRoster.Select(er =>
                {
                    double? dialIn = null;
                    double? qualTime = null;

                    if (classType == "Heads Up")
                        qualTime = er.driver.QualTime;
                    else if (classType == "Dial-In")
                        dialIn = er.car.DefaultDialIn;
                    else if (classType == "Bracket Class")
                        dialIn = fixedDial;

                    return new RaceSessionDriverEntry
                    {
                        DriverID = er.driver.Id,
                        DriverName = er.driver.Name,
                        CarID = er.car.CarID,
                        CarName = er.car.CarName,
                        ClassType = er.car.ClassType,
                        DialIn = dialIn,
                        QualifyingTime = qualTime,
                        Seed = null
                    };
                }).ToList()
            };


            Logger.Log($"[CREATE] StartRace → Roster={eventRoster.Count}, ClassType='{classType}', RaceType='{RaceSessionResult.RaceType}'");
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Logger.Log("[CREATE] SessionSetupForm cancelled.");
            Close();
        }
    }
}
