using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.Config;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.WPF.Windows
{
    public partial class SettingsWindow : Window
    {
        private const string ProductionLiveViewUrl = "https://stewmacrc.com";

        private readonly ThemeManager.AppTheme _originalTheme;
        private bool _loaded;

        public SettingsWindow()
        {
            InitializeComponent();
            _originalTheme = ThemeManager.Current;
            RbDark.IsChecked = _originalTheme == ThemeManager.AppTheme.Dark;
            RbLight.IsChecked = _originalTheme == ThemeManager.AppTheme.Light;
            ChkLogging.IsChecked = AppSettings.EnableLogging;
            ChkLiveBroadcast.IsChecked = AppSettings.LiveBroadcastEnabled;
            ChkDebugLogging.IsChecked = AppSettings.LiveBroadcastDebugLogging;
            TxtLogPath.Text = AppSettings.LogFilePath;
            _loaded = true;
        }

        // Live preview while the dialog is open.
        private void Theme_Changed(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            ThemeManager.Apply(RbLight.IsChecked == true
                ? ThemeManager.AppTheme.Light : ThemeManager.AppTheme.Dark);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.EnableLogging = ChkLogging.IsChecked == true;
            AppSettings.LiveBroadcastEnabled = ChkLiveBroadcast.IsChecked == true;
            AppSettings.LiveBroadcastDebugLogging = ChkDebugLogging.IsChecked == true;
            AppSettings.Theme = RbLight.IsChecked == true ? "Light" : "Dark";
            if (AppSettings.EnableLogging) Logger.Log("[SETTINGS] Logging enabled.");
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Revert any live theme preview.
            ThemeManager.Apply(_originalTheme);
            DialogResult = false;
        }

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
                MessageBox.Show("Could not open live view.\n\n" + ex.Message, "Live view",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) BtnCancel_Click(sender, e);
        }
    }
}
