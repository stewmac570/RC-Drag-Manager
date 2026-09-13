using System;
using System.IO;
using System.Windows;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Config;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.WPF.Windows;

namespace RCDragManagerProd.WPF
{
    public partial class App : Application
    {
        public static string ConnectionString { get; private set; }

        /// <summary>Full path of the live database file, so windows like Settings can
        /// hand it to the backup service.</summary>
        public static string DatabasePath { get; private set; }

        private const string AppDataFolder = "RC_Drag_Manager";
        private const string DbFileName = "race_data.db";

        private void App_Startup(object sender, StartupEventArgs e)
        {
            AppSettings.Load();
            ThemeManager.Apply(ThemeManager.FromSetting());

            DispatcherUnhandledException += (_, args) =>
            {
                Logger.Log($"[WPF][ERROR] {args.Exception}");
                MessageBox.Show(
                    args.Exception.Message,
                    "RC Drag Manager — Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            try
            {
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    AppDataFolder);
                Directory.CreateDirectory(dataDir);

                var dbPath = Path.Combine(dataDir, DbFileName);
                DatabasePath = dbPath;

                // Copy on startup BEFORE the database is opened, so the snapshot is
                // consistent (issue #405). Skipped on a brand-new install: there is
                // nothing worth backing up yet.
                var existedBefore = File.Exists(dbPath);
                if (!existedBefore)
                    using (new FileStream(dbPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read)) { }

                ConnectionString = $"Data Source={dbPath};Version=3;";
                Logger.Log($"[WPF] Startup | DB='{dbPath}'");

                if (existedBefore)
                {
                    try { new DatabaseBackupService(dbPath).BackupOnStartup(); }
                    catch (Exception ex) { Logger.Log($"[BACKUP][STARTUP] {ex}"); }
                }

                DatabaseInitializer.InitializeDatabase(ConnectionString);
                Logger.Log("[WPF] Database ready.");

                new LandingWindow(ConnectionString).Show();
            }
            catch (Exception ex)
            {
                Logger.Log($"[WPF][FATAL] {ex}");
                MessageBox.Show(
                    ex.Message,
                    "RC Drag Manager — Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}
