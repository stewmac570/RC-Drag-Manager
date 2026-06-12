using System.Windows;
using System.Windows.Input;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class TextSummaryWindow : Window
    {
        public TextSummaryWindow(string title, string body)
        {
            InitializeComponent();
            Title = title;
            LblTitle.Text = title;
            TxtBody.Text = body;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
