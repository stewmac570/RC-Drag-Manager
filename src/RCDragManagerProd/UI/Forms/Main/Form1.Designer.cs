using System;
using System.Drawing;
using System.Windows.Forms;

namespace RCDragManagerProd.UI.Forms
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
        private Label lblCurrentRaceLabel;
        private Label lblVs0;
        private Label lblOnDeck;
        private Label lblOnDeckD1;
        private Label lblVs1;
        private Label lblOnDeckD2;
        private Label lblInTheHole;
        private Label lblInHoleD1;
        private Label lblVs2;
        private Label lblInHoleD2;
        private Label lblDriversHeader;
        private Label lblPairingsHeader;
        private Label lblWinnersHeader;
        private Button btnReset;
        private Button btnEditResult;
        private Button btnSaveAndClose;
        private Button btnStandings;
        private Button btnGenerateLosersBracket;
        private Label lblRaceType;
        private ComboBox cmbRaceType;

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
            this.lblCurrentRaceLabel = new System.Windows.Forms.Label();
            this.lblVs0 = new System.Windows.Forms.Label();
            this.lblOnDeck = new System.Windows.Forms.Label();
            this.lblOnDeckD1 = new System.Windows.Forms.Label();
            this.lblVs1 = new System.Windows.Forms.Label();
            this.lblOnDeckD2 = new System.Windows.Forms.Label();
            this.lblInTheHole = new System.Windows.Forms.Label();
            this.lblInHoleD1 = new System.Windows.Forms.Label();
            this.lblVs2 = new System.Windows.Forms.Label();
            this.lblInHoleD2 = new System.Windows.Forms.Label();
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
            this.btnStandings = new System.Windows.Forms.Button();
            this.btnGenerateLosersBracket = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtName
            // 
            this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.txtName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtName.Location = new System.Drawing.Point(20, 75);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(190, 22);
            this.txtName.TabIndex = 6;
            // 
            // txtTime
            // 
            this.txtTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.txtTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtTime.Location = new System.Drawing.Point(20, 113);
            this.txtTime.Name = "txtTime";
            this.txtTime.Size = new System.Drawing.Size(188, 22);
            this.txtTime.TabIndex = 7;
            this.txtTime.TextChanged += new System.EventHandler(this.txtTime_TextChanged);
            // 
            // btnAddDriver
            // 
            this.btnAddDriver.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddDriver.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddDriver.Location = new System.Drawing.Point(224, 73);
            this.btnAddDriver.Name = "btnAddDriver";
            this.btnAddDriver.Size = new System.Drawing.Size(95, 30);
            this.btnAddDriver.TabIndex = 8;
            this.btnAddDriver.Text = "Add Driver";
            this.btnAddDriver.Click += new System.EventHandler(this.btnAddDriver_Click);
            // 
            // btnGenerateBracket
            // 
            this.btnGenerateBracket.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnGenerateBracket.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGenerateBracket.Location = new System.Drawing.Point(485, 434);
            this.btnGenerateBracket.Name = "btnGenerateBracket";
            this.btnGenerateBracket.Size = new System.Drawing.Size(200, 45);
            this.btnGenerateBracket.TabIndex = 15;
            this.btnGenerateBracket.Text = "Generate Bracket";
            this.btnGenerateBracket.Click += new System.EventHandler(this.btnGenerateBracket_Click);
            // 
            // btnNextRound
            // 
            this.btnNextRound.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnNextRound.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNextRound.Location = new System.Drawing.Point(704, 434);
            this.btnNextRound.Name = "btnNextRound";
            this.btnNextRound.Size = new System.Drawing.Size(194, 45);
            this.btnNextRound.TabIndex = 16;
            this.btnNextRound.Text = "Generate Next Round";
            this.btnNextRound.Click += new System.EventHandler(this.btnNextRound_Click);
            // 
            // lvDrivers
            // 
            this.lvDrivers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lvDrivers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colTime});
            this.lvDrivers.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lvDrivers.FullRowSelect = true;
            this.lvDrivers.HideSelection = false;
            this.lvDrivers.Location = new System.Drawing.Point(20, 150);
            this.lvDrivers.MultiSelect = false;
            this.lvDrivers.Name = "lvDrivers";
            this.lvDrivers.Size = new System.Drawing.Size(300, 275);
            this.lvDrivers.TabIndex = 9;
            this.lvDrivers.UseCompatibleStateImageBehavior = false;
            this.lvDrivers.View = System.Windows.Forms.View.Details;
            this.lvDrivers.SelectedIndexChanged += new System.EventHandler(this.lvDrivers_SelectedIndexChanged);
            // 
            // colName
            // 
            this.colName.Text = "Name";
            this.colName.Width = 200;
            // 
            // colTime
            // 
            this.colTime.Text = "Qual Time";
            this.colTime.Width = 100;
            // 
            // lvPairings
            // 
            this.lvPairings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lvPairings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colMatch,
            this.colDriver1,
            this.colDriver2});
            this.lvPairings.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lvPairings.FullRowSelect = true;
            this.lvPairings.HideSelection = false;
            this.lvPairings.Location = new System.Drawing.Point(335, 75);
            this.lvPairings.MultiSelect = false;
            this.lvPairings.Name = "lvPairings";
            this.lvPairings.Size = new System.Drawing.Size(350, 350);
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
            this.colDriver1.Width = 160;
            // 
            // colDriver2
            // 
            this.colDriver2.Text = "Driver 2";
            this.colDriver2.Width = 160;
            // 
            // btnWinner1
            //
            this.btnWinner1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnWinner1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnWinner1.Location = new System.Drawing.Point(485, 483);
            this.btnWinner1.Name = "btnWinner1";
            this.btnWinner1.Size = new System.Drawing.Size(200, 38);
            this.btnWinner1.TabIndex = 17;
            this.btnWinner1.Text = "—";
            this.btnWinner1.Click += new System.EventHandler(this.btnWinner1_Click);
            //
            // btnWinner2
            //
            this.btnWinner2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnWinner2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnWinner2.Location = new System.Drawing.Point(704, 483);
            this.btnWinner2.Name = "btnWinner2";
            this.btnWinner2.Size = new System.Drawing.Size(194, 38);
            this.btnWinner2.TabIndex = 18;
            this.btnWinner2.Text = "—";
            this.btnWinner2.Click += new System.EventHandler(this.btnWinner2_Click);
            // 
            // btnEditDriver
            // 
            this.btnEditDriver.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnEditDriver.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEditDriver.Location = new System.Drawing.Point(219, 434);
            this.btnEditDriver.Name = "btnEditDriver";
            this.btnEditDriver.Size = new System.Drawing.Size(100, 45);
            this.btnEditDriver.TabIndex = 10;
            this.btnEditDriver.Text = "Edit Driver";
            this.btnEditDriver.Click += new System.EventHandler(this.btnEditDriver_Click);
            // 
            // btnSetQualTime
            // 
            this.btnSetQualTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSetQualTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSetQualTime.Location = new System.Drawing.Point(224, 109);
            this.btnSetQualTime.Name = "btnSetQualTime";
            this.btnSetQualTime.Size = new System.Drawing.Size(95, 30);
            this.btnSetQualTime.TabIndex = 11;
            this.btnSetQualTime.Text = "Set Time";
            this.btnSetQualTime.Click += new System.EventHandler(this.btnSetQualTime_Click);
            // 
            // lblCurrentRaceLabel — left-hand row label for "Current race"
            //
            this.lblCurrentRaceLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCurrentRaceLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCurrentRaceLabel.Location = new System.Drawing.Point(375, 483);
            this.lblCurrentRaceLabel.Name = "lblCurrentRaceLabel";
            this.lblCurrentRaceLabel.Size = new System.Drawing.Size(110, 38);
            this.lblCurrentRaceLabel.TabIndex = 30;
            this.lblCurrentRaceLabel.Text = "Current race";
            this.lblCurrentRaceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblVs0 — "vs" separator in the Current race row
            //
            this.lblVs0.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblVs0.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVs0.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblVs0.Location = new System.Drawing.Point(689, 483);
            this.lblVs0.Name = "lblVs0";
            this.lblVs0.Size = new System.Drawing.Size(11, 38);
            this.lblVs0.TabIndex = 31;
            this.lblVs0.Text = "vs";
            this.lblVs0.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblOnDeck — left-hand row label for "On deck"
            //
            this.lblOnDeck.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblOnDeck.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOnDeck.Location = new System.Drawing.Point(375, 527);
            this.lblOnDeck.Name = "lblOnDeck";
            this.lblOnDeck.Size = new System.Drawing.Size(110, 32);
            this.lblOnDeck.TabIndex = 32;
            this.lblOnDeck.Text = "On deck";
            this.lblOnDeck.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblOnDeckD1 — left driver display for On deck row
            //
            this.lblOnDeckD1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblOnDeckD1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOnDeckD1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblOnDeckD1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOnDeckD1.Location = new System.Drawing.Point(485, 527);
            this.lblOnDeckD1.Name = "lblOnDeckD1";
            this.lblOnDeckD1.Size = new System.Drawing.Size(200, 32);
            this.lblOnDeckD1.TabIndex = 33;
            this.lblOnDeckD1.Text = "—";
            this.lblOnDeckD1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblVs1 — "vs" separator in the On deck row
            //
            this.lblVs1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblVs1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVs1.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblVs1.Location = new System.Drawing.Point(689, 527);
            this.lblVs1.Name = "lblVs1";
            this.lblVs1.Size = new System.Drawing.Size(11, 32);
            this.lblVs1.TabIndex = 34;
            this.lblVs1.Text = "vs";
            this.lblVs1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblOnDeckD2 — right driver display for On deck row
            //
            this.lblOnDeckD2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblOnDeckD2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOnDeckD2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblOnDeckD2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOnDeckD2.Location = new System.Drawing.Point(704, 527);
            this.lblOnDeckD2.Name = "lblOnDeckD2";
            this.lblOnDeckD2.Size = new System.Drawing.Size(194, 32);
            this.lblOnDeckD2.TabIndex = 35;
            this.lblOnDeckD2.Text = "—";
            this.lblOnDeckD2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblInTheHole — left-hand row label for "In the hole"
            //
            this.lblInTheHole.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblInTheHole.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInTheHole.Location = new System.Drawing.Point(375, 565);
            this.lblInTheHole.Name = "lblInTheHole";
            this.lblInTheHole.Size = new System.Drawing.Size(110, 32);
            this.lblInTheHole.TabIndex = 36;
            this.lblInTheHole.Text = "In the hole";
            this.lblInTheHole.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblInHoleD1 — left driver display for In the hole row
            //
            this.lblInHoleD1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblInHoleD1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblInHoleD1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblInHoleD1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInHoleD1.Location = new System.Drawing.Point(485, 565);
            this.lblInHoleD1.Name = "lblInHoleD1";
            this.lblInHoleD1.Size = new System.Drawing.Size(200, 32);
            this.lblInHoleD1.TabIndex = 37;
            this.lblInHoleD1.Text = "—";
            this.lblInHoleD1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblVs2 — "vs" separator in the In the hole row
            //
            this.lblVs2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblVs2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVs2.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblVs2.Location = new System.Drawing.Point(689, 565);
            this.lblVs2.Name = "lblVs2";
            this.lblVs2.Size = new System.Drawing.Size(11, 32);
            this.lblVs2.TabIndex = 38;
            this.lblVs2.Text = "vs";
            this.lblVs2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblInHoleD2 — right driver display for In the hole row
            //
            this.lblInHoleD2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblInHoleD2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblInHoleD2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblInHoleD2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInHoleD2.Location = new System.Drawing.Point(704, 565);
            this.lblInHoleD2.Name = "lblInHoleD2";
            this.lblInHoleD2.Size = new System.Drawing.Size(194, 32);
            this.lblInHoleD2.TabIndex = 39;
            this.lblInHoleD2.Text = "—";
            this.lblInHoleD2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblDriversHeader
            // 
            this.lblDriversHeader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.lblDriversHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDriversHeader.Location = new System.Drawing.Point(16, 50);
            this.lblDriversHeader.Name = "lblDriversHeader";
            this.lblDriversHeader.Size = new System.Drawing.Size(112, 20);
            this.lblDriversHeader.TabIndex = 5;
            this.lblDriversHeader.Text = "Driver List:";
            this.lblDriversHeader.Click += new System.EventHandler(this.lblDriversHeader_Click);
            // 
            // lblPairingsHeader
            // 
            this.lblPairingsHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPairingsHeader.Location = new System.Drawing.Point(332, 50);
            this.lblPairingsHeader.Name = "lblPairingsHeader";
            this.lblPairingsHeader.Size = new System.Drawing.Size(353, 20);
            this.lblPairingsHeader.TabIndex = 3;
            this.lblPairingsHeader.Text = "Current Round Pairings:";
            this.lblPairingsHeader.Click += new System.EventHandler(this.lblPairingsHeader_Click);
            // 
            // lblWinnersHeader
            // 
            this.lblWinnersHeader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblWinnersHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWinnersHeader.Location = new System.Drawing.Point(701, 50);
            this.lblWinnersHeader.Name = "lblWinnersHeader";
            this.lblWinnersHeader.Size = new System.Drawing.Size(353, 25);
            this.lblWinnersHeader.TabIndex = 12;
            this.lblWinnersHeader.Text = "Match Winners:";
            this.lblWinnersHeader.Click += new System.EventHandler(this.lblWinnersHeader_Click);
            // 
            // btnReset
            // 
            this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnReset.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnReset.Location = new System.Drawing.Point(1076, 126);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(100, 45);
            this.btnReset.TabIndex = 14;
            this.btnReset.Text = "Reset Race";
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnEditResult
            // 
            this.btnEditResult.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEditResult.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEditResult.Location = new System.Drawing.Point(1076, 75);
            this.btnEditResult.Name = "btnEditResult";
            this.btnEditResult.Size = new System.Drawing.Size(100, 45);
            this.btnEditResult.TabIndex = 20;
            this.btnEditResult.Text = "Edit Match Result";
            // 
            // btnSaveAndClose
            // 
            this.btnSaveAndClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveAndClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSaveAndClose.Location = new System.Drawing.Point(1076, 537);
            this.btnSaveAndClose.Name = "btnSaveAndClose";
            this.btnSaveAndClose.Size = new System.Drawing.Size(100, 45);
            this.btnSaveAndClose.TabIndex = 22;
            this.btnSaveAndClose.Text = "Save and Close";
            this.btnSaveAndClose.Click += new System.EventHandler(this.btnSaveAndClose_Click);
            // 
            // lblEventTitle
            // 
            this.lblEventTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEventTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblEventTitle.Location = new System.Drawing.Point(330, 8);
            this.lblEventTitle.Name = "lblEventTitle";
            this.lblEventTitle.Size = new System.Drawing.Size(724, 42);
            this.lblEventTitle.TabIndex = 0;
            this.lblEventTitle.Text = "Event:";
            this.lblEventTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEventTitle.Click += new System.EventHandler(this.lblEventTitle_Click);
            // 
            // lvWinners
            // 
            this.lvWinners.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lvWinners.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colMatchWin,
            this.colLoser,
            this.colWinner});
            this.lvWinners.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lvWinners.FullRowSelect = true;
            this.lvWinners.HideSelection = false;
            this.lvWinners.Location = new System.Drawing.Point(704, 75);
            this.lvWinners.MultiSelect = false;
            this.lvWinners.Name = "lvWinners";
            this.lvWinners.Size = new System.Drawing.Size(350, 350);
            this.lvWinners.TabIndex = 13;
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
            this.colLoser.Width = 160;
            // 
            // colWinner
            // 
            this.colWinner.Text = "Winner";
            this.colWinner.Width = 160;
            // 
            // lblRaceType
            // 
            this.lblRaceType.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRaceType.Location = new System.Drawing.Point(14, 11);
            this.lblRaceType.Name = "lblRaceType";
            this.lblRaceType.Size = new System.Drawing.Size(100, 20);
            this.lblRaceType.TabIndex = 1;
            this.lblRaceType.Text = "Race Type:";
            // 
            // cmbRaceType
            // 
            this.cmbRaceType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRaceType.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbRaceType.Items.AddRange(new object[] {
            "Pro Ladder",
            "Randomized",
            "Round Robin"});
            this.cmbRaceType.Location = new System.Drawing.Point(118, 11);
            this.cmbRaceType.Name = "cmbRaceType";
            this.cmbRaceType.Size = new System.Drawing.Size(150, 24);
            this.cmbRaceType.TabIndex = 2;
            this.cmbRaceType.SelectedIndexChanged += new System.EventHandler(this.cmbRaceType_SelectedIndexChanged);
            // 
            // btnStandings
            // 
            this.btnStandings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStandings.Enabled = false;
            this.btnStandings.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.btnStandings.Location = new System.Drawing.Point(1076, 177);
            this.btnStandings.Name = "btnStandings";
            this.btnStandings.Size = new System.Drawing.Size(100, 45);
            this.btnStandings.TabIndex = 23;
            this.btnStandings.Text = "Standings";
            this.btnStandings.Click += new System.EventHandler(this.btnStandings_Click);
            // 
            // btnGenerateLosersBracket
            // 
            this.btnGenerateLosersBracket.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGenerateLosersBracket.Enabled = false;
            this.btnGenerateLosersBracket.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGenerateLosersBracket.Location = new System.Drawing.Point(1076, 228);
            this.btnGenerateLosersBracket.Name = "btnGenerateLosersBracket";
            this.btnGenerateLosersBracket.Size = new System.Drawing.Size(100, 45);
            this.btnGenerateLosersBracket.TabIndex = 21;
            this.btnGenerateLosersBracket.Text = "Buy Back";
            this.btnGenerateLosersBracket.Click += new System.EventHandler(this.btnGenerateLosersBracket_Click);
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1200, 600);
            this.Controls.Add(this.lblEventTitle);
            this.Controls.Add(this.lblRaceType);
            this.Controls.Add(this.cmbRaceType);
            this.Controls.Add(this.lblPairingsHeader);
            this.Controls.Add(this.lvPairings);
            this.Controls.Add(this.lblDriversHeader);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.txtTime);
            this.Controls.Add(this.btnAddDriver);
            this.Controls.Add(this.lvDrivers);
            this.Controls.Add(this.btnEditDriver);
            this.Controls.Add(this.btnSetQualTime);
            this.Controls.Add(this.lblWinnersHeader);
            this.Controls.Add(this.lvWinners);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnGenerateBracket);
            this.Controls.Add(this.btnNextRound);
            this.Controls.Add(this.lblCurrentRaceLabel);
            this.Controls.Add(this.btnWinner1);
            this.Controls.Add(this.lblVs0);
            this.Controls.Add(this.btnWinner2);
            this.Controls.Add(this.lblOnDeck);
            this.Controls.Add(this.lblOnDeckD1);
            this.Controls.Add(this.lblVs1);
            this.Controls.Add(this.lblOnDeckD2);
            this.Controls.Add(this.lblInTheHole);
            this.Controls.Add(this.lblInHoleD1);
            this.Controls.Add(this.lblVs2);
            this.Controls.Add(this.lblInHoleD2);
            this.Controls.Add(this.btnEditResult);
            this.Controls.Add(this.btnStandings);
            this.Controls.Add(this.btnGenerateLosersBracket);
            this.Controls.Add(this.btnSaveAndClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RC Drag Manager Stable Build";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
