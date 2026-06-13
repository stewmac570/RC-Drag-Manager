using System.Windows;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.WPF.Windows
{
    public partial class DriverStatsWindow : Window
    {
        public DriverStatsWindow(Driver driver, string connectionString)
        {
            InitializeComponent();
            WindowSizing.FitToScreen(this);

            TbarTitle.Text = $"Stats — {driver.Name}";
            ValWins.Text = driver.TotalWins.ToString();
            ValLosses.Text = driver.TotalLosses.ToString();
            ValEventsEntered.Text = driver.EventsEntered.ToString();
            ValEventsWon.Text = driver.EventsWon.ToString();

            var service = new DriverStatsService(new RaceSessionRepository(connectionString));
            DgHistory.ItemsSource = service.GetMatchHistory(driver);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
