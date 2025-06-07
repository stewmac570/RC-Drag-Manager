using System;
using System.Windows.Forms;

namespace RCDragManagerProd
{
    public partial class AddEditQualTimeDialog : Form
    {
        public double? QualifyingTime { get; private set; }

        public AddEditQualTimeDialog(string driverName, double? existingTime)
        {
            InitializeComponent();

            lblDriverName.Text = driverName;
            numQualTime.Value = existingTime.HasValue ? (decimal)existingTime.Value : 0;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            QualifyingTime = (double)numQualTime.Value;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
