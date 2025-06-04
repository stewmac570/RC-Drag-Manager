using System.Windows.Forms;

namespace RCDragManager
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
        private Label lblThird;
        private Label lblFourth;
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
            this.txtName = new System.Windows.Forms.TextBox();
            this.txtTime = new System.Windows.Forms.TextBox();
            this.btnAddDriver = new System.Windows.Forms.Button();
            this.btnGenerateBracket = new System.Windows.Forms.Button();
            this.btnNextRound = new System.Windows.Forms.Button();
            this.lvDrivers = new System.Windows.Forms.ListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lstWinners = new System.Windows.Forms.ListBox();
            this.lstFullPairings = new System.Windows.Forms.ListBox();
            this.btnWinner1 = new System.Windows.Forms.Button();
            this.btnWinner2 = new System.Windows.Forms.Button();
            this.btnEditDriver = new System.Windows.Forms.Button();
            this.lblNext = new System.Windows.Forms.Label();
            this.lblThird = new System.Windows.Forms.Label();
            this.lblFourth = new System.Windows.Forms.Label();
            this.lblDriversHeader = new System.Windows.Forms.Label();
            this.lblPairingsHeader = new System.Windows.Forms.Label();
            this.lblWinnersHeader = new System.Windows.Forms.Label();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnEditResult = new System.Windows.Forms.Button();
            this.lblEventTitle = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(250, 100);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(185, 20);
            this.txtName.TabIndex = 7;
            // 
            // txtTime
            // 
            this.txtTime.Location = new System.Drawing.Point(455, 100);
            this.txtTime.Name = "txtTime";
            this.txtTime.Size = new System.Drawing.Size(80, 20);
            this.txtTime.TabIndex = 8;
            // 
            // btnAddDriver
            // 
            this.btnAddDriver.Location = new System.Drawing.Point(550, 100);
            this.btnAddDriver.Name = "btnAddDriver";
            this.btnAddDriver.Size = new System.Drawing.Size(100, 30);
            this.btnAddDriver.TabIndex = 9;
            this.btnAddDriver.Text = "Add Driver";
            this.btnAddDriver.Click += new System.EventHandler(this.btnAddDriver_Click);
            // 
            // btnGenerateBracket
            // 
            this.btnGenerateBracket.Location = new System.Drawing.Point(250, 390);
            this.btnGenerateBracket.Name = "btnGenerateBracket";
            this.btnGenerateBracket.Size = new System.Drawing.Size(200, 40);
            this.btnGenerateBracket.TabIndex = 10;
            this.btnGenerateBracket.Text = "Generate Bracket";
            this.btnGenerateBracket.Click += new System.EventHandler(this.btnGenerateBracket_Click);
            // 
            // btnNextRound
            // 
            this.btnNextRound.Location = new System.Drawing.Point(455, 390);
            this.btnNextRound.Name = "btnNextRound";
            this.btnNextRound.Size = new System.Drawing.Size(195, 40);
            this.btnNextRound.TabIndex = 11;
            this.btnNextRound.Text = "Generate Next Round";
            this.btnNextRound.Click += new System.EventHandler(this.btnNextRound_Click);
            // 
            // lvDrivers
            // 
            this.lvDrivers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colTime});
            this.lvDrivers.HideSelection = false;
            this.lvDrivers.Location = new System.Drawing.Point(250, 150);
            this.lvDrivers.Name = "lvDrivers";
            this.lvDrivers.Size = new System.Drawing.Size(400, 180);
            this.lvDrivers.TabIndex = 5;
            this.lvDrivers.UseCompatibleStateImageBehavior = false;
            this.lvDrivers.View = System.Windows.Forms.View.Details;
            // 
            // colName
            // 
            this.colName.Text = "Name";
            this.colName.Width = 180;
            // 
            // colTime
            // 
            this.colTime.Text = "Qual Time";
            this.colTime.Width = 100;
            // 
            // lstWinners
            // 
            this.lstWinners.Location = new System.Drawing.Point(678, 100);
            this.lstWinners.Name = "lstWinners";
            this.lstWinners.Size = new System.Drawing.Size(200, 394);
            this.lstWinners.TabIndex = 6;
            // 
            // lstFullPairings
            // 
            this.lstFullPairings.Location = new System.Drawing.Point(20, 100);
            this.lstFullPairings.Name = "lstFullPairings";
            this.lstFullPairings.Size = new System.Drawing.Size(200, 394);
            this.lstFullPairings.TabIndex = 4;
            // 
            // btnWinner1
            // 
            this.btnWinner1.Location = new System.Drawing.Point(250, 455);
            this.btnWinner1.Name = "btnWinner1";
            this.btnWinner1.Size = new System.Drawing.Size(199, 40);
            this.btnWinner1.TabIndex = 12;
            this.btnWinner1.Click += new System.EventHandler(this.btnWinner1_Click);
            // 
            // btnWinner2
            // 
            this.btnWinner2.Location = new System.Drawing.Point(455, 455);
            this.btnWinner2.Name = "btnWinner2";
            this.btnWinner2.Size = new System.Drawing.Size(195, 40);
            this.btnWinner2.TabIndex = 13;
            this.btnWinner2.Click += new System.EventHandler(this.btnWinner2_Click);
            // 
            // btnEditDriver
            // 
            this.btnEditDriver.Location = new System.Drawing.Point(550, 345);
            this.btnEditDriver.Name = "btnEditDriver";
            this.btnEditDriver.Size = new System.Drawing.Size(100, 30);
            this.btnEditDriver.TabIndex = 14;
            this.btnEditDriver.Text = "Edit Driver";
            this.btnEditDriver.Click += new System.EventHandler(this.btnEditDriver_Click);
            // 
            // lblNext
            // 
            this.lblNext.Location = new System.Drawing.Point(250, 510);
            this.lblNext.Name = "lblNext";
            this.lblNext.Size = new System.Drawing.Size(300, 20);
            this.lblNext.TabIndex = 17;
            this.lblNext.Text = "Up Next: --";
            // 
            // lblThird
            // 
            this.lblThird.Location = new System.Drawing.Point(250, 530);
            this.lblThird.Name = "lblThird";
            this.lblThird.Size = new System.Drawing.Size(300, 20);
            this.lblThird.TabIndex = 18;
            this.lblThird.Text = "3rd Up: --";
            // 
            // lblFourth
            // 
            this.lblFourth.Location = new System.Drawing.Point(250, 550);
            this.lblFourth.Name = "lblFourth";
            this.lblFourth.Size = new System.Drawing.Size(300, 20);
            this.lblFourth.TabIndex = 19;
            this.lblFourth.Text = "4th Up: --";
            // 
            // lblDriversHeader
            // 
            this.lblDriversHeader.Location = new System.Drawing.Point(250, 70);
            this.lblDriversHeader.Name = "lblDriversHeader";
            this.lblDriversHeader.Size = new System.Drawing.Size(200, 20);
            this.lblDriversHeader.TabIndex = 2;
            this.lblDriversHeader.Text = "Driver List:";
            // 
            // lblPairingsHeader
            // 
            this.lblPairingsHeader.Location = new System.Drawing.Point(20, 70);
            this.lblPairingsHeader.Name = "lblPairingsHeader";
            this.lblPairingsHeader.Size = new System.Drawing.Size(200, 20);
            this.lblPairingsHeader.TabIndex = 1;
            this.lblPairingsHeader.Text = "Current Round Pairings:";
            // 
            // lblWinnersHeader
            // 
            this.lblWinnersHeader.Location = new System.Drawing.Point(675, 70);
            this.lblWinnersHeader.Name = "lblWinnersHeader";
            this.lblWinnersHeader.Size = new System.Drawing.Size(200, 20);
            this.lblWinnersHeader.TabIndex = 3;
            this.lblWinnersHeader.Text = "Match Winners:";
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(20, 510);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(200, 40);
            this.btnReset.TabIndex = 15;
            this.btnReset.Text = "Reset Race";
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnEditResult
            // 
            this.btnEditResult.Location = new System.Drawing.Point(678, 510);
            this.btnEditResult.Name = "btnEditResult";
            this.btnEditResult.Size = new System.Drawing.Size(200, 40);
            this.btnEditResult.TabIndex = 16;
            this.btnEditResult.Text = "Edit Match Result";
            this.btnEditResult.Click += new System.EventHandler(this.btnEditResult_Click);
            // 
            // lblEventTitle
            // 
            this.lblEventTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblEventTitle.Location = new System.Drawing.Point(20, 10);
            this.lblEventTitle.Name = "lblEventTitle";
            this.lblEventTitle.Size = new System.Drawing.Size(860, 30);
            this.lblEventTitle.TabIndex = 0;
            this.lblEventTitle.Text = "Event: [Event Name]";
            this.lblEventTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(900, 600);
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
            this.Controls.Add(this.lblThird);
            this.Controls.Add(this.lblFourth);
            this.Name = "Form1";
            this.Text = "RC Drag Manager Stable Build";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
