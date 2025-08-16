using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.UI.Forms
{
    public partial class DriverManagerForm : Form
    {
        private readonly DriverRepository repository;
        private Driver selectedDriver;

        public DriverManagerForm()
        {
            InitializeComponent();
            repository = new DriverRepository("race_data.db");

            SetupDriverDetailsGrid();
            LoadDrivers();
        }

        public DriverManagerForm(DriverRepository repo)
        {
            InitializeComponent();
            repository = repo ?? new DriverRepository("race_data.db");

            SetupDriverDetailsGrid();
            LoadDrivers();
        }

        // ---------- UI wiring ----------

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // reload list + details from DB so stats are always current
            LoadDrivers();
            if (selectedDriver != null)
                ShowDriverDetails(selectedDriver.Id);
        }

        private void SetupDriverDetailsGrid()
        {
            lvDriverDetails.Columns.Clear();
            lvDriverDetails.View = View.Details;
            lvDriverDetails.FullRowSelect = true;
            lvDriverDetails.Columns.Add("Field", 150);
            lvDriverDetails.Columns.Add("Value", 300);
        }

        private void LoadDrivers()
        {
            var previousId = selectedDriver?.Id ?? 0;

            lstDrivers.Items.Clear();
            var drivers = repository.GetAllDrivers();

            foreach (var d in drivers.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                lstDrivers.Items.Add($"{d.Id}: {d.Name}");

            // restore selection if possible
            if (previousId > 0)
            {
                for (int i = 0; i < lstDrivers.Items.Count; i++)
                {
                    var txt = lstDrivers.Items[i].ToString();
                    if (txt.StartsWith(previousId.ToString() + ":", StringComparison.Ordinal))
                    {
                        lstDrivers.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private static int ParseIdFromListText(object listItem)
        {
            if (listItem == null) return 0;
            var s = listItem.ToString();
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var idx = s.IndexOf(':');
            if (idx <= 0) return 0;
            return int.TryParse(s.Substring(0, idx).Trim(), out var id) ? id : 0;
        }

        private void AddDetail(string field, string value)
        {
            var it = new ListViewItem(field);
            it.SubItems.Add(value ?? "");
            lvDriverDetails.Items.Add(it);
        }

        private void ShowDriverDetails(int driverId)
        {
            // ALWAYS read fresh from DB
            selectedDriver = repository.GetDriverById(driverId);

            lvDriverDetails.BeginUpdate();
            lvDriverDetails.Items.Clear();

            if (selectedDriver == null)
            {
                lvDriverDetails.EndUpdate();
                btnDriverStats.Enabled = false;
                return;
            }

            btnDriverStats.Enabled = true;

            AddDetail("Name", selectedDriver.Name);
            AddDetail("Qual Time", selectedDriver.QualTime?.ToString("0.000") ?? "");
            AddDetail("State", selectedDriver.State ?? "");
            AddDetail("Notes", selectedDriver.Notes ?? "");
            AddDetail("Wins", selectedDriver.TotalWins.ToString());
            AddDetail("Losses", selectedDriver.TotalLosses.ToString());
            AddDetail("Events Entered", selectedDriver.EventsEntered.ToString());

            // ✅ show the DB column we maintain when the Final is decided
            AddDetail("Events Won", selectedDriver.EventsWon.ToString());

            AddDetail("--- Cars ---", "");
            if (selectedDriver.Cars != null)
            {
                foreach (var car in selectedDriver.Cars)
                {
                    var dial = car.DefaultDialIn?.ToString("0.000") ?? "-";
                    AddDetail("Car", $"{car.CarName} - {car.ClassType} - {dial}");
                }
            }

            lvDriverDetails.EndUpdate();
        }

        // ---------- Events ----------

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

        // ADD DRIVER
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

        // EDIT DRIVER
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

        // DELETE DRIVER
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

        // ADD CAR
        private void btnAddCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Select a driver first.");
                return;
            }

            using (var dlg = new AddCarDialog())   // adding a NEW car
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                selectedDriver.Cars ??= new List<Car>();
                selectedDriver.Cars.Add(dlg.NewCar);

                repository.UpdateDriver(selectedDriver);
                Logger.Log($"[CARS] Added car '{dlg.NewCar.CarName}' to driver #{selectedDriver.Id}.");

                ShowDriverDetails(selectedDriver.Id);
            }
        }

        // EDIT CAR
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

            // map selected "Car" row to cars list index
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

                ShowDriverDetails(selectedDriver.Id);
            }
        }

        // DELETE CAR
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

        // SET QUAL TIME
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

        // SAVE & CLOSE
        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            if (selectedDriver != null)
                repository.UpdateDriver(selectedDriver);

            Close();
        }

        // DRIVER STATS POPUP
        private void btnDriverStats_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Select a driver first.");
                return;
            }

            using (var statsForm = new DriverStatsForm(selectedDriver, "race_data.db"))
            {
                statsForm.ShowDialog();
            }
        }
    }
}
