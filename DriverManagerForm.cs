using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace RCDragManagerProd
{
    public partial class DriverManagerForm : Form
    {
        private DriverRepository repository;
        private Driver selectedDriver;

        public DriverManagerForm()
        {
            InitializeComponent();
            string dbPath = "race_data.db";
            repository = new DriverRepository(dbPath);

            SetupDriverDetailsGrid();
            LoadDrivers();
        }

        public DriverManagerForm(DriverRepository repo)
        {
            InitializeComponent();
            repository = repo;

            SetupDriverDetailsGrid();
            LoadDrivers();
        }

        private void SetupDriverDetailsGrid()
        {
            lvDriverDetails.Columns.Clear();
            lvDriverDetails.Columns.Add("Field", 150);
            lvDriverDetails.Columns.Add("Value", 300);
        }

        private void LoadDrivers()
        {
            lstDrivers.Items.Clear();
            var drivers = repository.GetAllDrivers();

            foreach (var d in drivers.OrderBy(d => d.Name))
            {
                lstDrivers.Items.Add($"{d.Id}: {d.Name}");
            }
        }

        private void lstDrivers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstDrivers.SelectedIndex == -1)
            {
                selectedDriver = null;
                lvDriverDetails.Items.Clear();
                return;
            }

            string selected = lstDrivers.SelectedItem.ToString();
            int driverId = int.Parse(selected.Split(':')[0]);
            selectedDriver = repository.GetDriverById(driverId);
            LoadDriverDetails();
        }

        private void LoadDriverDetails()
        {
            lvDriverDetails.Items.Clear();
            if (selectedDriver == null) return;

            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Name", selectedDriver.Name }));
            lvDriverDetails.Items.Add(new ListViewItem(new[] {
                "Qual Time",
                selectedDriver.QualTime.HasValue ? selectedDriver.QualTime.Value.ToString("0.000") : ""
            }));

            lvDriverDetails.Items.Add(new ListViewItem(new[] { "State", selectedDriver.State ?? "" }));
            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Notes", selectedDriver.Notes ?? "" }));
            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Wins", selectedDriver.TotalWins.ToString() }));
            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Losses", selectedDriver.TotalLosses.ToString() }));
            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Events Entered", selectedDriver.EventsEntered.ToString() }));
            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Events Won", selectedDriver.EventsWon.ToString() }));

            lvDriverDetails.Items.Add(new ListViewItem(new[] { "--- Cars ---", "" }));

            foreach (var car in selectedDriver.Cars)
            {
                string carInfo = $"{car.CarName} - {car.ClassType} - {car.DefaultDialIn?.ToString("0.000") ?? ""}";
                lvDriverDetails.Items.Add(new ListViewItem(new[] { "Car", carInfo }));
            }
        }

        // ADD DRIVER — now uses AddDriverAndCarDialog

        private void btnAddDriver_Click(object sender, EventArgs e)
        {
            using (var dlg = new AddDriverAndCarDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var newDriver = new Driver
                    {
                        Name = dlg.DriverName,
                        Notes = "",
                        TotalWins = 0,
                        TotalLosses = 0,
                        EventsEntered = 0,
                        EventsWon = 0,
                        State = "", // no state on initial add
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
                    LoadDrivers();
                }
            }
        }

        // EDIT DRIVER — now uses EditDriverDialog

        private void btnEditDriver_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Select a driver to edit.");
                return;
            }

            var dlg = new EditDriverDialog(selectedDriver.Name, selectedDriver.State);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                selectedDriver.Name = dlg.DriverName;
                selectedDriver.State = dlg.State;
                repository.UpdateDriver(selectedDriver);
                LoadDrivers();
                LoadDriverDetails();
            }
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
            if (confirm == DialogResult.Yes)
            {
                repository.DeleteDriver(selectedDriver.Id);
                selectedDriver = null;
                LoadDrivers();
                lvDriverDetails.Items.Clear();
            }
        }

        // ADD CAR

        private void btnAddCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Select a driver first.");
                return;
            }

            var dlg = new AddCarDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                selectedDriver.Cars.Add(dlg.NewCar);
                repository.UpdateDriver(selectedDriver);
                LoadDriverDetails();
            }
        }

        // EDIT CAR

        private void btnEditCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null || selectedDriver.Cars.Count == 0)
            {
                MessageBox.Show("This driver has no cars to edit.");
                return;
            }

            if (lvDriverDetails.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a car from the list below to edit.");
                return;
            }

            var selectedItem = lvDriverDetails.SelectedItems[0];

            // Skip non-car rows
            if (selectedItem.Text != "Car")
            {
                MessageBox.Show("Please select a Car row to edit.");
                return;
            }

            // Determine which car row this is (since we add "--- Cars ---" first)
            int carListIndex = 0;
            int rowIndex = lvDriverDetails.Items.IndexOf(selectedItem);

            // Find first Car row in list to calculate offset
            for (int i = 0; i < lvDriverDetails.Items.Count; i++)
            {
                if (lvDriverDetails.Items[i].Text == "--- Cars ---")
                {
                    carListIndex = rowIndex - i - 1;
                    break;
                }
            }

            if (carListIndex < 0 || carListIndex >= selectedDriver.Cars.Count)
            {
                MessageBox.Show("Could not match selected car.");
                return;
            }

            var car = selectedDriver.Cars[carListIndex];
            var editDlg = new AddCarDialog(car);
            if (editDlg.ShowDialog() == DialogResult.OK)
            {
                car.CarName = editDlg.NewCar.CarName;
                car.ClassType = editDlg.NewCar.ClassType;
                car.DefaultDialIn = editDlg.NewCar.DefaultDialIn;
                repository.UpdateDriver(selectedDriver);
                LoadDriverDetails();
            }
        }


        // DELETE CAR

        private void btnDeleteCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null || selectedDriver.Cars.Count == 0)
            {
                MessageBox.Show("This driver has no cars to delete.");
                return;
            }

            if (lvDriverDetails.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a car from the list below to delete.");
                return;
            }

            var selectedItem = lvDriverDetails.SelectedItems[0];

            if (selectedItem.Text != "Car")
            {
                MessageBox.Show("Please select a Car row to delete.");
                return;
            }

            int carListIndex = 0;
            int rowIndex = lvDriverDetails.Items.IndexOf(selectedItem);

            for (int i = 0; i < lvDriverDetails.Items.Count; i++)
            {
                if (lvDriverDetails.Items[i].Text == "--- Cars ---")
                {
                    carListIndex = rowIndex - i - 1;
                    break;
                }
            }

            if (carListIndex < 0 || carListIndex >= selectedDriver.Cars.Count)
            {
                MessageBox.Show("Could not match selected car.");
                return;
            }

            var car = selectedDriver.Cars[carListIndex];

            var confirm = MessageBox.Show($"Delete car '{car.CarName}'?", "Confirm Delete", MessageBoxButtons.YesNo);
            if (confirm == DialogResult.Yes)
            {
                selectedDriver.Cars.RemoveAt(carListIndex);
                repository.UpdateDriver(selectedDriver);
                LoadDriverDetails();
            }
        }


        // SAVE & CLOSE

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            if (selectedDriver != null)
            {
                repository.UpdateDriver(selectedDriver);
            }
            this.Close();
        }
    }
}
