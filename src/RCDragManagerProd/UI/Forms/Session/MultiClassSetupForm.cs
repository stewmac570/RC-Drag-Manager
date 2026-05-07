using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    public partial class MultiClassSetupForm : Form
    {
        private readonly string _connectionString;
        private readonly DriverRepository _driverRepo;

        private readonly List<ClassConfig> _classList = new List<ClassConfig>();

        public MultiClassEvent MultiClassEventResult { get; private set; }

        private class ClassConfig
        {
            public string ClassName { get; set; }
            public string RaceType { get; set; }
            public string ClassType { get; set; }
            public double? FixedDialIn { get; set; }
            public string Variant { get; set; }
            public int? RoundsToRun { get; set; }
            public List<RaceSessionDriverEntry> DriverEntries { get; set; }
        }

        public MultiClassSetupForm(string connectionString)
        {
            InitializeComponent();

            _connectionString = connectionString;
            _driverRepo = new DriverRepository(connectionString);

            btnAddClass.Click += BtnAddClass_Click;
            btnEditClass.Click += BtnEditClass_Click;
            btnRemoveClass.Click += BtnRemoveClass_Click;
            btnStartRace.Click += BtnStartRace_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        // ── Class list management ─────────────────────────────────────────────

        private void AddClass(ClassConfig config)
        {
            foreach (var existing in _classList)
            {
                if (string.Equals(existing.ClassName, config.ClassName, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"A class named '{config.ClassName}' already exists. " +
                                    "Please use a different name.",
                                    "Duplicate Class", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
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
            using (var dlg = new MultiClassConfigDialog(_connectionString))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    AddClass(new ClassConfig
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

            using (var dlg = new MultiClassConfigDialog(_connectionString, existing))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _classList[idx] = new ClassConfig
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
            foreach (var cc in _classList)
            {
                if (cc.DriverEntries.Count == 0)
                {
                    MessageBox.Show(
                        $"Class '{cc.ClassName}' has no drivers. " +
                        "Add at least one driver or remove the class.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            foreach (var cc in _classList)
            {
                foreach (var entry in cc.DriverEntries)
                {
                    _driverRepo.IncrementEventsEntered(entry.DriverID);
                }
            }

            MultiClassEventResult = new MultiClassEvent
            {
                EventName = txtEventName.Text.Trim(),
                EventDate = dtpEventDate.Value.Date
            };

            foreach (var cc in _classList)
            {
                bool isRR = string.Equals(cc.RaceType, RaceTypes.RoundRobin, StringComparison.OrdinalIgnoreCase);

                var session = new RaceSession
                {
                    EventName          = MultiClassEventResult.EventName,
                    EventDate          = MultiClassEventResult.EventDate,
                    RaceType           = cc.RaceType ?? RaceTypes.RoundRobin,
                    ClassType          = cc.ClassName,
                    FixedDialIn        = cc.FixedDialIn,
                    RoundRobinVariant  = isRR ? cc.Variant : null,
                    RoundsToRun        = isRR ? cc.RoundsToRun : null,
                    DriverEntries      = cc.DriverEntries
                };
                MultiClassEventResult.ClassSessions.Add(session);

                if (isRR)
                {
                    Logger.Log($"[MULTICLASS][START][RR] Class '{cc.ClassName}' → Variant='{session.RoundRobinVariant}', RoundsToRun={(session.RoundsToRun.HasValue ? session.RoundsToRun.Value.ToString() : "null")}");
                }
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
