using System;
using System.Drawing;
using System.Windows.Forms;

namespace RCDragManagerProd.UI.Forms
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        // Top-level container panels
        private Panel pnlHeader;
        private Panel pnlBottom;
        private Panel pnlRail;
        private Panel pnlLeft;
        private TableLayoutPanel tlpMain;

        // Header
        private Label lblEventTitle;

        // Left column
        private Label lblDriversHeader;
        private TextBox txtName;
        private TableLayoutPanel tlpAddEdit;
        private Button btnAddDriver;
        private Button btnEditDriver;
        private TextBox txtTime;
        private TableLayoutPanel tlpSetTimes;
        private Button btnSetQualTime;
        private Button btnSetDialIn;
        private ListView lvDrivers;
        private ColumnHeader colName;
        private ColumnHeader colTime;
        private ColumnHeader colDialIn;

        // Center column (in tlpMain col 0)
        private Panel pnlCenter;
        private Label lblPairingsHeader;
        private ListView lvPairings;
        private ColumnHeader colMatch;
        private ColumnHeader colDriver1;
        private ColumnHeader colDriver2;

        // Right column (in tlpMain col 1)
        private Panel pnlRight;
        private Label lblWinnersHeader;
        private ListView lvWinners;
        private ColumnHeader colMatchWin;
        private ColumnHeader colWinner;
        private ColumnHeader colLoser;

        // Right rail
        private TableLayoutPanel tlpRail;
        private Button btnEditResult;
        private Button btnReset;
        private Button btnStandings;
        private Button btnGenerateLosersBracket;
        private Button btnShowQRCode;
        private Button btnSaveProgress;
        private Button btnCloseRace;

        // Bottom bar
        private TableLayoutPanel tlpBottom;
        private Button btnGenerateBracket;
        private TableLayoutPanel tlpRaceQueue;
        private Button btnNextRound;
        private Label lblCurrentRaceLabel;
        private Button btnWinner1;
        private Label lblVs0;
        private Button btnWinner2;
        private Label lblOnDeck;
        private Label lblOnDeckD1;
        private Label lblVs1;
        private Label lblOnDeckD2;
        private Label lblInTheHole;
        private Label lblInHoleD1;
        private Label lblVs2;
        private Label lblInHoleD2;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblEventTitle = new System.Windows.Forms.Label();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.tlpBottom = new System.Windows.Forms.TableLayoutPanel();
            this.btnGenerateBracket = new System.Windows.Forms.Button();
            this.tlpRaceQueue = new System.Windows.Forms.TableLayoutPanel();
            this.lblCurrentRaceLabel = new System.Windows.Forms.Label();
            this.btnWinner1 = new System.Windows.Forms.Button();
            this.lblVs0 = new System.Windows.Forms.Label();
            this.btnWinner2 = new System.Windows.Forms.Button();
            this.lblOnDeck = new System.Windows.Forms.Label();
            this.lblOnDeckD1 = new System.Windows.Forms.Label();
            this.lblVs1 = new System.Windows.Forms.Label();
            this.lblOnDeckD2 = new System.Windows.Forms.Label();
            this.lblInTheHole = new System.Windows.Forms.Label();
            this.lblInHoleD1 = new System.Windows.Forms.Label();
            this.lblVs2 = new System.Windows.Forms.Label();
            this.lblInHoleD2 = new System.Windows.Forms.Label();
            this.btnNextRound = new System.Windows.Forms.Button();
            this.pnlRail = new System.Windows.Forms.Panel();
            this.tlpRail = new System.Windows.Forms.TableLayoutPanel();
            this.btnEditResult = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnStandings = new System.Windows.Forms.Button();
            this.btnGenerateLosersBracket = new System.Windows.Forms.Button();
            this.btnShowQRCode = new System.Windows.Forms.Button();
            this.btnSaveProgress = new System.Windows.Forms.Button();
            this.btnCloseRace = new System.Windows.Forms.Button();
            this.pnlLeft = new System.Windows.Forms.Panel();
            this.lvDrivers = new System.Windows.Forms.ListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDialIn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tlpSetTimes = new System.Windows.Forms.TableLayoutPanel();
            this.btnSetQualTime = new System.Windows.Forms.Button();
            this.btnSetDialIn = new System.Windows.Forms.Button();
            this.txtTime = new System.Windows.Forms.TextBox();
            this.tlpAddEdit = new System.Windows.Forms.TableLayoutPanel();
            this.btnAddDriver = new System.Windows.Forms.Button();
            this.btnEditDriver = new System.Windows.Forms.Button();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblDriversHeader = new System.Windows.Forms.Label();
            this.tlpMain = new System.Windows.Forms.TableLayoutPanel();
            this.pnlCenter = new System.Windows.Forms.Panel();
            this.lvPairings = new System.Windows.Forms.ListView();
            this.colMatch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDriver1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDriver2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lblPairingsHeader = new System.Windows.Forms.Label();
            this.pnlRight = new System.Windows.Forms.Panel();
            this.lvWinners = new System.Windows.Forms.ListView();
            this.colMatchWin = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colWinner = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colLoser = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lblWinnersHeader = new System.Windows.Forms.Label();
            this.pnlHeader.SuspendLayout();
            this.pnlBottom.SuspendLayout();
            this.tlpBottom.SuspendLayout();
            this.tlpRaceQueue.SuspendLayout();
            this.pnlRail.SuspendLayout();
            this.tlpRail.SuspendLayout();
            this.pnlLeft.SuspendLayout();
            this.tlpSetTimes.SuspendLayout();
            this.tlpAddEdit.SuspendLayout();
            this.tlpMain.SuspendLayout();
            this.pnlCenter.SuspendLayout();
            this.pnlRight.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.Controls.Add(this.lblEventTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(884, 50);
            this.pnlHeader.TabIndex = 100;
            // 
            // lblEventTitle
            // 
            this.lblEventTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEventTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblEventTitle.Location = new System.Drawing.Point(0, 4);
            this.lblEventTitle.Name = "lblEventTitle";
            this.lblEventTitle.Size = new System.Drawing.Size(884, 42);
            this.lblEventTitle.TabIndex = 0;
            this.lblEventTitle.Text = "Event:";
            this.lblEventTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEventTitle.Click += new System.EventHandler(this.lblEventTitle_Click);
            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.tlpBottom);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 391);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(884, 170);
            this.pnlBottom.TabIndex = 101;
            // 
            // tlpBottom
            // 
            this.tlpBottom.ColumnCount = 3;
            this.tlpBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tlpBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tlpBottom.Controls.Add(this.btnGenerateBracket, 0, 0);
            this.tlpBottom.Controls.Add(this.tlpRaceQueue, 1, 0);
            this.tlpBottom.Controls.Add(this.btnNextRound, 2, 0);
            this.tlpBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpBottom.Location = new System.Drawing.Point(0, 0);
            this.tlpBottom.Margin = new System.Windows.Forms.Padding(0);
            this.tlpBottom.Name = "tlpBottom";
            this.tlpBottom.RowCount = 3;
            this.tlpBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.tlpBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.tlpBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.tlpBottom.Size = new System.Drawing.Size(884, 170);
            this.tlpBottom.TabIndex = 0;
            // 
            // btnGenerateBracket
            // 
            this.btnGenerateBracket.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGenerateBracket.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGenerateBracket.Location = new System.Drawing.Point(3, 3);
            this.btnGenerateBracket.Name = "btnGenerateBracket";
            this.tlpBottom.SetRowSpan(this.btnGenerateBracket, 3);
            this.btnGenerateBracket.Size = new System.Drawing.Size(194, 164);
            this.btnGenerateBracket.TabIndex = 15;
            this.btnGenerateBracket.Text = "Generate Bracket";
            this.btnGenerateBracket.Click += new System.EventHandler(this.btnGenerateBracket_Click);
            // 
            // tlpRaceQueue
            // 
            this.tlpRaceQueue.ColumnCount = 4;
            this.tlpRaceQueue.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tlpRaceQueue.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpRaceQueue.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlpRaceQueue.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpRaceQueue.Controls.Add(this.lblCurrentRaceLabel, 0, 0);
            this.tlpRaceQueue.Controls.Add(this.btnWinner1, 1, 0);
            this.tlpRaceQueue.Controls.Add(this.lblVs0, 2, 0);
            this.tlpRaceQueue.Controls.Add(this.btnWinner2, 3, 0);
            this.tlpRaceQueue.Controls.Add(this.lblOnDeck, 0, 1);
            this.tlpRaceQueue.Controls.Add(this.lblOnDeckD1, 1, 1);
            this.tlpRaceQueue.Controls.Add(this.lblVs1, 2, 1);
            this.tlpRaceQueue.Controls.Add(this.lblOnDeckD2, 3, 1);
            this.tlpRaceQueue.Controls.Add(this.lblInTheHole, 0, 2);
            this.tlpRaceQueue.Controls.Add(this.lblInHoleD1, 1, 2);
            this.tlpRaceQueue.Controls.Add(this.lblVs2, 2, 2);
            this.tlpRaceQueue.Controls.Add(this.lblInHoleD2, 3, 2);
            this.tlpRaceQueue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpRaceQueue.Location = new System.Drawing.Point(200, 0);
            this.tlpRaceQueue.Margin = new System.Windows.Forms.Padding(0);
            this.tlpRaceQueue.Name = "tlpRaceQueue";
            this.tlpRaceQueue.RowCount = 3;
            this.tlpBottom.SetRowSpan(this.tlpRaceQueue, 3);
            this.tlpRaceQueue.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.tlpRaceQueue.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.tlpRaceQueue.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.tlpRaceQueue.Size = new System.Drawing.Size(484, 170);
            this.tlpRaceQueue.TabIndex = 16;
            // 
            // lblCurrentRaceLabel
            // 
            this.lblCurrentRaceLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCurrentRaceLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCurrentRaceLabel.Location = new System.Drawing.Point(3, 0);
            this.lblCurrentRaceLabel.Name = "lblCurrentRaceLabel";
            this.lblCurrentRaceLabel.Size = new System.Drawing.Size(104, 56);
            this.lblCurrentRaceLabel.TabIndex = 30;
            this.lblCurrentRaceLabel.Text = "Current race";
            this.lblCurrentRaceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnWinner1
            // 
            this.btnWinner1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnWinner1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnWinner1.Location = new System.Drawing.Point(113, 3);
            this.btnWinner1.Name = "btnWinner1";
            this.btnWinner1.Size = new System.Drawing.Size(171, 50);
            this.btnWinner1.TabIndex = 17;
            this.btnWinner1.Text = "—";
            this.btnWinner1.Click += new System.EventHandler(this.btnWinner1_Click);
            // 
            // lblVs0
            // 
            this.lblVs0.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVs0.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVs0.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblVs0.Location = new System.Drawing.Point(287, 0);
            this.lblVs0.Margin = new System.Windows.Forms.Padding(0);
            this.lblVs0.Name = "lblVs0";
            this.lblVs0.Size = new System.Drawing.Size(20, 56);
            this.lblVs0.TabIndex = 31;
            this.lblVs0.Text = "vs";
            this.lblVs0.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnWinner2
            // 
            this.btnWinner2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnWinner2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnWinner2.Location = new System.Drawing.Point(310, 3);
            this.btnWinner2.Name = "btnWinner2";
            this.btnWinner2.Size = new System.Drawing.Size(171, 50);
            this.btnWinner2.TabIndex = 18;
            this.btnWinner2.Text = "—";
            this.btnWinner2.Click += new System.EventHandler(this.btnWinner2_Click);
            // 
            // lblOnDeck
            // 
            this.lblOnDeck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOnDeck.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOnDeck.Location = new System.Drawing.Point(3, 56);
            this.lblOnDeck.Name = "lblOnDeck";
            this.lblOnDeck.Size = new System.Drawing.Size(104, 56);
            this.lblOnDeck.TabIndex = 32;
            this.lblOnDeck.Text = "On deck";
            this.lblOnDeck.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblOnDeckD1
            // 
            this.lblOnDeckD1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblOnDeckD1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOnDeckD1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOnDeckD1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOnDeckD1.Location = new System.Drawing.Point(113, 59);
            this.lblOnDeckD1.Margin = new System.Windows.Forms.Padding(3);
            this.lblOnDeckD1.Name = "lblOnDeckD1";
            this.lblOnDeckD1.Size = new System.Drawing.Size(171, 50);
            this.lblOnDeckD1.TabIndex = 33;
            this.lblOnDeckD1.Text = "—";
            this.lblOnDeckD1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblVs1
            // 
            this.lblVs1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVs1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVs1.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblVs1.Location = new System.Drawing.Point(287, 56);
            this.lblVs1.Margin = new System.Windows.Forms.Padding(0);
            this.lblVs1.Name = "lblVs1";
            this.lblVs1.Size = new System.Drawing.Size(20, 56);
            this.lblVs1.TabIndex = 34;
            this.lblVs1.Text = "vs";
            this.lblVs1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblOnDeckD2
            // 
            this.lblOnDeckD2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblOnDeckD2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOnDeckD2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOnDeckD2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOnDeckD2.Location = new System.Drawing.Point(310, 59);
            this.lblOnDeckD2.Margin = new System.Windows.Forms.Padding(3);
            this.lblOnDeckD2.Name = "lblOnDeckD2";
            this.lblOnDeckD2.Size = new System.Drawing.Size(171, 50);
            this.lblOnDeckD2.TabIndex = 35;
            this.lblOnDeckD2.Text = "—";
            this.lblOnDeckD2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblInTheHole
            // 
            this.lblInTheHole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInTheHole.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInTheHole.Location = new System.Drawing.Point(3, 112);
            this.lblInTheHole.Name = "lblInTheHole";
            this.lblInTheHole.Size = new System.Drawing.Size(104, 58);
            this.lblInTheHole.TabIndex = 36;
            this.lblInTheHole.Text = "In the hole";
            this.lblInTheHole.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblInHoleD1
            // 
            this.lblInHoleD1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblInHoleD1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblInHoleD1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInHoleD1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInHoleD1.Location = new System.Drawing.Point(113, 115);
            this.lblInHoleD1.Margin = new System.Windows.Forms.Padding(3);
            this.lblInHoleD1.Name = "lblInHoleD1";
            this.lblInHoleD1.Size = new System.Drawing.Size(171, 52);
            this.lblInHoleD1.TabIndex = 37;
            this.lblInHoleD1.Text = "—";
            this.lblInHoleD1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblVs2
            // 
            this.lblVs2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVs2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVs2.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblVs2.Location = new System.Drawing.Point(287, 112);
            this.lblVs2.Margin = new System.Windows.Forms.Padding(0);
            this.lblVs2.Name = "lblVs2";
            this.lblVs2.Size = new System.Drawing.Size(20, 58);
            this.lblVs2.TabIndex = 38;
            this.lblVs2.Text = "vs";
            this.lblVs2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblInHoleD2
            // 
            this.lblInHoleD2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblInHoleD2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblInHoleD2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInHoleD2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInHoleD2.Location = new System.Drawing.Point(310, 115);
            this.lblInHoleD2.Margin = new System.Windows.Forms.Padding(3);
            this.lblInHoleD2.Name = "lblInHoleD2";
            this.lblInHoleD2.Size = new System.Drawing.Size(171, 52);
            this.lblInHoleD2.TabIndex = 39;
            this.lblInHoleD2.Text = "—";
            this.lblInHoleD2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnNextRound
            // 
            this.btnNextRound.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNextRound.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNextRound.Location = new System.Drawing.Point(687, 3);
            this.btnNextRound.Name = "btnNextRound";
            this.tlpBottom.SetRowSpan(this.btnNextRound, 3);
            this.btnNextRound.Size = new System.Drawing.Size(194, 164);
            this.btnNextRound.TabIndex = 16;
            this.btnNextRound.Text = "Generate Next Round";
            this.btnNextRound.Click += new System.EventHandler(this.btnNextRound_Click);
            // 
            // pnlRail
            // 
            this.pnlRail.Controls.Add(this.tlpRail);
            this.pnlRail.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlRail.Location = new System.Drawing.Point(768, 50);
            this.pnlRail.Name = "pnlRail";
            this.pnlRail.Padding = new System.Windows.Forms.Padding(0, 20, 0, 0);
            this.pnlRail.Size = new System.Drawing.Size(116, 341);
            this.pnlRail.TabIndex = 102;
            // 
            // tlpRail
            // 
            this.tlpRail.ColumnCount = 1;
            this.tlpRail.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpRail.Controls.Add(this.btnEditResult, 0, 0);
            this.tlpRail.Controls.Add(this.btnReset, 0, 1);
            this.tlpRail.Controls.Add(this.btnStandings, 0, 2);
            this.tlpRail.Controls.Add(this.btnGenerateLosersBracket, 0, 3);
            this.tlpRail.Controls.Add(this.btnShowQRCode, 0, 4);
            this.tlpRail.Controls.Add(this.btnSaveProgress, 0, 6);
            this.tlpRail.Controls.Add(this.btnCloseRace, 0, 7);
            this.tlpRail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpRail.Location = new System.Drawing.Point(0, 20);
            this.tlpRail.Margin = new System.Windows.Forms.Padding(0);
            this.tlpRail.Name = "tlpRail";
            this.tlpRail.RowCount = 8;
            this.tlpRail.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlpRail.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlpRail.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlpRail.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlpRail.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlpRail.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpRail.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlpRail.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlpRail.Size = new System.Drawing.Size(116, 321);
            this.tlpRail.TabIndex = 0;
            // 
            // btnEditResult
            // 
            this.btnEditResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnEditResult.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEditResult.Location = new System.Drawing.Point(3, 0);
            this.btnEditResult.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.btnEditResult.Name = "btnEditResult";
            this.btnEditResult.Size = new System.Drawing.Size(110, 47);
            this.btnEditResult.TabIndex = 20;
            this.btnEditResult.Text = "Edit Match Result";
            // 
            // btnReset
            // 
            this.btnReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReset.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnReset.Location = new System.Drawing.Point(3, 53);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(110, 44);
            this.btnReset.TabIndex = 14;
            this.btnReset.Text = "Reset Race";
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnStandings
            // 
            this.btnStandings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStandings.Enabled = false;
            this.btnStandings.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
            this.btnStandings.Location = new System.Drawing.Point(3, 103);
            this.btnStandings.Name = "btnStandings";
            this.btnStandings.Size = new System.Drawing.Size(110, 44);
            this.btnStandings.TabIndex = 23;
            this.btnStandings.Text = "Standings";
            this.btnStandings.Click += new System.EventHandler(this.btnStandings_Click);
            // 
            // btnGenerateLosersBracket
            // 
            this.btnGenerateLosersBracket.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGenerateLosersBracket.Enabled = false;
            this.btnGenerateLosersBracket.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGenerateLosersBracket.Location = new System.Drawing.Point(3, 153);
            this.btnGenerateLosersBracket.Name = "btnGenerateLosersBracket";
            this.btnGenerateLosersBracket.Size = new System.Drawing.Size(110, 44);
            this.btnGenerateLosersBracket.TabIndex = 21;
            this.btnGenerateLosersBracket.Text = "Buy Back";
            this.btnGenerateLosersBracket.Click += new System.EventHandler(this.btnGenerateLosersBracket_Click);
            // 
            // btnShowQRCode
            // 
            this.btnShowQRCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnShowQRCode.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnShowQRCode.Location = new System.Drawing.Point(3, 203);
            this.btnShowQRCode.Name = "btnShowQRCode";
            this.btnShowQRCode.Size = new System.Drawing.Size(110, 44);
            this.btnShowQRCode.TabIndex = 24;
            this.btnShowQRCode.Text = "Show QR Code";
            this.btnShowQRCode.Click += new System.EventHandler(this.btnShowQRCode_Click);
            //
            // btnSaveProgress
            //
            this.btnSaveProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveProgress.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSaveProgress.Location = new System.Drawing.Point(3, 274);
            this.btnSaveProgress.Name = "btnSaveProgress";
            this.btnSaveProgress.Size = new System.Drawing.Size(110, 44);
            this.btnSaveProgress.TabIndex = 22;
            this.btnSaveProgress.Text = "Save Progress";
            this.btnSaveProgress.Click += new System.EventHandler(this.btnSaveProgress_Click);
            //
            // btnCloseRace
            //
            this.btnCloseRace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCloseRace.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCloseRace.Location = new System.Drawing.Point(3, 324);
            this.btnCloseRace.Name = "btnCloseRace";
            this.btnCloseRace.Size = new System.Drawing.Size(110, 44);
            this.btnCloseRace.TabIndex = 25;
            this.btnCloseRace.Text = "Close Race";
            this.btnCloseRace.Click += new System.EventHandler(this.btnCloseRace_Click);
            // 
            // pnlLeft
            // 
            this.pnlLeft.Controls.Add(this.lvDrivers);
            this.pnlLeft.Controls.Add(this.tlpSetTimes);
            this.pnlLeft.Controls.Add(this.txtTime);
            this.pnlLeft.Controls.Add(this.tlpAddEdit);
            this.pnlLeft.Controls.Add(this.txtName);
            this.pnlLeft.Controls.Add(this.lblDriversHeader);
            this.pnlLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlLeft.Location = new System.Drawing.Point(0, 50);
            this.pnlLeft.Name = "pnlLeft";
            this.pnlLeft.Size = new System.Drawing.Size(224, 341);
            this.pnlLeft.TabIndex = 103;
            // 
            // lvDrivers
            // 
            this.lvDrivers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvDrivers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colTime,
            this.colDialIn});
            this.lvDrivers.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lvDrivers.FullRowSelect = true;
            this.lvDrivers.HideSelection = false;
            this.lvDrivers.Location = new System.Drawing.Point(8, 150);
            this.lvDrivers.MultiSelect = false;
            this.lvDrivers.Name = "lvDrivers";
            this.lvDrivers.Size = new System.Drawing.Size(208, 171);
            this.lvDrivers.TabIndex = 9;
            this.lvDrivers.UseCompatibleStateImageBehavior = false;
            this.lvDrivers.View = System.Windows.Forms.View.Details;
            this.lvDrivers.SelectedIndexChanged += new System.EventHandler(this.lvDrivers_SelectedIndexChanged);
            // 
            // colName
            // 
            this.colName.Text = "Name";
            this.colName.Width = 80;
            // 
            // colTime
            // 
            this.colTime.Text = "Qual Time";
            this.colTime.Width = 65;
            // 
            // colDialIn
            // 
            this.colDialIn.Text = "Dial-In";
            this.colDialIn.Width = 65;
            // 
            // tlpSetTimes
            // 
            this.tlpSetTimes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpSetTimes.ColumnCount = 2;
            this.tlpSetTimes.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpSetTimes.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpSetTimes.Controls.Add(this.btnSetQualTime, 0, 0);
            this.tlpSetTimes.Controls.Add(this.btnSetDialIn, 1, 0);
            this.tlpSetTimes.Location = new System.Drawing.Point(8, 112);
            this.tlpSetTimes.Margin = new System.Windows.Forms.Padding(0);
            this.tlpSetTimes.Name = "tlpSetTimes";
            this.tlpSetTimes.RowCount = 1;
            this.tlpSetTimes.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpSetTimes.Size = new System.Drawing.Size(208, 30);
            this.tlpSetTimes.TabIndex = 201;
            // 
            // btnSetQualTime
            // 
            this.btnSetQualTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSetQualTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSetQualTime.Location = new System.Drawing.Point(2, 2);
            this.btnSetQualTime.Margin = new System.Windows.Forms.Padding(2);
            this.btnSetQualTime.Name = "btnSetQualTime";
            this.btnSetQualTime.Size = new System.Drawing.Size(100, 26);
            this.btnSetQualTime.TabIndex = 11;
            this.btnSetQualTime.Text = "Set Time";
            this.btnSetQualTime.Click += new System.EventHandler(this.btnSetQualTime_Click);
            // 
            // btnSetDialIn
            // 
            this.btnSetDialIn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSetDialIn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSetDialIn.Location = new System.Drawing.Point(106, 2);
            this.btnSetDialIn.Margin = new System.Windows.Forms.Padding(2);
            this.btnSetDialIn.Name = "btnSetDialIn";
            this.btnSetDialIn.Size = new System.Drawing.Size(100, 26);
            this.btnSetDialIn.TabIndex = 25;
            this.btnSetDialIn.Text = "Set Dial-In";
            this.btnSetDialIn.Click += new System.EventHandler(this.btnSetDialIn_Click);
            // 
            // txtTime
            // 
            this.txtTime.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtTime.Location = new System.Drawing.Point(8, 84);
            this.txtTime.Name = "txtTime";
            this.txtTime.Size = new System.Drawing.Size(208, 22);
            this.txtTime.TabIndex = 7;
            this.txtTime.TextChanged += new System.EventHandler(this.txtTime_TextChanged);
            // 
            // tlpAddEdit
            // 
            this.tlpAddEdit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpAddEdit.ColumnCount = 2;
            this.tlpAddEdit.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpAddEdit.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpAddEdit.Controls.Add(this.btnAddDriver, 0, 0);
            this.tlpAddEdit.Controls.Add(this.btnEditDriver, 1, 0);
            this.tlpAddEdit.Location = new System.Drawing.Point(8, 48);
            this.tlpAddEdit.Margin = new System.Windows.Forms.Padding(0);
            this.tlpAddEdit.Name = "tlpAddEdit";
            this.tlpAddEdit.RowCount = 1;
            this.tlpAddEdit.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpAddEdit.Size = new System.Drawing.Size(208, 30);
            this.tlpAddEdit.TabIndex = 200;
            // 
            // btnAddDriver
            // 
            this.btnAddDriver.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAddDriver.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddDriver.Location = new System.Drawing.Point(2, 2);
            this.btnAddDriver.Margin = new System.Windows.Forms.Padding(2);
            this.btnAddDriver.Name = "btnAddDriver";
            this.btnAddDriver.Size = new System.Drawing.Size(100, 26);
            this.btnAddDriver.TabIndex = 8;
            this.btnAddDriver.Text = "Add Driver";
            this.btnAddDriver.Click += new System.EventHandler(this.btnAddDriver_Click);
            // 
            // btnEditDriver
            // 
            this.btnEditDriver.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnEditDriver.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEditDriver.Location = new System.Drawing.Point(106, 2);
            this.btnEditDriver.Margin = new System.Windows.Forms.Padding(2);
            this.btnEditDriver.Name = "btnEditDriver";
            this.btnEditDriver.Size = new System.Drawing.Size(100, 26);
            this.btnEditDriver.TabIndex = 10;
            this.btnEditDriver.Text = "Edit Driver";
            this.btnEditDriver.Click += new System.EventHandler(this.btnEditDriver_Click);
            // 
            // txtName
            // 
            this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtName.Location = new System.Drawing.Point(8, 20);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(208, 22);
            this.txtName.TabIndex = 6;
            // 
            // lblDriversHeader
            // 
            this.lblDriversHeader.AutoSize = true;
            this.lblDriversHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDriversHeader.Location = new System.Drawing.Point(8, 0);
            this.lblDriversHeader.Name = "lblDriversHeader";
            this.lblDriversHeader.Size = new System.Drawing.Size(69, 16);
            this.lblDriversHeader.TabIndex = 5;
            this.lblDriversHeader.Text = "Driver List:";
            this.lblDriversHeader.Click += new System.EventHandler(this.lblDriversHeader_Click);
            // 
            // tlpMain
            // 
            this.tlpMain.ColumnCount = 2;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpMain.Controls.Add(this.pnlCenter, 0, 0);
            this.tlpMain.Controls.Add(this.pnlRight, 1, 0);
            this.tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMain.Location = new System.Drawing.Point(224, 50);
            this.tlpMain.Margin = new System.Windows.Forms.Padding(0);
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.RowCount = 1;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.Size = new System.Drawing.Size(544, 341);
            this.tlpMain.TabIndex = 0;
            // 
            // pnlCenter
            // 
            this.pnlCenter.Controls.Add(this.lvPairings);
            this.pnlCenter.Controls.Add(this.lblPairingsHeader);
            this.pnlCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCenter.Location = new System.Drawing.Point(0, 0);
            this.pnlCenter.Margin = new System.Windows.Forms.Padding(0);
            this.pnlCenter.Name = "pnlCenter";
            this.pnlCenter.Size = new System.Drawing.Size(272, 341);
            this.pnlCenter.TabIndex = 104;
            // 
            // lvPairings
            // 
            this.lvPairings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colMatch,
            this.colDriver1,
            this.colDriver2});
            this.lvPairings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvPairings.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lvPairings.FullRowSelect = true;
            this.lvPairings.HideSelection = false;
            this.lvPairings.Location = new System.Drawing.Point(0, 20);
            this.lvPairings.MultiSelect = false;
            this.lvPairings.Name = "lvPairings";
            this.lvPairings.Size = new System.Drawing.Size(272, 321);
            this.lvPairings.TabIndex = 4;
            this.lvPairings.UseCompatibleStateImageBehavior = false;
            this.lvPairings.View = System.Windows.Forms.View.Details;
            // 
            // colMatch
            // 
            this.colMatch.Text = "M#";
            this.colMatch.Width = 35;
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
            // lblPairingsHeader
            // 
            this.lblPairingsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPairingsHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPairingsHeader.Location = new System.Drawing.Point(0, 0);
            this.lblPairingsHeader.Name = "lblPairingsHeader";
            this.lblPairingsHeader.Size = new System.Drawing.Size(272, 20);
            this.lblPairingsHeader.TabIndex = 3;
            this.lblPairingsHeader.Text = "Current Round Pairings:";
            this.lblPairingsHeader.Click += new System.EventHandler(this.lblPairingsHeader_Click);
            // 
            // pnlRight
            // 
            this.pnlRight.Controls.Add(this.lvWinners);
            this.pnlRight.Controls.Add(this.lblWinnersHeader);
            this.pnlRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRight.Location = new System.Drawing.Point(276, 0);
            this.pnlRight.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.pnlRight.Name = "pnlRight";
            this.pnlRight.Size = new System.Drawing.Size(268, 341);
            this.pnlRight.TabIndex = 105;
            // 
            // lvWinners
            // 
            this.lvWinners.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colMatchWin,
            this.colWinner,
            this.colLoser});
            this.lvWinners.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvWinners.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lvWinners.FullRowSelect = true;
            this.lvWinners.HideSelection = false;
            this.lvWinners.Location = new System.Drawing.Point(0, 20);
            this.lvWinners.MultiSelect = false;
            this.lvWinners.Name = "lvWinners";
            this.lvWinners.Size = new System.Drawing.Size(268, 321);
            this.lvWinners.TabIndex = 13;
            this.lvWinners.UseCompatibleStateImageBehavior = false;
            this.lvWinners.View = System.Windows.Forms.View.Details;
            // 
            // colMatchWin
            // 
            this.colMatchWin.Text = "M#";
            this.colMatchWin.Width = 35;
            // 
            // colWinner
            // 
            this.colWinner.Text = "Winner";
            this.colWinner.Width = 160;
            // 
            // colLoser
            // 
            this.colLoser.Text = "Loser";
            this.colLoser.Width = 160;
            // 
            // lblWinnersHeader
            // 
            this.lblWinnersHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblWinnersHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWinnersHeader.Location = new System.Drawing.Point(0, 0);
            this.lblWinnersHeader.Name = "lblWinnersHeader";
            this.lblWinnersHeader.Size = new System.Drawing.Size(268, 20);
            this.lblWinnersHeader.TabIndex = 12;
            this.lblWinnersHeader.Text = "Match Winners:";
            this.lblWinnersHeader.Click += new System.EventHandler(this.lblWinnersHeader_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(884, 561);
            this.Controls.Add(this.tlpMain);
            this.Controls.Add(this.pnlLeft);
            this.Controls.Add(this.pnlRail);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.pnlHeader);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RC Drag Manager Stable Build";
            this.pnlHeader.ResumeLayout(false);
            this.pnlBottom.ResumeLayout(false);
            this.tlpBottom.ResumeLayout(false);
            this.tlpRaceQueue.ResumeLayout(false);
            this.pnlRail.ResumeLayout(false);
            this.tlpRail.ResumeLayout(false);
            this.pnlLeft.ResumeLayout(false);
            this.pnlLeft.PerformLayout();
            this.tlpSetTimes.ResumeLayout(false);
            this.tlpAddEdit.ResumeLayout(false);
            this.tlpMain.ResumeLayout(false);
            this.pnlCenter.ResumeLayout(false);
            this.pnlRight.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
