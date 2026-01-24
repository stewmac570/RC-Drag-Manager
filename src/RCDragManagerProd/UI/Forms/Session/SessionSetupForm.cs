// SessionSetupForm.cs
using RCDragManagerProd.Domain;        // RaceSession, RaceSessionDriverEntry
using RCDragManagerProd.Logging;
using RCDragManagerProd.RandomMode;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.RoundRobinMode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DCar = RCDragManagerProd.Domain.Car;
// Aliases to avoid any namespace confusion in this file
using DDriver = RCDragManagerProd.Domain.Driver;

namespace RCDragManagerProd.UI.Forms
{
    public partial class SessionSetupForm : Form
    {
        private DriverRepository repository;

        private List<(DDriver driver, DCar car)> allEligibleDrivers;
        private List<(DDriver driver, DCar car)> eventRoster;

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

            eventRoster = new List<(DDriver, DCar)>();
            allEligibleDrivers = new List<(DDriver, DCar)>();

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
    }
}
