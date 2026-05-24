using System;
using System.Windows.Forms;
using System.Drawing;

namespace RCDragManagerProd.UI.Forms
{
    partial class AddDriverAndCarDialog
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblDriverName, lblCarName, lblClassType, lblDialIn;
        private TextBox txtDriverName, txtCarName, txtDialIn;
        private RadioButton rbHeadsUp, rbDial, rbIndex;
        private Button btnOK, btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Add Driver and Car";
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new Size(450, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            lblDriverName = new Label() { Text = "Driver Name:", Location = new Point(20, 20), AutoSize = true };
            txtDriverName = new TextBox() { Location = new Point(140, 20), Width = 250 };

            lblCarName = new Label() { Text = "Car Name:", Location = new Point(20, 60), AutoSize = true };
            txtCarName = new TextBox() { Location = new Point(140, 60), Width = 250 };

            lblClassType = new Label() { Text = "Class Type:", Location = new Point(20, 100), AutoSize = true };
            rbHeadsUp = new RadioButton() { Text = "Heads Up", Location = new Point(140, 100), AutoSize = true, Checked = true };
            rbDial = new RadioButton() { Text = "Dial", Location = new Point(240, 100), AutoSize = true };
            rbIndex = new RadioButton() { Text = "Index", Location = new Point(320, 100), AutoSize = true };

            lblDialIn = new Label() { Text = "Dial-In:", Location = new Point(20, 140), AutoSize = true };
            txtDialIn = new TextBox() { Location = new Point(140, 140), Width = 100, Enabled = false };

            btnOK = new Button() { Text = "OK", Location = new Point(100, 200), Size = new Size(100, 40) };
            btnCancel = new Button() { Text = "Cancel", Location = new Point(240, 200), Size = new Size(100, 40), DialogResult = DialogResult.Cancel };

            // Wire events
            btnOK.Click += new EventHandler(this.btnOK_Click);
            rbHeadsUp.CheckedChanged += new EventHandler(this.ClassTypeChanged);
            rbDial.CheckedChanged += new EventHandler(this.ClassTypeChanged);
            rbIndex.CheckedChanged += new EventHandler(this.ClassTypeChanged);

            // Add controls
            this.Controls.AddRange(new Control[] {
                lblDriverName, txtDriverName,
                lblCarName, txtCarName,
                lblClassType, rbHeadsUp, rbDial, rbIndex,
                lblDialIn, txtDialIn,
                btnOK, btnCancel
            });
        }
    }
}
