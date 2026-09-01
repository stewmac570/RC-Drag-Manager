using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.WPF.ViewModels;

namespace RCDragManagerProd.WPF.Dialogs
{
    /// <summary>
    /// Transactional editor for one race's driver roster, opened from the console
    /// before the first result. Two panes, the same shape as the class driver picker
    /// (#419): the driver database on the left, this race on the right, one click to
    /// move a driver either way. "New driver" covers a late entry who is not in the
    /// database yet.
    ///
    /// The dialog holds no roster rules — <see cref="RaceRosterViewModel"/> and
    /// <see cref="RaceRosterService"/> own validation, the database and the working copy.
    /// </summary>
    public partial class RaceRosterDialog : Window
    {
        private readonly RaceRosterViewModel _vm;

        /// <summary>The edited roster, valid after a successful save.</summary>
        public List<Driver> Drivers => _vm.Roster;

        public RaceRosterDialog(RaceRosterService service, IEnumerable<Driver> drivers, string className = null)
        {
            InitializeComponent();
            WindowSizing.FitToScreen(this);

            _vm = new RaceRosterViewModel(service, drivers);
            DataContext = _vm;

            if (!string.IsNullOrWhiteSpace(className))
                LblClass.Text = $"Edit race roster — {className}";

            Loaded += (_, __) => TxtSearch.Focus();
        }

        // ── Click a row to move a driver in or out of the race ────────────────

        private void DgAvailable_RowClick(object sender, MouseButtonEventArgs e) =>
            _vm.Add(RowUnder<RaceRosterAvailableRow>(e.OriginalSource as DependencyObject));

        private void DgEntered_RowClick(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            var row = RowUnder<RaceRosterEntryRow>(source);
            if (row == null) return;

            // The pencil cell renames; anywhere else on the row takes the driver out.
            if (FindParent<DataGridCell>(source)?.Column == ColRename)
            {
                Rename(row);
                return;
            }

            _vm.Remove(row);
        }

        private void Rename(RaceRosterEntryRow row)
        {
            var dlg = new AddEditDriverDialog(row.Name, row.State) { Owner = this };
            if (dlg.ShowDialog() != true) return;

            var error = _vm.Rename(row, dlg.DriverName, dlg.State);
            if (error != null) MessageDialog.Warn(this, error, "Rename driver");
        }

        // ── New driver ───────────────────────────────────────────────────────

        private void BtnNewDriver_Click(object sender, RoutedEventArgs e)
        {
            // Stays open for bulk entry, so each name commits through this callback
            // rather than on close (#417).
            new AddDriverDialog(_vm.CreateAndAdd) { Owner = this }.ShowDialog();
        }

        // ── Close ────────────────────────────────────────────────────────────

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.TrySave()) return;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void BtnClose_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        // ── Visual tree helpers ──────────────────────────────────────────────

        /// <summary>The row item under a clicked element, or null for header/empty space.</summary>
        private static T RowUnder<T>(DependencyObject d) where T : class =>
            FindParent<DataGridRow>(d)?.Item as T;

        private static T FindParent<T>(DependencyObject d) where T : DependencyObject
        {
            while (d != null && !(d is T))
                d = VisualTreeHelper.GetParent(d);
            return d as T;
        }
    }
}
