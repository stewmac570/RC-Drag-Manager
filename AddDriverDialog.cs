using System;
using System.Windows.Forms;

namespace RCDragManagerProd
{
    partial class AddDriverDialog
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblName, lblQualTime;
        private TextBox txtName, txtQualTime;
        private Button btnOK, btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Add Driver";
            this.ClientSize = new System.Drawing.Size(400, 180);
            this.StartPosition = FormStartPosition.CenterParent;

            lblName = new Label() { Text = "Driver Name:", Location = new System.Drawing.Point(30, 30) };
            txtName = new TextBox() { Location = new System.Drawing.Point(150, 30), Width = 200 };

            lblQualTime = new Label() { Text = "Qualifying Time:", Location = new System.Drawing.Point(30, 70) };
            txtQualTime = new TextBox() { Location = new System.Drawing.Point(150, 70), Width = 200 };

            btnOK = new Button() { Text = "Save and Close", Location = new System.Drawing.Point(80, 120) };
            btnCancel = new Button() { Text = "Cancel", Location = new System.Drawing.Point(220, 120), DialogResult = DialogResult.Cancel };

            btnOK.Click += new EventHandler(this.btnOK_Click);

            this.Controls.AddRange(new Control[] {
                lblName, txtName,
                lblQualTime, txtQualTime,
                btnOK, btnCancel
            });
        }
    }
}
