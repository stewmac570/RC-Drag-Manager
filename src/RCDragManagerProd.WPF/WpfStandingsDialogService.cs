using System.Windows;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.WPF.Dialogs;

namespace RCDragManagerProd.WPF
{
    /// <summary>
    /// Themed WPF standings display, passed to RaceController so Round Robin
    /// standings show in the app's dark/light scrollable window instead of the
    /// legacy WinForms ScrollableTextDialog.
    /// </summary>
    public sealed class WpfStandingsDialogService : IStandingsDialogService
    {
        public void Show(string title, string content)
        {
            // The legacy title carries a mis-encoded separator char; tidy it.
            var clean = (title ?? "Standings").Replace("�", "—");

            void Display()
            {
                var win = new TextSummaryWindow(clean, content) { Owner = ActiveWindow() };
                win.ShowDialog();
            }

            var app = Application.Current;
            if (app != null && !app.Dispatcher.CheckAccess()) app.Dispatcher.Invoke(Display);
            else Display();
        }

        private static Window ActiveWindow()
        {
            var app = Application.Current;
            if (app == null) return null;
            foreach (Window w in app.Windows)
                if (w.IsActive) return w;
            return app.MainWindow;
        }
    }
}
