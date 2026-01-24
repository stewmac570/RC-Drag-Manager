using System;
using System.Windows.Forms;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.UI.Forms
{
    public partial class DriverManagerForm : Form
    {
        private readonly DriverRepository repository;
        private Driver selectedDriver;

        public DriverManagerForm()
        {
            InitializeComponent();
            repository = new DriverRepository(Program.ConnectionString);

            SetupDriverDetailsGrid();
            LoadDrivers();
        }

        public DriverManagerForm(DriverRepository repo)
        {
            InitializeComponent();
            repository = repo ?? new DriverRepository(Program.ConnectionString);

            SetupDriverDetailsGrid();
            LoadDrivers();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            LoadDrivers();

            if (selectedDriver != null)
                ShowDriverDetails(selectedDriver.Id);
        }
    }
}
