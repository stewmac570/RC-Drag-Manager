using System.Windows;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.WPF.Views;

namespace RCDragManagerProd.WPF.Windows
{
    /// <summary>Standalone host for a single-class <see cref="RaceConsoleView"/>.</summary>
    public partial class RaceConsoleWindow : Window
    {
        private readonly RaceConsoleView _view;

        public RaceConsoleWindow(RaceController controller, string connectionString)
        {
            InitializeComponent();
            WindowSizing.FitToScreen(this);
            _view = new RaceConsoleView(controller, connectionString);
            ViewHost.Child = _view;
            Closed += (_, __) => _view.Teardown();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
