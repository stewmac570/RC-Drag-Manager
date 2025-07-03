using RCDragManagerProd;               // for RaceSession
using RCDragManagerProd.Controllers;   // for RaceController
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
            var session = new RaceSession();           // NEW
            var controller = new RaceController(session); // NEW
            Application.Run(new Form1(controller));       // NEW
        }
    }
}
