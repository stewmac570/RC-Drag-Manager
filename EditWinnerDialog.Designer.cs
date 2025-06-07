namespace RCDragManagerProd
{
    partial class EditWinnerDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ComboBox cmbWinner;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.cmbWinner = new System.Windows.Forms.ComboBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cmbWinner
            // 
            this.cmbWinner.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWinner.FormattingEnabled = true;
            this.cmbWinner.Location = new System.Drawing.Point(25, 20);
            this.cmbWinner.Name = "cmbWinner";
            this.cmbWinner.Size = new System.Drawing.Size(250, 21);
            this.cmbWinner.TabIndex = 0;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(40, 60);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 30);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(180, 60);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // EditWinnerDialog
            // 
            this.ClientSize = new System.Drawing.Size(300, 110);
            this.Controls.Add(this.cmbWinner);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Name = "EditWinnerDialog";
            this.Text = "Edit Match Result";
            this.ResumeLayout(false);
        }
    }
}
