using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

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
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => { Logger.Log($"[APP][UI-ERROR] {e.Exception}"); ShowFatal(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown fatal error");
                Logger.Log($"[APP][FATAL-DOMAIN] {ex}"); ShowFatal(ex);
            };

            try
            {
                string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolder);
                Directory.CreateDirectory(dataDir);

                string dbPath = Path.Combine(dataDir, DbFileName);
                if (!File.Exists(dbPath))
                {
                    using (var fs = new FileStream(dbPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read)) { }
                }

                ConnectionString = $"Data Source={dbPath};Version=3;";
                Logger.Log($"[APP] Startup | DataDir='{dataDir}' | DB='{dbPath}'");

                DatabaseInitializer.InitializeDatabase(ConnectionString);
                Logger.Log("[APP] Database ready.");

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
            MessageBox.Show(
                "A fatal error occurred and the application must close." + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine +
                "Check the log at: %APPDATA%\\RC_Drag_Manager\\app.log",
                "RC Drag Manager — Fatal Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
