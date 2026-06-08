using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.UI.Forms
{
    public static class DriverManagerCarEditFlow
    {
        public static bool TryApplyEditedCar(
            Driver selectedDriver,
            IReadOnlyList<int> displayedCarIds,
            int selectedDisplayCarIndex,
            Car editedValues)
        {
            if (selectedDriver == null || editedValues == null) return false;
            if (selectedDriver.Cars == null || selectedDriver.Cars.Count == 0) return false;
            if (displayedCarIds == null || selectedDisplayCarIndex < 0 || selectedDisplayCarIndex >= displayedCarIds.Count)
                return false;

            var selectedCarId = displayedCarIds[selectedDisplayCarIndex];
            var targetCar = selectedDriver.Cars.FirstOrDefault(c => c.CarID == selectedCarId);
            if (targetCar == null) return false;

            targetCar.CarName = editedValues.CarName;
            targetCar.ClassType = editedValues.ClassType;
            targetCar.DefaultDialIn = editedValues.DefaultDialIn;
            return true;
        }
    }

    public partial class DriverManagerForm
    {
        // ── Selection handlers ────────────────────────────────────────────────

        private void lvDrivers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvDrivers.SelectedItems.Count == 0)
            {
                selectedDriver = null;
                ClearCarsPanel();
                UpdateButtonStates();
                return;
            }

            var tag = lvDrivers.SelectedItems[0].Tag;
            if (!(tag is int id) || id <= 0)
            {
                selectedDriver = null;
                ClearCarsPanel();
                UpdateButtonStates();
                return;
            }

            selectedDriver = repository.GetDriverById(id);
            ShowCarsForDriver(selectedDriver);
            UpdateButtonStates();
        }

        private void lvCars_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        // ── Driver CRUD ───────────────────────────────────────────────────────

        private void btnAddDriver_Click(object sender, EventArgs e)
        {
            using (var dlg = new AddDriverAndCarDialog())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var newDriver = new Driver
                {
                    Name = dlg.DriverName,
                    Notes = "",
                    TotalWins = 0,
                    TotalLosses = 0,
                    EventsEntered = 0,
                    EventsWon = 0,
                    State = "",
                    Cars = new List<Car>()
                };

                var newCar = new Car
                {
                    CarName = dlg.CarName,
                    ClassType = dlg.ClassType,
                    DefaultDialIn = dlg.DialIn
                };

                newDriver.Cars.Add(newCar);
                repository.AddDriver(newDriver);

                Logger.Log($"[DRIVERS] Added '{newDriver.Name}' with car '{newCar.CarName}'.");

                selectedDriver = newDriver;
                LoadDrivers();
                ShowCarsForDriver(selectedDriver);
                UpdateButtonStates();
            }
        }

        private void btnEditDriver_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Select a driver to edit.");
                return;
            }

            using (var dlg = new EditDriverDialog(selectedDriver.Name, selectedDriver.State))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                selectedDriver.Name = dlg.DriverName;
                selectedDriver.State = dlg.State;
                repository.UpdateDriver(selectedDriver);

                LoadDrivers();
                ShowCarsForDriver(selectedDriver);
                UpdateButtonStates();
            }
        }

        private void btnDeleteDriver_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Select a driver to delete.");
                return;
            }

            var confirm = MessageBox.Show("Delete this driver?", "Confirm", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes) return;

            repository.DeleteDriver(selectedDriver.Id);
            Logger.Log($"[DRIVERS] Deleted id={selectedDriver.Id}.");
            selectedDriver = null;

            LoadDrivers();
            ClearCarsPanel();
            UpdateButtonStates();
        }

        // ── Car CRUD (Tag-only — no row-index math) ────────────────────────────

        private void btnAddCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null) return;

            using (var dlg = new AddCarDialog())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                selectedDriver.Cars ??= new List<Car>();
                selectedDriver.Cars.Add(dlg.NewCar);

                repository.UpdateDriver(selectedDriver);
                Logger.Log($"[CARS] Added car '{dlg.NewCar.CarName}' to driver #{selectedDriver.Id}.");

                RefreshDriverRowCarsCount(selectedDriver);
                ShowCarsForDriver(selectedDriver);
                UpdateButtonStates();
            }
        }

        private void btnEditCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null) return;
            if (lvCars.SelectedItems.Count == 0) return;

            var tag = lvCars.SelectedItems[0].Tag;
            if (!(tag is int carId) || carId <= 0) return;

            var currentCar = selectedDriver.Cars?.FirstOrDefault(c => c.CarID == carId);
            if (currentCar == null) return;

            using (var dlg = new AddCarDialog(currentCar))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var displayedCarIds = selectedDriver.Cars.Select(c => c.CarID).ToList();
                int selectedIndex = displayedCarIds.IndexOf(carId);

                if (!DriverManagerCarEditFlow.TryApplyEditedCar(
                    selectedDriver,
                    displayedCarIds,
                    selectedIndex,
                    dlg.NewCar))
                {
                    MessageBox.Show("Could not apply car edit.");
                    return;
                }

                repository.UpdateDriver(selectedDriver);
                Logger.Log($"[CARS] Updated car #{carId} for driver #{selectedDriver.Id}.");

                ShowCarsForDriver(selectedDriver);
                UpdateButtonStates();
            }
        }

        private void btnDeleteCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null) return;
            if (lvCars.SelectedItems.Count == 0) return;

            var tag = lvCars.SelectedItems[0].Tag;
            if (!(tag is int carId) || carId <= 0) return;

            var car = selectedDriver.Cars?.FirstOrDefault(c => c.CarID == carId);
            if (car == null) return;

            var confirm = MessageBox.Show($"Delete car '{car.CarName}'?", "Confirm Delete", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes) return;

            selectedDriver.Cars.Remove(car);
            repository.UpdateDriver(selectedDriver);
            Logger.Log($"[CARS] Deleted car #{carId} for driver #{selectedDriver.Id}.");

            RefreshDriverRowCarsCount(selectedDriver);
            ShowCarsForDriver(selectedDriver);
            UpdateButtonStates();
        }

        // ── Qual time + driver stats ──────────────────────────────────────────

        private void btnSetQualTime_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Please select a driver first.");
                return;
            }

            var driver = repository.GetDriverById(selectedDriver.Id);
            using (var dialog = new AddEditQualTimeDialog(driver.Name, driver.QualTime))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                if (dialog.QualifyingTime.HasValue)
                {
                    repository.UpdateQualifyingTime(driver.Id, dialog.QualifyingTime.Value);
                    Logger.Log($"[DRIVERS] Set QualTime for #{driver.Id} to {dialog.QualifyingTime.Value:0.000}");

                    // refresh the in-memory driver so the cars-grid qual-time column updates
                    selectedDriver = repository.GetDriverById(driver.Id);
                    LoadDrivers();
                    ShowCarsForDriver(selectedDriver);
                    UpdateButtonStates();
                }
            }
        }

        private void btnDriverStats_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Select a driver first.");
                return;
            }

            using (var statsForm = new DriverStatsForm(selectedDriver, Program.ConnectionString))
            {
                statsForm.ShowDialog(this);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // Updates only the Cars-count cell on the driver's row (avoids a full LoadDrivers
        // round-trip when only the car count changed).
        private void RefreshDriverRowCarsCount(Driver driver)
        {
            if (driver == null) return;

            foreach (ListViewItem item in lvDrivers.Items)
            {
                if (item.Tag is int id && id == driver.Id)
                {
                    int count = driver.Cars?.Count ?? 0;
                    // Cars column is the last (index 7) of 8
                    if (item.SubItems.Count >= 8)
                        item.SubItems[7].Text = count.ToString();
                    break;
                }
            }
        }
    }
}
