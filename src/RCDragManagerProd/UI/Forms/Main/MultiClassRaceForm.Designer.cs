namespace RCDragManagerProd.UI.Forms
{
    partial class MultiClassRaceForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Button btnSaveEvent;
        private System.Windows.Forms.Label lblSaveStatus;
        private System.Windows.Forms.Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.btnSaveEvent = new System.Windows.Forms.Button();
            this.lblSaveStatus = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();

            // tabControl
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1200, 750);
            this.tabControl.TabIndex = 0;
            this.tabControl.Selecting +=
                new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl_Selecting);
            this.tabControl.SelectedIndexChanged +=
                new System.EventHandler(this.tabControl_SelectedIndexChanged);

            // pnlBottom
            this.pnlBottom.Controls.Add(this.btnSaveEvent);
            this.pnlBottom.Controls.Add(this.lblSaveStatus);
            this.pnlBottom.Controls.Add(this.lblStatus);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Height = 40;
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.TabIndex = 1;

            // btnSaveEvent
            this.btnSaveEvent.Location = new System.Drawing.Point(8, 7);
            this.btnSaveEvent.Name = "btnSaveEvent";
            this.btnSaveEvent.Size = new System.Drawing.Size(100, 26);
            this.btnSaveEvent.TabIndex = 0;
            this.btnSaveEvent.Text = "Save Event";
            this.btnSaveEvent.UseVisualStyleBackColor = true;
            this.btnSaveEvent.Click += new System.EventHandler(this.btnSaveEvent_Click);

            // lblSaveStatus
            this.lblSaveStatus.AutoSize = true;
            this.lblSaveStatus.Location = new System.Drawing.Point(116, 13);
            this.lblSaveStatus.Name = "lblSaveStatus";
            this.lblSaveStatus.Size = new System.Drawing.Size(0, 13);
            this.lblSaveStatus.TabIndex = 1;
            this.lblSaveStatus.Text = "";

            // lblStatus
            this.lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblStatus.AutoSize = false;
            this.lblStatus.ForeColor = System.Drawing.Color.DarkRed;
            this.lblStatus.Location = new System.Drawing.Point(500, 13);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(692, 16);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // MultiClassRaceForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 790);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.pnlBottom);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "MultiClassRaceForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Multi-Class Event";
            this.pnlBottom.ResumeLayout(false);
            this.pnlBottom.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
