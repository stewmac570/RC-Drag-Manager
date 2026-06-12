using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class AddEditCarDialog : Window
    {
        public Car Result { get; private set; }

        public AddEditCarDialog(Car existing = null)
        {
            InitializeComponent();
            TitleText.Text = existing == null ? "Add car" : "Edit car";
            TxtCarName.Text = existing?.CarName ?? "";
            TxtClassType.Text = existing?.ClassType ?? "";
            TxtDialIn.Text = existing?.DefaultDialIn?.ToString("0.000") ?? "";
            TxtCarName.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCarName.Text))
            {
                TxtCarName.BorderBrush = FindResource("Brush.Danger") as System.Windows.Media.Brush;
                return;
            }

            double? dialIn = null;
            if (!string.IsNullOrWhiteSpace(TxtDialIn.Text) &&
                double.TryParse(TxtDialIn.Text, out var parsed))
                dialIn = parsed;

            Result = new Car
            {
                CarName = TxtCarName.Text.Trim(),
                ClassType = TxtClassType.Text.Trim(),
                DefaultDialIn = dialIn
            };

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
