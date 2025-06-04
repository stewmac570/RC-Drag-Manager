using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RCDragManager
{
    public partial class EditWinnerDialog : Form
    {
        public Driver SelectedWinner { get; private set; }

        public EditWinnerDialog(Driver driver1, Driver driver2)
        {
            InitializeComponent();

            cmbWinner.Items.Add(driver1);
            cmbWinner.Items.Add(driver2);
            cmbWinner.DisplayMember = "Name";
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (cmbWinner.SelectedItem == null)
            {
                MessageBox.Show("Please select a winner.");
                return;
            }

            SelectedWinner = (Driver)cmbWinner.SelectedItem;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
