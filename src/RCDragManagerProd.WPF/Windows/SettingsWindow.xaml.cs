using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.Config;
using RCDragManagerProd.Logging;
using RCDragManagerProd.WPF.Dialogs;

namespace RCDragManagerProd.WPF.Windows
{
    public partial class SettingsWindow : Window
    {
        private const string ProductionLiveViewUrl = "https://stewmacrc.com";

        private string _originalTheme;

        public SettingsWindow()
        {
            InitializeComponent();
            WindowSizing.RoundCorners(this);
            _originalTheme = AppSettings.Theme;
            RbDark.IsChecked = !string.Equals(_originalTheme, "Light", StringComparison.OrdinalIgnoreCase);
            RbLight.IsChecked = string.Equals(_originalTheme, "Light", StringComparison.OrdinalIgnoreCase);
            ChkLogging.IsChecked = AppSettings.EnableLogging;
            ChkLiveBroadcast.IsChecked = AppSettings.LiveBroadcastEnabled;
            ChkDebugLogging.IsChecked = AppSettings.LiveBroadcastDebugLogging;
            TxtApiKey.Text = AppSettings.ApiKey;
            TxtLogPath.Text = AppSettings.LogFilePath;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.EnableLogging = ChkLogging.IsChecked == true;
            AppSettings.LiveBroadcastEnabled = ChkLiveBroadcast.IsChecked == true;
            AppSettings.LiveBroadcastDebugLogging = ChkDebugLogging.IsChecked == true;
            AppSettings.ApiKey = TxtApiKey.Text;
            if (AppSettings.EnableLogging) Logger.Log("[SETTINGS] Logging enabled.");

            var newTheme = RbLight.IsChecked == true ? "Light" : "Dark";
            bool themeChanged = !string.Equals(_originalTheme, newTheme, StringComparison.OrdinalIgnoreCase);
            AppSettings.Theme = newTheme;

            DialogResult = true;

            // The theme is applied cleanly at startup, so a change takes effect on a
            // quick restart — avoids any partially-repainted live-switch state.
            if (themeChanged) RestartApp();
        }

        private static void RestartApp()
        {
            try
            {
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exe)) Process.Start(exe);
            }
            catch (Exception ex) { Logger.Log("[SETTINGS][RESTART] " + ex.Message); }
            Application.Current.Shutdown();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void BtnOpenLiveView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = ProductionLiveViewUrl, UseShellExecute = true });
                Logger.Log("[LIVE][OPEN] " + ProductionLiveViewUrl);
            }
            catch (Exception ex)
            {
                Logger.Log("[LIVE][FAIL] Open live view failed. " + ex.Message);
                MessageDialog.Error(this, "Could not open live view.\n\n" + ex.Message, "Live view");
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) BtnCancel_Click(sender, e);
        }
    }
}
