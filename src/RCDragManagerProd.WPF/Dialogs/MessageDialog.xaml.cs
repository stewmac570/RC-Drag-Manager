using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RCDragManagerProd.WPF.Dialogs
{
    /// <summary>
    /// Dark, themed replacement for the native MessageBox. Use the static helpers
    /// (Info / Warn / Error / Confirm) rather than constructing directly.
    /// </summary>
    public partial class MessageDialog : Window
    {
        public enum Kind { Info, Warn, Error, Confirm }

        private MessageDialog(Window owner, Kind kind, string title, string message, bool destructive)
        {
            InitializeComponent();
            WindowSizing.RoundCorners(this);
            Owner = owner ?? ActiveOwner();
            if (Owner == null) WindowStartupLocation = WindowStartupLocation.CenterScreen;

            TitleText.Text = title;
            MessageText.Text = message;

            string brushKey;
            switch (kind)
            {
                case Kind.Warn:    brushKey = "Brush.Accent"; break;
                case Kind.Error:   brushKey = "Brush.Danger"; break;
                case Kind.Confirm: brushKey = destructive ? "Brush.Danger" : "Brush.Primary"; break;
                default:           brushKey = "Brush.Primary"; break;
            }
            IconChip.Background = (Brush)FindResource(brushKey);

            if (kind == Kind.Confirm)
            {
                BtnSecondary.Visibility = Visibility.Visible;
                BtnSecondary.Content = "No";
                BtnPrimary.Content = "Yes";
                if (destructive)
                    BtnPrimary.Background = (Brush)FindResource("Brush.Danger");
            }
        }

        private static Window ActiveOwner()
        {
            var app = Application.Current;
            if (app == null) return null;
            foreach (Window w in app.Windows)
                if (w.IsActive) return w;
            return app.MainWindow;
        }

        // ── Static API ────────────────────────────────────────────────────────

        public static void Info(Window owner, string message, string title = "RC Drag Manager") =>
            Show(owner, Kind.Info, title, message, false);

        public static void Warn(Window owner, string message, string title = "RC Drag Manager") =>
            Show(owner, Kind.Warn, title, message, false);

        public static void Error(Window owner, string message, string title = "RC Drag Manager") =>
            Show(owner, Kind.Error, title, message, false);

        public static bool Confirm(Window owner, string message, string title = "RC Drag Manager",
                                   bool destructive = false) =>
            new MessageDialog(owner, Kind.Confirm, title, message, destructive).ShowDialog() == true;

        private static void Show(Window owner, Kind kind, string title, string message, bool destructive) =>
            new MessageDialog(owner, kind, title, message, destructive).ShowDialog();

        private void BtnPrimary_Click(object sender, RoutedEventArgs e) => DialogResult = true;
        private void BtnSecondary_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) DialogResult = false;
            else if (e.Key == Key.Enter) DialogResult = true;
        }
    }
}
