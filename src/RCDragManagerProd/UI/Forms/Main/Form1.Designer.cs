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
        private Label lblRaceType;          // legacy, removed in next commit
        private ComboBox cmbRaceType;       // legacy, removed in next commit

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
        private Button btnSaveAndClose;

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
            // ── Instantiate everything ──────────────────────────────────────────
            this.pnlHeader = new Panel();
            this.pnlBottom = new Panel();
            this.pnlRail = new Panel();
            this.pnlLeft = new Panel();
            this.tlpMain = new TableLayoutPanel();

            this.lblEventTitle = new Label();
            this.lblRaceType = new Label();
            this.cmbRaceType = new ComboBox();

            this.lblDriversHeader = new Label();
            this.txtName = new TextBox();
            this.tlpAddEdit = new TableLayoutPanel();
            this.btnAddDriver = new Button();
            this.btnEditDriver = new Button();
            this.txtTime = new TextBox();
            this.tlpSetTimes = new TableLayoutPanel();
            this.btnSetQualTime = new Button();
            this.btnSetDialIn = new Button();
            this.lvDrivers = new ListView();
            this.colName = new ColumnHeader();
            this.colTime = new ColumnHeader();
            this.colDialIn = new ColumnHeader();

            this.pnlCenter = new Panel();
            this.lblPairingsHeader = new Label();
            this.lvPairings = new ListView();
            this.colMatch = new ColumnHeader();
            this.colDriver1 = new ColumnHeader();
            this.colDriver2 = new ColumnHeader();

            this.pnlRight = new Panel();
            this.lblWinnersHeader = new Label();
            this.lvWinners = new ListView();
            this.colMatchWin = new ColumnHeader();
            this.colWinner = new ColumnHeader();
            this.colLoser = new ColumnHeader();

            this.tlpRail = new TableLayoutPanel();
            this.btnEditResult = new Button();
            this.btnReset = new Button();
            this.btnStandings = new Button();
            this.btnGenerateLosersBracket = new Button();
            this.btnShowQRCode = new Button();
            this.btnSaveAndClose = new Button();

            this.tlpBottom = new TableLayoutPanel();
            this.btnGenerateBracket = new Button();
            this.tlpRaceQueue = new TableLayoutPanel();
            this.btnNextRound = new Button();
            this.lblCurrentRaceLabel = new Label();
            this.btnWinner1 = new Button();
            this.lblVs0 = new Label();
            this.btnWinner2 = new Button();
            this.lblOnDeck = new Label();
            this.lblOnDeckD1 = new Label();
            this.lblVs1 = new Label();
            this.lblOnDeckD2 = new Label();
            this.lblInTheHole = new Label();
            this.lblInHoleD1 = new Label();
            this.lblVs2 = new Label();
            this.lblInHoleD2 = new Label();

            this.SuspendLayout();

            // ─────────────────────────────────────────────────────────────
            // Header panel (Dock=Top, Height=50)
            // ─────────────────────────────────────────────────────────────
            this.pnlHeader.SuspendLayout();

            this.lblEventTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.lblEventTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblEventTitle.Location = new Point(0, 4);
            this.lblEventTitle.Size = new Size(900, 42);
            this.lblEventTitle.Name = "lblEventTitle";
            this.lblEventTitle.TabIndex = 0;
            this.lblEventTitle.Text = "Event:";
            this.lblEventTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblEventTitle.Click += new EventHandler(this.lblEventTitle_Click);

            this.lblRaceType.AutoSize = true;
            this.lblRaceType.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblRaceType.Location = new Point(10, 14);
            this.lblRaceType.Name = "lblRaceType";
            this.lblRaceType.TabIndex = 1;
            this.lblRaceType.Text = "Race Type:";

            this.cmbRaceType.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbRaceType.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.cmbRaceType.Items.AddRange(new object[] { "Pro Ladder", "Randomized", "Round Robin" });
            this.cmbRaceType.Location = new Point(95, 11);
            this.cmbRaceType.Name = "cmbRaceType";
            this.cmbRaceType.Size = new Size(150, 24);
            this.cmbRaceType.TabIndex = 2;
            this.cmbRaceType.SelectedIndexChanged += new EventHandler(this.cmbRaceType_SelectedIndexChanged);

            this.pnlHeader.Controls.Add(this.lblRaceType);
            this.pnlHeader.Controls.Add(this.cmbRaceType);
            this.pnlHeader.Controls.Add(this.lblEventTitle);
            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Location = new Point(0, 0);
            this.pnlHeader.Size = new Size(900, 50);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.TabIndex = 100;

            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();

            // ─────────────────────────────────────────────────────────────
            // Bottom panel (Dock=Bottom, Height=170)
            // tlpBottom: 3 cols (200 / fill / 200) × 3 rows, RowSpan=3 on each cell
            // tlpRaceQueue: 4 cols (110 / fill / 20 / fill) × 3 rows
            // ─────────────────────────────────────────────────────────────
            this.pnlBottom.SuspendLayout();
            this.tlpBottom.SuspendLayout();
            this.tlpRaceQueue.SuspendLayout();

            // btnGenerateBracket (left, RowSpan=3)
            this.btnGenerateBracket.Dock = DockStyle.Fill;
            this.btnGenerateBracket.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnGenerateBracket.Margin = new Padding(3);
            this.btnGenerateBracket.Name = "btnGenerateBracket";
            this.btnGenerateBracket.TabIndex = 15;
            this.btnGenerateBracket.Text = "Generate Bracket";
            this.btnGenerateBracket.Click += new EventHandler(this.btnGenerateBracket_Click);

            // btnNextRound (right, RowSpan=3)
            this.btnNextRound.Dock = DockStyle.Fill;
            this.btnNextRound.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnNextRound.Margin = new Padding(3);
            this.btnNextRound.Name = "btnNextRound";
            this.btnNextRound.TabIndex = 16;
            this.btnNextRound.Text = "Generate Next Round";
            this.btnNextRound.Click += new EventHandler(this.btnNextRound_Click);

            // tlpRaceQueue inner controls
            this.lblCurrentRaceLabel.Dock = DockStyle.Fill;
            this.lblCurrentRaceLabel.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.lblCurrentRaceLabel.Margin = new Padding(3, 0, 3, 0);
            this.lblCurrentRaceLabel.Name = "lblCurrentRaceLabel";
            this.lblCurrentRaceLabel.TabIndex = 30;
            this.lblCurrentRaceLabel.Text = "Current race";
            this.lblCurrentRaceLabel.TextAlign = ContentAlignment.MiddleRight;

            this.btnWinner1.Dock = DockStyle.Fill;
            this.btnWinner1.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnWinner1.Margin = new Padding(3);
            this.btnWinner1.Name = "btnWinner1";
            this.btnWinner1.TabIndex = 17;
            this.btnWinner1.Text = "—";
            this.btnWinner1.Click += new EventHandler(this.btnWinner1_Click);

            this.lblVs0.Dock = DockStyle.Fill;
            this.lblVs0.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblVs0.ForeColor = SystemColors.GrayText;
            this.lblVs0.Margin = new Padding(0);
            this.lblVs0.Name = "lblVs0";
            this.lblVs0.TabIndex = 31;
            this.lblVs0.Text = "vs";
            this.lblVs0.TextAlign = ContentAlignment.MiddleCenter;

            this.btnWinner2.Dock = DockStyle.Fill;
            this.btnWinner2.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnWinner2.Margin = new Padding(3);
            this.btnWinner2.Name = "btnWinner2";
            this.btnWinner2.TabIndex = 18;
            this.btnWinner2.Text = "—";
            this.btnWinner2.Click += new EventHandler(this.btnWinner2_Click);

            this.lblOnDeck.Dock = DockStyle.Fill;
            this.lblOnDeck.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblOnDeck.Margin = new Padding(3, 0, 3, 0);
            this.lblOnDeck.Name = "lblOnDeck";
            this.lblOnDeck.TabIndex = 32;
            this.lblOnDeck.Text = "On deck";
            this.lblOnDeck.TextAlign = ContentAlignment.MiddleRight;

            this.lblOnDeckD1.BackColor = Color.WhiteSmoke;
            this.lblOnDeckD1.BorderStyle = BorderStyle.FixedSingle;
            this.lblOnDeckD1.Dock = DockStyle.Fill;
            this.lblOnDeckD1.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblOnDeckD1.Margin = new Padding(3);
            this.lblOnDeckD1.Name = "lblOnDeckD1";
            this.lblOnDeckD1.TabIndex = 33;
            this.lblOnDeckD1.Text = "—";
            this.lblOnDeckD1.TextAlign = ContentAlignment.MiddleCenter;

            this.lblVs1.Dock = DockStyle.Fill;
            this.lblVs1.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblVs1.ForeColor = SystemColors.GrayText;
            this.lblVs1.Margin = new Padding(0);
            this.lblVs1.Name = "lblVs1";
            this.lblVs1.TabIndex = 34;
            this.lblVs1.Text = "vs";
            this.lblVs1.TextAlign = ContentAlignment.MiddleCenter;

            this.lblOnDeckD2.BackColor = Color.WhiteSmoke;
            this.lblOnDeckD2.BorderStyle = BorderStyle.FixedSingle;
            this.lblOnDeckD2.Dock = DockStyle.Fill;
            this.lblOnDeckD2.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblOnDeckD2.Margin = new Padding(3);
            this.lblOnDeckD2.Name = "lblOnDeckD2";
            this.lblOnDeckD2.TabIndex = 35;
            this.lblOnDeckD2.Text = "—";
            this.lblOnDeckD2.TextAlign = ContentAlignment.MiddleCenter;

            this.lblInTheHole.Dock = DockStyle.Fill;
            this.lblInTheHole.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblInTheHole.Margin = new Padding(3, 0, 3, 0);
            this.lblInTheHole.Name = "lblInTheHole";
            this.lblInTheHole.TabIndex = 36;
            this.lblInTheHole.Text = "In the hole";
            this.lblInTheHole.TextAlign = ContentAlignment.MiddleRight;

            this.lblInHoleD1.BackColor = Color.WhiteSmoke;
            this.lblInHoleD1.BorderStyle = BorderStyle.FixedSingle;
            this.lblInHoleD1.Dock = DockStyle.Fill;
            this.lblInHoleD1.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblInHoleD1.Margin = new Padding(3);
            this.lblInHoleD1.Name = "lblInHoleD1";
            this.lblInHoleD1.TabIndex = 37;
            this.lblInHoleD1.Text = "—";
            this.lblInHoleD1.TextAlign = ContentAlignment.MiddleCenter;

            this.lblVs2.Dock = DockStyle.Fill;
            this.lblVs2.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblVs2.ForeColor = SystemColors.GrayText;
            this.lblVs2.Margin = new Padding(0);
            this.lblVs2.Name = "lblVs2";
            this.lblVs2.TabIndex = 38;
            this.lblVs2.Text = "vs";
            this.lblVs2.TextAlign = ContentAlignment.MiddleCenter;

            this.lblInHoleD2.BackColor = Color.WhiteSmoke;
            this.lblInHoleD2.BorderStyle = BorderStyle.FixedSingle;
            this.lblInHoleD2.Dock = DockStyle.Fill;
            this.lblInHoleD2.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblInHoleD2.Margin = new Padding(3);
            this.lblInHoleD2.Name = "lblInHoleD2";
            this.lblInHoleD2.TabIndex = 39;
            this.lblInHoleD2.Text = "—";
            this.lblInHoleD2.TextAlign = ContentAlignment.MiddleCenter;

            this.tlpRaceQueue.ColumnCount = 4;
            this.tlpRaceQueue.RowCount = 3;
            this.tlpRaceQueue.Dock = DockStyle.Fill;
            this.tlpRaceQueue.Margin = new Padding(0);
            this.tlpRaceQueue.Name = "tlpRaceQueue";
            this.tlpRaceQueue.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            this.tlpRaceQueue.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpRaceQueue.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            this.tlpRaceQueue.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpRaceQueue.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            this.tlpRaceQueue.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            this.tlpRaceQueue.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));
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

            this.tlpBottom.ColumnCount = 3;
            this.tlpBottom.RowCount = 3;
            this.tlpBottom.Dock = DockStyle.Fill;
            this.tlpBottom.Margin = new Padding(0);
            this.tlpBottom.Name = "tlpBottom";
            this.tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            this.tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tlpBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            this.tlpBottom.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            this.tlpBottom.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            this.tlpBottom.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));
            this.tlpBottom.Controls.Add(this.btnGenerateBracket, 0, 0);
            this.tlpBottom.SetRowSpan(this.btnGenerateBracket, 3);
            this.tlpBottom.Controls.Add(this.tlpRaceQueue, 1, 0);
            this.tlpBottom.SetRowSpan(this.tlpRaceQueue, 3);
            this.tlpBottom.Controls.Add(this.btnNextRound, 2, 0);
            this.tlpBottom.SetRowSpan(this.btnNextRound, 3);

            this.pnlBottom.Controls.Add(this.tlpBottom);
            this.pnlBottom.Dock = DockStyle.Bottom;
            this.pnlBottom.Size = new Size(900, 170);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.TabIndex = 101;

            this.tlpRaceQueue.ResumeLayout(false);
            this.tlpBottom.ResumeLayout(false);
            this.pnlBottom.ResumeLayout(false);

            // ─────────────────────────────────────────────────────────────
            // Right rail (Dock=Right, Width=116)
            // tlpRail: 1 col × 7 rows (5×Absolute50 / 1×Percent100 / 1×Absolute50)
            // ─────────────────────────────────────────────────────────────
            this.pnlRail.SuspendLayout();
            this.tlpRail.SuspendLayout();

            this.btnEditResult.Dock = DockStyle.Fill;
            this.btnEditResult.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnEditResult.Margin = new Padding(3);
            this.btnEditResult.Name = "btnEditResult";
            this.btnEditResult.TabIndex = 20;
            this.btnEditResult.Text = "Edit Match Result";

            this.btnReset.Dock = DockStyle.Fill;
            this.btnReset.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnReset.Margin = new Padding(3);
            this.btnReset.Name = "btnReset";
            this.btnReset.TabIndex = 14;
            this.btnReset.Text = "Reset Race";
            this.btnReset.Click += new EventHandler(this.btnReset_Click);

            this.btnStandings.Dock = DockStyle.Fill;
            this.btnStandings.Enabled = false;
            this.btnStandings.Font = new Font("Microsoft Sans Serif", 9.75F);
            this.btnStandings.Margin = new Padding(3);
            this.btnStandings.Name = "btnStandings";
            this.btnStandings.TabIndex = 23;
            this.btnStandings.Text = "Standings";
            this.btnStandings.Click += new EventHandler(this.btnStandings_Click);

            this.btnGenerateLosersBracket.Dock = DockStyle.Fill;
            this.btnGenerateLosersBracket.Enabled = false;
            this.btnGenerateLosersBracket.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnGenerateLosersBracket.Margin = new Padding(3);
            this.btnGenerateLosersBracket.Name = "btnGenerateLosersBracket";
            this.btnGenerateLosersBracket.TabIndex = 21;
            this.btnGenerateLosersBracket.Text = "Buy Back";
            this.btnGenerateLosersBracket.Click += new EventHandler(this.btnGenerateLosersBracket_Click);

            this.btnShowQRCode.Dock = DockStyle.Fill;
            this.btnShowQRCode.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnShowQRCode.Margin = new Padding(3);
            this.btnShowQRCode.Name = "btnShowQRCode";
            this.btnShowQRCode.TabIndex = 24;
            this.btnShowQRCode.Text = "Show QR Code";
            this.btnShowQRCode.Click += new EventHandler(this.btnShowQRCode_Click);

            this.btnSaveAndClose.Dock = DockStyle.Fill;
            this.btnSaveAndClose.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnSaveAndClose.Margin = new Padding(3);
            this.btnSaveAndClose.Name = "btnSaveAndClose";
            this.btnSaveAndClose.TabIndex = 22;
            this.btnSaveAndClose.Text = "Save and Close";
            this.btnSaveAndClose.Click += new EventHandler(this.btnSaveAndClose_Click);

            this.tlpRail.ColumnCount = 1;
            this.tlpRail.RowCount = 7;
            this.tlpRail.Dock = DockStyle.Fill;
            this.tlpRail.Margin = new Padding(0);
            this.tlpRail.Name = "tlpRail";
            this.tlpRail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tlpRail.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.tlpRail.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.tlpRail.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.tlpRail.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.tlpRail.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.tlpRail.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.tlpRail.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.tlpRail.Controls.Add(this.btnEditResult, 0, 0);
            this.tlpRail.Controls.Add(this.btnReset, 0, 1);
            this.tlpRail.Controls.Add(this.btnStandings, 0, 2);
            this.tlpRail.Controls.Add(this.btnGenerateLosersBracket, 0, 3);
            this.tlpRail.Controls.Add(this.btnShowQRCode, 0, 4);
            // Row 5 is the spacer (empty; SizeType.Percent 100)
            this.tlpRail.Controls.Add(this.btnSaveAndClose, 0, 6);

            this.pnlRail.Controls.Add(this.tlpRail);
            this.pnlRail.Dock = DockStyle.Right;
            this.pnlRail.Size = new Size(116, 380);
            this.pnlRail.Name = "pnlRail";
            this.pnlRail.TabIndex = 102;

            this.tlpRail.ResumeLayout(false);
            this.pnlRail.ResumeLayout(false);

            // ─────────────────────────────────────────────────────────────
            // Left column (Dock=Left, Width=224)
            // Absolute layout with Anchor; two TLP rows for paired buttons.
            // ─────────────────────────────────────────────────────────────
            this.pnlLeft.SuspendLayout();
            this.tlpAddEdit.SuspendLayout();
            this.tlpSetTimes.SuspendLayout();

            this.colName.Text = "Name";
            this.colName.Width = 80;
            this.colTime.Text = "Qual Time";
            this.colTime.Width = 65;
            this.colDialIn.Text = "Dial-In";
            this.colDialIn.Width = 65;

            this.lblDriversHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.lblDriversHeader.AutoSize = true;
            this.lblDriversHeader.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblDriversHeader.Location = new Point(8, 8);
            this.lblDriversHeader.Name = "lblDriversHeader";
            this.lblDriversHeader.TabIndex = 5;
            this.lblDriversHeader.Text = "Driver List:";
            this.lblDriversHeader.Click += new EventHandler(this.lblDriversHeader_Click);

            this.txtName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtName.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.txtName.Location = new Point(8, 32);
            this.txtName.Name = "txtName";
            this.txtName.Size = new Size(208, 22);
            this.txtName.TabIndex = 6;

            this.btnAddDriver.Dock = DockStyle.Fill;
            this.btnAddDriver.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnAddDriver.Margin = new Padding(2);
            this.btnAddDriver.Name = "btnAddDriver";
            this.btnAddDriver.TabIndex = 8;
            this.btnAddDriver.Text = "Add Driver";
            this.btnAddDriver.Click += new EventHandler(this.btnAddDriver_Click);

            this.btnEditDriver.Dock = DockStyle.Fill;
            this.btnEditDriver.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnEditDriver.Margin = new Padding(2);
            this.btnEditDriver.Name = "btnEditDriver";
            this.btnEditDriver.TabIndex = 10;
            this.btnEditDriver.Text = "Edit Driver";
            this.btnEditDriver.Click += new EventHandler(this.btnEditDriver_Click);

            this.tlpAddEdit.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.tlpAddEdit.ColumnCount = 2;
            this.tlpAddEdit.RowCount = 1;
            this.tlpAddEdit.Location = new Point(8, 60);
            this.tlpAddEdit.Margin = new Padding(0);
            this.tlpAddEdit.Name = "tlpAddEdit";
            this.tlpAddEdit.Size = new Size(208, 30);
            this.tlpAddEdit.TabIndex = 200;
            this.tlpAddEdit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpAddEdit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpAddEdit.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.tlpAddEdit.Controls.Add(this.btnAddDriver, 0, 0);
            this.tlpAddEdit.Controls.Add(this.btnEditDriver, 1, 0);

            this.txtTime.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtTime.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.txtTime.Location = new Point(8, 96);
            this.txtTime.Name = "txtTime";
            this.txtTime.Size = new Size(208, 22);
            this.txtTime.TabIndex = 7;
            this.txtTime.TextChanged += new EventHandler(this.txtTime_TextChanged);

            this.btnSetQualTime.Dock = DockStyle.Fill;
            this.btnSetQualTime.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnSetQualTime.Margin = new Padding(2);
            this.btnSetQualTime.Name = "btnSetQualTime";
            this.btnSetQualTime.TabIndex = 11;
            this.btnSetQualTime.Text = "Set Time";
            this.btnSetQualTime.Click += new EventHandler(this.btnSetQualTime_Click);

            this.btnSetDialIn.Dock = DockStyle.Fill;
            this.btnSetDialIn.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.btnSetDialIn.Margin = new Padding(2);
            this.btnSetDialIn.Name = "btnSetDialIn";
            this.btnSetDialIn.TabIndex = 25;
            this.btnSetDialIn.Text = "Set Dial-In";
            this.btnSetDialIn.Click += new EventHandler(this.btnSetDialIn_Click);

            this.tlpSetTimes.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.tlpSetTimes.ColumnCount = 2;
            this.tlpSetTimes.RowCount = 1;
            this.tlpSetTimes.Location = new Point(8, 124);
            this.tlpSetTimes.Margin = new Padding(0);
            this.tlpSetTimes.Name = "tlpSetTimes";
            this.tlpSetTimes.Size = new Size(208, 30);
            this.tlpSetTimes.TabIndex = 201;
            this.tlpSetTimes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpSetTimes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpSetTimes.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.tlpSetTimes.Controls.Add(this.btnSetQualTime, 0, 0);
            this.tlpSetTimes.Controls.Add(this.btnSetDialIn, 1, 0);

            this.lvDrivers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.lvDrivers.Columns.AddRange(new ColumnHeader[] { this.colName, this.colTime, this.colDialIn });
            this.lvDrivers.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lvDrivers.FullRowSelect = true;
            this.lvDrivers.HideSelection = false;
            this.lvDrivers.Location = new Point(8, 162);
            this.lvDrivers.MultiSelect = false;
            this.lvDrivers.Name = "lvDrivers";
            this.lvDrivers.Size = new Size(208, 210);
            this.lvDrivers.TabIndex = 9;
            this.lvDrivers.UseCompatibleStateImageBehavior = false;
            this.lvDrivers.View = View.Details;
            this.lvDrivers.SelectedIndexChanged += new EventHandler(this.lvDrivers_SelectedIndexChanged);

            this.pnlLeft.Controls.Add(this.lvDrivers);
            this.pnlLeft.Controls.Add(this.tlpSetTimes);
            this.pnlLeft.Controls.Add(this.txtTime);
            this.pnlLeft.Controls.Add(this.tlpAddEdit);
            this.pnlLeft.Controls.Add(this.txtName);
            this.pnlLeft.Controls.Add(this.lblDriversHeader);
            this.pnlLeft.Dock = DockStyle.Left;
            this.pnlLeft.Padding = new Padding(0);
            this.pnlLeft.Size = new Size(224, 380);
            this.pnlLeft.Name = "pnlLeft";
            this.pnlLeft.TabIndex = 103;

            this.tlpAddEdit.ResumeLayout(false);
            this.tlpSetTimes.ResumeLayout(false);
            this.pnlLeft.ResumeLayout(false);
            this.pnlLeft.PerformLayout();

            // ─────────────────────────────────────────────────────────────
            // Center & Right columns (in tlpMain)
            // ─────────────────────────────────────────────────────────────
            this.pnlCenter.SuspendLayout();
            this.pnlRight.SuspendLayout();
            this.tlpMain.SuspendLayout();

            this.colMatch.Text = "M#";
            this.colMatch.Width = 35;
            this.colDriver1.Text = "Driver 1";
            this.colDriver1.Width = 160;
            this.colDriver2.Text = "Driver 2";
            this.colDriver2.Width = 160;

            this.lvPairings.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.lvPairings.Columns.AddRange(new ColumnHeader[] { this.colMatch, this.colDriver1, this.colDriver2 });
            this.lvPairings.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lvPairings.FullRowSelect = true;
            this.lvPairings.HideSelection = false;
            this.lvPairings.Location = new Point(0, 25);
            this.lvPairings.MultiSelect = false;
            this.lvPairings.Name = "lvPairings";
            this.lvPairings.Size = new Size(420, 350);
            this.lvPairings.TabIndex = 4;
            this.lvPairings.UseCompatibleStateImageBehavior = false;
            this.lvPairings.View = View.Details;

            this.lblPairingsHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.lblPairingsHeader.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblPairingsHeader.Location = new Point(0, 0);
            this.lblPairingsHeader.Size = new Size(420, 20);
            this.lblPairingsHeader.Name = "lblPairingsHeader";
            this.lblPairingsHeader.TabIndex = 3;
            this.lblPairingsHeader.Text = "Current Round Pairings:";
            this.lblPairingsHeader.Click += new EventHandler(this.lblPairingsHeader_Click);

            this.pnlCenter.Controls.Add(this.lvPairings);
            this.pnlCenter.Controls.Add(this.lblPairingsHeader);
            this.pnlCenter.Dock = DockStyle.Fill;
            this.pnlCenter.Margin = new Padding(0);
            this.pnlCenter.Name = "pnlCenter";
            this.pnlCenter.TabIndex = 104;

            this.colMatchWin.Text = "M#";
            this.colMatchWin.Width = 35;
            this.colWinner.Text = "Winner";
            this.colWinner.Width = 160;
            this.colLoser.Text = "Loser";
            this.colLoser.Width = 160;

            this.lvWinners.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.lvWinners.Columns.AddRange(new ColumnHeader[] { this.colMatchWin, this.colWinner, this.colLoser });
            this.lvWinners.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lvWinners.FullRowSelect = true;
            this.lvWinners.HideSelection = false;
            this.lvWinners.Location = new Point(0, 25);
            this.lvWinners.MultiSelect = false;
            this.lvWinners.Name = "lvWinners";
            this.lvWinners.Size = new Size(420, 350);
            this.lvWinners.TabIndex = 13;
            this.lvWinners.UseCompatibleStateImageBehavior = false;
            this.lvWinners.View = View.Details;

            this.lblWinnersHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.lblWinnersHeader.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.lblWinnersHeader.Location = new Point(0, 0);
            this.lblWinnersHeader.Size = new Size(420, 25);
            this.lblWinnersHeader.Name = "lblWinnersHeader";
            this.lblWinnersHeader.TabIndex = 12;
            this.lblWinnersHeader.Text = "Match Winners:";
            this.lblWinnersHeader.Click += new EventHandler(this.lblWinnersHeader_Click);

            this.pnlRight.Controls.Add(this.lvWinners);
            this.pnlRight.Controls.Add(this.lblWinnersHeader);
            this.pnlRight.Dock = DockStyle.Fill;
            this.pnlRight.Margin = new Padding(0);
            this.pnlRight.Name = "pnlRight";
            this.pnlRight.TabIndex = 105;

            this.tlpMain.ColumnCount = 2;
            this.tlpMain.RowCount = 1;
            this.tlpMain.Dock = DockStyle.Fill;
            this.tlpMain.Margin = new Padding(0);
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.tlpMain.Controls.Add(this.pnlCenter, 0, 0);
            this.tlpMain.Controls.Add(this.pnlRight, 1, 0);

            this.pnlCenter.ResumeLayout(false);
            this.pnlRight.ResumeLayout(false);
            this.tlpMain.ResumeLayout(false);

            // ─────────────────────────────────────────────────────────────
            // Form
            // Add docked panels in this order so docking precedence resolves
            // correctly: Header (top, full width) → Bottom (bottom, full width)
            // → Rail (right of middle) → Left (left of middle) → tlpMain (fill).
            // ─────────────────────────────────────────────────────────────
            this.AutoScaleMode = AutoScaleMode.None;
            this.MinimumSize = new Size(900, 600);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "RC Drag Manager Stable Build";

            this.Controls.Add(this.tlpMain);
            this.Controls.Add(this.pnlLeft);
            this.Controls.Add(this.pnlRail);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.pnlHeader);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
