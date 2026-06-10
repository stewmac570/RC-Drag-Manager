using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    public partial class MultiClassSetupForm : Form
    {
        private readonly MultiClassSetupService _service;

        private readonly List<ClassConfigDto> _classList = new List<ClassConfigDto>();

        public MultiClassEvent MultiClassEventResult { get; private set; }

        public MultiClassSetupForm(string connectionString)
            : this(new MultiClassSetupService(new DriverRepository(connectionString)))
        {
        }

        internal MultiClassSetupForm(MultiClassSetupService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            InitializeComponent();

            btnAddClass.Click += BtnAddClass_Click;
            btnEditClass.Click += BtnEditClass_Click;
            btnRemoveClass.Click += BtnRemoveClass_Click;
            btnStartRace.Click += BtnStartRace_Click;
            btnCancel.Click += BtnCancel_Click;

            // Double-click a class row to open Edit Class (issue #264). Shortcut only —
            // the Edit Class button remains and Remove stays button-only. BtnEditClass_Click
            // already no-ops when nothing is selected, so empty-area double-clicks are safe.
            lvClasses.DoubleClick += BtnEditClass_Click;
        }

        // ── Class list management ─────────────────────────────────────────────

        private void AddClass(ClassConfigDto config)
        {
            var existingNames = new List<string>();
            foreach (var cc in _classList)
                existingNames.Add(cc.ClassName);

            string error = _service.ValidateClassName(config.ClassName, existingNames);
            if (error != null)
            {
                MessageBox.Show(error, "Duplicate Class", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _classList.Add(config);
            RefreshClassList();
        }

        private void RemoveSelectedClass()
        {
            if (lvClasses.SelectedItems.Count == 0) return;
            int idx = lvClasses.SelectedItems[0].Index;
            _classList.RemoveAt(idx);
            RefreshClassList();
        }

        private void RefreshClassList()
        {
            lvClasses.Items.Clear();
            foreach (var cc in _classList)
            {
                var item = new ListViewItem(cc.ClassName);
                item.SubItems.Add(cc.RaceType ?? "");
                item.SubItems.Add(cc.ClassType ?? "");
                // Rounds (N) only meaningful for Round Robin QMDRA
                item.SubItems.Add(cc.RoundsToRun.HasValue ? cc.RoundsToRun.Value.ToString() : "-");
                item.SubItems.Add(cc.DriverEntries.Count.ToString());
                lvClasses.Items.Add(item);
            }
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void BtnAddClass_Click(object sender, EventArgs e)
        {
            using (var dlg = new MultiClassConfigDialog(_service))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    AddClass(new ClassConfigDto
                    {
                        ClassName     = dlg.ClassName,
                        RaceType      = dlg.RaceType,
                        ClassType     = dlg.ClassType,
                        FixedDialIn   = dlg.FixedDialIn,
                        Variant       = dlg.Variant,
                        RoundsToRun   = dlg.RoundsToRun,
                        DriverEntries = dlg.BuiltDriverEntries
                    });
                }
            }
        }

        private void BtnEditClass_Click(object sender, EventArgs e)
        {
            if (lvClasses.SelectedItems.Count == 0) return;
            int idx = lvClasses.SelectedItems[0].Index;
            var cc = _classList[idx];

            var existing = new MultiClassConfigDialogValues
            {
                ClassName     = cc.ClassName,
                RaceType      = cc.RaceType,
                ClassType     = cc.ClassType,
                FixedDialIn   = cc.FixedDialIn,
                Variant       = cc.Variant,
                RoundsToRun   = cc.RoundsToRun,
                DriverEntries = cc.DriverEntries
            };

            using (var dlg = new MultiClassConfigDialog(_service, existing))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _classList[idx] = new ClassConfigDto
                    {
                        ClassName     = dlg.ClassName,
                        RaceType      = dlg.RaceType,
                        ClassType     = dlg.ClassType,
                        FixedDialIn   = dlg.FixedDialIn,
                        Variant       = dlg.Variant,
                        RoundsToRun   = dlg.RoundsToRun,
                        DriverEntries = dlg.BuiltDriverEntries
                    };
                    RefreshClassList();
                }
            }
        }

        private void BtnRemoveClass_Click(object sender, EventArgs e)
        {
            RemoveSelectedClass();
        }

        private void BtnStartRace_Click(object sender, EventArgs e)
        {
            string error = _service.ValidateCanStart(_classList);
            if (error != null)
            {
                MessageBox.Show(error, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                MultiClassEventResult = _service.StartEvent(
                    txtEventName.Text.Trim(),
                    dtpEventDate.Value.Date,
                    _classList);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Log($"[UI][MultiClassSetup] StartEvent failed: {ex}");
                MessageBox.Show("Failed to start the race event. Check the log for details.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
