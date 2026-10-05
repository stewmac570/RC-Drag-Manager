using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.WPF.Views
{
    /// <summary>
    /// The event's Money tab: one row per driver and car, a track-days column, and an
    /// entry and buyback tick per class. All rules live in <see cref="MoneySheetService"/>.
    /// </summary>
    public partial class MoneySheetView : UserControl
    {
        private readonly MoneySheetService _service;
        private readonly DriverRepository _drivers;
        private readonly ObservableCollection<MoneyRow> _rows = new ObservableCollection<MoneyRow>();

        public MoneySheetView(MoneySheetService service, DriverRepository drivers)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _drivers = drivers;
            InitializeComponent();

            TxtTrackFee.Text = Money(_service.Sheet.TrackFee);
            TxtEntryFee.Text = Money(_service.Sheet.EntryFee);
            TxtBuybackFee.Text = Money(_service.Sheet.BuybackFee);

            BuildColumns();
            DgMoney.ItemsSource = _rows;
            Reload();
        }

        // ── Columns ───────────────────────────────────────────────────────────

        private void BuildColumns()
        {
            DgMoney.Columns.Add(new DataGridTextColumn { Header = "Driver", Binding = new Binding(nameof(MoneyRow.DriverName)), IsReadOnly = true, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            DgMoney.Columns.Add(new DataGridTextColumn { Header = "Car", Binding = new Binding(nameof(MoneyRow.CarName)), IsReadOnly = true, Width = 120 });
            DgMoney.Columns.Add(new DataGridTextColumn
            {
                Header = "Track days paid",
                Binding = new Binding(nameof(MoneyRow.TrackDays)) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus },
                Width = 110
            });

            var classes = _service.ClassNames;
            for (int i = 0; i < classes.Count; i++)
            {
                DgMoney.Columns.Add(CheckColumn($"{classes[i]} entry", $"{nameof(MoneyRow.Entry)}[{i}]"));
                DgMoney.Columns.Add(CheckColumn($"{classes[i]} buyback", $"{nameof(MoneyRow.Buyback)}[{i}]"));
            }

            DgMoney.Columns.Add(new DataGridTextColumn { Header = "Paid", Binding = new Binding(nameof(MoneyRow.TotalText)), IsReadOnly = true, Width = 80 });

            var remove = new DataGridTemplateColumn { Header = "", Width = 70 };
            var button = new FrameworkElementFactory(typeof(Button));
            button.SetValue(ContentProperty, "Remove");
            button.SetValue(StyleProperty, FindResource("Style.Button.Toolbar.Danger"));
            button.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler(BtnRemove_Click));
            remove.CellTemplate = new DataTemplate { VisualTree = button };
            DgMoney.Columns.Add(remove);
        }

        private static DataGridCheckBoxColumn CheckColumn(string header, string path) => new DataGridCheckBoxColumn
        {
            Header = header,
            Binding = new Binding(path) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
            Width = DataGridLength.Auto
        };

        // ── Rows ──────────────────────────────────────────────────────────────

        private void Reload()
        {
            _rows.Clear();
            var classes = _service.ClassNames;
            foreach (var e in _service.Sheet.Entries
                         .OrderBy(x => x.DriverName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.CarName, StringComparer.OrdinalIgnoreCase))
                _rows.Add(new MoneyRow(e, classes, _service, OnRowChanged));
            UpdateTotal();
        }

        private void OnRowChanged()
        {
            UpdateTotal();
            Save();
        }

        private void UpdateTotal()
        {
            foreach (var r in _rows) r.RefreshTotal();
            LblTotal.Text = $"Total paid: {Money(_service.Total)}";
        }

        private void Save()
        {
            var error = _service.Save();
            LblMessage.Text = error ?? $"Saved {DateTime.Now:HH:mm:ss}";
        }

        // ── Commands ──────────────────────────────────────────────────────────

        private void BtnAddAll_Click(object sender, RoutedEventArgs e)
        {
            int added = _service.AddEveryoneFromClasses();
            Reload();
            Save();
            LblMessage.Text = added == 0 ? "Everyone in the classes is already on the sheet." : $"Added {added}.";
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e) => AddByName();

        private void TxtAddName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { AddByName(); e.Handled = true; }
        }

        private void AddByName()
        {
            var name = (TxtAddName.Text ?? "").Trim();
            if (name.Length == 0) { LblMessage.Text = "Type a driver name."; return; }

            var driver = (_drivers?.GetAllDrivers() ?? new List<Driver>())
                .FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));
            if (driver == null)
            {
                LblMessage.Text = $"No driver called '{name}'. Add them in Driver Manager first.";
                return;
            }

            // One row per car; a driver with no car gets one row with no car.
            var cars = (driver.Cars ?? new List<Car>()).Select(c => c.CarName ?? "").DefaultIfEmpty("").ToList();
            var errors = cars.Select(c => _service.AddDriver(driver, c)).Where(x => x != null).ToList();
            Reload();
            Save();
            TxtAddName.Text = "";
            LblMessage.Text = errors.Count == cars.Count ? errors[0] : $"Added {driver.Name}.";
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is MoneyRow row)) return;
            _service.Remove(row.Entry_);
            Reload();
            Save();
        }

        private void Fee_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var sheet = _service.Sheet;
            sheet.TrackFee = ParseFee(TxtTrackFee, sheet.TrackFee);
            sheet.EntryFee = ParseFee(TxtEntryFee, sheet.EntryFee);
            sheet.BuybackFee = ParseFee(TxtBuybackFee, sheet.BuybackFee);
            OnRowChanged();
        }

        private static decimal ParseFee(TextBox box, decimal current)
        {
            var text = (box.Text ?? "").Trim().TrimStart('$');
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) && v >= 0)
            {
                box.Text = Money(v);
                return v;
            }
            box.Text = Money(current);
            return current;
        }

        private static string Money(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        // ── Row model ─────────────────────────────────────────────────────────

        /// <summary>One sheet row as the grid sees it.</summary>
        public sealed class MoneyRow : INotifyPropertyChanged
        {
            private readonly MoneySheetService _service;
            private readonly Action _changed;

            public MoneyRow(MoneySheetEntry entry, List<string> classes, MoneySheetService service, Action changed)
            {
                Entry_ = entry;
                _service = service;
                _changed = changed;
                Entry = new PaidFlags(entry.EntriesPaid, classes, Changed);
                Buyback = new PaidFlags(entry.BuybacksPaid, classes, Changed);
            }

            internal MoneySheetEntry Entry_ { get; }

            public string DriverName => Entry_.DriverName;
            public string CarName => Entry_.CarName;

            public int TrackDays
            {
                get => Entry_.TrackDaysPaid;
                set
                {
                    var v = Math.Max(0, value);
                    if (v == Entry_.TrackDaysPaid) return;
                    Entry_.TrackDaysPaid = v;
                    OnPropertyChanged();
                    Changed();
                }
            }

            public PaidFlags Entry { get; }
            public PaidFlags Buyback { get; }

            public string TotalText => "$" + _service.TotalFor(Entry_).ToString("0.##", CultureInfo.InvariantCulture);

            public void RefreshTotal() => OnPropertyChanged(nameof(TotalText));

            private void Changed() => _changed?.Invoke();

            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string n = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }

        /// <summary>Per-class paid ticks, bound by class index (class names can hold
        /// characters a binding path cannot).</summary>
        public sealed class PaidFlags : INotifyPropertyChanged
        {
            private readonly List<string> _paid;
            private readonly List<string> _classes;
            private readonly Action _changed;

            public PaidFlags(List<string> paid, List<string> classes, Action changed)
            {
                _paid = paid;
                _classes = classes;
                _changed = changed;
            }

            public bool this[int index]
            {
                get => index >= 0 && index < _classes.Count && MoneySheetService.IsPaid(_paid, _classes[index]);
                set
                {
                    if (index < 0 || index >= _classes.Count || this[index] == value) return;
                    MoneySheetService.SetPaid(_paid, _classes[index], value);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                    _changed?.Invoke();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
