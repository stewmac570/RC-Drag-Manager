namespace RCDragManagerProd.UI.Forms
{
    partial class BuybackDriverSelectionForm
    {
#pragma warning disable CS0169
        private System.ComponentModel.IContainer components;
#pragma warning restore CS0169
        private System.Windows.Forms.CheckedListBox checkedListBoxDrivers;
        private System.Windows.Forms.Button btnConfirm;
        private System.Windows.Forms.Button btnNoBuyback;

        private void InitializeComponent()
        {
            this.checkedListBoxDrivers = new System.Windows.Forms.CheckedListBox();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.btnNoBuyback = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // checkedListBoxDrivers
            // 
            this.checkedListBoxDrivers.CheckOnClick = true;
            this.checkedListBoxDrivers.FormattingEnabled = true;
            this.checkedListBoxDrivers.Location = new System.Drawing.Point(12, 12);
            this.checkedListBoxDrivers.Name = "checkedListBoxDrivers";
            this.checkedListBoxDrivers.Size = new System.Drawing.Size(260, 199);
            this.checkedListBoxDrivers.DisplayMember = "Name";
            this.checkedListBoxDrivers.ValueMember = "Id";
            // 
            // btnConfirm
            // 
            this.btnConfirm.Anchor = ((System.Windows.Forms.AnchorStyles)
                                              (System.Windows.Forms.AnchorStyles.Bottom |
                                               System.Windows.Forms.AnchorStyles.Right));
            this.btnConfirm.Location = new System.Drawing.Point(197, 225);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(75, 23);
            this.btnConfirm.Text = "Confirm Buybacks";
            // 
            // btnNoBuyback
            // 
            this.btnNoBuyback.Anchor = ((System.Windows.Forms.AnchorStyles)
                                                (System.Windows.Forms.AnchorStyles.Bottom |
                                                 System.Windows.Forms.AnchorStyles.Left));
            this.btnNoBuyback.Location = new System.Drawing.Point(12, 225);
            this.btnNoBuyback.Name = "btnNoBuyback";
            this.btnNoBuyback.Size = new System.Drawing.Size(100, 23);
            this.btnNoBuyback.Text = "No Buyback";
            this.btnNoBuyback.DialogResult = System.Windows.Forms.DialogResult.No;
            // 
            // BuybackDriverSelectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(284, 260);
            this.Controls.Add(this.btnNoBuyback);
            this.Controls.Add(this.checkedListBoxDrivers);
            this.Controls.Add(this.btnConfirm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BuybackDriverSelectionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Buyback Drivers";
            this.AcceptButton = this.btnConfirm;
            this.ResumeLayout(false);
        }
    }
}
