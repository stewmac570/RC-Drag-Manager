using System.Drawing;
using System.Windows.Forms;

namespace RCDragManagerProd.UI.Forms
{
    partial class MultiClassConfigDialog
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblClassName;
        private TextBox txtClassName;
        private GroupBox grpVariant;
        private RadioButton rbStandard;
        private RadioButton rbQmdra;
        private Label lblRoundsToRun;
        private NumericUpDown nudRoundsToRun;
        private Label lblDrivers;
        private ListView lvDrivers;
        private Label lblDialInOverride;
        private TextBox txtDialInOverride;
        private Button btnOk;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Class Configuration";
            this.ClientSize = new Size(640, 530);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Class name
            lblClassName = new Label { Text = "Class Name:", Location = new Point(20, 20), AutoSize = true };
            txtClassName = new TextBox { Location = new Point(110, 17), Width = 220 };

            // Variant group
            grpVariant = new GroupBox { Text = "Variant", Location = new Point(20, 52), Size = new Size(600, 62) };
            rbStandard = new RadioButton { Text = "Standard", Location = new Point(20, 28), AutoSize = true, Checked = true };
            rbQmdra = new RadioButton { Text = "QMDRA", Location = new Point(120, 28), AutoSize = true };
            lblRoundsToRun = new Label { Text = "Rounds (N):", Location = new Point(230, 31), AutoSize = true, Visible = false };
            nudRoundsToRun = new NumericUpDown
            {
                Location = new Point(325, 28),
                Width = 70,
                Minimum = 1,
                Maximum = 100,
                Value = 3,
                Visible = false
            };
            grpVariant.Controls.AddRange(new Control[] { rbStandard, rbQmdra, lblRoundsToRun, nudRoundsToRun });

            // Driver list
            lblDrivers = new Label { Text = "Drivers (check to include):", Location = new Point(20, 130), AutoSize = true };
            lvDrivers = new ListView
            {
                Location = new Point(20, 152),
                Size = new Size(600, 260),
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true
            };
            lvDrivers.Columns.Add("Driver", 155);
            lvDrivers.Columns.Add("Car", 140);
            lvDrivers.Columns.Add("Default Dial-In", 115);
            lvDrivers.Columns.Add("Override Dial-In", 115);

            // Dial-in override editor
            lblDialInOverride = new Label { Text = "Override Dial-In:", Location = new Point(20, 427), AutoSize = true };
            txtDialInOverride = new TextBox { Location = new Point(135, 424), Width = 110, Enabled = false };

            // Buttons
            btnOk = new Button { Text = "OK", Location = new Point(440, 483), Size = new Size(85, 30) };
            btnCancel = new Button { Text = "Cancel", Location = new Point(535, 483), Size = new Size(85, 30) };

            Controls.AddRange(new Control[]
            {
                lblClassName, txtClassName,
                grpVariant,
                lblDrivers, lvDrivers,
                lblDialInOverride, txtDialInOverride,
                btnOk, btnCancel
            });
        }
    }
}
