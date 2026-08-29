using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using RCDragManagerProd.AppServices;

namespace RCDragManagerProd.WPF.ViewModels
{
    /// <summary>
    /// Backs <c>LoadSessionWindow</c>: lists all saved single-class sessions and
    /// multi-class events in one browsable list, with load + delete.
    /// </summary>
    public sealed class LoadSessionViewModel : INotifyPropertyChanged
    {
        private readonly LoadSessionService _service;

        public ObservableCollection<LoadEventRow> Events { get; } = new ObservableCollection<LoadEventRow>();

        private LoadEventRow _selected;
        public LoadEventRow Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelection)); }
        }

        public bool HasSelection => _selected != null;

        public Visibility EmptyVisibility =>
            Events.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        public LoadSessionViewModel(LoadSessionService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public void Load()
        {
            var rows = new List<LoadEventRow>();

            foreach (var e in _service.ListMultiClassEvents())
            {
                rows.Add(new LoadEventRow
                {
                    Id = e.Id,
                    IsMultiClass = true,
                    EventName = string.IsNullOrWhiteSpace(e.EventName) ? "Multi-class event" : e.EventName,
                    Kind = "Multi-class",
                    Detail = $"{e.ClassCount} class{(e.ClassCount == 1 ? "" : "es")}",
                    EventDate = e.EventDate
                });
            }

            foreach (var s in _service.ListSessions())
            {
                rows.Add(new LoadEventRow
                {
                    Id = s.Id,
                    IsMultiClass = false,
                    EventName = string.IsNullOrWhiteSpace(s.EventName) ? "Untitled event" : s.EventName,
                    Kind = "Single class",
                    Detail = BuildDetail(s.RaceType, s.ClassType),
                    EventDate = s.EventDate
                });
            }

            Events.Clear();
            foreach (var r in rows.OrderByDescending(r => r.EventDate))
                Events.Add(r);

            Selected = null;
            OnPropertyChanged(nameof(EmptyVisibility));
        }

        public LoadResult LoadSelected()
        {
            if (_selected == null) return LoadResult.Fail("No event selected.");
            return _selected.IsMultiClass
                ? _service.LoadMultiClassEvent(_selected.Id)
                : _service.LoadSingleClassSession(_selected.Id);
        }

        public void DeleteSelected()
        {
            if (_selected == null) return;
            if (_selected.IsMultiClass) _service.DeleteMultiClassEvent(_selected.Id);
            else _service.DeleteSession(_selected.Id);
            Load();
        }

        private static string BuildDetail(string raceType, string classType)
        {
            var parts = new[] { raceType, classType }.Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(" · ", parts);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
