using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.WPF.Dialogs;
using RCDragManagerProd.WPF.ViewModels;

namespace RCDragManagerProd.WPF.Windows
{
    public partial class DriverManagerWindow : Window
    {
        private readonly DriverManagerViewModel _vm;
        private readonly string _connectionString;

        public DriverManagerWindow(string connectionString)
        {
            _connectionString = connectionString;
            InitializeComponent();
            _vm = new DriverManagerViewModel(new DriverRepository(connectionString));
            DataContext = _vm;
            _vm.Load();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        // ── Search ───────────────────────────────────────────────────────────

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) =>
            _vm.Filter = TxtSearch.Text;

        // ── Driver actions ───────────────────────────────────────────────────

        private void BtnAddDriver_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddEditDriverDialog { Owner = this };
            if (dlg.ShowDialog() != true) return;

            var car = new Car { CarName = "Default", ClassType = "", DefaultDialIn = null };
            _vm.AddDriverWithCar(dlg.DriverName, car);
        }

        private void BtnEditDriver_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedDriver == null) return;
            var dlg = new AddEditDriverDialog(_vm.SelectedDriver.Name, _vm.SelectedDriver.State)
                      { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _vm.UpdateDriver(dlg.DriverName, dlg.State);
        }

        private void BtnDeleteDriver_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedDriver == null) return;
            var result = MessageBox.Show(
                $"Delete '{_vm.SelectedDriver.Name}'? This cannot be undone.",
                "Delete driver", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            _vm.DeleteDriver();
        }

        private void BtnSetQualTime_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedDriver == null) return;
            var fresh = _vm.GetSelectedDriverFresh();
            var dlg = new SetQualTimeDialog(fresh.Name, fresh.QualTime) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _vm.SetQualTime(dlg.QualTime);
        }

        private void BtnStats_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedDriver == null) return;
            var fresh = _vm.GetSelectedDriverFresh();
            var statsWin = new DriverStatsWindow(fresh, _connectionString) { Owner = this };
            statsWin.ShowDialog();
        }

        private void DgDrivers_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_vm.SelectedDriver != null)
                BtnEditDriver_Click(sender, e);
        }

        // ── Car actions ──────────────────────────────────────────────────────

        private void BtnAddCar_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedDriver == null) return;
            var dlg = new AddEditCarDialog { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _vm.AddCar(dlg.Result);
        }

        private void BtnEditCar_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedCar == null) return;
            var dlg = new AddEditCarDialog(_vm.SelectedCar) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            _vm.EditCar(_vm.SelectedCar.CarID, dlg.Result);
        }

        private void BtnDeleteCar_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedCar == null) return;
            var result = MessageBox.Show(
                $"Delete car '{_vm.SelectedCar.CarName}'?",
                "Delete car", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            _vm.DeleteCar();
        }

        private void DgCars_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_vm.SelectedCar != null)
                BtnEditCar_Click(sender, e);
        }
    }
}
