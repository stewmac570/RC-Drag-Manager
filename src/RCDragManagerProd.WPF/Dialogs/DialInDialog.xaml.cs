using System.Windows;
using System.Windows.Input;

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
            var val = TxtDialIn.Text.Trim();
            if (string.IsNullOrEmpty(val))
            {
                Cleared = true;
                DialogResult = true;
                return;
            }
            if (!double.TryParse(val, out var parsed) || parsed <= 0)
            {
                TxtDialIn.BorderBrush = FindResource("Brush.Danger") as System.Windows.Media.Brush;
                return;
            }
            DialIn = parsed;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) DialogResult = false;
        }
    }
}
