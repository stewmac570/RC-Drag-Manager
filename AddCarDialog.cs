using System;
using System.Windows.Forms;

namespace RCDragManager
{
    public partial class AddCarDialog : Form
    {
        public Car NewCar { get; private set; }

        public AddCarDialog()
        {
            InitializeComponent();
            rbHeadsUp.Checked = true;
            UpdateDialInEnabled();
        }

        public AddCarDialog(Car existingCar) : this()
        {
            txtCarName.Text = existingCar.CarName;
            switch (existingCar.ClassType)
            {
                case "Heads Up": rbHeadsUp.Checked = true; break;
                case "Dial": rbDial.Checked = true; break;
                case "Index": rbIndex.Checked = true; break;
            }
            txtDialIn.Text = existingCar.DefaultDialIn.HasValue ? existingCar.DefaultDialIn.Value.ToString("0.000") : "";
            UpdateDialInEnabled();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            string carName = txtCarName.Text.Trim();
            string classType = rbHeadsUp.Checked ? "Heads Up" : (rbDial.Checked ? "Dial" : "Index");

            double? dialIn = null;
            if (classType != "Heads Up")
            {
                if (!double.TryParse(txtDialIn.Text, out double parsed))
                {
                    MessageBox.Show("Enter valid Dial-In.");
                    return;
                }
                dialIn = parsed;
            }

            if (string.IsNullOrEmpty(carName))
            {
                MessageBox.Show("Car Name is required.");
                return;
            }

            NewCar = new Car
            {
                CarName = carName,
                ClassType = classType,
                DefaultDialIn = dialIn
            };

            DialogResult = DialogResult.OK;
        }

        private void UpdateDialInEnabled()
        {
            txtDialIn.Enabled = !rbHeadsUp.Checked;
        }

        private void rbHeadsUp_CheckedChanged(object sender, EventArgs e) => UpdateDialInEnabled();
        private void rbDial_CheckedChanged(object sender, EventArgs e) => UpdateDialInEnabled();
        private void rbIndex_CheckedChanged(object sender, EventArgs e) => UpdateDialInEnabled();
    }
}
