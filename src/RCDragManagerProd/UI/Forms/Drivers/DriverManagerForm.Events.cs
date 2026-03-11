using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.UI.Forms
{
    public partial class DriverManagerForm
    {
        private void lstDrivers_SelectedIndexChanged(object sender, EventArgs e)
        {
            var id = ParseIdFromListText(lstDrivers.SelectedItem);
            if (id <= 0)
            {
                selectedDriver = null;
                lvDriverDetails.Items.Clear();
                btnDriverStats.Enabled = false;
                return;
            }

            ShowDriverDetails(id);
        }

        private void btnAddDriver_Click(object sender, EventArgs e)
        {
            using (var dlg = new AddDriverAndCarDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

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

                LoadDrivers();
                ShowDriverDetails(newDriver.Id);
            }
        }

        private void btnEditDriver_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Select a driver to edit.");
                return;
            }

            var dlg = new EditDriverDialog(selectedDriver.Name, selectedDriver.State);
            if (dlg.ShowDialog() != DialogResult.OK) return;

            selectedDriver.Name = dlg.DriverName;
            selectedDriver.State = dlg.State;
            repository.UpdateDriver(selectedDriver);

            LoadDrivers();
            ShowDriverDetails(selectedDriver.Id);
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
            lvDriverDetails.Items.Clear();
        }

        private void btnAddCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Select a driver first.");
                return;
            }

            using (var dlg = new AddCarDialog())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                selectedDriver.Cars ??= new List<Car>();
                selectedDriver.Cars.Add(dlg.NewCar);

                repository.UpdateDriver(selectedDriver);
                Logger.Log($"[CARS] Added car '{dlg.NewCar.CarName}' to driver #{selectedDriver.Id}.");

                ShowDriverDetails(selectedDriver.Id);
            }
        }

        private void btnEditCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null || selectedDriver.Cars == null || selectedDriver.Cars.Count == 0)
            {
                MessageBox.Show("This driver has no cars to edit.");
                return;
            }

            if (lvDriverDetails.SelectedItems.Count == 0 || lvDriverDetails.SelectedItems[0].Text != "Car")
            {
                MessageBox.Show("Select a Car row to edit.");
                return;
            }

            int rowIndex = lvDriverDetails.Items.IndexOf(lvDriverDetails.SelectedItems[0]);
            int headerIndex = -1;
            for (int i = 0; i < lvDriverDetails.Items.Count; i++)
            {
                if (lvDriverDetails.Items[i].Text == "--- Cars ---")
                {
                    headerIndex = i;
                    break;
                }
            }

            int carIndex = (headerIndex >= 0) ? (rowIndex - headerIndex - 1) : -1;
            if (carIndex < 0 || carIndex >= selectedDriver.Cars.Count)
            {
                MessageBox.Show("Could not match selected car.");
                return;
            }

            var car = selectedDriver.Cars[carIndex];
            using (var dlg = new AddCarDialog(car))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                car.CarName = dlg.NewCar.CarName;
                car.ClassType = dlg.NewCar.ClassType;
                car.DefaultDialIn = dlg.NewCar.DefaultDialIn;

                repository.UpdateDriver(selectedDriver);
                Logger.Log($"[CARS] Updated car for driver #{selectedDriver.Id}.");

                LoadDrivers();
                ShowDriverDetails(selectedDriver.Id);
            }
        }

        private void btnDeleteCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null || selectedDriver.Cars == null || selectedDriver.Cars.Count == 0)
            {
                MessageBox.Show("This driver has no cars to delete.");
                return;
            }

            if (lvDriverDetails.SelectedItems.Count == 0 || lvDriverDetails.SelectedItems[0].Text != "Car")
            {
                MessageBox.Show("Select a Car row to delete.");
                return;
            }

            int rowIndex = lvDriverDetails.Items.IndexOf(lvDriverDetails.SelectedItems[0]);
            int headerIndex = -1;
            for (int i = 0; i < lvDriverDetails.Items.Count; i++)
            {
                if (lvDriverDetails.Items[i].Text == "--- Cars ---")
                {
                    headerIndex = i;
                    break;
                }
            }

            int carIndex = (headerIndex >= 0) ? (rowIndex - headerIndex - 1) : -1;
            if (carIndex < 0 || carIndex >= selectedDriver.Cars.Count)
            {
                MessageBox.Show("Could not match selected car.");
                return;
            }

            var car = selectedDriver.Cars[carIndex];
            var confirm = MessageBox.Show($"Delete car '{car.CarName}'?", "Confirm Delete", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes) return;

            selectedDriver.Cars.RemoveAt(carIndex);
            repository.UpdateDriver(selectedDriver);
            Logger.Log($"[CARS] Deleted car for driver #{selectedDriver.Id}.");

            ShowDriverDetails(selectedDriver.Id);
        }

        private void btnSetQualTime_Click(object sender, EventArgs e)
        {
            var id = ParseIdFromListText(lstDrivers.SelectedItem);
            if (id <= 0)
            {
                MessageBox.Show("Please select a driver first.");
                return;
            }

            var driver = repository.GetDriverById(id);
            using (var dialog = new AddEditQualTimeDialog(driver.Name, driver.QualTime))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;

                if (dialog.QualifyingTime.HasValue)
                {
                    repository.UpdateQualifyingTime(id, dialog.QualifyingTime.Value);
                    Logger.Log($"[DRIVERS] Set QualTime for #{id} to {dialog.QualifyingTime.Value:0.000}");

                    LoadDrivers();
                    ShowDriverDetails(id);
                }
            }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            if (selectedDriver != null)
                repository.UpdateDriver(selectedDriver);

            Close();
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
                statsForm.ShowDialog();
            }
        }
    }
}
