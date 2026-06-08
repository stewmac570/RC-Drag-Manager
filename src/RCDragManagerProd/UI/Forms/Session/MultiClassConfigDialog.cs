using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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

        private TextBox txtSearch;

        // Click-to-sort state for the driver ListView. _columnBaseText holds each
        // header's text without the ▲/▼ glyph so the indicator can be re-applied
        // cleanly on every sort.
        private readonly DriverColumnSorter _sorter = new DriverColumnSorter();
        private string[] _columnBaseText;

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

            CreateSearchControl();
            LoadDrivers();
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
            lvDrivers.DoubleClick += LvDrivers_DoubleClick;
            txtDialInOverride.Leave += TxtDialInOverride_Leave;
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += BtnCancel_Click;
            btnAddNewDriver.Click += BtnAddNewDriver_Click;

            SelectRaceTypeCard(_selectedRaceType);
            UpdateClassTypeUi();

            SetupSorting();
        }

        // ── Fit to screen ─────────────────────────────────────────────────────

        /// <summary>
        /// On a 14" laptop at 150% scaling (or 1366x768 at 100%) the design-time
        /// height exceeds the screen, which used to push the OK/Cancel buttons
        /// off-screen. Clamp the window to the screen working area and keep it
        /// fully on-screen; the docked button bar stays visible and the content
        /// panel scrolls when space is tight.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Rectangle workingArea = Screen.FromControl(this).WorkingArea;

            int newWidth = Math.Min(Width, workingArea.Width);
            int newHeight = Math.Min(Height, workingArea.Height);
            if (newWidth != Width || newHeight != Height)
                Size = new Size(newWidth, newHeight);

            int x = Math.Max(workingArea.Left, Math.Min(Left, workingArea.Right - Width));
            int y = Math.Max(workingArea.Top, Math.Min(Top, workingArea.Bottom - Height));
            Location = new Point(x, y);
        }

        // ── Search control ────────────────────────────────────────────────────

        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private void CreateSearchControl()
        {
            // The State column used to be added as a side effect of the (now
            // removed) filter row. State is kept as a display column, so its
            // creation is relocated here to survive the filter-row removal.
            if (lvDrivers.Columns.Count == 5)
                lvDrivers.Columns.Add("State", 70, HorizontalAlignment.Left);

            // A single search box fills the slot vacated by the three combos,
            // stretching from just right of "Add New Driver" to the list's edge.
            int left = btnAddNewDriver.Right + 8;
            txtSearch = new TextBox
            {
                Left = left,
                Top = btnAddNewDriver.Top + 4,
                Width = lvDrivers.Right - left,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlContent.Controls.Add(txtSearch); txtSearch.BringToFront();

            // Native cue text shown greyed when the box is empty and unfocused.
            // wParam = 0 → the cue hides as soon as the box gains focus.
            SendMessage(txtSearch.Handle, EM_SETCUEBANNER, IntPtr.Zero, "Search drivers...");

            txtSearch.TextChanged += SearchChanged;
        }

        private void LoadDrivers()
        {
            _allDrivers = _driverRepo.GetAllDrivers() ?? new List<Driver>();
        }

        private void SearchChanged(object sender, EventArgs e)
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

            string search = txtSearch?.Text?.Trim() ?? "";

            _suppressRosterEvents = true;
            lvDrivers.BeginUpdate();
            try
            {
                lvDrivers.Items.Clear();
                foreach (var driver in _allDrivers)
                {
                    // Case-insensitive "contains" match on driver name only.
                    // Empty search box shows every driver.
                    if (!string.IsNullOrEmpty(search) &&
                        (driver.Name ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    var car = driver.Cars?.FirstOrDefault();
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

        // ── Column sorting ────────────────────────────────────────────────────

        /// <summary>
        /// Wires click-to-sort on the driver ListView. Captures the unadorned
        /// header texts, installs the comparer, and applies the default sort
        /// (Driver Name ascending) once the list is populated.
        /// </summary>
        private void SetupSorting()
        {
            _columnBaseText = new string[lvDrivers.Columns.Count];
            for (int i = 0; i < lvDrivers.Columns.Count; i++)
                _columnBaseText[i] = lvDrivers.Columns[i].Text;

            _sorter.Column = 0;
            _sorter.Order = SortOrder.Ascending;
            lvDrivers.ListViewItemSorter = _sorter;
            lvDrivers.ColumnClick += LvDrivers_ColumnClick;

            lvDrivers.Sort();
            UpdateSortIndicators();
        }

        private void LvDrivers_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _sorter.Column)
            {
                // Same column again → flip direction.
                _sorter.Order = _sorter.Order == SortOrder.Ascending
                    ? SortOrder.Descending
                    : SortOrder.Ascending;
            }
            else
            {
                // New column → start ascending.
                _sorter.Column = e.Column;
                _sorter.Order = SortOrder.Ascending;
            }

            lvDrivers.Sort();
            UpdateSortIndicators();
        }

        /// <summary>
        /// Appends ▲ / ▼ to the active sort column header and strips it from the
        /// rest, using the captured base texts so glyphs never accumulate.
        /// </summary>
        private void UpdateSortIndicators()
        {
            if (_columnBaseText == null) return;

            for (int i = 0; i < lvDrivers.Columns.Count && i < _columnBaseText.Length; i++)
            {
                string baseText = _columnBaseText[i];
                lvDrivers.Columns[i].Text = i == _sorter.Column
                    ? baseText + (_sorter.Order == SortOrder.Descending ? " ▼" : " ▲")
                    : baseText;
            }
        }

        /// <summary>
        /// Sorts driver rows by a single column and direction. Dial-In (col 3)
        /// and Override Dial-In (col 4) compare numerically; everything else is
        /// case-insensitive text. Blank numeric cells sort to the bottom when
        /// ascending and to the top when descending (the ascending result puts
        /// blanks last, then the whole result is negated for descending).
        /// </summary>
        private sealed class DriverColumnSorter : System.Collections.IComparer
        {
            public int Column;
            public SortOrder Order = SortOrder.Ascending;

            public int Compare(object a, object b)
            {
                var x = (ListViewItem)a;
                var y = (ListViewItem)b;

                string xt = Column < x.SubItems.Count ? x.SubItems[Column].Text : "";
                string yt = Column < y.SubItems.Count ? y.SubItems[Column].Text : "";

                int result;
                if (Column == 3 || Column == 4)
                {
                    bool xHas = double.TryParse(xt, out double xv);
                    bool yHas = double.TryParse(yt, out double yv);

                    if (!xHas && !yHas) result = 0;
                    else if (!xHas) result = 1;        // blank after numeric (ascending → bottom)
                    else if (!yHas) result = -1;
                    else result = xv.CompareTo(yv);
                }
                else
                {
                    result = string.Compare(xt, yt, StringComparison.OrdinalIgnoreCase);
                }

                return Order == SortOrder.Descending ? -result : result;
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

            LoadDrivers();
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

        // Double-click is the consistent primary shortcut for the setup roster
        // (issues #261/#262): an unincluded driver is included; an already-included
        // driver in a Dial-In class opens the per-driver override editor. Other
        // class types have no per-driver dial-in, so an included row does nothing
        // rather than risk an accidental removal.
        private void LvDrivers_DoubleClick(object sender, EventArgs e)
        {
            var hit = lvDrivers.HitTest(lvDrivers.PointToClient(Cursor.Position));
            var item = hit.Item;
            if (item == null || !(item.Tag is int driverId)) return;

            if (!item.Checked)
            {
                item.Checked = true;   // LvDrivers_ItemChecked records the id
                return;
            }

            if (rbDialIn.Checked)
                EditDialInOverride(item, driverId);
        }

        // Small modal to set/update/clear a per-driver dial-in override. Mirrors the
        // race-console dial-in editor so the override column updates immediately on save.
        private void EditDialInOverride(ListViewItem item, int driverId)
        {
            _dialInOverrides.TryGetValue(driverId, out var current);

            using (var dlg = new Form())
            {
                dlg.Text = "Edit Dial-In Override";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(320, 120);
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;

                var lbl = new Label
                {
                    Text = "Dial-in override (seconds):",
                    AutoSize = true,
                    Location = new Point(16, 18)
                };

                var txt = new TextBox
                {
                    Text = current?.ToString("F3") ?? string.Empty,
                    Location = new Point(16, 40),
                    Width = 140
                };
                txt.SelectAll();

                var btnOk = new Button
                {
                    Text = "OK",
                    Location = new Point(16, 78),
                    Size = new Size(70, 26),
                    DialogResult = DialogResult.OK
                };

                var btnClear = new Button
                {
                    Text = "Clear",
                    Location = new Point(92, 78),
                    Size = new Size(64, 26)
                };
                btnClear.Click += (_, __) => txt.Clear();

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(224, 78),
                    Size = new Size(80, 26),
                    DialogResult = DialogResult.Cancel
                };

                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;
                dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnClear, btnCancel });

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                string val = txt.Text.Trim();
                if (string.IsNullOrEmpty(val))
                {
                    _dialInOverrides.Remove(driverId);
                    item.SubItems[4].Text = "";
                }
                else if (double.TryParse(val, out double parsed))
                {
                    _dialInOverrides[driverId] = parsed;
                    item.SubItems[4].Text = parsed.ToString("F3");
                }
                else
                {
                    RaceDialogs.Warn(this, "Invalid value — enter a number or leave blank to clear.", "Invalid Dial-In");
                }
            }
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
