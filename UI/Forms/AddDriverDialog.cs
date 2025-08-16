using System;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RCDragManagerProd.UI.Forms
{
    public partial class AddDriverDialog : Form
    {
        public string DriverName { get; set; }
        public double QualTime { get; set; }

        public AddDriverDialog()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a driver name.");
                return;
            }

            if (!double.TryParse(txtQualTime.Text.Trim(), out double qualTime))
            {
                MessageBox.Show("Please enter a valid qualifying time.");
                return;
            }

            DriverName = name;
            QualTime = qualTime;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
