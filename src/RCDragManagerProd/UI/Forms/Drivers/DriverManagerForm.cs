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
            WireGridShortcuts();
            LoadDrivers();
        }

        public DriverManagerForm(DriverRepository repo)
        {
            InitializeComponent();
            repository = repo ?? new DriverRepository(Program.ConnectionString);
            WireGridShortcuts();
            LoadDrivers();
        }

        // Double-click a driver/car row to open its existing edit dialog (issue #263).
        // Buttons remain the explicit actions; delete stays button-only. Guarded by
        // HitTest so a double-click on empty space is ignored.
        private void WireGridShortcuts()
        {
            lvDrivers.DoubleClick += (s, e) =>
            {
                if (lvDrivers.HitTest(lvDrivers.PointToClient(Cursor.Position)).Item != null)
                    btnEditDriver_Click(s, e);
            };
            lvCars.DoubleClick += (s, e) =>
            {
                if (lvCars.HitTest(lvCars.PointToClient(Cursor.Position)).Item != null)
                    btnEditCar_Click(s, e);
            };
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            LoadDrivers();

            if (selectedDriver != null)
                ShowCarsForDriver(selectedDriver);
        }
    }
}
