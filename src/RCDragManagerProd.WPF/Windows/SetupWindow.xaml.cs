using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.WPF.Dialogs;
using RCDragManagerProd.WPF.ViewModels;

namespace RCDragManagerProd.WPF.Windows
{
    public partial class SetupWindow : Window
    {
        private readonly SetupViewModel _vm;

        /// <summary>The created event, available to the caller after a successful Start.</summary>
        public MultiClassEvent CreatedEvent { get; private set; }

        public SetupWindow(string connectionString)
        {
            InitializeComponent();
            var service = new MultiClassSetupService(new DriverRepository(connectionString));
            var eventRepo = new MultiClassEventRepository(connectionString);
            _vm = new SetupViewModel(service, eventRepo);
            DataContext = _vm;
            Loaded += (_, __) => TxtEventName.Focus();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

        // ── Class list ───────────────────────────────────────────────────────

        private void BtnAddClass_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ClassConfigDialog(_vm.Service) { Owner = this };
            if (dlg.ShowDialog() != true) return;

            var error = _vm.AddClass(dlg.Result);
            if (error != null)
                MessageDialog.Warn(this, error, "Duplicate class");
        }

        private void BtnEditClass_Click(object sender, RoutedEventArgs e)
        {
            int idx = _vm.IndexOf(_vm.SelectedClass);
            var existing = _vm.GetConfig(idx);
            if (existing == null) return;

            var dlg = new ClassConfigDialog(_vm.Service, existing) { Owner = this };
            if (dlg.ShowDialog() != true) return;

            _vm.ReplaceClass(idx, dlg.Result);
        }

        private void BtnRemoveClass_Click(object sender, RoutedEventArgs e)
        {
            int idx = _vm.IndexOf(_vm.SelectedClass);
            if (idx < 0) return;
            _vm.RemoveClass(idx);
        }

        private void DgClasses_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_vm.SelectedClass != null)
                BtnEditClass_Click(sender, e);
        }

        // ── Start ────────────────────────────────────────────────────────────

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var error = _vm.ValidateCanStart();
            if (error != null)
            {
                MessageDialog.Warn(this, error, "Validation");
                return;
            }

            CreatedEvent = _vm.StartEvent();
            DialogResult = true;
        }
    }
}
