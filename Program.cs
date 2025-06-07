using System;
using System.Windows.Forms;

namespace RCDragManagerProd
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            string dbPath = "race_data.db";
            string connectionString = $"Data Source={dbPath};Version=3;";

            DatabaseInitializer.InitializeDatabase(connectionString);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LandingForm());
        }
    }
}
