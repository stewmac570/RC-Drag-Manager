using System;
using System.Windows.Forms;

namespace RCDragManagerProd
{
    public partial class EditDriverDialog : Form
    {
        public string DriverName { get; private set; }
        public string State { get; private set; }

        public EditDriverDialog(string currentName, string currentState)
        {
            InitializeComponent();

            txtDriverName.Text = currentName;
            if (!string.IsNullOrEmpty(currentState))
                cbState.SelectedItem = currentState;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDriverName.Text))
            {
                MessageBox.Show("Driver name cannot be blank.");
                return;
            }

            DriverName = txtDriverName.Text.Trim();
            State = cbState.SelectedItem?.ToString() ?? "";
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
