using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class ClassCompletionWindow : Window
    {
        private readonly RaceSession _session;

        public ClassCompletionWindow(RaceSession session)
        {
            _session = session;
            InitializeComponent();
            WindowSizing.RoundCorners(this);
            DataContext = ClassCompletionPresentationBuilder.Build(session);
        }

        private void BtnFullResults_Click(object sender, RoutedEventArgs e) =>
            new RaceResultsWindow(_session) { Owner = this }.ShowDialog();

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
