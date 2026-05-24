using System;
using System.Drawing;
using System.Windows.Forms;

namespace RCDragManagerProd.UI.Forms
{
    partial class AddCarDialog
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblCarName;
        private Label lblClassType;
        private Label lblDialIn;

        private TextBox txtCarName;
        private TextBox txtDialIn;

        private RadioButton rbHeadsUp;
        private RadioButton rbDial;
        private RadioButton rbIndex;

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
            this.Text = "Add / Edit Car";
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new Size(450, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            lblCarName = new Label() { Text = "Car Name:", Location = new Point(20, 20), AutoSize = true };
            txtCarName = new TextBox() { Location = new Point(140, 20), Width = 250 };

            lblClassType = new Label() { Text = "Class Type:", Location = new Point(20, 60), AutoSize = true };
            rbHeadsUp = new RadioButton() { Text = "Heads Up", Location = new Point(140, 60), AutoSize = true, Checked = true };
            rbDial = new RadioButton() { Text = "Dial", Location = new Point(240, 60), AutoSize = true };
            rbIndex = new RadioButton() { Text = "Index", Location = new Point(320, 60), AutoSize = true };

            rbHeadsUp.CheckedChanged += new EventHandler(this.ClassTypeChanged);
            rbDial.CheckedChanged += new EventHandler(this.ClassTypeChanged);
            rbIndex.CheckedChanged += new EventHandler(this.ClassTypeChanged);

            lblDialIn = new Label() { Text = "Dial-In:", Location = new Point(20, 100), AutoSize = true };
            txtDialIn = new TextBox() { Location = new Point(140, 100), Width = 100, Enabled = false };

            btnOK = new Button() { Text = "OK", Location = new Point(100, 180), Size = new Size(100, 40) };
            btnCancel = new Button() { Text = "Cancel", Location = new Point(240, 180), Size = new Size(100, 40), DialogResult = DialogResult.Cancel };

            btnOK.Click += new EventHandler(this.btnOK_Click);

            this.Controls.AddRange(new Control[]
            {
                lblCarName, txtCarName,
                lblClassType, rbHeadsUp, rbDial, rbIndex,
                lblDialIn, txtDialIn,
                btnOK, btnCancel
            });
        }
    }
}
