using System;
using System.IO;
using System.Windows;
using RCDragManagerProd.Config;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.WPF.Windows;

namespace RCDragManagerProd.WPF
{
    public partial class App : Application
    {
        public static string ConnectionString { get; private set; }

        private const string AppDataFolder = "RC_Drag_Manager";
        private const string DbFileName = "race_data.db";

        private void App_Startup(object sender, StartupEventArgs e)
        {
            AppSettings.Load();

            // Merge the code-built (mutable) theme brushes, then Styles.xaml so its
            // StaticResource brush references resolve against them. Order matters.
            var theme = ThemeManager.FromSetting();
            Resources.MergedDictionaries.Add(ThemeManager.BuildBrushDictionary(theme));
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("Resources/Styles.xaml", UriKind.Relative)
            });

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
                if (!File.Exists(dbPath))
                    using (new FileStream(dbPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read)) { }

                ConnectionString = $"Data Source={dbPath};Version=3;";
                Logger.Log($"[WPF] Startup | DB='{dbPath}'");

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
