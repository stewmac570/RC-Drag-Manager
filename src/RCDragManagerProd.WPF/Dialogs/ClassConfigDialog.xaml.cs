using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
            WindowSizing.FitToScreen(this);
            _vm = new ClassConfigViewModel(service, existing);
            DataContext = _vm;

            TbarTitle.Text = _vm.IsEdit ? "Edit class" : "Add class";
            BtnOk.Content = _vm.IsEdit ? "Save class" : "Add class";

            _vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ClassConfigViewModel.OverrideColumnVisibility))
                    SyncOverrideColumn();
            };
            SyncOverrideColumn();

            Loaded += (_, __) => TxtClassName.Focus();
        }

        private void SyncOverrideColumn() =>
            ColOverride.Visibility = _vm.OverrideColumnVisibility;

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

        // ── Race type cards ──────────────────────────────────────────────────

        private void CardProLadder_Click(object sender, RoutedEventArgs e) =>
            _vm.SelectedRaceType = ClassConfigViewModel.ProLadder;

        private void CardRandomDraw_Click(object sender, RoutedEventArgs e) =>
            _vm.SelectedRaceType = ClassConfigViewModel.RandomDraw;

        private void CardRoundRobin_Click(object sender, RoutedEventArgs e) =>
            _vm.SelectedRaceType = ClassConfigViewModel.RoundRobin;

        // ── Click a row to move a driver in or out of the class ──────────────

        private void DgRoster_RowClick(object sender, MouseButtonEventArgs e) =>
            _vm.Include(RowUnder(e.OriginalSource as DependencyObject));

        private void DgSelected_RowClick(object sender, MouseButtonEventArgs e)
        {
            var d = e.OriginalSource as DependencyObject;

            // The Override cell is editable, so a click there must edit it rather
            // than pull the driver back out of the class.
            if (FindParentCell(d) is DataGridCell cell &&
                cell.Column?.Header is string header &&
                header == "Override")
                return;

            _vm.Exclude(RowUnder(d));
        }

        /// <summary>The roster row under a clicked element, or null for header/empty space.</summary>
        private static DriverRosterRow RowUnder(DependencyObject d)
        {
            while (d != null && !(d is DataGridRow))
                d = VisualTreeHelper.GetParent(d);
            return (d as DataGridRow)?.Item as DriverRosterRow;
        }

        private static DataGridCell FindParentCell(DependencyObject d)
        {
            while (d != null && !(d is DataGridCell))
                d = VisualTreeHelper.GetParent(d);
            return d as DataGridCell;
        }

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
                MessageDialog.Warn(this, error, "Validation");
                return;
            }
            Result = result;
            DialogResult = true;
        }
    }
}
