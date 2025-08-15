using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

namespace RCDragManagerProd
{
    public partial class SessionSetupForm : Form
    {
        private DriverRepository repository;
        private List<(Driver driver, Car car)> allEligibleDrivers;
        private List<(Driver driver, Car car)> eventRoster;

        // runtime-only filter UI (NOT in Designer)
        private Panel pnlFilters;
        private ComboBox cmbFilterCar;
        private ComboBox cmbFilterClass;
        private ComboBox cmbFilterState;

        // Suppress ItemChecked during bulk rebuilds
        private bool _suppressRosterEvents = false;


        public RaceSession RaceSessionResult { get; private set; }

        public SessionSetupForm(DriverRepository repo)
        {
            InitializeComponent();

            repository = repo ?? throw new ArgumentNullException(nameof(repo));

            // create lists FIRST so early filter events can't null-ref
            eventRoster = new List<(Driver, Car)>();
            allEligibleDrivers = new List<(Driver, Car)>();

            CreateFilterControls();
            FillFilterCombos();

            rbHeadsUp.CheckedChanged += ClassSelectionChanged;
            rbBracket.CheckedChanged += ClassSelectionChanged;
            rbDialIn.CheckedChanged += ClassSelectionChanged;

            btnAddNewDriver.Click += BtnAddNewDriver_Click;
            btnStartRace.Click += BtnStartRace_Click;
            btnCancel.Click += BtnCancel_Click;

            lvEventRoster.CheckBoxes = true;
            lvEventRoster.ItemChecked += LvEventRoster_ItemChecked;

            RefreshDriverList();
        }


        private void CreateFilterControls()
        {
            // Host on the same container as the button/list
            var host = lvEventRoster.Parent ?? this;

            // Remove any old runtime panel/row we may have created earlier
            if (pnlFilters != null && !pnlFilters.IsDisposed)
            {
                try { host.Controls.Remove(pnlFilters); } catch { }
                pnlFilters.Dispose();
                pnlFilters = null;
            }

            // ---- Tunables (adjust these to taste) ----
            int comboW = 120;  // dropdown width
            int labelToBox = 4;   // gap between label and its dropdown
            int groupGap = 12;  // gap between (Car group) and (Class group), etc.

            // Start row position (same as Add New Driver row)
            int y = (btnAddNewDriver != null && !btnAddNewDriver.IsDisposed)
                        ? btnAddNewDriver.Top
                        : Math.Max(0, lvEventRoster.Top - 28);

            // Start X: immediately to the right of the Add button (8px gap)
            int x = (btnAddNewDriver != null && !btnAddNewDriver.IsDisposed)
                        ? btnAddNewDriver.Right + 8
                        : lvEventRoster.Left;

            // Measure label widths so text never clips
            int carLblW = TextRenderer.MeasureText("Car:", this.Font).Width;
            int classLblW = TextRenderer.MeasureText("Class:", this.Font).Width;
            int stateLblW = TextRenderer.MeasureText("State:", this.Font).Width;

            // Create labels + combos (added directly to host; no panel/tlp)
            var lblCar = new Label
            {
                Text = "Car:",
                AutoSize = true,
                Left = x,
                Top = y + 6
            };
            host.Controls.Add(lblCar);
            lblCar.BringToFront();

            cmbFilterCar = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = lblCar.Left + carLblW + labelToBox,
                Top = y + 2,
                Width = comboW
            };
            host.Controls.Add(cmbFilterCar);
            cmbFilterCar.BringToFront();

            // Next group: Class
            int nextX = cmbFilterCar.Left + comboW + groupGap;

            var lblClass = new Label
            {
                Text = "Class:",
                AutoSize = true,
                Left = nextX,
                Top = y + 6
            };
            host.Controls.Add(lblClass);
            lblClass.BringToFront();

            cmbFilterClass = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = lblClass.Left + classLblW + labelToBox,
                Top = y + 2,
                Width = comboW
            };
            host.Controls.Add(cmbFilterClass);
            cmbFilterClass.BringToFront();

            // Next group: State
            nextX = cmbFilterClass.Left + comboW + groupGap;

            var lblState = new Label
            {
                Text = "State:",
                AutoSize = true,
                Left = nextX,
                Top = y + 6
            };
            host.Controls.Add(lblState);
            lblState.BringToFront();

            cmbFilterState = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = lblState.Left + stateLblW + labelToBox,
                Top = y + 2,
                Width = comboW
            };
            host.Controls.Add(cmbFilterState);
            cmbFilterState.BringToFront();

            // Wire the one handler (re-attach every rebuild is fine)
            cmbFilterCar.SelectedIndexChanged += FilterChanged;
            cmbFilterClass.SelectedIndexChanged += FilterChanged;
            cmbFilterState.SelectedIndexChanged += FilterChanged;

            // Ensure the State column exists (5th column) once
            if (lvEventRoster.Columns.Count == 4)
                lvEventRoster.Columns.Add("State", 70, HorizontalAlignment.Left);

            Logger.Log($"[CREATE:FILTER] Row placed at Y={y}, X={x} (comboW={comboW}, labelToBox={labelToBox}, groupGap={groupGap}).");
        }
        private void FillFilterCombos()
        {
            try
            {
                var allDrivers = repository.GetAllDrivers() ?? new List<Driver>();

                // Car names
                var carNames = allDrivers
                    .SelectMany(d => d.Cars ?? new List<Car>())
                    .Select(c => c.CarName)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .ToList();

                cmbFilterCar.Items.Clear();
                cmbFilterCar.Items.Add("(All)");
                foreach (var n in carNames) cmbFilterCar.Items.Add(n);
                cmbFilterCar.SelectedIndex = 0;

                // Classes (fixed set)
                cmbFilterClass.Items.Clear();
                cmbFilterClass.Items.AddRange(new object[] { "(All)", "Heads Up", "Bracket", "Dial In" });
                cmbFilterClass.SelectedIndex = 0;

                // States
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

                Logger.Log("[CREATE:FILTER] Filter combos populated.");
            }
            catch (Exception ex)
            {
                Logger.Log($"[CREATE:FILTER][ERROR] FillFilterCombos failed: {ex}");
            }
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            var car = cmbFilterCar?.SelectedItem?.ToString() ?? "(null)";
            var cls = cmbFilterClass?.SelectedItem?.ToString() ?? "(null)";
            var state = cmbFilterState?.SelectedItem?.ToString() ?? "(null)";
            Logger.Log($"[CREATE:FILTER] Change → Car='{car}', Class='{cls}', State='{state}'");
            RefreshDriverList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Roster + selection
        // ─────────────────────────────────────────────────────────────────────
        private void RefreshDriverList()
        {
            // make sure lists exist
            allEligibleDrivers ??= new List<(Driver, Car)>();
            eventRoster ??= new List<(Driver, Car)>();

            // Active filter values
            string carFilter = (cmbFilterCar?.SelectedItem as string) ?? "(All)";
            string classFilter = (cmbFilterClass?.SelectedItem as string) ?? "(All)";
            string stateFilter = (cmbFilterState?.SelectedItem as string) ?? "(All)";
            bool useRadioClass = (classFilter == "(All)");

            Logger.Log($"[FILTER] Rebuild → Car='{carFilter}', Class='{classFilter}'(useRadio={useRadioClass}), State='{stateFilter}'");

            // Rebuild list without destroying the roster
            _suppressRosterEvents = true;
            lvEventRoster.BeginUpdate();
            try
            {
                lvEventRoster.Items.Clear();
                allEligibleDrivers.Clear();

                var drivers = repository.GetAllDrivers();

                foreach (var driver in drivers)
                {
                    // State filter (on driver)
                    if (stateFilter != "(All)" &&
                        !string.Equals(driver.State ?? "", stateFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (var car in driver.Cars)
                    {
                        bool eligible = false;

                        // Class – either from radios OR explicit class filter
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

                        // Car filter
                        if (carFilter != "(All)" &&
                            !string.Equals(car.CarName, carFilter, StringComparison.OrdinalIgnoreCase))
                            eligible = false;

                        if (!eligible) continue;

                        allEligibleDrivers.Add((driver, car));

                        var item = new ListViewItem(new string[]
                        {
                    driver.Name,
                    car.CarName,
                    car.ClassType,
                    car.DefaultDialIn?.ToString("0.000") ?? "-",
                    driver.State ?? ""
                        })
                        { Tag = (driver, car) };

                        // Re-apply check if this pair is already in the roster
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


        private void LvEventRoster_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suppressRosterEvents) return;

            if (e.Item?.Tag is ValueTuple<Driver, Car> t)
            {
                var (driver, car) = t;

                if (e.Item.Checked)
                {
                    if (!eventRoster.Any(x => x.driver.Id == driver.Id && x.car.CarID == car.CarID))
                    {
                        eventRoster.Add((driver, car));
                        Logger.Log($"[ROSTER] + Added  d#{driver.Id}:{driver.Name}  car#{car.CarID}:{car.CarName}");
                    }
                }
                else
                {
                    int removed = eventRoster.RemoveAll(x => x.driver.Id == driver.Id && x.car.CarID == car.CarID);
                    Logger.Log($"[ROSTER] - Removed d#{driver.Id}:{driver.Name}  car#{car.CarID}:{car.CarName}  (removed={removed})");
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

        // ─────────────────────────────────────────────────────────────────────
        // Buttons
        // ─────────────────────────────────────────────────────────────────────
        private void BtnAddNewDriver_Click(object sender, EventArgs e)
        {
            using (var addDialog = new AddDriverAndCarDialog())
            {
                if (addDialog.ShowDialog() != DialogResult.OK) return;

                var newDriver = new Driver
                {
                    Name = addDialog.DriverName,
                    Cars = new List<Car>()
                };
                repository.AddDriver(newDriver);

                var insertedDriver = repository.GetAllDrivers().First(d => d.Name == newDriver.Name);

                var newCar = new Car
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

            // +1 Events Entered for every driver in roster (if persisted)
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

            RaceSessionResult = new RaceSession
            {
                EventName = txtEventName.Text.Trim(),
                EventDate = dateRaceDate.Value.Date,
                RaceType = cmbRaceType.SelectedItem?.ToString() ?? "Pro Ladder",
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
