using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RCDragManagerProd
{
    public partial class BuybackDriverSelectionForm : Form
    {
        public List<Driver> SelectedDrivers { get; private set; } = new List<Driver>();

        public BuybackDriverSelectionForm(List<Driver> drivers)
        {
            InitializeComponent();

            // populate list
            foreach (var d in drivers)
                checkedListBoxDrivers.Items.Add(d, false);

            btnConfirm.Click += BtnConfirm_Click;
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            SelectedDrivers = checkedListBoxDrivers
                .CheckedItems
                .Cast<Driver>()
                .ToList();

            if (!SelectedDrivers.Any())
            {
                MessageBox.Show("Select at least one buyback driver.",
                                "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
