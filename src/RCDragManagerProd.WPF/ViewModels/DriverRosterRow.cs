using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RCDragManagerProd.WPF.ViewModels
{
    /// <summary>
    /// One selectable driver row in the class-config roster. Tracks inclusion
    /// (<see cref="IsChecked"/>) and an optional per-driver dial-in override
    /// (<see cref="OverrideText"/>, only meaningful for Dial-In classes).
    /// </summary>
    public sealed class DriverRosterRow : INotifyPropertyChanged
    {
        public int DriverId { get; set; }
        public string Name { get; set; }
        public string CarName { get; set; }
        public string ClassType { get; set; }
        public string State { get; set; }
        public double? DefaultDialIn { get; set; }

        public string DefaultDialInText =>
            DefaultDialIn.HasValue ? DefaultDialIn.Value.ToString("0.000") : "";

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(); }
        }

        private string _overrideText = "";
        public string OverrideText
        {
            get => _overrideText;
            set { _overrideText = value ?? ""; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
