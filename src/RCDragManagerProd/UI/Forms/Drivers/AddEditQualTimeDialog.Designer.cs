using System.Windows.Forms;
using System.Drawing;

namespace RCDragManagerProd.UI.Forms
{
    partial class AddEditQualTimeDialog
    {
        private Label lblPrompt;
        private Label lblDriverName;
        private NumericUpDown numQualTime;
        private Button btnSave;
        private Button btnCancel;

        private void InitializeComponent()
        {
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Set Qualifying Time";

            lblPrompt = new Label
            {
                Text = "Driver:",
                Location = new Point(20, 20),
                Size = new Size(100, 30)
            };
            this.Controls.Add(lblPrompt);

            lblDriverName = new Label
            {
                Location = new Point(120, 20),
                Size = new Size(250, 30)
            };
            this.Controls.Add(lblDriverName);

            numQualTime = new NumericUpDown
            {
                Location = new Point(120, 70),
                Size = new Size(150, 30),
                DecimalPlaces = 3,
                Minimum = 0,
                Maximum = 100
            };
            this.Controls.Add(numQualTime);

            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(80, 130),
                Size = new Size(100, 30)
            };
            btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(200, 130),
                Size = new Size(100, 30)
            };
            btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            this.Controls.Add(btnCancel);
        }
    }
}
