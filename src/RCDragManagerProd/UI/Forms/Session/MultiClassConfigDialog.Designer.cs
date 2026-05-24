using System.Drawing;
using System.Windows.Forms;

namespace RCDragManagerProd.UI.Forms
{
    partial class MultiClassConfigDialog
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblClassName;
        private TextBox txtClassName;
        private TableLayoutPanel pnlCardRow;
        private Panel pnlCardProLadder;
        private Panel pnlCardRandomDraw;
        private Panel pnlCardRoundRobin;
        private Panel pnlRrConfig;
        private Label lblRrRounds;
        private NumericUpDown nudRoundsToRun;
        private CheckBox chkBuybackRace;
        private Label lblBuybackHint;
        private GroupBox grpClassType;
        private RadioButton rbHeadsUp;
        private RadioButton rbBracket;
        private RadioButton rbDialIn;
        private Label lblFixedDialIn;
        private TextBox txtFixedDialIn;
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
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new Size(900, 770);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Class name
            lblClassName = new Label { Text = "Class Name:", Location = new Point(20, 20), AutoSize = true };
            txtClassName = new TextBox { Location = new Point(110, 17), Width = 220 };

            // Race format cards (replace the old Race Type dropdown)
            // Cards live inside a TableLayoutPanel so they share the full width
            // of the driver ListView below in three equal columns and stretch
            // together if the form is ever resized.
            pnlCardRow = new TableLayoutPanel
            {
                Location = new Point(20, 55),
                Size = new Size(860, 70),
                ColumnCount = 3,
                RowCount = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlCardRow.ColumnStyles.Clear();
            pnlCardRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlCardRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            pnlCardRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlCardRow.RowStyles.Clear();
            pnlCardRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            pnlCardProLadder  = CreateRaceTypeCard("Pro Ladder",  "Pro Ladder",  "NHRA bracket",  Point.Empty);
            pnlCardRandomDraw = CreateRaceTypeCard("Random Draw", "Random Draw", "Single elim",   Point.Empty);
            pnlCardRoundRobin = CreateRaceTypeCard("Round Robin", "Round Robin", "Points based",  Point.Empty);

            pnlCardProLadder.Dock  = DockStyle.Fill;
            pnlCardRandomDraw.Dock = DockStyle.Fill;
            pnlCardRoundRobin.Dock = DockStyle.Fill;

            pnlCardRow.Controls.Add(pnlCardProLadder,  0, 0);
            pnlCardRow.Controls.Add(pnlCardRandomDraw, 1, 0);
            pnlCardRow.Controls.Add(pnlCardRoundRobin, 2, 0);

            // Round Robin config panel (visible only when Round Robin card is selected)
            // Single horizontal row: [Rounds: <nud>]   [☑ Buyback race for 4th finals spot]
            // Hint text below, left-aligned under the checkbox.
            pnlRrConfig = new Panel
            {
                Location = new Point(20, 135),
                Size = new Size(860, 70),
                BackColor = Color.FromArgb(240, 253, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Visible = true
            };
            lblRrRounds = new Label
            {
                Text = "Rounds:",
                Location = new Point(15, 20),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            nudRoundsToRun = new NumericUpDown
            {
                Location = new Point(75, 17),
                Width = 60,
                Minimum = 1,
                Maximum = 100,
                Value = 3
            };
            chkBuybackRace = new CheckBox
            {
                Text = "Buyback race for 4th finals spot",
                Location = new Point(165, 19),
                AutoSize = true,
                Checked = true,
                BackColor = Color.Transparent
            };
            lblBuybackHint = new Label
            {
                Text = "Unchecked → all drivers advance in ranked order",
                Location = new Point(165, 45),
                AutoSize = true,
                ForeColor = Color.Gray,
                BackColor = Color.Transparent
            };
            pnlRrConfig.Controls.AddRange(new Control[] { lblRrRounds, nudRoundsToRun, chkBuybackRace, lblBuybackHint });

            // Class type group
            grpClassType = new GroupBox { Text = "Class Type", Location = new Point(20, 235), Size = new Size(860, 62) };
            rbHeadsUp = new RadioButton { Text = "Heads Up",      Location = new Point(20, 28),  AutoSize = true, Checked = true };
            rbBracket = new RadioButton { Text = "Bracket Class", Location = new Point(130, 28), AutoSize = true };
            rbDialIn  = new RadioButton { Text = "Dial-In",        Location = new Point(265, 28), AutoSize = true };
            lblFixedDialIn = new Label  { Text = "Fixed Dial-In:", Location = new Point(355, 31), AutoSize = true, Visible = false };
            txtFixedDialIn = new TextBox { Location = new Point(450, 28), Width = 100, Visible = false };
            grpClassType.Controls.AddRange(new Control[] { rbHeadsUp, rbBracket, rbDialIn, lblFixedDialIn, txtFixedDialIn });

            // Add New Driver button
            btnAddNewDriver = new Button { Text = "Add New Driver", Location = new Point(20, 310), Size = new Size(140, 30) };

            // Filter controls are created dynamically in CreateFilterControls()

            // Driver list
            lblDrivers = new Label { Text = "Drivers (check to include):", Location = new Point(20, 348), AutoSize = true };
            lvDrivers = new ListView
            {
                Location = new Point(20, 368),
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
            lblDialInOverride = new Label { Text = "Override Dial-In:", Location = new Point(20, 658), AutoSize = true };
            txtDialInOverride = new TextBox { Location = new Point(145, 655), Width = 110, Enabled = false };

            // Buttons
            btnOk     = new Button { Text = "OK",     Location = new Point(690, 720), Size = new Size(85, 30) };
            btnCancel = new Button { Text = "Cancel", Location = new Point(790, 720), Size = new Size(85, 30) };

            Controls.AddRange(new Control[]
            {
                lblClassName, txtClassName,
                pnlCardRow,
                pnlRrConfig,
                grpClassType,
                btnAddNewDriver,
                lblDrivers, lvDrivers,
                lblDialInOverride, txtDialInOverride,
                btnOk, btnCancel
            });
        }

        private Panel CreateRaceTypeCard(string raceType, string title, string subtitle, Point location)
        {
            var card = new Panel
            {
                Tag = raceType,
                Location = location,
                Size = new Size(200, 70),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            var titleLabel = new Label
            {
                Text = title,
                Location = new Point(12, 12),
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            var subLabel = new Label
            {
                Text = subtitle,
                Location = new Point(12, 38),
                AutoSize = true,
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(titleLabel);
            card.Controls.Add(subLabel);
            return card;
        }
    }
}
