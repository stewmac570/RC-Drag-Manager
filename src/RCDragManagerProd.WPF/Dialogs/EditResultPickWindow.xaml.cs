using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.WPF.ViewModels;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class EditResultPickWindow : Window
    {
        public int SelectedMatchId { get; private set; }

        public EditResultPickWindow(IList<WinnerDisplayRow> results)
        {
            InitializeComponent();
            Dg.ItemsSource = results;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e) => Confirm();
        private void Dg_DoubleClick(object sender, MouseButtonEventArgs e) => Confirm();

        private void Confirm()
        {
            if (Dg.SelectedItem is WinnerDisplayRow row && row.MatchId > 0)
            {
                SelectedMatchId = row.MatchId;
                DialogResult = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) DialogResult = false;
        }
    }
}
