using System.Drawing;
using System.Windows.Forms;

namespace RCDragManagerProd.UI.Forms
{
    partial class MultiClassSetupForm
    {
        private System.ComponentModel.IContainer components = null;

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
            this.ClientSize = new Size(700, 480);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            // Event name
            lblEventName = new Label { Text = "Event Name:", Location = new Point(20, 20), AutoSize = true };
            txtEventName = new TextBox { Location = new Point(115, 17), Width = 280 };

            // Event date
            lblEventDate = new Label { Text = "Event Date:", Location = new Point(420, 20), AutoSize = true };
            dtpEventDate = new DateTimePicker
            {
                Location = new Point(505, 17),
                Width = 170,
                Format = DateTimePickerFormat.Short
            };

            // Class list
            lblClasses = new Label { Text = "Classes:", Location = new Point(20, 60), AutoSize = true };
            lvClasses = new ListView
            {
                Location = new Point(20, 82),
                Size = new Size(540, 300),
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false
            };
            lvClasses.Columns.Add("Class Name", 180);
            lvClasses.Columns.Add("Variant", 120);
            lvClasses.Columns.Add("Rounds (N)", 90);
            lvClasses.Columns.Add("Drivers", 80);

            // Class management buttons (stacked to the right of the list)
            btnAddClass = new Button { Text = "Add Class", Location = new Point(575, 82), Size = new Size(105, 32) };
            btnEditClass = new Button { Text = "Edit Class", Location = new Point(575, 124), Size = new Size(105, 32) };
            btnRemoveClass = new Button { Text = "Remove Class", Location = new Point(575, 166), Size = new Size(105, 32) };

            // Bottom buttons
            btnStartRace = new Button { Text = "Start Race", Location = new Point(490, 430), Size = new Size(100, 32) };
            btnCancel = new Button { Text = "Cancel", Location = new Point(600, 430), Size = new Size(80, 32) };

            Controls.AddRange(new Control[]
            {
                lblEventName, txtEventName,
                lblEventDate, dtpEventDate,
                lblClasses, lvClasses,
                btnAddClass, btnEditClass, btnRemoveClass,
                btnStartRace, btnCancel
            });
        }
    }
}
