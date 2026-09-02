using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.WPF.Dialogs
{
    /// <summary>Which tab the results window opens on.</summary>
    public enum ResultsTab
    {
        /// <summary>The bracket ladder — the default, and where the window always
        /// lands unless the operator asked for something specific.</summary>
        Ladder = 0,
        ResultsList = 1,
        RoundRobinStandings = 2,
        Winner = 3
    }

    public partial class RaceResultsWindow : Window
    {
        /// <param name="initialTab">
        /// Only pass this when the operator asked for that view by name — the
        /// Standings button, say. Anything that opens the window on its own opens it
        /// on the first tab; landing on the last one made the window feel like it had
        /// jumped somewhere.
        /// </param>
        public RaceResultsWindow(RaceSession session, ResultsTab initialTab = ResultsTab.Ladder)
        {
            InitializeComponent();
            WindowSizing.FitToScreen(this);

            var presentation = RaceResultsPresentationBuilder.Build(session);
            DataContext = presentation;

            // The winner board is built by ClassCompletionPresentationBuilder, which
            // reads RaceResultsPresentationBuilder itself — so it is attached here
            // rather than nested inside the presentation it depends on.
            WinnerTab.DataContext = ClassCompletionPresentationBuilder.Build(session);

            ResultsTabs.SelectedIndex = Selectable(presentation, initialTab);
        }

        /// <summary>
        /// The requested tab, or the Ladder when that tab has nothing to show — WPF
        /// would otherwise open on a disabled tab with a blank body.
        /// </summary>
        private static int Selectable(RaceResultsPresentation presentation, ResultsTab tab)
        {
            switch (tab)
            {
                case ResultsTab.RoundRobinStandings when !presentation.HasRoundRobinStandings:
                case ResultsTab.Winner when !presentation.HasWinner:
                    return (int)ResultsTab.Ladder;
                default:
                    return (int)tab;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
