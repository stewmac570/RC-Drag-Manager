using System.Windows;
using System.Windows.Input;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class EditResultDialog : Window
    {
        /// <summary>0 = cancelled, 1 = Driver1 wins, 2 = Driver2 wins (engine option semantics).</summary>
        public int Choice { get; private set; }

        public EditResultDialog(int matchId, string roundLabel, string d1, string d2, bool bye1, bool bye2)
        {
            InitializeComponent();
            TitleText.Text = $"Edit result — M{matchId} ({roundLabel})";
            Btn1.Content = $"Set winner: {d1}";
            Btn2.Content = $"Set winner: {d2}";
            Btn1.IsEnabled = !bye1;
            Btn2.IsEnabled = !bye2;
        }

        private void Btn1_Click(object sender, RoutedEventArgs e) { Choice = 1; DialogResult = true; }
        private void Btn2_Click(object sender, RoutedEventArgs e) { Choice = 2; DialogResult = true; }
        private void BtnCancel_Click(object sender, RoutedEventArgs e) { Choice = 0; DialogResult = false; }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { Choice = 0; DialogResult = false; }
            else if ((e.Key == Key.D1 || e.Key == Key.NumPad1) && Btn1.IsEnabled) { Choice = 1; DialogResult = true; }
            else if ((e.Key == Key.D2 || e.Key == Key.NumPad2) && Btn2.IsEnabled) { Choice = 2; DialogResult = true; }
        }
    }
}
