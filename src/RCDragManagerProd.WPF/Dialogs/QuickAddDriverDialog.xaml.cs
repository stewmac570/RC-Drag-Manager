using System.Windows;
using System.Windows.Input;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class QuickAddDriverDialog : Window
    {
        public string DriverName => TxtDriverName.Text.Trim();
        public string CarName => TxtCarName.Text.Trim();
        public string ClassType => TxtClassType.Text.Trim();
        public double? DialIn { get; private set; }

        public QuickAddDriverDialog()
        {
            InitializeComponent();
            TxtDriverName.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtDriverName.Text))
            {
                TxtDriverName.BorderBrush = FindResource("Brush.Danger") as System.Windows.Media.Brush;
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtCarName.Text))
            {
                TxtCarName.BorderBrush = FindResource("Brush.Danger") as System.Windows.Media.Brush;
                return;
            }

            if (!string.IsNullOrWhiteSpace(TxtDialIn.Text) &&
                double.TryParse(TxtDialIn.Text.Trim(), out var parsed))
                DialIn = parsed;

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) DialogResult = false;
        }
    }
}
