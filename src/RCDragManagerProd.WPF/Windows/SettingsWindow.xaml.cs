using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using RCDragManagerProd.AppServices;
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

        // ── Backups (issue #405) ──────────────────────────────────────────────

        private DatabaseBackupService BackupService() =>
            new DatabaseBackupService(App.DatabasePath ??
                throw new InvalidOperationException("Database path is not available."));

        /// <summary>Copies the database to a folder the operator picks (USB stick
        /// friendly). The view only runs the dialogs; the copy lives in the service.</summary>
        private void BtnBackupNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Title = "Back up database",
                    FileName = $"race_data_{DateTime.Now:yyyy-MM-dd}.db",
                    Filter = "SQLite database (*.db)|*.db"
                };
                if (dlg.ShowDialog(this) != true) return;

                var written = BackupService().BackupTo(dlg.FileName);
                if (written == null)
                {
                    MessageDialog.Error(this, "There is no database file to back up yet.",
                        "Back up database");
                    return;
                }

                MessageDialog.Info(this, $"Database backed up to:\n\n{written}", "Backup complete");
            }
            catch (Exception ex)
            {
                Logger.Log("[SETTINGS][BACKUP] " + ex.Message);
                MessageDialog.Error(this, "Could not back up the database.\n\n" + ex.Message,
                    "Back up database");
            }
        }

        /// <summary>Replaces the live database with a validated backup. The current
        /// database is snapshotted first and the confirmation names where it goes.</summary>
        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Restore database from backup",
                    Filter = "SQLite database (*.db)|*.db"
                };
                if (dlg.ShowDialog(this) != true) return;
                string source = dlg.FileName;

                if (!MessageDialog.Confirm(this,
                        "Restore this backup? It replaces everything in the current database — drivers, stats, and saved events.\n\n" +
                        "The current database is snapshotted into the Backups folder first, so this can be undone.",
                        "Restore database", destructive: true))
                    return;

                var result = BackupService().RestoreFrom(source);
                if (!result.Success)
                {
                    MessageDialog.Error(this, result.Error, "Restore database");
                    return;
                }

                MessageDialog.Info(this,
                    "The backup has been restored. The app will restart.\n\n" +
                    "The database it replaced is kept at:\n" +
                    (result.SafetySnapshotPath ?? "(no previous database existed)"),
                    "Restore complete");

                RestartApp();
            }
            catch (Exception ex)
            {
                Logger.Log("[SETTINGS][RESTORE] " + ex.Message);
                MessageDialog.Error(this, "Could not restore the database.\n\n" + ex.Message,
                    "Restore database");
            }
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
                MessageDialog.Error(this, "Could not open live view.\n\n" + ex.Message, "Live view");
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) BtnCancel_Click(sender, e);
        }
    }
}
