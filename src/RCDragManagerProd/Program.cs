using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

using RCDragManagerProd.UI.Forms;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.Config;

//claude agent test

namespace RCDragManagerProd
{
    internal static class Program
    {
        public static string ConnectionString { get; private set; }

        private const string AppDataFolder = "RC_Drag_Manager";
        private const string DbFileName = "race_data.db";

        [STAThread]
        private static void Main()
        {
            // Load persisted settings BEFORE any logging happens
            AppSettings.Load();

            // Global exception hooks
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                Logger.Log($"[APP][UI-ERROR] {e.Exception}");
                ShowFatal(e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var ex = e.ExceptionObject as Exception
                         ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown fatal error");
                Logger.Log($"[APP][FATAL-DOMAIN] {ex}");
                ShowFatal(ex);
            };
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Logger.Log($"[APP][ASYNC-ERROR] {e.Exception}");
                e.SetObserved();
            };

            try
            {
                // Ensure %APPDATA%\RC_Drag_Manager exists
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    AppDataFolder);
                Directory.CreateDirectory(dataDir);

                // Ensure DB file exists
                var dbPath = Path.Combine(dataDir, DbFileName);
                if (!File.Exists(dbPath))
                {
                    using var _ = new FileStream(dbPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
                }

                ConnectionString = $"Data Source={dbPath};Version=3;";
                Logger.Log($"[APP] Startup | DataDir='{dataDir}' | DB='{dbPath}' | Logging={(AppSettings.EnableLogging ? "ON" : "OFF")}");

                // Initialize schema/tables once on startup
                DatabaseInitializer.InitializeDatabase(ConnectionString);
                Logger.Log("[APP] Database ready.");

                // Classic WinForms initialization (works on .NET Framework and older targets)
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Logger.Log("[APP] Showing LandingForm");
                Application.Run(new LandingForm(ConnectionString));
                Logger.Log("[APP] LandingForm closed — exiting.");
            }
            catch (Exception ex)
            {
                Logger.Log($"[APP][FATAL] {ex}");
                ShowFatal(ex);
            }
        }

        private static void ShowFatal(Exception ex)
        {
            try
            {
                MessageBox.Show(
                    "A fatal error occurred and the application must close." + Environment.NewLine + Environment.NewLine +
                    ex.Message + Environment.NewLine + Environment.NewLine +
                    $"Check the log at: {AppSettings.LogFilePath}",
                    "RC Drag Manager — Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch { /* ignore if headless */ }
        }
    }
}
