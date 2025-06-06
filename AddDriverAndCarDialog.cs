using System;
using System.Reflection;
using System.Windows.Forms;

namespace RCDragManager
{
    public partial class AddDriverAndCarDialog : Form
    {
        public string DriverName { get; private set; }
        public string CarName { get; private set; }
        public string ClassType { get; private set; }
        public double? DialIn { get; private set; }

        public AddDriverAndCarDialog()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string driverName = txtDriverName.Text.Trim();
            string carName = txtCarName.Text.Trim();

            if (string.IsNullOrEmpty(driverName) || string.IsNullOrEmpty(carName))
            {
                MessageBox.Show("Please enter both Driver Name and Car Name.");
                return;
            }

            double? dialIn = null;
            if ((rbDial.Checked || rbIndex.Checked) && !string.IsNullOrEmpty(txtDialIn.Text))
            {
                if (!double.TryParse(txtDialIn.Text.Trim(), out double parsedDial))
                {
                    MessageBox.Show("Please enter a valid Dial-In.");
                    return;
                }
                dialIn = parsedDial;
            }

            DriverName = driverName;
            CarName = carName;
            ClassType = rbHeadsUp.Checked ? "Heads Up" : rbDial.Checked ? "Dial" : "Index";
            DialIn = dialIn;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ClassTypeChanged(object sender, EventArgs e)
        {
            if (rbHeadsUp.Checked)
            {
                txtDialIn.Enabled = false;
                txtDialIn.Text = "";
            }
            else
            {
                txtDialIn.Enabled = true;
            }
        }
    }
}
