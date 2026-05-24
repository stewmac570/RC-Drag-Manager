namespace RCDragManagerProd.UI.Forms
{
    partial class DriverStatsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblSummary;
        private System.Windows.Forms.ListView lvMatches;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblSummary = new System.Windows.Forms.Label();
            this.lvMatches = new System.Windows.Forms.ListView();
            this.btnClose = new System.Windows.Forms.Button();

            // 
            // lblHeader
            // 
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Location = new System.Drawing.Point(50, 10);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Size = new System.Drawing.Size(800, 30);
            this.lblHeader.Text = "Driver Statistics";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 
            // lblSummary
            // 
            this.lblSummary.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            this.lblSummary.Location = new System.Drawing.Point(50, 50);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Size = new System.Drawing.Size(800, 25);
            this.lblSummary.Text = "Wins: 0 | Losses: 0 | Events Entered: 0 | Events Won: 0";
            this.lblSummary.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 
            // lvMatches
            // 
            this.lvMatches.FullRowSelect = true;
            this.lvMatches.GridLines = true;
            this.lvMatches.HideSelection = false;
            this.lvMatches.Location = new System.Drawing.Point(50, 90);
            this.lvMatches.Name = "lvMatches";
            this.lvMatches.Size = new System.Drawing.Size(800, 400);
            this.lvMatches.TabIndex = 0;
            this.lvMatches.View = System.Windows.Forms.View.Details;
            this.lvMatches.Columns.Add("Event", 260);
            this.lvMatches.Columns.Add("Date", 100);
            this.lvMatches.Columns.Add("Round", 80);
            this.lvMatches.Columns.Add("Opponent", 240);
            this.lvMatches.Columns.Add("Result", 100);

            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(762, 510);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(90, 40);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            // 
            // DriverStatsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.lblHeader);
            this.Controls.Add(this.lblSummary);
            this.Controls.Add(this.lvMatches);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "DriverStatsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Driver Stats";
            this.ResumeLayout(false);
        }
    }
}
