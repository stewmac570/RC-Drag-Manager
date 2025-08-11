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
        private ListView lvWinners;
        private ColumnHeader colMatchWin;
        private ColumnHeader colLoser;
        private ColumnHeader colWinner;
        private ListView lvPairings;
        private ColumnHeader colMatch;
        private ColumnHeader colDriver1;
        private ColumnHeader colDriver2;
        private Button btnWinner1;
        private Button btnWinner2;
        private Button btnEditDriver;
        private Button btnSetQualTime;
        private Label lblNext;
        private Label lblDriversHeader;
        private Label lblPairingsHeader;
        private Label lblWinnersHeader;
        private Button btnReset;
        private Button btnEditResult;
        private Button btnSaveAndClose;
        private Button btnGenerateLosersBracket;

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
            this.lvPairings = new System.Windows.Forms.ListView();
            this.colMatch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDriver1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDriver2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnWinner1 = new System.Windows.Forms.Button();
            this.btnWinner2 = new System.Windows.Forms.Button();
            this.btnEditDriver = new System.Windows.Forms.Button();
            this.btnSetQualTime = new System.Windows.Forms.Button();
            this.lblNext = new System.Windows.Forms.Label();
            this.lblDriversHeader = new System.Windows.Forms.Label();
            this.lblPairingsHeader = new System.Windows.Forms.Label();
            this.lblWinnersHeader = new System.Windows.Forms.Label();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnEditResult = new System.Windows.Forms.Button();
            this.btnSaveAndClose = new System.Windows.Forms.Button();
            this.lblEventTitle = new System.Windows.Forms.Label();
            this.lvWinners = new System.Windows.Forms.ListView();
            this.colMatchWin = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colLoser = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colWinner = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lblRaceType = new System.Windows.Forms.Label();
            this.cmbRaceType = new System.Windows.Forms.ComboBox();
            //this.btnGenerateLosersBracket = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(250, 80);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(185, 20);
            this.txtName.TabIndex = 7;
            // 
            // txtTime
            // 
            this.txtTime.Location = new System.Drawing.Point(455, 80);
            this.txtTime.Name = "txtTime";
            this.txtTime.Size = new System.Drawing.Size(80, 20);
            this.txtTime.TabIndex = 8;
            // 
            // btnAddDriver
            // 
            this.btnAddDriver.Location = new System.Drawing.Point(550, 80);
            this.btnAddDriver.Name = "btnAddDriver";
            this.btnAddDriver.Size = new System.Drawing.Size(100, 40);
            this.btnAddDriver.TabIndex = 9;
            this.btnAddDriver.Text = "Add Driver";
            this.btnAddDriver.Click += new System.EventHandler(this.btnAddDriver_Click);
            // 
            // btnGenerateBracket
            // 
            this.btnGenerateBracket.Location = new System.Drawing.Point(249, 376);
            this.btnGenerateBracket.Name = "btnGenerateBracket";
            this.btnGenerateBracket.Size = new System.Drawing.Size(200, 45);
            this.btnGenerateBracket.TabIndex = 10;
            this.btnGenerateBracket.Text = "Generate Bracket";
            this.btnGenerateBracket.Click += new System.EventHandler(this.btnGenerateBracket_Click);
            // 
            // btnNextRound
            // 
            this.btnNextRound.Location = new System.Drawing.Point(455, 376);
            this.btnNextRound.Name = "btnNextRound";
            this.btnNextRound.Size = new System.Drawing.Size(200, 45);
            this.btnNextRound.TabIndex = 11;
            this.btnNextRound.Text = "Generate Next Round";
            this.btnNextRound.Click += new System.EventHandler(this.btnNextRound_Click);
            // 
            // lvDrivers
            // 
            this.lvDrivers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colTime});
            this.lvDrivers.FullRowSelect = true;
            this.lvDrivers.HideSelection = false;
            this.lvDrivers.Location = new System.Drawing.Point(250, 126);
            this.lvDrivers.MultiSelect = false;
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
            // lvPairings
            // 
            this.lvPairings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colMatch,
            this.colDriver1,
            this.colDriver2});
            this.lvPairings.HideSelection = false;
            this.lvPairings.Location = new System.Drawing.Point(20, 80);
            this.lvPairings.MultiSelect = false;
            this.lvPairings.Name = "lvPairings";
            this.lvPairings.Size = new System.Drawing.Size(200, 355);
            this.lvPairings.TabIndex = 4;
            this.lvPairings.UseCompatibleStateImageBehavior = false;
            this.lvPairings.View = System.Windows.Forms.View.Details;
            // 
            // colMatch
            // 
            this.colMatch.Text = "M#";
            this.colMatch.Width = 40;
            // 
            // colDriver1
            // 
            this.colDriver1.Text = "Driver 1";
            this.colDriver1.Width = 75;
            // 
            // colDriver2
            // 
            this.colDriver2.Text = "Driver 2";
            this.colDriver2.Width = 75;
            // 
            // btnWinner1
            // 
            this.btnWinner1.Location = new System.Drawing.Point(253, 441);
            this.btnWinner1.Name = "btnWinner1";
            this.btnWinner1.Size = new System.Drawing.Size(200, 45);
            this.btnWinner1.TabIndex = 12;
            this.btnWinner1.Click += new System.EventHandler(this.btnWinner1_Click);
            // 
            // btnWinner2
            // 
            this.btnWinner2.Location = new System.Drawing.Point(456, 441);
            this.btnWinner2.Name = "btnWinner2";
            this.btnWinner2.Size = new System.Drawing.Size(200, 45);
            this.btnWinner2.TabIndex = 13;
            this.btnWinner2.Click += new System.EventHandler(this.btnWinner2_Click);
            // 
            // btnEditDriver
            // 
            this.btnEditDriver.Location = new System.Drawing.Point(435, 320);
            this.btnEditDriver.Name = "btnEditDriver";
            this.btnEditDriver.Size = new System.Drawing.Size(100, 40);
            this.btnEditDriver.TabIndex = 14;
            this.btnEditDriver.Text = "Edit Driver";
            this.btnEditDriver.Click += new System.EventHandler(this.btnEditDriver_Click);
            // 
            // btnSetQualTime
            // 
            this.btnSetQualTime.Location = new System.Drawing.Point(550, 320);
            this.btnSetQualTime.Name = "btnSetQualTime";
            this.btnSetQualTime.Size = new System.Drawing.Size(100, 40);
            this.btnSetQualTime.TabIndex = 15;
            this.btnSetQualTime.Text = "Set Qual Time";
            this.btnSetQualTime.Click += new System.EventHandler(this.btnSetQualTime_Click);
            // 
            // lblNext
            // 
            this.lblNext.Location = new System.Drawing.Point(256, 505);
            this.lblNext.Name = "lblNext";
            this.lblNext.Size = new System.Drawing.Size(400, 20);
            this.lblNext.TabIndex = 19;
            this.lblNext.Text = "Up Next: --";
            // 
            // lblDriversHeader
            // 
            this.lblDriversHeader.Location = new System.Drawing.Point(250, 60);
            this.lblDriversHeader.Name = "lblDriversHeader";
            this.lblDriversHeader.Size = new System.Drawing.Size(200, 20);
            this.lblDriversHeader.TabIndex = 2;
            this.lblDriversHeader.Text = "Driver List:";
            // 
            // lblPairingsHeader
            // 
            this.lblPairingsHeader.Location = new System.Drawing.Point(20, 60);
            this.lblPairingsHeader.Name = "lblPairingsHeader";
            this.lblPairingsHeader.Size = new System.Drawing.Size(200, 20);
            this.lblPairingsHeader.TabIndex = 1;
            this.lblPairingsHeader.Text = "Current Round Pairings:";
            // 
            // lblWinnersHeader
            // 
            this.lblWinnersHeader.Location = new System.Drawing.Point(675, 60);
            this.lblWinnersHeader.Name = "lblWinnersHeader";
            this.lblWinnersHeader.Size = new System.Drawing.Size(200, 20);
            this.lblWinnersHeader.TabIndex = 3;
            this.lblWinnersHeader.Text = "Match Winners:";
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(20, 441);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(200, 45);
            this.btnReset.TabIndex = 16;
            this.btnReset.Text = "Reset Race";
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnEditResult
            // 
            this.btnEditResult.Location = new System.Drawing.Point(678, 441);
            this.btnEditResult.Name = "btnEditResult";
            this.btnEditResult.Size = new System.Drawing.Size(200, 45);
            this.btnEditResult.TabIndex = 17;
            this.btnEditResult.Text = "Edit Match Result";
            //this.btnEditResult.Click += new System.EventHandler(this.btnEditResult_Click);
            // 
            // btnSaveAndClose
            // 
            this.btnSaveAndClose.Location = new System.Drawing.Point(775, 548);
            this.btnSaveAndClose.Name = "btnSaveAndClose";
            this.btnSaveAndClose.Size = new System.Drawing.Size(100, 40);
            this.btnSaveAndClose.TabIndex = 18;
            this.btnSaveAndClose.Text = "Save and Close";
            this.btnSaveAndClose.Click += new System.EventHandler(this.btnSaveAndClose_Click);
            // 
            // lblEventTitle
            // 
            this.lblEventTitle.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEventTitle.Location = new System.Drawing.Point(20, 10);
            this.lblEventTitle.Name = "lblEventTitle";
            this.lblEventTitle.Size = new System.Drawing.Size(860, 35);
            this.lblEventTitle.TabIndex = 0;
            this.lblEventTitle.Text = "Event:";
            this.lblEventTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lvWinners
            // 
            this.lvWinners.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colMatchWin,
            this.colLoser,
            this.colWinner});
            this.lvWinners.HideSelection = false;
            this.lvWinners.Location = new System.Drawing.Point(678, 80);
            this.lvWinners.MultiSelect = false;
            this.lvWinners.Name = "lvWinners";
            this.lvWinners.Size = new System.Drawing.Size(200, 355);
            this.lvWinners.TabIndex = 6;
            this.lvWinners.UseCompatibleStateImageBehavior = false;
            this.lvWinners.View = System.Windows.Forms.View.Details;
            // 
            // colMatchWin
            // 
            this.colMatchWin.Text = "M#";
            this.colMatchWin.Width = 40;
            // 
            // colLoser
            // 
            this.colLoser.Text = "Loser";
            this.colLoser.Width = 75;
            // 
            // colWinner
            // 
            this.colWinner.Text = "Winner";
            this.colWinner.Width = 75;
            // 
            // lblRaceType
            // 
            this.lblRaceType.Location = new System.Drawing.Point(12, 9);
            this.lblRaceType.Name = "lblRaceType";
            this.lblRaceType.Size = new System.Drawing.Size(100, 20);
            this.lblRaceType.TabIndex = 0;
            this.lblRaceType.Text = "Race Type:";
            // 
            // cmbRaceType
            // 
            this.cmbRaceType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRaceType.Items.AddRange(new object[] {
            "Pro Ladder",
            "Randomized",
            "Round Robin"});
            this.cmbRaceType.Location = new System.Drawing.Point(118, 6);
            this.cmbRaceType.Name = "cmbRaceType";
            this.cmbRaceType.Size = new System.Drawing.Size(150, 21);
            this.cmbRaceType.TabIndex = 50;
            // 
            // btnGenerateLosersBracket
            this.btnGenerateLosersBracket = new System.Windows.Forms.Button();
            this.btnGenerateLosersBracket.Enabled = false;
            this.btnGenerateLosersBracket.Location = new System.Drawing.Point(678, 489);
            this.btnGenerateLosersBracket.Name = "btnGenerateLosersBracket";
            this.btnGenerateLosersBracket.Size = new System.Drawing.Size(200, 45);
            this.btnGenerateLosersBracket.TabIndex = 20;
            this.btnGenerateLosersBracket.Text = "Buy Back";
            this.btnGenerateLosersBracket.Click += new System.EventHandler(this.btnGenerateLosersBracket_Click);
            this.Controls.Add(this.btnGenerateLosersBracket);


            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(900, 600);
            //this.Controls.Add(this.btnGenerateLosersBracket);
            this.Controls.Add(this.lblRaceType);
            this.Controls.Add(this.cmbRaceType);
            this.Controls.Add(this.lblEventTitle);
            this.Controls.Add(this.lblPairingsHeader);
            this.Controls.Add(this.lblDriversHeader);
            this.Controls.Add(this.lblWinnersHeader);
            this.Controls.Add(this.lvPairings);
            this.Controls.Add(this.lvDrivers);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.txtTime);
            this.Controls.Add(this.btnAddDriver);
            this.Controls.Add(this.btnGenerateBracket);
            this.Controls.Add(this.btnNextRound);
            this.Controls.Add(this.btnWinner1);
            this.Controls.Add(this.btnWinner2);
            this.Controls.Add(this.btnEditDriver);
            this.Controls.Add(this.btnSetQualTime);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnEditResult);
            this.Controls.Add(this.btnSaveAndClose);
            this.Controls.Add(this.lblNext);
            this.Controls.Add(this.lvWinners);
            this.Name = "Form1";
            this.Text = "RC Drag Manager Stable Build";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

    }
}
