using System.Drawing;
using System.Windows.Forms;

namespace RCDragManagerProd.UI.Forms
{
    partial class MultiClassSetupForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblTitle;
        private GroupBox grpEventDetails;
        private Label lblEventName;
        private TextBox txtEventName;
        private Label lblEventDate;
        private DateTimePicker dtpEventDate;
        private Label lblClasses;
        private ListView lvClasses;
        private Button btnAddClass;
        private Button btnEditClass;
        private Button btnRemoveClass;
        private Button btnStartRace;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "New Multi-Class Event";
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new Size(900, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            // Title label
            lblTitle = new Label
            {
                Text = "New Multi-Class Event",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Location = new Point(0, 10),
                Size = new Size(900, 40)
            };

            // Event details group
            grpEventDetails = new GroupBox
            {
                Text = "Event Details",
                Location = new Point(20, 60),
                Size = new Size(860, 80)
            };

            lblEventName = new Label { Text = "Event Name:", Location = new Point(20, 30), AutoSize = true };
            txtEventName = new TextBox { Location = new Point(115, 27), Width = 280 };
            lblEventDate = new Label { Text = "Event Date:", Location = new Point(420, 30), AutoSize = true };
            dtpEventDate = new DateTimePicker
            {
                Location = new Point(505, 27),
                Width = 170,
                Format = DateTimePickerFormat.Short
            };
            grpEventDetails.Controls.AddRange(new Control[]
            {
                lblEventName, txtEventName,
                lblEventDate, dtpEventDate
            });

            // Class list
            lblClasses = new Label { Text = "Classes:", Location = new Point(20, 155), AutoSize = true };
            lvClasses = new ListView
            {
                Location = new Point(20, 175),
                Size = new Size(730, 270),
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false
            };
            lvClasses.Columns.Add("Class Name", 150);
            lvClasses.Columns.Add("Race Type", 120);
            lvClasses.Columns.Add("Class Type", 100);
            lvClasses.Columns.Add("Rounds (N)", 80);
            lvClasses.Columns.Add("Drivers", 70);

            // Class management buttons (stacked to the right of the list)
            btnAddClass    = new Button { Text = "Add Class",    Location = new Point(762, 175), Size = new Size(110, 32) };
            btnEditClass   = new Button { Text = "Edit Class",   Location = new Point(762, 217), Size = new Size(110, 32) };
            btnRemoveClass = new Button { Text = "Remove Class", Location = new Point(762, 259), Size = new Size(110, 32) };

            // Bottom buttons
            btnStartRace = new Button { Text = "Start Race", Location = new Point(570, 540), Size = new Size(140, 40) };
            btnCancel    = new Button { Text = "Cancel",     Location = new Point(720, 540), Size = new Size(140, 40) };

            Controls.AddRange(new Control[]
            {
                lblTitle,
                grpEventDetails,
                lblClasses, lvClasses,
                btnAddClass, btnEditClass, btnRemoveClass,
                btnStartRace, btnCancel
            });
        }
    }
}
