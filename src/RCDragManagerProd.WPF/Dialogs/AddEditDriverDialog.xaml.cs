using System.Windows;
using System.Windows.Input;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class AddEditDriverDialog : Window
    {
        public string DriverName => TxtName.Text.Trim();
        public string State => TxtState.Text.Trim();

        public AddEditDriverDialog(string existingName = null, string existingState = null)
        {
            InitializeComponent();
            TitleText.Text = existingName == null ? "Add driver" : "Edit driver";
            TxtName.Text = existingName ?? "";
            TxtState.Text = existingState ?? "";
            TxtName.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                TxtName.BorderBrush = FindResource("Brush.Danger") as System.Windows.Media.Brush;
                return;
            }
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
