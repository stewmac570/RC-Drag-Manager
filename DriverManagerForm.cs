using System;
using System.Linq;
using System.Windows.Forms;

namespace RCDragManager
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

        // ================= ADD DRIVER =================

        private void btnAddDriver_Click(object sender, EventArgs e)
        {
            var dlg = new AddDriverDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var newDriver = new Driver
                {
                    Name = dlg.DriverName,
                    QualTime = dlg.QualTime,
                    Notes = ""
                };

                repository.AddDriver(newDriver);
                LoadDrivers();
            }
        }

        // ================= EDIT DRIVER =================

        private void btnEditDriver_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null)
            {
                MessageBox.Show("Select a driver to edit.");
                return;
            }

            var dlg = new AddDriverDialog();
            dlg.DriverName = selectedDriver.Name;
            dlg.QualTime = selectedDriver.QualTime ?? 0.0;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                selectedDriver.Name = dlg.DriverName;
                selectedDriver.QualTime = dlg.QualTime;
                repository.UpdateDriver(selectedDriver);
                LoadDrivers();
            }
        }

        // ================= DELETE DRIVER =================

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

        // ================= ADD CAR =================

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

        // ================= EDIT CAR =================

        private void btnEditCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null || selectedDriver.Cars.Count == 0)
            {
                MessageBox.Show("This driver has no cars to edit.");
                return;
            }

            var dlg = new SelectCarDialog(selectedDriver.Cars);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var car = dlg.SelectedCar;
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
        }

        // ================= DELETE CAR =================

        private void btnDeleteCar_Click(object sender, EventArgs e)
        {
            if (selectedDriver == null || selectedDriver.Cars.Count == 0)
            {
                MessageBox.Show("This driver has no cars to delete.");
                return;
            }

            var dlg = new SelectCarDialog(selectedDriver.Cars);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var car = dlg.SelectedCar;
                selectedDriver.Cars.Remove(car);
                repository.UpdateDriver(selectedDriver);
                LoadDriverDetails();
            }
        }

        // ================= SAVE CHANGES (not yet used) =================

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
