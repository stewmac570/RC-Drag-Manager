using System.Windows.Forms;
using System.Drawing;

namespace RCDragManagerProd.UI.Forms
{
    partial class DriverManagerForm
    {
        private System.ComponentModel.IContainer components = null;

        // Driver grid (replaces lstDrivers)
        private ListView lvDrivers;
        private ColumnHeader colDriverName;
        private ColumnHeader colDriverState;
        private ColumnHeader colDriverWins;
        private ColumnHeader colDriverLosses;
        private ColumnHeader colDriverEvents;
        private ColumnHeader colDriverEventWins;
        private ColumnHeader colDriverQualTime;
        private ColumnHeader colDriverCars;

        // Cars header + grid (replaces lvDriverDetails Field/Value table)
        private Label lblCarsHeader;
        private ListView lvCars;
        private ColumnHeader colCarName;
        private ColumnHeader colCarClass;
        private ColumnHeader colCarDialIn;

        // Hosts the three docked controls above (lvDrivers / lblCarsHeader / lvCars)
        // so Dock=Top / Dock=Top / Dock=Fill stacks cleanly without colliding
        // with the form-level title and button columns.
        private Panel pnlContent;

        // Driver buttons (left column)
        private Button btnAddDriver;
        private Button btnEditDriver;
        private Button btnDeleteDriver;

        // Car / driver-meta buttons (right column)
        private Button btnAddCar;
        private Button btnEditCar;
        private Button btnDeleteCar;
        private Button btnSetQualTime;
        private Button btnDriverStats;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lvDrivers = new ListView();
            this.colDriverName = new ColumnHeader();
            this.colDriverState = new ColumnHeader();
            this.colDriverWins = new ColumnHeader();
            this.colDriverLosses = new ColumnHeader();
            this.colDriverEvents = new ColumnHeader();
            this.colDriverEventWins = new ColumnHeader();
            this.colDriverQualTime = new ColumnHeader();
            this.colDriverCars = new ColumnHeader();
            this.lblCarsHeader = new Label();
            this.lvCars = new ListView();
            this.colCarName = new ColumnHeader();
            this.colCarClass = new ColumnHeader();
            this.colCarDialIn = new ColumnHeader();
            this.pnlContent = new Panel();
            this.btnAddDriver = new Button();
            this.btnEditDriver = new Button();
            this.btnDeleteDriver = new Button();
            this.btnAddCar = new Button();
            this.btnEditCar = new Button();
            this.btnDeleteCar = new Button();
            this.btnSetQualTime = new Button();
            this.btnDriverStats = new Button();

            this.pnlContent.SuspendLayout();
            this.SuspendLayout();

            // ── Left column buttons (driver CRUD) ───────────────────────────────
            this.btnAddDriver.Location = new Point(20, 20);
            this.btnAddDriver.Size = new Size(180, 40);
            this.btnAddDriver.Text = "Add Driver";
            this.btnAddDriver.Click += new System.EventHandler(this.btnAddDriver_Click);

            this.btnEditDriver.Location = new Point(20, 80);
            this.btnEditDriver.Size = new Size(180, 40);
            this.btnEditDriver.Text = "Edit Driver";
            this.btnEditDriver.Click += new System.EventHandler(this.btnEditDriver_Click);

            this.btnDeleteDriver.Location = new Point(20, 140);
            this.btnDeleteDriver.Size = new Size(180, 40);
            this.btnDeleteDriver.Text = "Delete Driver";
            this.btnDeleteDriver.Click += new System.EventHandler(this.btnDeleteDriver_Click);

            // ── Right column buttons (cars + qual time + stats) ─────────────────
            this.btnAddCar.Location = new Point(722, 20);
            this.btnAddCar.Size = new Size(150, 40);
            this.btnAddCar.Text = "Add Car";
            this.btnAddCar.Click += new System.EventHandler(this.btnAddCar_Click);

            this.btnEditCar.Location = new Point(722, 80);
            this.btnEditCar.Size = new Size(150, 40);
            this.btnEditCar.Text = "Edit Car";
            this.btnEditCar.Click += new System.EventHandler(this.btnEditCar_Click);

            this.btnDeleteCar.Location = new Point(722, 140);
            this.btnDeleteCar.Size = new Size(150, 40);
            this.btnDeleteCar.Text = "Delete Car";
            this.btnDeleteCar.Click += new System.EventHandler(this.btnDeleteCar_Click);

            this.btnSetQualTime.Location = new Point(722, 200);
            this.btnSetQualTime.Size = new Size(150, 40);
            this.btnSetQualTime.Text = "Set Qual Time";
            this.btnSetQualTime.Click += new System.EventHandler(this.btnSetQualTime_Click);

            this.btnDriverStats.Location = new Point(722, 260);
            this.btnDriverStats.Size = new Size(150, 40);
            this.btnDriverStats.Text = "Driver Stats";
            this.btnDriverStats.Enabled = false;
            this.btnDriverStats.Click += new System.EventHandler(this.btnDriverStats_Click);

            // ── Driver grid columns (lvDrivers) ────────────────────────────────
            this.colDriverName.Text = "Name";
            this.colDriverName.Width = 130;
            this.colDriverState.Text = "State";
            this.colDriverState.Width = 40;
            this.colDriverWins.Text = "Wins";
            this.colDriverWins.Width = 44;
            this.colDriverWins.TextAlign = HorizontalAlignment.Right;
            this.colDriverLosses.Text = "Losses";
            this.colDriverLosses.Width = 50;
            this.colDriverLosses.TextAlign = HorizontalAlignment.Right;
            this.colDriverEvents.Text = "Events";
            this.colDriverEvents.Width = 50;
            this.colDriverEvents.TextAlign = HorizontalAlignment.Right;
            this.colDriverEventWins.Text = "Event wins";
            this.colDriverEventWins.Width = 68;
            this.colDriverEventWins.TextAlign = HorizontalAlignment.Right;
            this.colDriverQualTime.Text = "Qual time";
            this.colDriverQualTime.Width = 65;
            this.colDriverQualTime.TextAlign = HorizontalAlignment.Right;
            this.colDriverCars.Text = "Cars";
            this.colDriverCars.Width = 40;
            this.colDriverCars.TextAlign = HorizontalAlignment.Right;

            // ── lvDrivers ───────────────────────────────────────────────────────
            this.lvDrivers.Columns.AddRange(new ColumnHeader[] {
                this.colDriverName,
                this.colDriverState,
                this.colDriverWins,
                this.colDriverLosses,
                this.colDriverEvents,
                this.colDriverEventWins,
                this.colDriverQualTime,
                this.colDriverCars
            });
            this.lvDrivers.View = View.Details;
            this.lvDrivers.FullRowSelect = true;
            this.lvDrivers.MultiSelect = false;
            this.lvDrivers.HideSelection = false;
            this.lvDrivers.Dock = DockStyle.Top;
            this.lvDrivers.Height = 240;
            this.lvDrivers.Name = "lvDrivers";
            this.lvDrivers.SelectedIndexChanged += new System.EventHandler(this.lvDrivers_SelectedIndexChanged);

            // ── Cars header label ──────────────────────────────────────────────
            this.lblCarsHeader.Dock = DockStyle.Top;
            this.lblCarsHeader.Height = 22;
            this.lblCarsHeader.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblCarsHeader.Padding = new Padding(4, 4, 0, 0);
            this.lblCarsHeader.Name = "lblCarsHeader";
            this.lblCarsHeader.Text = "Cars";

            // ── Cars grid columns (lvCars) ─────────────────────────────────────
            this.colCarName.Text = "Car name";
            this.colCarName.Width = 200;
            this.colCarClass.Text = "Class";
            this.colCarClass.Width = 110;
            this.colCarDialIn.Text = "Dial-in";
            this.colCarDialIn.Width = 75;
            this.colCarDialIn.TextAlign = HorizontalAlignment.Right;

            // ── lvCars ─────────────────────────────────────────────────────────
            this.lvCars.Columns.AddRange(new ColumnHeader[] {
                this.colCarName,
                this.colCarClass,
                this.colCarDialIn
            });
            this.lvCars.View = View.Details;
            this.lvCars.FullRowSelect = true;
            this.lvCars.MultiSelect = false;
            this.lvCars.HideSelection = false;
            this.lvCars.Dock = DockStyle.Fill;
            this.lvCars.Name = "lvCars";
            this.lvCars.SelectedIndexChanged += new System.EventHandler(this.lvCars_SelectedIndexChanged);

            // ── Content panel (hosts the three docked controls) ────────────────
            // Dock-add order matters: last-added docks first. To get
            //   lvDrivers (top, 180) → lblCarsHeader (top, 22) → lvCars (fill rest)
            // we add in reverse: lvCars first (Fill), then lblCarsHeader (Top),
            // then lvDrivers (Top — added last so it docks to the very top).
            this.pnlContent.Location = new Point(220, 20);
            this.pnlContent.Size = new Size(494, 560);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Controls.Add(this.lvCars);
            this.pnlContent.Controls.Add(this.lblCarsHeader);
            this.pnlContent.Controls.Add(this.lvDrivers);

            // ── Form root ──────────────────────────────────────────────────────
            this.Controls.Add(this.btnAddDriver);
            this.Controls.Add(this.btnEditDriver);
            this.Controls.Add(this.btnDeleteDriver);
            this.Controls.Add(this.btnAddCar);
            this.Controls.Add(this.btnEditCar);
            this.Controls.Add(this.btnDeleteCar);
            this.Controls.Add(this.btnSetQualTime);
            this.Controls.Add(this.btnDriverStats);
            this.Controls.Add(this.pnlContent);

            this.ClientSize = new Size(900, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "DriverManagerForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Driver Manager";

            this.pnlContent.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
