using System.Windows.Forms;
using System.Drawing;

namespace RCDragManagerProd
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


        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblEventTitle = new System.Windows.Forms.Label();
            this.lstDrivers = new System.Windows.Forms.ListBox();
            this.btnAddDriver = new System.Windows.Forms.Button();
            this.btnEditDriver = new System.Windows.Forms.Button();
            this.btnDeleteDriver = new System.Windows.Forms.Button();
            this.lvDriverDetails = new System.Windows.Forms.ListView();
            this.btnAddCar = new System.Windows.Forms.Button();
            this.btnEditCar = new System.Windows.Forms.Button();
            this.btnDeleteCar = new System.Windows.Forms.Button();
            this.btnSaveChanges = new System.Windows.Forms.Button();
            this.btnSetQualTime = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblEventTitle
            // 
            this.lblEventTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblEventTitle.Location = new System.Drawing.Point(20, 10);
            this.lblEventTitle.Name = "lblEventTitle";
            this.lblEventTitle.Size = new System.Drawing.Size(860, 30);
            this.lblEventTitle.TabIndex = 0;
            this.lblEventTitle.Text = "Driver Manager";
            this.lblEventTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lstDrivers
            // 
            this.lstDrivers.Location = new System.Drawing.Point(220, 80);
            this.lstDrivers.Name = "lstDrivers";
            this.lstDrivers.Size = new System.Drawing.Size(464, 95);
            this.lstDrivers.TabIndex = 1;
            this.lstDrivers.SelectedIndexChanged += new System.EventHandler(this.lstDrivers_SelectedIndexChanged);
            // 
            // btnAddDriver
            // 
            this.btnAddDriver.Location = new System.Drawing.Point(20, 80);
            this.btnAddDriver.Name = "btnAddDriver";
            this.btnAddDriver.Size = new System.Drawing.Size(180, 40);
            this.btnAddDriver.TabIndex = 2;
            this.btnAddDriver.Text = "Add Driver";
            this.btnAddDriver.Click += new System.EventHandler(this.btnAddDriver_Click);
            // 
            // btnEditDriver
            // 
            this.btnEditDriver.Location = new System.Drawing.Point(20, 140);
            this.btnEditDriver.Name = "btnEditDriver";
            this.btnEditDriver.Size = new System.Drawing.Size(180, 40);
            this.btnEditDriver.TabIndex = 3;
            this.btnEditDriver.Text = "Edit Driver";
            this.btnEditDriver.Click += new System.EventHandler(this.btnEditDriver_Click);
            // 
            // btnDeleteDriver
            // 
            this.btnDeleteDriver.Location = new System.Drawing.Point(20, 200);
            this.btnDeleteDriver.Name = "btnDeleteDriver";
            this.btnDeleteDriver.Size = new System.Drawing.Size(180, 40);
            this.btnDeleteDriver.TabIndex = 4;
            this.btnDeleteDriver.Text = "Delete Driver";
            this.btnDeleteDriver.Click += new System.EventHandler(this.btnDeleteDriver_Click);
            // 
            // lvDriverDetails
            // 
            this.lvDriverDetails.FullRowSelect = true;
            this.lvDriverDetails.HideSelection = false;
            this.lvDriverDetails.Location = new System.Drawing.Point(220, 222);
            this.lvDriverDetails.Name = "lvDriverDetails";
            this.lvDriverDetails.Size = new System.Drawing.Size(464, 297);
            this.lvDriverDetails.TabIndex = 5;
            this.lvDriverDetails.UseCompatibleStateImageBehavior = false;
            this.lvDriverDetails.View = System.Windows.Forms.View.Details;
            // 
            // btnAddCar
            // 
            this.btnAddCar.Location = new System.Drawing.Point(722, 80);
            this.btnAddCar.Name = "btnAddCar";
            this.btnAddCar.Size = new System.Drawing.Size(150, 40);
            this.btnAddCar.TabIndex = 6;
            this.btnAddCar.Text = "Add Car";
            this.btnAddCar.Click += new System.EventHandler(this.btnAddCar_Click);
            // 
            // btnEditCar
            // 
            this.btnEditCar.Location = new System.Drawing.Point(722, 140);
            this.btnEditCar.Name = "btnEditCar";
            this.btnEditCar.Size = new System.Drawing.Size(150, 40);
            this.btnEditCar.TabIndex = 7;
            this.btnEditCar.Text = "Edit Car";
            this.btnEditCar.Click += new System.EventHandler(this.btnEditCar_Click);
            // 
            // btnDeleteCar
            // 
            this.btnDeleteCar.Location = new System.Drawing.Point(722, 200);
            this.btnDeleteCar.Name = "btnDeleteCar";
            this.btnDeleteCar.Size = new System.Drawing.Size(150, 40);
            this.btnDeleteCar.TabIndex = 8;
            this.btnDeleteCar.Text = "Delete Car";
            this.btnDeleteCar.Click += new System.EventHandler(this.btnDeleteCar_Click);
            // 
            // btnSaveChanges
            // 
            this.btnSaveChanges.Location = new System.Drawing.Point(722, 530);
            this.btnSaveChanges.Name = "btnSaveChanges";
            this.btnSaveChanges.Size = new System.Drawing.Size(150, 40);
            this.btnSaveChanges.TabIndex = 10;
            this.btnSaveChanges.Text = "Save and Close";
            this.btnSaveChanges.Click += new System.EventHandler(this.btnSaveChanges_Click);
            // 
            // btnSetQualTime
            // 
            this.btnSetQualTime.Location = new System.Drawing.Point(722, 260);
            this.btnSetQualTime.Name = "btnSetQualTime";
            this.btnSetQualTime.Size = new System.Drawing.Size(150, 40);
            this.btnSetQualTime.TabIndex = 9;
            this.btnSetQualTime.Text = "Set Qual Time";
            this.btnSetQualTime.Click += new System.EventHandler(this.btnSetQualTime_Click);
            // 
            // DriverManagerForm
            // 
            this.ClientSize = new System.Drawing.Size(900, 600);
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
            this.Controls.Add(this.btnSaveChanges);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "DriverManagerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Driver Manager";
            this.ResumeLayout(false);

        }
    }
}
