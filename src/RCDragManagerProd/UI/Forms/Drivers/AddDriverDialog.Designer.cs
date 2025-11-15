using System;
using System.Drawing;
using System.Windows.Forms;

namespace RCDragManagerProd.UI.Forms
{
    partial class AddDriverDialog
    {
        private Label lblName;
        private Label lblQualTime;
        private TextBox txtName;
        private TextBox txtQualTime;
        private Button btnOK;
        private Button btnCancel;

        private void InitializeComponent()
        {
            this.Text = "Add Driver";
            this.ClientSize = new Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            lblName = new Label { Text = "Driver Name:", Location = new Point(20, 20), AutoSize = true };
            txtName = new TextBox { Location = new Point(140, 20), Width = 200 };

            lblQualTime = new Label { Text = "Qualifying Time:", Location = new Point(20, 60), AutoSize = true };
            txtQualTime = new TextBox { Location = new Point(140, 60), Width = 100 };

            btnOK = new Button { Text = "OK", Location = new Point(80, 120), Size = new Size(100, 35) };
            btnCancel = new Button { Text = "Cancel", Location = new Point(200, 120), Size = new Size(100, 35), DialogResult = DialogResult.Cancel };

            btnOK.Click += new EventHandler(this.btnOK_Click);

            this.Controls.AddRange(new Control[]
            {
                lblName, txtName,
                lblQualTime, txtQualTime,
                btnOK, btnCancel
            });
        }
    }
}
