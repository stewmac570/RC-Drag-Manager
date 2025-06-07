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
        }

        public AddCarDialog(Car carToEdit)
        {
            InitializeComponent();

            txtCarName.Text = carToEdit.CarName;

            if (carToEdit.ClassType == "Heads Up")
                rbHeadsUp.Checked = true;
            else if (carToEdit.ClassType == "Dial")
                rbDial.Checked = true;
            else if (carToEdit.ClassType == "Index")
                rbIndex.Checked = true;

            if (carToEdit.DefaultDialIn.HasValue)
                txtDialIn.Text = carToEdit.DefaultDialIn.Value.ToString("0.000");

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

        private void btnOK_Click(object sender, EventArgs e)
        {
            string carName = txtCarName.Text.Trim();
            if (string.IsNullOrEmpty(carName))
            {
                MessageBox.Show("Please enter Car Name.");
                return;
            }

            string classType = rbHeadsUp.Checked ? "Heads Up" : rbDial.Checked ? "Dial" : "Index";

            double? dialIn = null;
            if ((rbDial.Checked || rbIndex.Checked) && !string.IsNullOrEmpty(txtDialIn.Text))
            {
                if (!double.TryParse(txtDialIn.Text.Trim(), out double parsedDial))
                {
                    MessageBox.Show("Please enter valid Dial-In.");
                    return;
                }
                dialIn = parsedDial;
            }

            NewCar = new Car
            {
                CarName = carName,
                ClassType = classType,
                DefaultDialIn = dialIn
            };

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
