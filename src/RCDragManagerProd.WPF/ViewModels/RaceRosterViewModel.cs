using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.WPF.ViewModels
{
    /// <summary>
    /// Backs <c>RaceRosterDialog</c>: the driver database on the left, this race's
    /// drivers on the right. Edits are made against a private copy of the roster and
    /// only handed back when the operator saves, so Cancel really cancels.
    ///
    /// The one exception is a brand-new driver: they are written to the driver database
    /// as soon as they are created, because that is where their id comes from. Cancelling
    /// afterwards leaves them in the database but out of the race — the same thing the
    /// setup screen's "Add new driver" does.
    /// </summary>
    public sealed class RaceRosterViewModel : INotifyPropertyChanged
    {
        private readonly RaceRosterService _service;
        private readonly List<Driver> _roster;
        private List<Driver> _database;

        /// <summary>Drivers in the database but not in this race, filtered by <see cref="Search"/>.</summary>
        public ObservableCollection<RaceRosterAvailableRow> Available { get; } =
            new ObservableCollection<RaceRosterAvailableRow>();

        /// <summary>
        /// Drivers in this race. Never filtered by the search box — the operator checks
        /// this pane against the sign-up sheet, and a search that hid half of it is what
        /// made the class picker unusable in #419.
        /// </summary>
        public ObservableCollection<RaceRosterEntryRow> Entered { get; } =
            new ObservableCollection<RaceRosterEntryRow>();

        public RaceRosterViewModel(RaceRosterService service, IEnumerable<Driver> roster)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _roster = (roster ?? Enumerable.Empty<Driver>())
                .Where(d => d != null)
                .Select(CloneDriver)
                .ToList();

            _database = _service.GetDatabaseDrivers();
            Refresh();
        }

        /// <summary>The edited roster, valid after a successful save.</summary>
        public List<Driver> Roster => _roster;

        // ── Search ────────────────────────────────────────────────────────────

        private string _search = "";
        public string Search
        {
            get => _search;
            set { _search = value ?? ""; OnPropertyChanged(); Refresh(); }
        }

        // ── Counts and messages ───────────────────────────────────────────────

        public string EnteredSummary =>
            _roster.Count == 1 ? "1 driver in this race" : $"{_roster.Count} drivers in this race";

        public bool HasEntries => _roster.Count > 0;

        private string _error;
        public string Error
        {
            get => _error;
            private set { _error = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(_error);

        public void ClearError() => Error = null;

        // ── Commands ──────────────────────────────────────────────────────────

        /// <summary>Moves a database driver into the race.</summary>
        public void Add(RaceRosterAvailableRow row)
        {
            if (row == null) return;

            var dbDriver = _database.FirstOrDefault(d => d.Id == row.DriverId);
            var result = _service.AddFromDatabase(dbDriver, _roster);
            Error = result.Success ? null : result.Error;
            Refresh();
        }

        /// <summary>Takes a driver out of the race. The driver database is untouched.</summary>
        public void Remove(RaceRosterEntryRow row)
        {
            if (row == null) return;

            _service.Remove(_roster.FirstOrDefault(d => d.Id == row.DriverId), _roster);
            Error = null;
            Refresh();
        }

        /// <summary>
        /// Creates a driver and enters them in the race. Returns an operator-facing error
        /// message, or null on success — the shape the bulk add-driver dialog expects.
        /// </summary>
        public string CreateAndAdd(string name, string qualTimeText)
        {
            var result = _service.CreateAndAdd(name, qualTimeText, _roster);
            if (!result.Success) return result.Error;

            _database = _service.GetDatabaseDrivers();
            Error = null;
            Refresh();
            return null;
        }

        /// <summary>Renames a driver in the race and in the database. Null on success.</summary>
        public string Rename(RaceRosterEntryRow row, string newName, string newState)
        {
            var error = _service.Rename(
                _roster.FirstOrDefault(d => d.Id == row?.DriverId), newName, newState, _roster);
            if (error != null) return error;

            _database = _service.GetDatabaseDrivers();
            Error = null;
            Refresh();
            return null;
        }

        /// <summary>Whether the roster can be saved; sets <see cref="Error"/> when it cannot.</summary>
        public bool TrySave()
        {
            Error = _service.ValidateRoster(_roster);
            return !HasError;
        }

        // ── Rebuild ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            var entered = new HashSet<int>(_roster.Select(d => d.Id));

            Available.Clear();
            foreach (var d in _database
                         .Where(d => !entered.Contains(d.Id))
                         .Where(d => string.IsNullOrWhiteSpace(_search) ||
                                     (d.Name ?? "").IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
                         .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
            {
                var car = d.Cars?.FirstOrDefault();
                Available.Add(new RaceRosterAvailableRow
                {
                    DriverId = d.Id,
                    Name = d.Name,
                    CarName = car?.CarName ?? "",
                    State = d.State ?? ""
                });
            }

            Entered.Clear();
            foreach (var d in _roster.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                Entered.Add(new RaceRosterEntryRow
                {
                    DriverId = d.Id,
                    Name = d.Name,
                    State = d.State ?? "",
                    QualText = d.QualTime.HasValue ? d.QualTime.Value.ToString("0.000") : "—"
                });

            OnPropertyChanged(nameof(EnteredSummary));
            OnPropertyChanged(nameof(HasEntries));
        }

        /// <summary>
        /// A working copy, so Cancel discards the operator's edits. Cars come along
        /// because the console seeds a late entry's dial-in from their car default.
        /// </summary>
        private static Driver CloneDriver(Driver source) => new Driver
        {
            Id = source.Id,
            Name = source.Name,
            QualTime = source.QualTime,
            Notes = source.Notes,
            Seed = source.Seed,
            State = source.State,
            Cars = source.Cars == null ? new List<Car>() : new List<Car>(source.Cars)
        };

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    /// <summary>One database driver offered in the left pane.</summary>
    public sealed class RaceRosterAvailableRow
    {
        public int DriverId { get; set; }
        public string Name { get; set; }
        public string CarName { get; set; }
        public string State { get; set; }
    }

    /// <summary>One driver entered in this race, shown in the right pane.</summary>
    public sealed class RaceRosterEntryRow
    {
        public int DriverId { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public string QualText { get; set; }
    }
}
