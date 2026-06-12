using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.WPF.ViewModels
{
    /// <summary>
    /// Backs <c>SetupWindow</c>: event name + date and the list of configured
    /// classes. Wraps <see cref="MultiClassSetupService"/> for validation and
    /// event construction.
    /// </summary>
    public sealed class SetupViewModel : INotifyPropertyChanged
    {
        private readonly MultiClassSetupService _service;
        private readonly MultiClassEventRepository _eventRepo;

        public ObservableCollection<ClassRow> Classes { get; } = new ObservableCollection<ClassRow>();

        /// <summary>The underlying configured classes, parallel to <see cref="Classes"/>.</summary>
        private readonly List<ClassConfigDto> _configs = new List<ClassConfigDto>();

        public MultiClassSetupService Service => _service;

        public SetupViewModel(MultiClassSetupService service, MultiClassEventRepository eventRepo)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _eventRepo = eventRepo ?? throw new ArgumentNullException(nameof(eventRepo));
        }

        private string _eventName = "";
        public string EventName
        {
            get => _eventName;
            set { _eventName = value; OnPropertyChanged(); }
        }

        private DateTime _eventDate = DateTime.Today;
        public DateTime EventDate
        {
            get => _eventDate;
            set { _eventDate = value; OnPropertyChanged(); }
        }

        private ClassRow _selectedClass;
        public ClassRow SelectedClass
        {
            get => _selectedClass;
            set { _selectedClass = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelection)); }
        }

        public bool HasSelection => _selectedClass != null;

        // ── Class list management ─────────────────────────────────────────────

        /// <summary>Adds a configured class. Returns an error string if the name clashes, else null.</summary>
        public string AddClass(ClassConfigDto config)
        {
            var existing = _configs.Select(c => c.ClassName);
            string error = _service.ValidateClassName(config.ClassName, existing);
            if (error != null) return error;

            _configs.Add(config);
            Classes.Add(ToRow(config));
            return null;
        }

        public ClassConfigDto GetConfig(int index) =>
            index >= 0 && index < _configs.Count ? _configs[index] : null;

        public void ReplaceClass(int index, ClassConfigDto config)
        {
            if (index < 0 || index >= _configs.Count) return;
            _configs[index] = config;
            Classes[index] = ToRow(config);
        }

        public void RemoveClass(int index)
        {
            if (index < 0 || index >= _configs.Count) return;
            _configs.RemoveAt(index);
            Classes.RemoveAt(index);
        }

        public int IndexOf(ClassRow row) => Classes.IndexOf(row);

        // ── Start ─────────────────────────────────────────────────────────────

        public string ValidateCanStart() => _service.ValidateCanStart(_configs);

        /// <summary>Builds, persists, and returns the new event.</summary>
        public MultiClassEvent StartEvent()
        {
            var evt = _service.StartEvent(EventName?.Trim() ?? "", EventDate.Date, _configs);
            _eventRepo.SaveEvent(evt);
            return evt;
        }

        private static ClassRow ToRow(ClassConfigDto c) => new ClassRow
        {
            ClassName = c.ClassName,
            RaceType = c.RaceType ?? "",
            ClassType = c.ClassType ?? "",
            Rounds = c.RoundsToRun.HasValue ? c.RoundsToRun.Value.ToString() : "—",
            DriverCount = c.DriverEntries?.Count ?? 0
        };

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public sealed class ClassRow
    {
        public string ClassName { get; set; }
        public string RaceType { get; set; }
        public string ClassType { get; set; }
        public string Rounds { get; set; }
        public int DriverCount { get; set; }
    }
}
