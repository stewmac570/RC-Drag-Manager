using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.WPF.ViewModels;
using RCDragManagerProd.WPF.Views;

namespace RCDragManagerProd.WPF.Windows
{
    /// <summary>Standalone host for a single-class <see cref="RaceConsoleView"/>.</summary>
    public partial class RaceConsoleWindow : Window
    {
        private readonly RaceConsoleView _view;
        private readonly RaceController _controller;
        private readonly RaceSession _session;
        private EventSettingsView _settingsView;

        public RaceConsoleWindow(RaceController controller, string connectionString)
        {
            InitializeComponent();
            WindowSizing.FitToScreen(this);

            _controller = controller;
            _session = controller?.Session;

            _view = new RaceConsoleView(controller, connectionString);
            BuildTabs();
            Closed += (_, __) => _view.Teardown();

            // Show the event on the live site as soon as the console opens, even before
            // a bracket is generated (mirrors the multi-class window behaviour).
            Loaded += (_, __) =>
            {
                try { controller.BroadcastLiveSnapshot("EventStarted"); }
                catch { /* live broadcast is best-effort */ }
            };
        }

        // ── Tabs: settings first, then the class console (#415) ───────────────

        private void BuildTabs()
        {
            _settingsView = new EventSettingsView
            {
                ResetClass = ResetClass,
                SetBuybacks = SetBuybacks,
                RequestRefresh = RefreshSettingsTab
            };

            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock
            {
                Text = "",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 12,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            header.Children.Add(new TextBlock
            {
                Text = "Event settings",
                VerticalAlignment = VerticalAlignment.Center
            });

            Tabs.Items.Add(new TabItem { Header = header, Content = _settingsView });
            Tabs.Items.Add(new TabItem
            {
                Header = new TextBlock { Text = _session?.ClassType ?? "Class" },
                Content = _view
            });

            RefreshSettingsTab();
            Tabs.SelectedIndex = 1;   // land on the class; settings is just to hand
            Tabs.SelectionChanged += (_, e) =>
            {
                if (e.OriginalSource == Tabs && Tabs.SelectedIndex == 0) RefreshSettingsTab();
            };
        }

        private void RefreshSettingsTab()
        {
            if (_settingsView == null || _session == null) return;

            _settingsView.SetHeader(_session.EventName, $"{_session.EventDate:ddd d MMM yyyy} · 1 class");

            _settingsView.SetClasses(new List<EventSettingsRow>
            {
                EventSettingsRowBuilder.Build(
                    0, _session, "Class",
                    complete: _controller.IsCompleted,
                    roundRobinComplete: _controller.IsRrComplete(),
                    bracketStarted: _controller.HasBracketStarted)
            });
        }

        private string ResetClass(int index)
        {
            var check = EventSettingsService.CanResetClass(_controller.IsCompleted);
            if (!check.IsAllowed) return check.Reason;

            _view.ResetClass();
            return null;
        }

        private string SetBuybacks(int index, bool enabled)
        {
            if (_session == null) return "This class has no saved settings to change.";

            bool currentlyOn = EventSettingsService.BuybacksEnabledIn(_session.RoundRobinVariant);
            if (currentlyOn == enabled) return null;

            var check = EventSettingsService.CanChangeBuybacks(
                _session.RaceType,
                _controller.IsCompleted,
                _controller.IsRrComplete(),
                (_session.BuybackDrivers?.Count ?? 0) > 0,
                turningOff: !enabled,
                roundsToRun: _session.RoundsToRun);
            if (!check.IsAllowed) return check.Reason;

            _session.RoundRobinVariant = EventSettingsService.VariantFor(enabled);
            _view.SaveProgressQuiet();
            return null;
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
