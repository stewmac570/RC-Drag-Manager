using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    public partial class DriverManagerForm
    {
        private void LoadDrivers()
        {
            var previousId = selectedDriver?.Id ?? 0;

            lvDrivers.BeginUpdate();
            lvDrivers.Items.Clear();

            var drivers = _drivers.GetAllDrivers();

            foreach (var d in drivers.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
            {
                var item = new ListViewItem(d.Name ?? string.Empty) { Tag = d.Id };
                item.SubItems.Add(d.State ?? string.Empty);
                item.SubItems.Add(d.TotalWins.ToString());
                item.SubItems.Add(d.TotalLosses.ToString());
                item.SubItems.Add(d.EventsEntered.ToString());
                item.SubItems.Add(d.EventsWon.ToString());
                item.SubItems.Add(d.QualTime?.ToString("0.000") ?? "—");
                item.SubItems.Add((d.Cars?.Count ?? 0).ToString());
                lvDrivers.Items.Add(item);
            }

            lvDrivers.EndUpdate();

            // Re-select the previously-selected driver by Tag, if still present.
            if (previousId > 0)
            {
                foreach (ListViewItem item in lvDrivers.Items)
                {
                    if (item.Tag is int id && id == previousId)
                    {
                        item.Selected = true;
                        item.Focused = true;
                        item.EnsureVisible();
                        break;
                    }
                }
            }

            UpdateButtonStates();
        }

        private void ShowCarsForDriver(Driver driver)
        {
            if (driver == null)
            {
                ClearCarsPanel();
                return;
            }

            lblCarsHeader.Text = $"Cars — {driver.Name}";

            lvCars.BeginUpdate();
            lvCars.Items.Clear();

            if (driver.Cars != null)
            {
                foreach (var car in driver.Cars)
                {
                    var item = new ListViewItem(car.CarName ?? string.Empty) { Tag = car.CarID };
                    item.SubItems.Add(car.ClassType ?? string.Empty);
                    item.SubItems.Add(car.DefaultDialIn?.ToString("0.000") ?? "—");
                    lvCars.Items.Add(item);
                }
            }

            lvCars.EndUpdate();
        }

        private void ClearCarsPanel()
        {
            lblCarsHeader.Text = "Cars";
            lvCars.Items.Clear();
        }

        private void UpdateButtonStates()
        {
            bool hasDriver = lvDrivers.SelectedItems.Count > 0;
            bool hasCar = lvCars.SelectedItems.Count > 0;

            btnAddDriver.Enabled = true;
            btnEditDriver.Enabled = hasDriver;
            btnDeleteDriver.Enabled = hasDriver;
            btnSetQualTime.Enabled = hasDriver;
            btnDriverStats.Enabled = hasDriver;

            btnAddCar.Enabled = hasDriver;
            btnEditCar.Enabled = hasCar;
            btnDeleteCar.Enabled = hasCar;
        }
    }
}
