using System.Windows.Forms;
using System.Drawing;

namespace RCDragManagerProd.UI.Forms
{
    partial class SessionSetupForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblTitle;
        private GroupBox grpEventDetails;
        private Label lblEventName;
        private TextBox txtEventName;
        private Label lblRaceDate;
        private DateTimePicker dateRaceDate;
        private Label lblRaceType;
        private ComboBox cmbRaceType;

        // Round Robin config UI (shown only when Race Type == "Round Robin")
        private Label lblRRVariant;
        private ComboBox cmbRRVariant;
        private Label lblRoundsToRun;
        private NumericUpDown nudRoundsToRun;


        private GroupBox grpClassSelection;
        private RadioButton rbHeadsUp;
        private RadioButton rbBracket;
        private RadioButton rbDialIn;
        private Label lblFixedDial;
        private TextBox txtFixedDial;

        private GroupBox grpDriverSelection;
        private Button btnAddNewDriver;
#pragma warning disable CS0169
        private Button btnAddDriverFromList;
#pragma warning restore CS0169
        private ListView lvEventRoster;

        private Button btnStartRace;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Session Setup";
            this.ClientSize = new Size(900, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;

            lblTitle = new Label();
            lblTitle.Text = "Create New Race Session";
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Size = new Size(900, 40);
            lblTitle.Location = new Point(0, 10);
            Controls.Add(lblTitle);

            grpEventDetails = new GroupBox();
            grpEventDetails.Text = "Event Details";
            grpEventDetails.Size = new Size(860, 140);

            grpEventDetails.Location = new Point(20, 60);
            Controls.Add(grpEventDetails);

            lblEventName = new Label() { Text = "Event Name:", AutoSize = true, Location = new Point(20, 35) };
            txtEventName = new TextBox() { Location = new Point(120, 30), Width = 280 };
            lblRaceDate = new Label() { Text = "Race Date:", AutoSize = true, Location = new Point(420, 35) };
            dateRaceDate = new DateTimePicker() { Location = new Point(500, 30), Width = 200 };
            lblRaceType = new Label() { Text = "Race Type:", AutoSize = true, Location = new Point(20, 70) };
            cmbRaceType = new ComboBox() { Location = new Point(120, 65), Width = 200 };
            cmbRaceType.Items.AddRange(new string[] { "Pro Ladder", "Random Draw", "Round Robin" });
            cmbRaceType.SelectedIndex = 0;
            cmbRaceType.SelectedIndexChanged += CmbRaceType_SelectedIndexChanged;



            // Round Robin: Variant + Rounds (hidden unless Race Type == Round Robin)
            lblRRVariant = new Label()
            {
                Text = "RR Variant:",
                AutoSize = true,
                Location = new Point(340, 70),
                Visible = false
            };

            cmbRRVariant = new ComboBox()
            {
                Location = new Point(420, 65),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };
            cmbRRVariant.Items.AddRange(new string[] { "Standard", "QMDRA" });
            cmbRRVariant.SelectedIndex = 0;
            cmbRRVariant.SelectedIndexChanged += CmbRRVariant_SelectedIndexChanged;


            lblRoundsToRun = new Label()
            {
                Text = "Rounds (N):",
                AutoSize = true,
                Location = new Point(580, 70),
                Visible = false
            };

            nudRoundsToRun = new NumericUpDown()
            {
                Location = new Point(670, 65),
                Width = 70,
                Minimum = 1,
                Maximum = 100,
                Value = 3,
                Visible = false,
                Enabled = false
            };


            grpEventDetails.Controls.AddRange(new Control[] {
                lblEventName, txtEventName,
                lblRaceDate, dateRaceDate,
                lblRaceType, cmbRaceType,
                lblRRVariant, cmbRRVariant,
                lblRoundsToRun, nudRoundsToRun
            });


            grpClassSelection = new GroupBox();
            grpClassSelection.Text = "Class Selection";
            grpClassSelection.Size = new Size(860, 80);
            grpClassSelection.Location = new Point(20, 170);
            Controls.Add(grpClassSelection);

            rbHeadsUp = new RadioButton() { Text = "Heads Up", Location = new Point(30, 35), AutoSize = true, Checked = true };
            rbBracket = new RadioButton() { Text = "Bracket Class", Location = new Point(160, 35), AutoSize = true };
            rbDialIn = new RadioButton() { Text = "Dial-In", Location = new Point(320, 35), AutoSize = true };
            lblFixedDial = new Label() { Text = "Fixed Dial:", Location = new Point(480, 42), AutoSize = true, Visible = false };
            txtFixedDial = new TextBox() { Location = new Point(560, 40), Width = 100, Visible = false };

            grpClassSelection.Controls.AddRange(new Control[] { rbHeadsUp, rbBracket, rbDialIn, lblFixedDial, txtFixedDial });

            grpDriverSelection = new GroupBox();
            grpDriverSelection.Text = "Driver Selection";
            grpDriverSelection.Size = new Size(860, 270);
            grpDriverSelection.Location = new Point(20, 260);
            Controls.Add(grpDriverSelection);

            btnAddNewDriver = new Button() { Text = "Add New Driver", Location = new Point(30, 30), Size = new Size(180, 40) };
            //btnAddDriverFromList = new Button() { Text = "Add Driver From List", Location = new Point(230, 30), Size = new Size(200, 40) };

            lvEventRoster = new ListView()
            {
                Location = new Point(30, 80),
                Size = new Size(800, 170),
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true
            };
            lvEventRoster.Columns.Add("Driver", 200);
            lvEventRoster.Columns.Add("Car", 200);
            lvEventRoster.Columns.Add("Class Type", 150);
            lvEventRoster.Columns.Add("Dial-In", 100);

            grpDriverSelection.Controls.AddRange(new Control[] { btnAddNewDriver, lvEventRoster });


            btnStartRace = new Button() { Text = "Start Race", Location = new Point(570, 540), Size = new Size(140, 40) };
            btnCancel = new Button() { Text = "Cancel", Location = new Point(720, 540), Size = new Size(140, 40) };

            Controls.AddRange(new Control[] { btnStartRace, btnCancel });

            // Initialize RR UI state based on default Race Type
            UpdateRoundRobinUiState();

        }
    }
}
