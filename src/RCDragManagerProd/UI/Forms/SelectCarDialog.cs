using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RCDragManagerProd.Domain; // Assuming Car is defined in this namespace

namespace RCDragManagerProd.UI.Forms
{
    public partial class SelectCarDialog : Form
    {
        private List<Car> cars;
        public Car SelectedCar { get; private set; }

        public SelectCarDialog(List<Car> carList)
        {
            InitializeComponent();
            cars = carList;
            LoadCars();
        }

        private void LoadCars()
        {
            lstCars.Items.Clear();
            foreach (var car in cars)
            {
                string carInfo = $"{car.CarName} - {car.ClassType} - {car.DefaultDialIn?.ToString("0.000") ?? ""}";
                lstCars.Items.Add(carInfo);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (lstCars.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a car.");
                return;
            }

            SelectedCar = cars[lstCars.SelectedIndex];
            DialogResult = DialogResult.OK;
        }
    }
}
