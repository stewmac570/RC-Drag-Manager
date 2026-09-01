using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.WPF.ViewModels
{
    /// <summary>
    /// Backs <c>ClassConfigDialog</c>: class name, race type (Pro Ladder /
    /// Random Draw / Round Robin), class type (Heads Up / Bracket Class /
    /// Dial-In), Round-Robin options, and the selectable driver roster.
    /// </summary>
    public sealed class ClassConfigViewModel : INotifyPropertyChanged
    {
        public const string ProLadder  = "Pro Ladder";
        public const string RandomDraw  = "Random Draw";
        public const string RoundRobin  = "Round Robin";

        private readonly MultiClassSetupService _service;
        private readonly List<DriverRosterRow> _allRows = new List<DriverRosterRow>();

        /// <summary>Drivers not yet in the class, filtered by <see cref="Search"/>.</summary>
        public ObservableCollection<DriverRosterRow> Roster { get; } = new ObservableCollection<DriverRosterRow>();

        /// <summary>
        /// Drivers already in the class. Deliberately never filtered by the search
        /// box — hiding them behind a search is what made it impossible to check
        /// the class against a paper sign-up sheet (#419).
        /// </summary>
        public ObservableCollection<DriverRosterRow> Selected { get; } = new ObservableCollection<DriverRosterRow>();

        public bool IsEdit { get; }

        public ClassConfigViewModel(MultiClassSetupService service, ClassConfigDto existing = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            IsEdit = existing != null;

            LoadRoster();
            _selectedRaceType = RoundRobin;
            _isHeadsUp = true;

            if (existing != null)
                ApplyExisting(existing);

            RefreshRoster();
        }

        // ── Class name ────────────────────────────────────────────────────────

        private string _className = "";
        public string ClassName
        {
            get => _className;
            set { _className = value; OnPropertyChanged(); }
        }

        // ── Race type (card selection) ────────────────────────────────────────

        private string _selectedRaceType;
        public string SelectedRaceType
        {
            get => _selectedRaceType;
            set
            {
                _selectedRaceType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsProLadderSelected));
                OnPropertyChanged(nameof(IsRandomDrawSelected));
                OnPropertyChanged(nameof(IsRoundRobinSelected));
                OnPropertyChanged(nameof(RrConfigVisibility));
            }
        }

        public bool IsProLadderSelected  => _selectedRaceType == ProLadder;
        public bool IsRandomDrawSelected => _selectedRaceType == RandomDraw;
        public bool IsRoundRobinSelected => _selectedRaceType == RoundRobin;

        public Visibility RrConfigVisibility =>
            IsRoundRobinSelected ? Visibility.Visible : Visibility.Collapsed;

        // ── Class type (radio) ────────────────────────────────────────────────

        private bool _isHeadsUp;
        public bool IsHeadsUp
        {
            get => _isHeadsUp;
            set { _isHeadsUp = value; OnPropertyChanged(); OnClassTypeChanged(); }
        }

        private bool _isBracket;
        public bool IsBracket
        {
            get => _isBracket;
            set { _isBracket = value; OnPropertyChanged(); OnClassTypeChanged(); }
        }

        private bool _isDialIn;
        public bool IsDialIn
        {
            get => _isDialIn;
            set { _isDialIn = value; OnPropertyChanged(); OnClassTypeChanged(); }
        }

        private void OnClassTypeChanged()
        {
            OnPropertyChanged(nameof(FixedDialInVisibility));
            OnPropertyChanged(nameof(OverrideColumnEnabled));
            OnPropertyChanged(nameof(OverrideColumnVisibility));
        }

        public Visibility FixedDialInVisibility =>
            _isBracket ? Visibility.Visible : Visibility.Collapsed;

        public bool OverrideColumnEnabled => _isDialIn;

        /// <summary>Dial-in overrides only mean anything for a Dial-In class.</summary>
        public Visibility OverrideColumnVisibility =>
            _isDialIn ? Visibility.Visible : Visibility.Collapsed;

        private string _fixedDialInText = "";
        public string FixedDialInText
        {
            get => _fixedDialInText;
            set { _fixedDialInText = value; OnPropertyChanged(); }
        }

        // ── Round Robin options ───────────────────────────────────────────────

        // Off by default: most classes run without a buyback, so the RD had to
        // remember to untick it every time. Ticking it in is the deliberate act.
        private bool _buybackEnabled;
        public bool BuybackEnabled
        {
            get => _buybackEnabled;
            set { _buybackEnabled = value; OnPropertyChanged(); }
        }

        // String-backed so the field accepts free typing (clearing/retyping) without
        // per-keystroke int-conversion binding errors. Parsed in BuildResult.
        private string _roundsText = "3";
        public string RoundsText
        {
            get => _roundsText;
            set { _roundsText = value; OnPropertyChanged(); }
        }

        // ── Search ────────────────────────────────────────────────────────────

        private string _search = "";
        public string Search
        {
            get => _search;
            set { _search = value ?? ""; OnPropertyChanged(); RefreshRoster(); }
        }

        public int CheckedCount => _allRows.Count(r => r.IsChecked);

        /// <summary>Live count for the "In this class" header, checked against the sign-up sheet.</summary>
        public string SelectedSummary =>
            CheckedCount == 1 ? "1 driver in this class" : $"{CheckedCount} drivers in this class";

        public bool HasSelection => CheckedCount > 0;

        // ── Include / exclude ─────────────────────────────────────────────────

        /// <summary>Puts a driver in the class. Safe to call on a driver already in it.</summary>
        public void Include(DriverRosterRow row)
        {
            if (row == null || row.IsChecked) return;
            row.IsChecked = true;
            RefreshRoster();
        }

        /// <summary>Takes a driver out of the class, discarding any dial-in override.</summary>
        public void Exclude(DriverRosterRow row)
        {
            if (row == null || !row.IsChecked) return;
            row.IsChecked = false;
            row.OverrideText = "";
            RefreshRoster();
        }

        // ── Roster loading ────────────────────────────────────────────────────

        private void LoadRoster()
        {
            _allRows.Clear();
            foreach (var d in _service.GetAllDrivers())
            {
                var car = d.Cars?.FirstOrDefault();
                _allRows.Add(new DriverRosterRow
                {
                    DriverId = d.Id,
                    Name = d.Name,
                    CarName = car?.CarName ?? "",
                    ClassType = car?.ClassType ?? "",
                    State = d.State ?? "",
                    DefaultDialIn = car?.DefaultDialIn
                });
            }
        }

        private void RefreshRoster()
        {
            Roster.Clear();
            foreach (var r in _allRows
                         .Where(r => !r.IsChecked)
                         .Where(r => string.IsNullOrWhiteSpace(_search) ||
                                     (r.Name ?? "").IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
                         .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
                Roster.Add(r);

            Selected.Clear();
            foreach (var r in _allRows
                         .Where(r => r.IsChecked)
                         .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
                Selected.Add(r);

            OnPropertyChanged(nameof(CheckedCount));
            OnPropertyChanged(nameof(SelectedSummary));
            OnPropertyChanged(nameof(HasSelection));
        }

        public void QuickAddDriver(string name, Car car)
        {
            _service.QuickAddDriver(name, car);
            // Preserve current checked/override state by reloading then re-applying.
            var checkedIds = new HashSet<int>(_allRows.Where(r => r.IsChecked).Select(r => r.DriverId));
            var overrides = _allRows.Where(r => !string.IsNullOrWhiteSpace(r.OverrideText))
                                    .ToDictionary(r => r.DriverId, r => r.OverrideText);
            LoadRoster();
            foreach (var r in _allRows)
            {
                if (checkedIds.Contains(r.DriverId)) r.IsChecked = true;
                if (overrides.TryGetValue(r.DriverId, out var ov)) r.OverrideText = ov;
            }
            RefreshRoster();
        }

        // ── Apply existing (edit mode) ────────────────────────────────────────

        private void ApplyExisting(ClassConfigDto c)
        {
            ClassName = c.ClassName ?? "";
            SelectedRaceType = string.IsNullOrEmpty(c.RaceType) ? RoundRobin : c.RaceType;

            if (string.Equals(c.ClassType, "Bracket Class", StringComparison.OrdinalIgnoreCase))
            {
                _isHeadsUp = false; _isBracket = true; _isDialIn = false;
                if (c.FixedDialIn.HasValue) FixedDialInText = c.FixedDialIn.Value.ToString("0.000");
            }
            else if (string.Equals(c.ClassType, "Dial-In", StringComparison.OrdinalIgnoreCase))
            {
                _isHeadsUp = false; _isBracket = false; _isDialIn = true;
            }
            else
            {
                _isHeadsUp = true; _isBracket = false; _isDialIn = false;
            }
            OnPropertyChanged(nameof(IsHeadsUp));
            OnPropertyChanged(nameof(IsBracket));
            OnPropertyChanged(nameof(IsDialIn));
            OnClassTypeChanged();

            BuybackEnabled = !string.Equals(c.Variant, "QMDRA", StringComparison.OrdinalIgnoreCase);
            if (c.RoundsToRun.HasValue) RoundsText = c.RoundsToRun.Value.ToString();

            bool wasDialIn = string.Equals(c.ClassType, "Dial-In", StringComparison.OrdinalIgnoreCase);
            foreach (var entry in c.DriverEntries ?? new List<RaceSessionDriverEntry>())
            {
                var row = _allRows.FirstOrDefault(r => r.DriverId == entry.DriverID);
                if (row == null) continue;
                row.IsChecked = true;
                if (wasDialIn && entry.DialIn.HasValue)
                    row.OverrideText = entry.DialIn.Value.ToString("0.000");
            }
        }

        // ── Build result ──────────────────────────────────────────────────────

        /// <summary>Validates and produces the configured class, or returns null with an error message.</summary>
        public ClassConfigDto BuildResult(out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(ClassName))
            {
                error = "Please enter a class name.";
                return null;
            }

            string classType = _isBracket ? "Bracket Class" : _isDialIn ? "Dial-In" : "Heads Up";

            double? fixedDialIn = null;
            if (_isBracket && double.TryParse(FixedDialInText.Trim(), out var fd))
                fixedDialIn = fd;

            string variant = null;
            int? rounds = null;
            if (IsRoundRobinSelected)
            {
                variant = BuybackEnabled ? "Standard" : "QMDRA";
                if (!int.TryParse(RoundsText?.Trim(), out var n) || n <= 0)
                {
                    error = "Rounds must be a whole number of at least 1.";
                    return null;
                }
                rounds = n;
            }

            var checkedIds = _allRows.Where(r => r.IsChecked).Select(r => r.DriverId).ToList();

            var overrides = new Dictionary<int, double?>();
            if (_isDialIn)
                foreach (var r in _allRows.Where(r => r.IsChecked && !string.IsNullOrWhiteSpace(r.OverrideText)))
                    if (double.TryParse(r.OverrideText.Trim(), out var ov))
                        overrides[r.DriverId] = ov;

            var allDrivers = _service.GetAllDrivers();
            var entries = _service.BuildDriverEntries(
                checkedIds, allDrivers, classType, fixedDialIn, overrides, ClassName.Trim());

            return new ClassConfigDto
            {
                ClassName = ClassName.Trim(),
                RaceType = SelectedRaceType,
                ClassType = classType,
                FixedDialIn = fixedDialIn,
                Variant = variant,
                RoundsToRun = rounds,
                DriverEntries = entries
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
