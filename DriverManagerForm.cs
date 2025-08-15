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
                btnDriverStats.Enabled = false; // Disable button if no driver selected
                return;
            }

            string selected = lstDrivers.SelectedItem.ToString();
            int driverId = int.Parse(selected.Split(':')[0]);
            selectedDriver = repository.GetDriverById(driverId);
            LoadDriverDetails();

            btnDriverStats.Enabled = true; // Enable button when driver selected
        }


        private void LoadDriverDetails()
        {
            lvDriverDetails.Items.Clear();
            if (selectedDriver == null) return;

            // 🔎 Compute event wins from saved sessions (Final winners)
            int computedEventWins = ComputeEventsWonFromHistory(selectedDriver.Id);
            Logger.Log($"[STATS] DriverId={selectedDriver.Id} '{selectedDriver.Name}' → EventsWon(computed)={computedEventWins}");

            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Name", selectedDriver.Name }));
            lvDriverDetails.Items.Add(new ListViewItem(new[]
            {
        "Qual Time",
        selectedDriver.QualTime.HasValue ? selectedDriver.QualTime.Value.ToString("0.000") : ""
    }));

            lvDriverDetails.Items.Add(new ListViewItem(new[] { "State", selectedDriver.State ?? "" }));
            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Notes", selectedDriver.Notes ?? "" }));
            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Wins", selectedDriver.TotalWins.ToString() }));
            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Losses", selectedDriver.TotalLosses.ToString() }));
            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Events Entered", selectedDriver.EventsEntered.ToString() }));

            // ✅ Show computed event wins (not the stale DB column)
            var computedWins = repository.ComputeEventsWonFromSavedSessions(selectedDriver.Id);
            lvDriverDetails.Items.Add(new ListViewItem(new[] { "Events Won", computedWins.ToString() }));


            lvDriverDetails.Items.Add(new ListViewItem(new[] { "--- Cars ---", "" }));

            foreach (var car in selectedDriver.Cars)
            {
                string carInfo = $"{car.CarName} - {car.ClassType} - {car.DefaultDialIn?.ToString("0.000") ?? ""}";
                lvDriverDetails.Items.Add(new ListViewItem(new[] { "Car", carInfo }));
            }
        }


        // ADD DRIVER

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
                    LoadDrivers();
                }
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

            var dlg = new AddCarDialog(); // ✅ NO PARAMS — you're ADDING a new car
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                selectedDriver.Cars.Add(dlg.NewCar); // ✅ NewCar is correct
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

            if (selectedItem.Text != "Car")
            {
                MessageBox.Show("Please select a Car row to edit.");
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

            Car car = selectedDriver.Cars[carListIndex];
            AddCarDialog dlg = new AddCarDialog(car);

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                car.CarName = dlg.NewCar.CarName;
                car.ClassType = dlg.NewCar.ClassType;
                car.DefaultDialIn = dlg.NewCar.DefaultDialIn;

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

        // SET QUAL TIME

        private void btnSetQualTime_Click(object sender, EventArgs e)
        {
            if (lstDrivers.SelectedItem == null)
            {
                MessageBox.Show("Please select a driver first.");
                return;
            }

            string selectedText = lstDrivers.SelectedItem.ToString();
            int driverId = int.Parse(selectedText.Split(':')[0]);

            var driver = repository.GetDriverById(driverId);
            var dialog = new AddEditQualTimeDialog(driver.Name, driver.QualTime);

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (dialog.QualifyingTime.HasValue)
                {
                    repository.UpdateQualifyingTime(driverId, dialog.QualifyingTime.Value);
                    LoadDrivers();
                    lstDrivers.SelectedIndex = lstDrivers.Items
                        .Cast<string>()
                        .ToList()
                        .FindIndex(item => item.StartsWith(driverId.ToString() + ":"));
                }
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


        // BUTTON CLICK EVENT: Show Driver Stats
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
        // Count events where the driver won the FINAL.
        // Handles normal Pro Ladder and our Final-4 after Round Robin.
        // Uses whichever ladder size best matches the saved results (session size or 4).
        private int ComputeEventsWonFromHistory(int driverId)
        {
            try
            {
                var repo = new RaceSessionRepository("race_data.db");
                var summaries = repo.GetAllSessions();
                int wins = 0;

                foreach (var s in summaries)
                {
                    var session = repo.LoadSession(s.Id);
                    if (session?.SavedResults == null || session.SavedResults.Count == 0)
                        continue;

                    // Build candidate ladders and pick the one matching most results
                    var candidateSizes = new HashSet<int> { session.DriverEntries?.Count ?? 0, 4 };
                    candidateSizes.RemoveWhere(n => n <= 0);

                    int bestSize = 0;
                    int bestHit = -1;
                    var resultIds = session.SavedResults.Select(r => r.MatchId).ToHashSet();

                    foreach (var size in candidateSizes)
                    {
                        var ladder = ProLadder.GetLadder(size);
                        var ladderIds = ladder.Select(m => m.MatchId).ToHashSet();
                        int hits = resultIds.Count(id => ladderIds.Contains(id));
                        if (hits > bestHit) { bestHit = hits; bestSize = size; }
                    }

                    var bestLadder = bestSize > 0 ? ProLadder.GetLadder(bestSize) : new List<ProLadder.LadderMatch>();

                    // Find the FINAL in that ladder
                    var finalMatch = bestLadder.FirstOrDefault(m =>
                        string.Equals(m.RoundLabel, "Final", StringComparison.OrdinalIgnoreCase));

                    if (finalMatch == null)
                    {
                        Logger.Log($"[STATS] Session {s.Id} — no Final match in chosen ladder (size={bestSize}).");
                        continue;
                    }

                    var finalResult = session.SavedResults.FirstOrDefault(r => r.MatchId == finalMatch.MatchId);
                    if (finalResult != null && finalResult.WinnerDriverId == driverId)
                        wins++;
                }

                return wins;
            }
            catch (Exception ex)
            {
                Logger.Log($"[STATS][ERROR] ComputeEventsWonFromHistory: {ex}");
                return 0;
            }
        }


    }
}
