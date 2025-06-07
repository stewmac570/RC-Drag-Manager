using System;
using System.Windows.Forms;
using System.Drawing;

namespace RCDragManagerProd
{
    partial class EditDriverDialog
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblDriverName;
        private TextBox txtDriverName;
        private Label lblState;
        private ComboBox cbState;
        private Button btnOK;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Edit Driver";
            this.ClientSize = new Size(450, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            lblDriverName = new Label() { Text = "Driver Name:", Location = new Point(20, 20), AutoSize = true };
            txtDriverName = new TextBox() { Location = new Point(120, 20), Width = 280 };

            lblState = new Label() { Text = "State:", Location = new Point(20, 60), AutoSize = true };
            cbState = new ComboBox() { Location = new Point(120, 60), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };

            cbState.Items.AddRange(new string[]
            {
                "", "NSW", "QLD", "VIC", "WA", "SA", "TAS", "NT", "ACT",
            });

            btnOK = new Button() { Text = "OK", Location = new Point(100, 150), Size = new Size(100, 40) };
            btnCancel = new Button() { Text = "Cancel", Location = new Point(240, 150), Size = new Size(100, 40), DialogResult = DialogResult.Cancel };

            btnOK.Click += new EventHandler(this.btnOK_Click);

            this.Controls.AddRange(new Control[] {
                lblDriverName, txtDriverName,
                lblState, cbState,
                btnOK, btnCancel
            });
        }
    }
}
