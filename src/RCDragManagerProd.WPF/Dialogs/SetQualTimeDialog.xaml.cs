using System.Windows;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class SetQualTimeDialog : Window
    {
        public double QualTime { get; private set; }

        public SetQualTimeDialog(string driverName, double? current)
        {
            InitializeComponent();
            TitleText.Text = $"Qual time — {driverName}";
            TxtQualTime.Text = current?.ToString("0.000") ?? "";
            TxtQualTime.Focus();
            TxtQualTime.SelectAll();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(TxtQualTime.Text, out var val) || val <= 0)
            {
                TxtQualTime.BorderBrush = FindResource("Brush.Danger") as System.Windows.Media.Brush;
                return;
            }
            QualTime = val;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;
    }
}
