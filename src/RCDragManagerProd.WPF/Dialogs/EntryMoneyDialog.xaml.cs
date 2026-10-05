using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.WPF.Views;

namespace RCDragManagerProd.WPF.Dialogs
{
    /// <summary>
    /// Entry money, taken on the setup screen before racing starts. Edits go straight
    /// into the setup's money sheet, which becomes the event's when it starts.
    /// </summary>
    public partial class EntryMoneyDialog : Window
    {
        public EntryMoneyDialog(MoneySheetService service, DriverRepository drivers)
        {
            InitializeComponent();
            WindowSizing.FitToScreen(this);
            Host.Content = new MoneySheetView(service, drivers, MoneySheetMode.Entries);
        }

        private void BtnDone_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
