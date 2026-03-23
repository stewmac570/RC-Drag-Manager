using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RCDragManagerProd.Domain;
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
        public string Variant { get; set; }
        public int? RoundsToRun { get; set; }
        public List<RaceSessionDriverEntry> DriverEntries { get; set; }
    }

    public partial class MultiClassConfigDialog : Form
    {
        private readonly DriverRepository _driverRepo;
        private readonly List<Driver> _allDrivers;
        private readonly Dictionary<int, double?> _dialInOverrides = new Dictionary<int, double?>();

        public string ClassName { get; private set; }
        public string Variant { get; private set; }
        public int? RoundsToRun { get; private set; }
        public List<RaceSessionDriverEntry> BuiltDriverEntries { get; private set; }

        public MultiClassConfigDialog(string connectionString,
                                       MultiClassConfigDialogValues existing = null)
        {
            InitializeComponent();

            _driverRepo = new DriverRepository(connectionString);
            _allDrivers = _driverRepo.GetAllDrivers();

            PopulateDriverList(existing);

            if (existing != null)
                LoadExistingValues(existing);

            rbStandard.CheckedChanged += RbVariant_CheckedChanged;
            rbQmdra.CheckedChanged += RbVariant_CheckedChanged;
            lvDrivers.SelectedIndexChanged += LvDrivers_SelectedIndexChanged;
            txtDialInOverride.Leave += TxtDialInOverride_Leave;
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += BtnCancel_Click;

            UpdateRoundsVisibility();
        }

        // ── Driver list ──────────────────────────────────────────────────────

        private void PopulateDriverList(MultiClassConfigDialogValues existing)
        {
            lvDrivers.Items.Clear();

            HashSet<int> enteredIds = null;
            Dictionary<int, double?> existingDialIns = null;
            if (existing?.DriverEntries != null)
            {
                enteredIds = new HashSet<int>(existing.DriverEntries.Select(e => e.DriverID));
                existingDialIns = existing.DriverEntries
                    .Where(e => e.DialIn.HasValue)
                    .ToDictionary(e => e.DriverID, e => e.DialIn);
            }

            foreach (var driver in _allDrivers)
            {
                var car = driver.Cars.FirstOrDefault();
                double? defaultDialIn = car?.DefaultDialIn;

                var item = new ListViewItem(driver.Name);
                item.SubItems.Add(car?.CarName ?? "");
                item.SubItems.Add(defaultDialIn.HasValue ? defaultDialIn.Value.ToString("F3") : "");
                item.SubItems.Add(""); // override column — filled below if editing
                item.Tag = driver.Id;
                lvDrivers.Items.Add(item);

                if (enteredIds != null && enteredIds.Contains(driver.Id))
                {
                    item.Checked = true;
                    if (existingDialIns != null && existingDialIns.TryGetValue(driver.Id, out var dialIn))
                    {
                        _dialInOverrides[driver.Id] = dialIn;
                        item.SubItems[3].Text = dialIn.Value.ToString("F3");
                    }
                }
            }
        }

        private void LoadExistingValues(MultiClassConfigDialogValues existing)
        {
            txtClassName.Text = existing.ClassName ?? "";
            if (string.Equals(existing.Variant, "QMDRA", StringComparison.OrdinalIgnoreCase))
            {
                rbQmdra.Checked = true;
                if (existing.RoundsToRun.HasValue)
                    nudRoundsToRun.Value = existing.RoundsToRun.Value;
            }
            else
            {
                rbStandard.Checked = true;
            }
        }

        // ── Variant radio buttons ─────────────────────────────────────────────

        private void RbVariant_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRoundsVisibility();
        }

        private void UpdateRoundsVisibility()
        {
            bool isQmdra = rbQmdra.Checked;
            lblRoundsToRun.Visible = isQmdra;
            nudRoundsToRun.Visible = isQmdra;
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
                item.SubItems[3].Text = "";
            }
            else if (double.TryParse(text, out double val))
            {
                _dialInOverrides[driverId] = val;
                item.SubItems[3].Text = val.ToString("F3");
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
            Variant = rbQmdra.Checked ? "QMDRA" : "Standard";
            RoundsToRun = rbQmdra.Checked ? (int?)Convert.ToInt32(nudRoundsToRun.Value) : null;

            BuiltDriverEntries = new List<RaceSessionDriverEntry>();
            foreach (ListViewItem item in lvDrivers.CheckedItems)
            {
                int driverId = (int)item.Tag;
                var driver = _allDrivers.First(d => d.Id == driverId);
                var car = driver.Cars.FirstOrDefault();
                double? dialIn = _dialInOverrides.TryGetValue(driverId, out var overrideVal)
                    ? overrideVal
                    : car?.DefaultDialIn;

                BuiltDriverEntries.Add(new RaceSessionDriverEntry
                {
                    DriverID = driver.Id,
                    DriverName = driver.Name,
                    CarID = car?.CarID ?? 0,
                    CarName = car?.CarName ?? "",
                    ClassType = ClassName,
                    DialIn = dialIn
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
