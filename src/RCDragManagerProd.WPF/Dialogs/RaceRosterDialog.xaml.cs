using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.WPF.Dialogs
{
    /// <summary>Transactional editor for one race's driver roster.</summary>
    public partial class RaceRosterDialog : Window
    {
        private readonly SessionRosterService _rosterService = new SessionRosterService();
        private readonly List<Driver> _drivers;

        public List<Driver> Drivers => _drivers.Select(CloneDriver).ToList();

        public RaceRosterDialog(IEnumerable<Driver> drivers)
        {
            _drivers = (drivers ?? Enumerable.Empty<Driver>()).Select(CloneDriver).ToList();
            InitializeComponent();
            RefreshRoster();
        }

        private void RefreshRoster(int? selectId = null)
        {
            DgRoster.ItemsSource = null;
            DgRoster.ItemsSource = _drivers.OrderBy(d => d.Name).ToList();
            CountText.Text = _drivers.Count == 1 ? "1 driver" : $"{_drivers.Count} drivers";
            if (selectId.HasValue)
                DgRoster.SelectedItem = DgRoster.Items.Cast<Driver>()
                    .FirstOrDefault(d => d.Id == selectId.Value);
            ErrorText.Visibility = Visibility.Collapsed;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            new AddDriverDialog((name, qual) =>
            {
                var result = _rosterService.AddOrUpdate(name, qual, _drivers);
                if (!result.Success) return result.Error;
                RefreshRoster(result.Driver.Id);
                return null;
            }) { Owner = this }.ShowDialog();
            RefreshRoster();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!(DgRoster.SelectedItem is Driver driver))
            {
                ShowError("Select the driver you want to edit.");
                return;
            }

            var dlg = new AddEditDriverDialog(driver.Name, driver.State) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            driver.Name = dlg.DriverName;
            driver.State = dlg.State;
            RefreshRoster(driver.Id);
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (!(DgRoster.SelectedItem is Driver driver))
            {
                ShowError("Select the driver you want to remove.");
                return;
            }

            if (!MessageDialog.Confirm(this, $"Remove {driver.Name} from this race?",
                    "Remove driver", destructive: true)) return;

            _drivers.Remove(driver);
            RefreshRoster();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_drivers.Count < 2)
            {
                ShowError("A race roster needs at least two drivers.");
                return;
            }
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private static Driver CloneDriver(Driver source) => new Driver
        {
            Id = source.Id,
            Name = source.Name,
            QualTime = source.QualTime,
            Notes = source.Notes,
            TotalWins = source.TotalWins,
            TotalLosses = source.TotalLosses,
            EventsEntered = source.EventsEntered,
            EventsWon = source.EventsWon,
            Seed = source.Seed,
            State = source.State,
            Cars = source.Cars == null ? new List<Car>() : new List<Car>(source.Cars)
        };
    }
}
