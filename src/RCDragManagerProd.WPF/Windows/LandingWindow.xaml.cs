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

        // Opens the multi-class race window (one console tab per class). The window
        // replays saved bracket state itself on load, so 'restore' is implicit.
        private void OpenConsole(RCDragManagerProd.Domain.MultiClassEvent evt, bool restore)
        {
            if (evt?.ClassSessions == null || evt.ClassSessions.Count == 0)
            {
                MessageBox.Show("This event has no class sessions to race.", "RC Drag Manager",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            new MultiClassRaceWindow(evt, _connectionString).Show();
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

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow { Owner = this }.ShowDialog();
            _vm.Load();
        }

        private void BtnLiveScoreboard_Click(object sender, RoutedEventArgs e) =>
            new LiveScoreboardWindow { Owner = this }.ShowDialog();

        private void BtnExit_Click(object sender, RoutedEventArgs e) =>
            Application.Current.Shutdown();
    }
}
