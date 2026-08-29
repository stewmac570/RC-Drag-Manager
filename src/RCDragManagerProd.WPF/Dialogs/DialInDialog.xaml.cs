using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.AppServices;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class DialInDialog : Window
    {
        public double DialIn { get; private set; }
        public bool Cleared { get; private set; }

        public DialInDialog(string driverName, double? current)
        {
            InitializeComponent();
            TitleText.Text = $"Dial-in — {driverName}";
            TxtDialIn.Text = current?.ToString("0.000") ?? "";
            TxtDialIn.Focus();
            TxtDialIn.SelectAll();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Same parser as the inline grid cell (#416), so both entry points accept
            // exactly the same input.
            var parsed = RaceConsoleService.ParseDialIn(TxtDialIn.Text);
            if (!parsed.Success)
            {
                TxtDialIn.BorderBrush = FindResource("Brush.Danger") as System.Windows.Media.Brush;
                return;
            }

            Cleared = parsed.Cleared;
            DialIn = parsed.DialIn ?? 0;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) DialogResult = false;
        }
    }
}
