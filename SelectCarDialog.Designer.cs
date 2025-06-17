namespace RCDragManagerProd
{
    partial class SelectCarDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox lstCars;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lstCars = new System.Windows.Forms.ListBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Size = new System.Drawing.Size(80, 30);


            this.SuspendLayout();

            // lstCars
            this.lstCars.Location = new System.Drawing.Point(20, 20);
            this.lstCars.Size = new System.Drawing.Size(300, 200);
            this.Controls.Add(this.lstCars);

            // btnOK
            this.btnOK.Text = "OK";
            this.btnOK.Location = new System.Drawing.Point(60, 240);
            this.btnOK.Size = new System.Drawing.Size(80, 30);
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            this.Controls.Add(this.btnOK);

            // btnCancel
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Location = new System.Drawing.Point(180, 240);
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            //this.btnCancel.DialogResult = DialogResult.Cancel;
            this.Controls.Add(this.btnCancel);

            // SelectCarDialog
            this.ClientSize = new System.Drawing.Size(350, 300);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Car";
            this.ResumeLayout(false);
        }
    }
}
