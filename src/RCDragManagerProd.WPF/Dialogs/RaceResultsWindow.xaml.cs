using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class RaceResultsWindow : Window
    {
        public RaceResultsWindow(RaceSession session)
        {
            InitializeComponent();
            WindowSizing.RoundCorners(this);
            DataContext = RaceResultsPresentationBuilder.Build(session);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
