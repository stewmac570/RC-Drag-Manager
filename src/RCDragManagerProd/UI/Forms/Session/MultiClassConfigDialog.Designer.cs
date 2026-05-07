using System.Drawing;
using System.Windows.Forms;

namespace RCDragManagerProd.UI.Forms
{
    partial class MultiClassConfigDialog
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblClassName;
        private TextBox txtClassName;
        private Label lblRaceType;
        private ComboBox cmbRaceType;
        private GroupBox grpClassType;
        private RadioButton rbHeadsUp;
        private RadioButton rbBracket;
        private RadioButton rbDialIn;
        private Label lblFixedDialIn;
        private TextBox txtFixedDialIn;
        private GroupBox grpVariant;
        private RadioButton rbStandard;
        private RadioButton rbQmdra;
        private Label lblRoundsToRun;
        private NumericUpDown nudRoundsToRun;
        private Button btnAddNewDriver;
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
            this.ClientSize = new Size(900, 700);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Class name
            lblClassName = new Label { Text = "Class Name:", Location = new Point(20, 20), AutoSize = true };
            txtClassName = new TextBox { Location = new Point(110, 17), Width = 220 };

            // Race type
            lblRaceType = new Label { Text = "Race Type:", Location = new Point(20, 55), AutoSize = true };
            cmbRaceType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(110, 52),
                Width = 220
            };
            cmbRaceType.Items.AddRange(new string[] { "Pro Ladder", "Random Draw", "Round Robin" });
            cmbRaceType.SelectedIndex = 0;

            // Class type group
            grpClassType = new GroupBox { Text = "Class Type", Location = new Point(20, 84), Size = new Size(640, 62) };
            rbHeadsUp = new RadioButton { Text = "Heads Up",     Location = new Point(20, 28), AutoSize = true, Checked = true };
            rbBracket = new RadioButton { Text = "Bracket Class", Location = new Point(130, 28), AutoSize = true };
            rbDialIn  = new RadioButton { Text = "Dial-In",       Location = new Point(265, 28), AutoSize = true };
            lblFixedDialIn = new Label  { Text = "Fixed Dial-In:", Location = new Point(355, 31), AutoSize = true, Visible = false };
            txtFixedDialIn = new TextBox { Location = new Point(450, 28), Width = 100, Visible = false };
            grpClassType.Controls.AddRange(new Control[] { rbHeadsUp, rbBracket, rbDialIn, lblFixedDialIn, txtFixedDialIn });

            // Round Robin variant group (visible only when Race Type == Round Robin)
            grpVariant = new GroupBox { Text = "Round Robin Variant", Location = new Point(20, 155), Size = new Size(600, 62), Visible = false };
            rbStandard = new RadioButton { Text = "Standard", Location = new Point(20, 28), AutoSize = true, Checked = true };
            rbQmdra    = new RadioButton { Text = "QMDRA",    Location = new Point(120, 28), AutoSize = true };
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

            // Add New Driver button
            btnAddNewDriver = new Button { Text = "Add New Driver", Location = new Point(20, 228), Size = new Size(140, 30) };

            // Filter controls are created dynamically in CreateFilterControls()

            // Driver list
            lblDrivers = new Label { Text = "Drivers (check to include):", Location = new Point(20, 263), AutoSize = true };
            lvDrivers = new ListView
            {
                Location = new Point(20, 283),
                Size = new Size(860, 280),
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true
            };
            lvDrivers.Columns.Add("Driver", 200);
            lvDrivers.Columns.Add("Car", 200);
            lvDrivers.Columns.Add("Class Type", 150);
            lvDrivers.Columns.Add("Dial-In", 100);
            lvDrivers.Columns.Add("Override Dial-In", 115);

            // Dial-in override editor
            lblDialInOverride = new Label { Text = "Override Dial-In:", Location = new Point(20, 573), AutoSize = true };
            txtDialInOverride = new TextBox { Location = new Point(145, 570), Width = 110, Enabled = false };

            // Buttons
            btnOk     = new Button { Text = "OK",     Location = new Point(690, 648), Size = new Size(85, 30) };
            btnCancel = new Button { Text = "Cancel",  Location = new Point(790, 648), Size = new Size(85, 30) };

            Controls.AddRange(new Control[]
            {
                lblClassName, txtClassName,
                lblRaceType, cmbRaceType,
                grpClassType,
                grpVariant,
                btnAddNewDriver,
                lblDrivers, lvDrivers,
                lblDialInOverride, txtDialInOverride,
                btnOk, btnCancel
            });
        }
    }
}
