using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.WPF.Dialogs
{
    public partial class BuybackDialog : Window
    {
        private readonly List<BuybackRow> _rows;

        public List<Driver> SelectedDrivers { get; private set; } = new List<Driver>();

        public BuybackDialog(IList<Driver> eligible)
        {
            InitializeComponent();
            _rows = eligible.Select(d => new BuybackRow { Driver = d, Name = d.Name }).ToList();
            IcDrivers.ItemsSource = _rows;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            SelectedDrivers = _rows.Where(r => r.IsChecked).Select(r => r.Driver).ToList();
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) DialogResult = false;
        }

        private sealed class BuybackRow : INotifyPropertyChanged
        {
            public Driver Driver { get; set; }
            public string Name { get; set; }

            private bool _isChecked;
            public bool IsChecked
            {
                get => _isChecked;
                set { _isChecked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked))); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
