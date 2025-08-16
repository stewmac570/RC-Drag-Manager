using System.Windows.Forms;
using System.Drawing;

namespace RCDragManagerProd.UI.Forms
{
    partial class DriverManagerForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblEventTitle;
        private ListBox lstDrivers;
        private Button btnAddDriver;
        private Button btnEditDriver;
        private Button btnDeleteDriver;
        private ListView lvDriverDetails;
        private Button btnAddCar;
        private Button btnEditCar;
        private Button btnDeleteCar;
        private Button btnSaveChanges;
        private Button btnSetQualTime;
        private Button btnDriverStats; // ✅ NEW BUTTON

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblEventTitle = new Label();
            this.lstDrivers = new ListBox();
            this.btnAddDriver = new Button();
            this.btnEditDriver = new Button();
            this.btnDeleteDriver = new Button();
            this.lvDriverDetails = new ListView();
            this.btnAddCar = new Button();
            this.btnEditCar = new Button();
            this.btnDeleteCar = new Button();
            this.btnSaveChanges = new Button();
            this.btnSetQualTime = new Button();
            this.btnDriverStats = new Button(); // ✅ NEW BUTTON
            this.SuspendLayout();

            // lblEventTitle
            this.lblEventTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblEventTitle.Location = new Point(20, 10);
            this.lblEventTitle.Size = new Size(860, 30);
            this.lblEventTitle.Text = "Driver Manager";
            this.lblEventTitle.TextAlign = ContentAlignment.MiddleCenter;

            // lstDrivers
            this.lstDrivers.Location = new Point(220, 80);
            this.lstDrivers.Size = new Size(464, 95);
            this.lstDrivers.SelectedIndexChanged += new System.EventHandler(this.lstDrivers_SelectedIndexChanged);

            // btnAddDriver
            this.btnAddDriver.Location = new Point(20, 80);
            this.btnAddDriver.Size = new Size(180, 40);
            this.btnAddDriver.Text = "Add Driver";
            this.btnAddDriver.Click += new System.EventHandler(this.btnAddDriver_Click);

            // btnEditDriver
            this.btnEditDriver.Location = new Point(20, 140);
            this.btnEditDriver.Size = new Size(180, 40);
            this.btnEditDriver.Text = "Edit Driver";
            this.btnEditDriver.Click += new System.EventHandler(this.btnEditDriver_Click);

            // btnDeleteDriver
            this.btnDeleteDriver.Location = new Point(20, 200);
            this.btnDeleteDriver.Size = new Size(180, 40);
            this.btnDeleteDriver.Text = "Delete Driver";
            this.btnDeleteDriver.Click += new System.EventHandler(this.btnDeleteDriver_Click);

            // lvDriverDetails
            this.lvDriverDetails.FullRowSelect = true;
            this.lvDriverDetails.HideSelection = false;
            this.lvDriverDetails.Location = new Point(220, 222);
            this.lvDriverDetails.Size = new Size(464, 297);
            this.lvDriverDetails.View = View.Details;

            // btnAddCar
            this.btnAddCar.Location = new Point(722, 80);
            this.btnAddCar.Size = new Size(150, 40);
            this.btnAddCar.Text = "Add Car";
            this.btnAddCar.Click += new System.EventHandler(this.btnAddCar_Click);

            // btnEditCar
            this.btnEditCar.Location = new Point(722, 140);
            this.btnEditCar.Size = new Size(150, 40);
            this.btnEditCar.Text = "Edit Car";
            this.btnEditCar.Click += new System.EventHandler(this.btnEditCar_Click);

            // btnDeleteCar
            this.btnDeleteCar.Location = new Point(722, 200);
            this.btnDeleteCar.Size = new Size(150, 40);
            this.btnDeleteCar.Text = "Delete Car";
            this.btnDeleteCar.Click += new System.EventHandler(this.btnDeleteCar_Click);

            // btnSetQualTime
            this.btnSetQualTime.Location = new Point(722, 260);
            this.btnSetQualTime.Size = new Size(150, 40);
            this.btnSetQualTime.Text = "Set Qual Time";
            this.btnSetQualTime.Click += new System.EventHandler(this.btnSetQualTime_Click);

            // btnDriverStats (NEW BUTTON ✅)
            this.btnDriverStats.Location = new Point(722, 320);
            this.btnDriverStats.Size = new Size(150, 40);
            this.btnDriverStats.Text = "Driver Stats";
            this.btnDriverStats.Enabled = false;
            this.btnDriverStats.Click += new System.EventHandler(this.btnDriverStats_Click);

            // btnSaveChanges
            this.btnSaveChanges.Location = new Point(722, 530);
            this.btnSaveChanges.Size = new Size(150, 40);
            this.btnSaveChanges.Text = "Save and Close";
            this.btnSaveChanges.Click += new System.EventHandler(this.btnSaveChanges_Click);

            // Add all controls ONCE only
            this.Controls.Add(this.lblEventTitle);
            this.Controls.Add(this.lstDrivers);
            this.Controls.Add(this.btnAddDriver);
            this.Controls.Add(this.btnEditDriver);
            this.Controls.Add(this.btnDeleteDriver);
            this.Controls.Add(this.lvDriverDetails);
            this.Controls.Add(this.btnAddCar);
            this.Controls.Add(this.btnEditCar);
            this.Controls.Add(this.btnDeleteCar);
            this.Controls.Add(this.btnSetQualTime);
            this.Controls.Add(this.btnDriverStats); // ✅ ADDED HERE CLEARLY
            this.Controls.Add(this.btnSaveChanges);

            // Form Settings (defined once)
            this.ClientSize = new Size(900, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "DriverManagerForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Driver Manager";
            this.ResumeLayout(false);
        }
    }
}
