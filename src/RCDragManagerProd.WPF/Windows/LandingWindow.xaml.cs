using System;
using System.Linq;
using System.Windows;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.WPF.ViewModels;

namespace RCDragManagerProd.WPF.Windows
{
    public partial class LandingWindow : Window
    {
        private readonly string _connectionString;
        private readonly LandingViewModel _vm;

        public LandingWindow(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            InitializeComponent();

            var driverRepo = new DriverRepository(connectionString);
            var sessionRepo = new RaceSessionRepository(connectionString);
            var multiClassRepo = new MultiClassEventRepository(connectionString);
            var loadService = new LoadSessionService(sessionRepo, multiClassRepo);

            _vm = new LandingViewModel(loadService, driverRepo);
            DataContext = _vm;
            _vm.Load();
        }

        // ── Title bar controls ───────────────────────────────────────────────

        private void BtnClose_Click(object sender, RoutedEventArgs e) =>
            Application.Current.Shutdown();

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        // ── Primary action buttons ───────────────────────────────────────────

        private void BtnStartNewEvent_Click(object sender, RoutedEventArgs e)
        {
            var setup = new SetupWindow(_connectionString) { Owner = this };
            if (setup.ShowDialog() == true && setup.CreatedEvent != null)
            {
                OpenConsole(setup.CreatedEvent, restore: false);
            }
            _vm.Load();
        }

        // Temporary single-class launch: opens the race console on the event's first
        // class session. The multi-class tab wrapper replaces this in the next screen.
        private void OpenConsole(RCDragManagerProd.Domain.MultiClassEvent evt, bool restore)
        {
            var session = evt?.ClassSessions?.FirstOrDefault();
            if (session == null)
            {
                MessageBox.Show("This event has no class sessions to race.", "RC Drag Manager",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var controller = new RCDragManagerProd.Controllers.RaceController(session);
            var console = new RaceConsoleWindow(controller, _connectionString) { Owner = this };
            console.Show();
            if (restore)
            {
                try { controller.RestoreFromSave(); } catch { }
            }
        }

        private void BtnLoadSaved_Click(object sender, RoutedEventArgs e)
        {
            var load = new LoadSessionWindow(_connectionString) { Owner = this };
            if (load.ShowDialog() == true && load.ResumedEvent != null)
            {
                OpenConsole(load.ResumedEvent, restore: true);
            }
            _vm.Load();
        }

        private void EventCard_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is FrameworkElement el && el.Tag is RecentEventRow row)
            {
                MessageBox.Show($"Resume '{row.EventName}' — coming soon.", "RC Drag Manager",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ── Sidebar tools ────────────────────────────────────────────────────

        private void BtnDriverManager_Click(object sender, RoutedEventArgs e)
        {
            new DriverManagerWindow(_connectionString) { Owner = this }.ShowDialog();
            _vm.Load();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Settings — coming soon.", "RC Drag Manager",
                MessageBoxButton.OK, MessageBoxImage.Information);

        private void BtnLiveScoreboard_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Live scoreboard — coming soon.", "RC Drag Manager",
                MessageBoxButton.OK, MessageBoxImage.Information);

        private void BtnExit_Click(object sender, RoutedEventArgs e) =>
            Application.Current.Shutdown();
    }
}
