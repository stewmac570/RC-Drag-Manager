using RCDragManagerProd;               // DatabaseInitializer
using System;
using System.Windows.Forms;

namespace RCDragManagerProd
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) =>
                {
                    Logger.Log($"[APP][UI-ERROR] {e.Exception}");
                    MessageBox.Show("An unexpected error occurred. Check the log for details.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    var ex = e.ExceptionObject as Exception;
                    Logger.Log($"[APP][FATAL] {ex}");
                };

                string dbPath = "race_data.db";
                string connectionString = $"Data Source={dbPath};Version=3;";

                Logger.Log("[APP] Starting RC Drag Manager");
                DatabaseInitializer.InitializeDatabase(connectionString);
                Logger.Log($"[APP] Database initialized at '{dbPath}'");

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Logger.Log("[APP] Showing LandingForm");
                Application.Run(new LandingForm());
                Logger.Log("[APP] LandingForm closed — exiting application");
            }
            catch (Exception ex)
            {
                Logger.Log($"[APP][FATAL] Unhandled in Main: {ex}");
                MessageBox.Show("A fatal error occurred. The application will close.", "Fatal Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
