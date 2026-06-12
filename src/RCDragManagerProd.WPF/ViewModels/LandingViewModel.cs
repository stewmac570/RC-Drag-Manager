using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Config;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.WPF.ViewModels
{
    public sealed class LandingViewModel : INotifyPropertyChanged
    {
        private readonly LoadSessionService _loadService;
        private readonly DriverRepository _driverRepo;

        public ObservableCollection<RecentEventRow> RecentEvents { get; } = new ObservableCollection<RecentEventRow>();

        public string VersionText => "v2.0.0 · .NET Framework 4.8";

        private string _driverCountText = "";
        public string DriverCountText
        {
            get => _driverCountText;
            private set { _driverCountText = value; OnPropertyChanged(); }
        }

        public bool IsLiveConnected => AppSettings.LiveBroadcastEnabled;

        public string LiveStatusText => IsLiveConnected ? "stewmacrc.com" : "Live server offline";

        public string LiveStatusDetail => IsLiveConnected ? "Connected" : "Not configured";

        public Visibility EmptyEventsVisibility =>
            RecentEvents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        public LandingViewModel(LoadSessionService loadService, DriverRepository driverRepo)
        {
            _loadService = loadService ?? throw new ArgumentNullException(nameof(loadService));
            _driverRepo = driverRepo ?? throw new ArgumentNullException(nameof(driverRepo));
        }

        public void Load()
        {
            var rows = new List<RecentEventRow>();

            foreach (var e in _loadService.ListMultiClassEvents())
            {
                rows.Add(new RecentEventRow
                {
                    Id = e.Id,
                    IsMultiClass = true,
                    EventName = string.IsNullOrWhiteSpace(e.EventName) ? "Multi-class event" : e.EventName,
                    SubText = FormatDate(e.EventDate) + $" · {e.ClassCount} class{(e.ClassCount == 1 ? "" : "es")}",
                    EventDate = e.EventDate
                });
            }

            foreach (var s in _loadService.ListSessions())
            {
                rows.Add(new RecentEventRow
                {
                    Id = s.Id,
                    IsMultiClass = false,
                    EventName = string.IsNullOrWhiteSpace(s.EventName) ? "Untitled session" : s.EventName,
                    SubText = FormatDate(s.EventDate) + BuildTypeLabel(s.RaceType, s.ClassType),
                    EventDate = s.EventDate
                });
            }

            RecentEvents.Clear();
            foreach (var r in rows.OrderByDescending(r => r.EventDate).Take(5))
                RecentEvents.Add(r);

            OnPropertyChanged(nameof(EmptyEventsVisibility));

            var drivers = _driverRepo.GetAllDrivers();
            DriverCountText = $"{drivers.Count} registered driver{(drivers.Count == 1 ? "" : "s")}";
        }

        private static string FormatDate(DateTime d) =>
            d == DateTime.MinValue ? "Unknown date" : d.ToString("ddd d MMM yyyy");

        private static string BuildTypeLabel(string raceType, string classType)
        {
            var parts = new[] { raceType, classType }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var label = string.Join(" · ", parts);
            return string.IsNullOrEmpty(label) ? "" : " · " + label;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
