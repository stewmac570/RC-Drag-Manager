using System;
using System.Windows.Forms;

namespace RCDragManager
{
    partial class AddCarDialog
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblCarName, lblClassType, lblDialIn;
        private TextBox txtCarName, txtDialIn;
        private RadioButton rbHeadsUp, rbDial, rbIndex;
        private Button btnOK, btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Add Car";
            this.ClientSize = new System.Drawing.Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblCarName = new Label() { Text = "Car Name:", Location = new System.Drawing.Point(20, 20) };
            txtCarName = new TextBox() { Location = new System.Drawing.Point(120, 20), Width = 240 };

            lblClassType = new Label() { Text = "Class Type:", Location = new System.Drawing.Point(20, 60) };
            rbHeadsUp = new RadioButton() { Text = "Heads Up", Location = new System.Drawing.Point(120, 60) };
            rbDial = new RadioButton() { Text = "Dial", Location = new System.Drawing.Point(220, 60) };
            rbIndex = new RadioButton() { Text = "Index", Location = new System.Drawing.Point(300, 60) };

            lblDialIn = new Label() { Text = "Dial-In:", Location = new System.Drawing.Point(20, 100) };
            txtDialIn = new TextBox() { Location = new System.Drawing.Point(120, 100), Width = 100 };

            btnOK = new Button() { Text = "OK", Location = new System.Drawing.Point(80, 160) };
            btnCancel = new Button() { Text = "Cancel", Location = new System.Drawing.Point(220, 160), DialogResult = DialogResult.Cancel };

            btnOK.Click += new EventHandler(this.btnOK_Click);
            rbHeadsUp.CheckedChanged += new EventHandler(this.rbHeadsUp_CheckedChanged);
            rbDial.CheckedChanged += new EventHandler(this.rbDial_CheckedChanged);
            rbIndex.CheckedChanged += new EventHandler(this.rbIndex_CheckedChanged);

            this.Controls.AddRange(new Control[] {
                lblCarName, txtCarName,
                lblClassType, rbHeadsUp, rbDial, rbIndex,
                lblDialIn, txtDialIn,
                btnOK, btnCancel
            });
        }
    }
}
