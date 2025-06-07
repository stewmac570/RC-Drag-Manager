using System.Windows.Forms;
using System.Drawing;

namespace RCDragManagerProd
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblEventTitle;
        private TextBox txtName;
        private TextBox txtTime;
        private Button btnAddDriver;
        private Button btnGenerateBracket;
        private Button btnNextRound;
        private ListView lvDrivers;
        private ColumnHeader colName;
        private ColumnHeader colTime;
        private ListBox lstWinners;
        private ListBox lstFullPairings;
        private Button btnWinner1;
        private Button btnWinner2;
        private Button btnEditDriver;
        private Label lblNext;
        private Label lblDriversHeader;
        private Label lblPairingsHeader;
        private Label lblWinnersHeader;
        private Button btnReset;
        private Button btnEditResult;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtName = new TextBox();
            this.txtTime = new TextBox();
            this.btnAddDriver = new Button();
            this.btnGenerateBracket = new Button();
            this.btnNextRound = new Button();
            this.lvDrivers = new ListView();
            this.colName = new ColumnHeader();
            this.colTime = new ColumnHeader();
            this.lstWinners = new ListBox();
            this.lstFullPairings = new ListBox();
            this.btnWinner1 = new Button();
            this.btnWinner2 = new Button();
            this.btnEditDriver = new Button();
            this.lblNext = new Label();
            this.lblDriversHeader = new Label();
            this.lblPairingsHeader = new Label();
            this.lblWinnersHeader = new Label();
            this.btnReset = new Button();
            this.btnEditResult = new Button();
            this.lblEventTitle = new Label();

            this.SuspendLayout();

            // txtName
            this.txtName.Location = new Point(250, 100);
            this.txtName.Size = new Size(185, 20);

            // txtTime
            this.txtTime.Location = new Point(455, 100);
            this.txtTime.Size = new Size(80, 20);

            // btnAddDriver
            this.btnAddDriver.Location = new Point(550, 100);
            this.btnAddDriver.Size = new Size(100, 30);
            this.btnAddDriver.Text = "Add Driver";
            this.btnAddDriver.Click += new System.EventHandler(this.btnAddDriver_Click);

            // btnGenerateBracket
            this.btnGenerateBracket.Location = new Point(250, 390);
            this.btnGenerateBracket.Size = new Size(200, 40);
            this.btnGenerateBracket.Text = "Generate Bracket";
            this.btnGenerateBracket.Click += new System.EventHandler(this.btnGenerateBracket_Click);

            // btnNextRound
            this.btnNextRound.Location = new Point(455, 390);
            this.btnNextRound.Size = new Size(195, 40);
            this.btnNextRound.Text = "Generate Next Round";
            this.btnNextRound.Click += new System.EventHandler(this.btnNextRound_Click);

            // lvDrivers
            this.lvDrivers.Columns.AddRange(new ColumnHeader[] { this.colName, this.colTime });
            this.lvDrivers.Location = new Point(250, 150);
            this.lvDrivers.Size = new Size(400, 180);
            this.lvDrivers.View = View.Details;
            this.lvDrivers.FullRowSelect = true;
            this.lvDrivers.MultiSelect = false;
            this.lvDrivers.HideSelection = false;

            // colName
            this.colName.Text = "Name";
            this.colName.Width = 180;

            // colTime
            this.colTime.Text = "Qual Time";
            this.colTime.Width = 100;

            // lstWinners
            this.lstWinners.Location = new Point(678, 100);
            this.lstWinners.Size = new Size(200, 394);

            // lstFullPairings
            this.lstFullPairings.Location = new Point(20, 100);
            this.lstFullPairings.Size = new Size(200, 394);

            // btnWinner1
            this.btnWinner1.Location = new Point(250, 455);
            this.btnWinner1.Size = new Size(199, 40);
            this.btnWinner1.Click += new System.EventHandler(this.btnWinner1_Click);

            // btnWinner2
            this.btnWinner2.Location = new Point(455, 455);
            this.btnWinner2.Size = new Size(195, 40);
            this.btnWinner2.Click += new System.EventHandler(this.btnWinner2_Click);

            // btnEditDriver
            this.btnEditDriver.Location = new Point(550, 345);
            this.btnEditDriver.Size = new Size(100, 30);
            this.btnEditDriver.Text = "Edit Driver";
            this.btnEditDriver.Click += new System.EventHandler(this.btnEditDriver_Click);

            // lblNext
            this.lblNext.Location = new Point(250, 510);
            this.lblNext.Size = new Size(400, 20);
            this.lblNext.Text = "Up Next: --";

            // lblDriversHeader
            this.lblDriversHeader.Location = new Point(250, 70);
            this.lblDriversHeader.Size = new Size(200, 20);
            this.lblDriversHeader.Text = "Driver List:";

            // lblPairingsHeader
            this.lblPairingsHeader.Location = new Point(20, 70);
            this.lblPairingsHeader.Size = new Size(200, 20);
            this.lblPairingsHeader.Text = "Current Round Pairings:";

            // lblWinnersHeader
            this.lblWinnersHeader.Location = new Point(675, 70);
            this.lblWinnersHeader.Size = new Size(200, 20);
            this.lblWinnersHeader.Text = "Match Winners:";

            // btnReset
            this.btnReset.Location = new Point(20, 510);
            this.btnReset.Size = new Size(200, 40);
            this.btnReset.Text = "Reset Race";
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);

            // btnEditResult
            this.btnEditResult.Location = new Point(678, 510);
            this.btnEditResult.Size = new Size(200, 40);
            this.btnEditResult.Text = "Edit Match Result";
            this.btnEditResult.Click += new System.EventHandler(this.btnEditResult_Click);

            // lblEventTitle
            this.lblEventTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblEventTitle.Location = new Point(20, 10);
            this.lblEventTitle.Size = new Size(860, 30);
            this.lblEventTitle.Text = "Event:";
            this.lblEventTitle.TextAlign = ContentAlignment.MiddleCenter;

            // Form1
            this.ClientSize = new Size(900, 600);
            this.Controls.Add(this.lblEventTitle);
            this.Controls.Add(this.lblPairingsHeader);
            this.Controls.Add(this.lblDriversHeader);
            this.Controls.Add(this.lblWinnersHeader);
            this.Controls.Add(this.lstFullPairings);
            this.Controls.Add(this.lvDrivers);
            this.Controls.Add(this.lstWinners);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.txtTime);
            this.Controls.Add(this.btnAddDriver);
            this.Controls.Add(this.btnGenerateBracket);
            this.Controls.Add(this.btnNextRound);
            this.Controls.Add(this.btnWinner1);
            this.Controls.Add(this.btnWinner2);
            this.Controls.Add(this.btnEditDriver);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnEditResult);
            this.Controls.Add(this.lblNext);
            this.Name = "Form1";
            this.Text = "RC Drag Manager Stable Build";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
