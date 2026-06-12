using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.WPF.ViewModels
{
    public sealed class DriverManagerViewModel : INotifyPropertyChanged
    {
        private readonly DriverManagerService _service;

        public ObservableCollection<Driver> Drivers { get; } = new ObservableCollection<Driver>();
        public ObservableCollection<Car> Cars { get; } = new ObservableCollection<Car>();

        private Driver _selectedDriver;
        public Driver SelectedDriver
        {
            get => _selectedDriver;
            set
            {
                _selectedDriver = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasDriver));
                RefreshCars();
            }
        }

        private Car _selectedCar;
        public Car SelectedCar
        {
            get => _selectedCar;
            set { _selectedCar = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasCar)); }
        }

        private string _filter = "";
        public string Filter
        {
            get => _filter;
            set { _filter = value ?? ""; OnPropertyChanged(); ApplyFilter(); }
        }

        public bool HasDriver => _selectedDriver != null;
        public bool HasCar => _selectedCar != null;

        private string _carsHeader = "Cars";
        public string CarsHeader
        {
            get => _carsHeader;
            private set { _carsHeader = value; OnPropertyChanged(); }
        }

        public DriverManagerViewModel(DriverRepository repo)
        {
            _service = new DriverManagerService(repo);
        }

        public void Load()
        {
            var prev = _selectedDriver?.Id ?? 0;
            RefreshDriverList(prev);
        }

        private void RefreshDriverList(int reSelectId = 0)
        {
            var all = _service.GetAllDrivers()
                              .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                              .ToList();

            Drivers.Clear();
            foreach (var d in all)
            {
                if (string.IsNullOrWhiteSpace(_filter) ||
                    (d.Name ?? "").IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (d.State ?? "").IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    Drivers.Add(d);
            }

            SelectedDriver = reSelectId > 0
                ? Drivers.FirstOrDefault(d => d.Id == reSelectId)
                : null;
        }

        private void ApplyFilter() => RefreshDriverList(_selectedDriver?.Id ?? 0);

        private void RefreshCars()
        {
            Cars.Clear();
            SelectedCar = null;

            if (_selectedDriver == null)
            {
                CarsHeader = "Cars";
                return;
            }

            CarsHeader = $"Cars — {_selectedDriver.Name}";
            var fresh = _service.GetDriverById(_selectedDriver.Id);
            if (fresh?.Cars != null)
                foreach (var c in fresh.Cars)
                    Cars.Add(c);
        }

        // ── Driver CRUD ───────────────────────────────────────────────────────

        public Driver AddDriverWithCar(string name, Car firstCar)
        {
            var d = _service.AddDriverWithCar(name, firstCar);
            RefreshDriverList(d.Id);
            return d;
        }

        public void UpdateDriver(string name, string state)
        {
            if (_selectedDriver == null) return;
            _service.UpdateDriverDetails(_selectedDriver, name, state);
            RefreshDriverList(_selectedDriver.Id);
        }

        public void DeleteDriver()
        {
            if (_selectedDriver == null) return;
            _service.DeleteDriver(_selectedDriver.Id);
            RefreshDriverList(0);
            Cars.Clear();
            CarsHeader = "Cars";
        }

        public void SetQualTime(double time)
        {
            if (_selectedDriver == null) return;
            var updated = _service.SetQualifyingTime(_selectedDriver.Id, time);
            RefreshDriverList(updated.Id);
        }

        // ── Car CRUD ──────────────────────────────────────────────────────────

        public void AddCar(Car car)
        {
            if (_selectedDriver == null) return;
            _service.AddCar(_selectedDriver, car);
            RefreshCars();
        }

        public bool EditCar(int carId, Car edited)
        {
            if (_selectedDriver == null) return false;
            var ok = _service.ApplyCarEdit(_selectedDriver, carId, edited);
            if (ok) RefreshCars();
            return ok;
        }

        public void DeleteCar()
        {
            if (_selectedDriver == null || _selectedCar == null) return;
            _service.DeleteCar(_selectedDriver, _selectedCar.CarID);
            RefreshCars();
        }

        public Driver GetSelectedDriverFresh() =>
            _selectedDriver != null ? _service.GetDriverById(_selectedDriver.Id) : null;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
