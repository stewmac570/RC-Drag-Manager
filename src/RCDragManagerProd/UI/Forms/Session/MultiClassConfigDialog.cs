using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    /// <summary>
    /// Carries existing values into MultiClassConfigDialog for an Edit operation.
    /// Pass null for an Add operation.
    /// </summary>
    public class MultiClassConfigDialogValues
    {
        public string ClassName { get; set; }
        public string RaceType { get; set; }
        public string ClassType { get; set; }
        public double? FixedDialIn { get; set; }
        public string Variant { get; set; }
        public int? RoundsToRun { get; set; }
        public List<RaceSessionDriverEntry> DriverEntries { get; set; }
    }

    public partial class MultiClassConfigDialog : Form
    {
        private readonly DriverRepository _driverRepo;
        private List<Driver> _allDrivers = new List<Driver>();
        private readonly Dictionary<int, double?> _dialInOverrides = new Dictionary<int, double?>();
        private readonly HashSet<int> _checkedDriverIds = new HashSet<int>();
        private bool _suppressRosterEvents = false;

        private ComboBox cmbFilterCar;
        private ComboBox cmbFilterClass;
        private ComboBox cmbFilterState;

        private string _selectedRaceType = RaceTypes.RoundRobin;

        public string ClassName { get; private set; }
        public string RaceType { get; private set; }
        public string ClassType { get; private set; }
        public double? FixedDialIn { get; private set; }
        public string Variant { get; private set; }
        public int? RoundsToRun { get; private set; }
        public List<RaceSessionDriverEntry> BuiltDriverEntries { get; private set; }

        public MultiClassConfigDialog(string connectionString,
                                       MultiClassConfigDialogValues existing = null)
        {
            InitializeComponent();

            _driverRepo = new DriverRepository(connectionString);

            CreateFilterControls();
            FillFilterCombos();
            PopulateDriverList(existing);

            if (existing != null)
                LoadExistingValues(existing);

            WireRaceTypeCard(pnlCardProLadder);
            WireRaceTypeCard(pnlCardRandomDraw);
            WireRaceTypeCard(pnlCardRoundRobin);
            rbBracket.CheckedChanged += RbClassType_CheckedChanged;
            rbHeadsUp.CheckedChanged += RbClassType_CheckedChanged;
            rbDialIn.CheckedChanged  += RbClassType_CheckedChanged;
            lvDrivers.SelectedIndexChanged += LvDrivers_SelectedIndexChanged;
            lvDrivers.ItemChecked += LvDrivers_ItemChecked;
            txtDialInOverride.Leave += TxtDialInOverride_Leave;
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += BtnCancel_Click;
            btnAddNewDriver.Click += BtnAddNewDriver_Click;

            SelectRaceTypeCard(_selectedRaceType);
            UpdateClassTypeUi();
        }

        // ── Filter controls ───────────────────────────────────────────────────

        private void CreateFilterControls()
        {
            int comboW = 120, labelToBox = 4, groupGap = 12;
            int y = btnAddNewDriver.Top;
            int x = btnAddNewDriver.Right + 8;

            int carLblW   = TextRenderer.MeasureText("Car:",   this.Font).Width;
            int classLblW = TextRenderer.MeasureText("Class:", this.Font).Width;
            int stateLblW = TextRenderer.MeasureText("State:", this.Font).Width;

            var lblCar = new Label { Text = "Car:", AutoSize = true, Left = x, Top = y + 6 };
            Controls.Add(lblCar); lblCar.BringToFront();
            cmbFilterCar = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = lblCar.Left + carLblW + labelToBox,
                Top = y + 2,
                Width = comboW
            };
            Controls.Add(cmbFilterCar); cmbFilterCar.BringToFront();

            int nextX = cmbFilterCar.Left + comboW + groupGap;
            var lblClass = new Label { Text = "Class:", AutoSize = true, Left = nextX, Top = y + 6 };
            Controls.Add(lblClass); lblClass.BringToFront();
            cmbFilterClass = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = lblClass.Left + classLblW + labelToBox,
                Top = y + 2,
                Width = comboW
            };
            Controls.Add(cmbFilterClass); cmbFilterClass.BringToFront();

            nextX = cmbFilterClass.Left + comboW + groupGap;
            var lblState = new Label { Text = "State:", AutoSize = true, Left = nextX, Top = y + 6 };
            Controls.Add(lblState); lblState.BringToFront();
            cmbFilterState = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = lblState.Left + stateLblW + labelToBox,
                Top = y + 2,
                Width = comboW
            };
            Controls.Add(cmbFilterState); cmbFilterState.BringToFront();

            cmbFilterCar.SelectedIndexChanged   += FilterChanged;
            cmbFilterClass.SelectedIndexChanged += FilterChanged;
            cmbFilterState.SelectedIndexChanged += FilterChanged;

            if (lvDrivers.Columns.Count == 5)
                lvDrivers.Columns.Add("State", 70, HorizontalAlignment.Left);
        }

        private void FillFilterCombos()
        {
            _allDrivers = _driverRepo.GetAllDrivers() ?? new List<Driver>();

            var carNames = _allDrivers
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

            cmbFilterClass.Items.Clear();
            cmbFilterClass.Items.AddRange(new object[] { "(All)", "Heads Up", "Bracket", "Dial In" });
            cmbFilterClass.SelectedIndex = 0;

            var states = _allDrivers
                .Select(d => d.State)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            cmbFilterState.Items.Clear();
            cmbFilterState.Items.Add("(All)");
            foreach (var s in states) cmbFilterState.Items.Add(s);
            cmbFilterState.SelectedIndex = 0;
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            PopulateDriverList(null);
        }

        // ── Driver list ───────────────────────────────────────────────────────

        private void PopulateDriverList(MultiClassConfigDialogValues existing)
        {
            if (existing != null)
            {
                _checkedDriverIds.Clear();
                // Only treat saved DialIn values as per-driver overrides when the existing
                // class was Dial-In. For Heads Up the DialIn is null; for Bracket Class it
                // is the FixedDialIn applied to every driver — neither is a true override.
                bool wasDialInClass = string.Equals(
                    existing.ClassType, "Dial-In", StringComparison.OrdinalIgnoreCase);
                foreach (var entry in existing.DriverEntries ?? new List<RaceSessionDriverEntry>())
                {
                    _checkedDriverIds.Add(entry.DriverID);
                    if (wasDialInClass && entry.DialIn.HasValue)
                        _dialInOverrides[entry.DriverID] = entry.DialIn;
                }
            }

            string carFilter   = cmbFilterCar?.SelectedItem?.ToString()   ?? "(All)";
            string classFilter = cmbFilterClass?.SelectedItem?.ToString() ?? "(All)";
            string stateFilter = cmbFilterState?.SelectedItem?.ToString() ?? "(All)";

            _suppressRosterEvents = true;
            lvDrivers.BeginUpdate();
            try
            {
                lvDrivers.Items.Clear();
                foreach (var driver in _allDrivers)
                {
                    if (stateFilter != "(All)" &&
                        !string.Equals(driver.State ?? "", stateFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var car = driver.Cars?.FirstOrDefault();

                    if (classFilter != "(All)" &&
                        !string.Equals(car?.ClassType ?? "", classFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (carFilter != "(All)" &&
                        !string.Equals(car?.CarName ?? "", carFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    double? defaultDialIn = car?.DefaultDialIn;

                    var item = new ListViewItem(driver.Name);
                    item.SubItems.Add(car?.CarName ?? "");
                    item.SubItems.Add(car?.ClassType ?? "");
                    item.SubItems.Add(defaultDialIn.HasValue ? defaultDialIn.Value.ToString("F3") : "");

                    string overrideText = _dialInOverrides.TryGetValue(driver.Id, out var ov) && ov.HasValue
                        ? ov.Value.ToString("F3") : "";
                    item.SubItems.Add(overrideText);

                    item.SubItems.Add(driver.State ?? "");

                    item.Tag = driver.Id;
                    item.Checked = _checkedDriverIds.Contains(driver.Id);
                    lvDrivers.Items.Add(item);
                }
            }
            finally
            {
                lvDrivers.EndUpdate();
                _suppressRosterEvents = false;
            }
        }

        private void LoadExistingValues(MultiClassConfigDialogValues existing)
        {
            txtClassName.Text = existing.ClassName ?? "";

            // Race type
            if (!string.IsNullOrEmpty(existing.RaceType))
            {
                SelectRaceTypeCard(existing.RaceType);
            }

            // Class type (Heads Up / Bracket Class / Dial-In)
            if (string.Equals(existing.ClassType, "Bracket Class", StringComparison.OrdinalIgnoreCase))
            {
                rbBracket.Checked = true;
                if (existing.FixedDialIn.HasValue)
                    txtFixedDialIn.Text = existing.FixedDialIn.Value.ToString("F3");
            }
            else if (string.Equals(existing.ClassType, "Dial-In", StringComparison.OrdinalIgnoreCase))
            {
                rbDialIn.Checked = true;
            }
            else
            {
                rbHeadsUp.Checked = true;
            }

            // Round Robin variant: Standard → buyback checked, QMDRA → buyback unchecked
            chkBuybackRace.Checked = !string.Equals(existing.Variant, "QMDRA", StringComparison.OrdinalIgnoreCase);
            if (existing.RoundsToRun.HasValue)
                nudRoundsToRun.Value = existing.RoundsToRun.Value;
        }

        // ── ItemChecked — persistent source of truth ──────────────────────────

        private void LvDrivers_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suppressRosterEvents) return;
            if (e.Item?.Tag is int driverId)
            {
                if (e.Item.Checked) _checkedDriverIds.Add(driverId);
                else _checkedDriverIds.Remove(driverId);
            }
        }

        // ── Add New Driver ────────────────────────────────────────────────────

        private void BtnAddNewDriver_Click(object sender, EventArgs e)
        {
            using (var addDialog = new AddDriverAndCarDialog())
            {
                if (addDialog.ShowDialog() != DialogResult.OK) return;

                var newDriver = new Driver { Name = addDialog.DriverName, Cars = new List<Car>() };
                _driverRepo.AddDriver(newDriver);

                var insertedDriver = _driverRepo.GetAllDrivers()
                    .First(d => d.Name == newDriver.Name);

                var newCar = new Car
                {
                    CarName = addDialog.CarName,
                    ClassType = addDialog.ClassType,
                    DefaultDialIn = addDialog.DialIn
                };
                _driverRepo.AddCar(insertedDriver.Id, newCar);
            }

            FillFilterCombos();
            PopulateDriverList(null);
        }

        // ── Race type selection (card-based) ──────────────────────────────────

        private void WireRaceTypeCard(Panel card)
        {
            string raceType = (string)card.Tag;
            EventHandler clickHandler = (s, e) => SelectRaceTypeCard(raceType);
            card.Click += clickHandler;
            foreach (Control child in card.Controls)
                child.Click += clickHandler;
            card.Paint += RaceCard_Paint;
        }

        private void SelectRaceTypeCard(string raceType)
        {
            _selectedRaceType = raceType;

            bool isRR = string.Equals(raceType, RaceTypes.RoundRobin, StringComparison.OrdinalIgnoreCase);
            pnlRrConfig.Visible = isRR;

            UpdateCardBackground(pnlCardProLadder);
            UpdateCardBackground(pnlCardRandomDraw);
            UpdateCardBackground(pnlCardRoundRobin);

            pnlCardProLadder.Invalidate();
            pnlCardRandomDraw.Invalidate();
            pnlCardRoundRobin.Invalidate();
        }

        private void UpdateCardBackground(Panel card)
        {
            bool isSelected = string.Equals((string)card.Tag, _selectedRaceType, StringComparison.OrdinalIgnoreCase);
            card.BackColor = isSelected ? Color.FromArgb(209, 250, 229) : Color.White;
        }

        private void RaceCard_Paint(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            bool isSelected = string.Equals((string)p.Tag, _selectedRaceType, StringComparison.OrdinalIgnoreCase);
            var color = isSelected ? Color.FromArgb(16, 185, 129) : Color.FromArgb(220, 220, 220);
            int width = isSelected ? 2 : 1;
            using (var pen = new Pen(color, width))
            {
                var rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        // ── Class type selection ──────────────────────────────────────────────

        private void RbClassType_CheckedChanged(object sender, EventArgs e)
        {
            UpdateClassTypeUi();
        }

        private void UpdateClassTypeUi()
        {
            bool isBracket = rbBracket.Checked;
            lblFixedDialIn.Visible = isBracket;
            txtFixedDialIn.Visible = isBracket;
        }

        // ── Dial-in override editing ──────────────────────────────────────────

        private void LvDrivers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvDrivers.SelectedItems.Count == 0)
            {
                txtDialInOverride.Text = "";
                txtDialInOverride.Enabled = false;
                return;
            }

            int driverId = (int)lvDrivers.SelectedItems[0].Tag;
            txtDialInOverride.Enabled = true;
            txtDialInOverride.Text = _dialInOverrides.TryGetValue(driverId, out var val) && val.HasValue
                ? val.Value.ToString("F3")
                : "";
        }

        private void TxtDialInOverride_Leave(object sender, EventArgs e)
        {
            if (lvDrivers.SelectedItems.Count == 0) return;

            var item = lvDrivers.SelectedItems[0];
            int driverId = (int)item.Tag;
            string text = txtDialInOverride.Text.Trim();

            if (string.IsNullOrEmpty(text))
            {
                _dialInOverrides.Remove(driverId);
                item.SubItems[4].Text = "";
            }
            else if (double.TryParse(text, out double val))
            {
                _dialInOverrides[driverId] = val;
                item.SubItems[4].Text = val.ToString("F3");
            }
        }

        // ── OK / Cancel ───────────────────────────────────────────────────────

        private void BtnOk_Click(object sender, EventArgs e)
        {
            string className = txtClassName.Text.Trim();
            if (string.IsNullOrEmpty(className))
            {
                MessageBox.Show("Please enter a class name.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ClassName = className;
            RaceType  = _selectedRaceType ?? RaceTypes.RoundRobin;
            ClassType = rbHeadsUp.Checked ? "Heads Up" :
                        rbBracket.Checked ? "Bracket Class" : "Dial-In";

            if (rbBracket.Checked && double.TryParse(txtFixedDialIn.Text.Trim(), out double fd))
                FixedDialIn = fd;
            else
                FixedDialIn = null;

            bool isRR = string.Equals(RaceType, RaceTypes.RoundRobin, StringComparison.OrdinalIgnoreCase);

            if (isRR)
            {
                // Buyback checked → Standard variant; unchecked → QMDRA (all advance)
                Variant = chkBuybackRace.Checked ? "Standard" : "QMDRA";

                int n = (int)nudRoundsToRun.Value;
                if (n <= 0)
                {
                    MessageBox.Show("Rounds must be at least 1.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Logger.Log("[MULTICLASS][CFG][RR] BLOCKED OK → invalid Rounds.");
                    return;
                }
                RoundsToRun = n;
            }
            else
            {
                Variant = null;
                RoundsToRun = null;
            }

            if (isRR)
            {
                Logger.Log($"[MULTICLASS][CFG][RR] '{className}' → Variant='{Variant}', RoundsToRun={(RoundsToRun.HasValue ? RoundsToRun.Value.ToString() : "null")}");
            }

            BuiltDriverEntries = new List<RaceSessionDriverEntry>();
            foreach (var driverId in _checkedDriverIds)
            {
                var driver = _allDrivers.FirstOrDefault(d => d.Id == driverId);
                if (driver == null) continue;

                var car = driver.Cars?.FirstOrDefault();

                double? dialIn;
                if (rbBracket.Checked)
                    dialIn = FixedDialIn;
                else if (rbHeadsUp.Checked)
                    dialIn = null;
                else
                    dialIn = _dialInOverrides.TryGetValue(driverId, out var overrideVal)
                        ? overrideVal
                        : car?.DefaultDialIn;

                BuiltDriverEntries.Add(new RaceSessionDriverEntry
                {
                    DriverID   = driver.Id,
                    DriverName = driver.Name,
                    CarID      = car?.CarID ?? 0,
                    CarName    = car?.CarName ?? "",
                    ClassType  = ClassName,
                    DialIn     = dialIn
                });
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
