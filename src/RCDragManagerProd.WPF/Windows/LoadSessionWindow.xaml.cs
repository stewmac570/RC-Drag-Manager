using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.WPF.ViewModels;

namespace RCDragManagerProd.WPF.Windows
{
    public partial class LoadSessionWindow : Window
    {
        private readonly LoadSessionViewModel _vm;

        /// <summary>The event chosen to resume, valid after a successful load (DialogResult true).</summary>
        public MultiClassEvent ResumedEvent { get; private set; }

        public LoadSessionWindow(string connectionString)
        {
            InitializeComponent();
            var service = new LoadSessionService(
                new RaceSessionRepository(connectionString),
                new MultiClassEventRepository(connectionString));
            _vm = new LoadSessionViewModel(service);
            DataContext = _vm;
            _vm.Load();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.Selected == null) return;

            var result = _vm.LoadSelected();
            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage, "Load error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ResumedEvent = result.Event;
            DialogResult = true;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.Selected == null) return;
            var confirm = MessageBox.Show(
                $"Permanently delete '{_vm.Selected.EventName}'? This cannot be undone.",
                "Delete event", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            _vm.DeleteSelected();
        }

        private void DgEvents_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_vm.Selected != null)
                BtnLoad_Click(sender, e);
        }
    }
}
