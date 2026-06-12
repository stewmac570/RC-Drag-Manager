using System.Windows;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.WPF.ViewModels;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class ClassConfigDialog : Window
    {
        private readonly ClassConfigViewModel _vm;

        /// <summary>The configured class, valid after a successful OK.</summary>
        public ClassConfigDto Result { get; private set; }

        public ClassConfigDialog(MultiClassSetupService service, ClassConfigDto existing = null)
        {
            InitializeComponent();
            _vm = new ClassConfigViewModel(service, existing);
            DataContext = _vm;

            TbarTitle.Text = _vm.IsEdit ? "Edit class" : "Add class";
            BtnOk.Content = _vm.IsEdit ? "Save class" : "Add class";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

        // ── Race type cards ──────────────────────────────────────────────────

        private void CardProLadder_Click(object sender, RoutedEventArgs e) =>
            _vm.SelectedRaceType = ClassConfigViewModel.ProLadder;

        private void CardRandomDraw_Click(object sender, RoutedEventArgs e) =>
            _vm.SelectedRaceType = ClassConfigViewModel.RandomDraw;

        private void CardRoundRobin_Click(object sender, RoutedEventArgs e) =>
            _vm.SelectedRaceType = ClassConfigViewModel.RoundRobin;

        // ── Add new driver ───────────────────────────────────────────────────

        private void BtnAddNewDriver_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new QuickAddDriverDialog { Owner = this };
            if (dlg.ShowDialog() != true) return;

            var car = new Car
            {
                CarName = dlg.CarName,
                ClassType = dlg.ClassType,
                DefaultDialIn = dlg.DialIn
            };
            _vm.QuickAddDriver(dlg.DriverName, car);
        }

        // ── OK ───────────────────────────────────────────────────────────────

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            var result = _vm.BuildResult(out var error);
            if (result == null)
            {
                MessageBox.Show(error, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Result = result;
            DialogResult = true;
        }
    }
}
