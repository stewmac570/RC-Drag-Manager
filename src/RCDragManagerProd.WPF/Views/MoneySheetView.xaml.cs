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
    /// <summary>What the money sheet is being used for.</summary>
    public enum MoneySheetMode
    {
        /// <summary>Setup screen, before racing: track fees and class entries are taken.</summary>
        Entries,

        /// <summary>Running event: entries are locked and only buybacks are taken.</summary>
        Buybacks
    }

    /// <summary>
    /// The money sheet. Prices along the top (track fee, an entry price per class, and a
    /// buyback price for classes that run buybacks), tick boxes per competitor, and a
    /// total row under every column. The footer shows each class's pot (entries plus
    /// buybacks); the track fee is never in a pot. All rules live in
    /// <see cref="MoneySheetService"/>.
    /// </summary>
    public partial class MoneySheetView : UserControl
    {
        private readonly MoneySheetService _service;
        private readonly DriverRepository _drivers;
        private readonly MoneySheetMode _mode;
        private readonly List<MoneySheetClass> _classes;
        private readonly ObservableCollection<MoneyRow> _rows = new ObservableCollection<MoneyRow>();

        public MoneySheetView(MoneySheetService service, DriverRepository drivers, MoneySheetMode mode)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _drivers = drivers;
            _mode = mode;
            InitializeComponent();

            _classes = _service.Classes;
            bool entries = mode == MoneySheetMode.Entries;
            LblTitle.Text = entries ? "Entry money" : "Buybacks";
            AddBar.Visibility = entries ? Visibility.Visible : Visibility.Collapsed;

            // Opens on everyone entered in the classes: at setup to take entries, and
            // in the event so an older event without a sheet can still take buybacks.
            _service.AddEveryoneFromClasses();

            BuildPrices();
            BuildColumns();
            DgMoney.ItemsSource = _rows;
            Reload();
        }

        // ── Prices ────────────────────────────────────────────────────────────

        private void BuildPrices()
        {
            bool entries = _mode == MoneySheetMode.Entries;
            var sheet = _service.Sheet;

            PricePanel.Children.Add(PriceBox("Track fee", sheet.TrackFee, readOnly: !entries, v => sheet.TrackFee = v));

            foreach (var cls in _classes)
            {
                var price = _service.PriceFor(cls.Name);
                PricePanel.Children.Add(PriceBox($"{cls.Name} entry", price.Entry, readOnly: !entries, v => price.Entry = v));
                // Buyback price stays editable in the event: it may only be agreed on the day.
                if (cls.HasBuybacks)
                    PricePanel.Children.Add(PriceBox($"{cls.Name} buyback", price.Buyback, readOnly: false, v => price.Buyback = v));
            }
        }

        private FrameworkElement PriceBox(string label, decimal value, bool readOnly, Action<decimal> apply)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 20, 8) };
            panel.Children.Add(new TextBlock
            {
                Text = label + " $",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                FontSize = (double)FindResource("FontSize.Sm"),
                Foreground = (System.Windows.Media.Brush)FindResource("Brush.TextMuted")
            });
            var box = new TextBox
            {
                Text = Money(value),
                Width = 64,
                IsReadOnly = readOnly,
                Style = (Style)FindResource("Style.TextBox.Compact")
            };
            box.LostKeyboardFocus += (_, __) =>
            {
                var text = (box.Text ?? "").Trim().TrimStart('$');
                if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) && v >= 0)
                {
                    apply(v);
                    box.Text = Money(v);
                    OnRowChanged();
                }
                else
                {
                    LblMessage.Text = $"{label}: enter a dollar amount.";
                }
            };
            panel.Children.Add(box);
            return panel;
        }

        // ── Columns ───────────────────────────────────────────────────────────

        private void BuildColumns()
        {
            bool entries = _mode == MoneySheetMode.Entries;

            DgMoney.Columns.Add(new DataGridTextColumn { Header = "Driver", Binding = new Binding(nameof(MoneyRow.DriverName)), IsReadOnly = true, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            DgMoney.Columns.Add(new DataGridTextColumn { Header = "Car", Binding = new Binding(nameof(MoneyRow.CarName)), IsReadOnly = true, Width = 130 });
            DgMoney.Columns.Add(TickColumn("Track fee", nameof(MoneyRow.TrackFee), nameof(MoneyRow.TrackFeeTotal), editable: entries));

            for (int i = 0; i < _classes.Count; i++)
            {
                DgMoney.Columns.Add(TickColumn($"{_classes[i].Name} entry",
                    $"{nameof(MoneyRow.Entry)}[{i}]", $"{nameof(MoneyRow.EntryTotal)}[{i}]", editable: entries));
                if (!entries && _classes[i].HasBuybacks)
                    DgMoney.Columns.Add(TickColumn($"{_classes[i].Name} buyback",
                        $"{nameof(MoneyRow.Buyback)}[{i}]", $"{nameof(MoneyRow.BuybackTotal)}[{i}]", editable: true));
            }

            DgMoney.Columns.Add(new DataGridTextColumn { Header = "Paid", Binding = new Binding(nameof(MoneyRow.TotalText)), IsReadOnly = true, Width = 80 });

            if (entries)
            {
                var remove = new DataGridTemplateColumn { Header = "", Width = 80 };
                var button = new FrameworkElementFactory(typeof(Button));
                button.SetValue(ContentProperty, "Remove");
                button.SetValue(StyleProperty, FindResource("Style.Button.Toolbar.Danger"));
                button.SetBinding(VisibilityProperty, new Binding(nameof(MoneyRow.TickVisibility)));
                button.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler(BtnRemove_Click));
                remove.CellTemplate = new DataTemplate { VisualTree = button };
                DgMoney.Columns.Add(remove);
            }
        }

        /// <summary>A tick box for each competitor; on the total row, the column's total.</summary>
        private static DataGridTemplateColumn TickColumn(string header, string tickPath, string totalPath, bool editable)
        {
            var root = new FrameworkElementFactory(typeof(Grid));

            var tick = new FrameworkElementFactory(typeof(CheckBox));
            tick.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,
                new Binding(tickPath) { Mode = editable ? BindingMode.TwoWay : BindingMode.OneWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            tick.SetValue(IsEnabledProperty, editable);
            tick.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            tick.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            tick.SetBinding(VisibilityProperty, new Binding(nameof(MoneyRow.TickVisibility)));
            root.AppendChild(tick);

            var total = new FrameworkElementFactory(typeof(TextBlock));
            total.SetBinding(TextBlock.TextProperty, new Binding(totalPath));
            total.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            total.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            total.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            total.SetBinding(VisibilityProperty, new Binding(nameof(MoneyRow.TotalVisibility)));
            root.AppendChild(total);

            return new DataGridTemplateColumn
            {
                Header = header,
                CellTemplate = new DataTemplate { VisualTree = root },
                Width = DataGridLength.Auto,
                MinWidth = 80
            };
        }

        // ── Rows and totals ───────────────────────────────────────────────────

        private void Reload()
        {
            _rows.Clear();
            var names = _classes.Select(c => c.Name).ToList();
            foreach (var e in _service.Sheet.Entries
                         .OrderBy(x => x.DriverName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.CarName, StringComparer.OrdinalIgnoreCase))
                _rows.Add(new MoneyRow(e, names, _service, OnRowChanged));
            _rows.Add(MoneyRow.TotalRow(names, _service));
            UpdateTotals();
        }

        private void OnRowChanged()
        {
            UpdateTotals();
            Save();
        }

        private void UpdateTotals()
        {
            foreach (var r in _rows) r.Refresh();
            var pots = _classes.Select(c => $"{c.Name} pot {Dollars(_service.PotFor(c.Name))}");
            LblPots.Text = string.Join("   ·   ", pots) +
                           $"      Track fees {Dollars(_service.TrackFees)} (not in a pot)";
        }

        private void Save()
        {
            var error = _service.Save();
            if (error != null) LblMessage.Text = error;
            else if (_mode == MoneySheetMode.Buybacks) LblMessage.Text = $"Saved {DateTime.Now:HH:mm:ss}";
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
            if (!((sender as FrameworkElement)?.DataContext is MoneyRow row) || row.Source == null) return;
            _service.Remove(row.Source);
            Reload();
            Save();
        }

        private static string Money(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);
        private static string Dollars(decimal v) => "$" + Money(v);

        // ── Row model ─────────────────────────────────────────────────────────

        /// <summary>One competitor's row, or (with no source) the total row at the bottom.</summary>
        public sealed class MoneyRow : INotifyPropertyChanged
        {
            private readonly MoneySheetService _service;
            private readonly List<string> _classes;
            private readonly Action _changed;

            public MoneyRow(MoneySheetEntry entry, List<string> classes, MoneySheetService service, Action changed)
            {
                Source = entry;
                _classes = classes;
                _service = service;
                _changed = changed;
                Entry = new PaidFlags(entry?.EntriesPaid ?? new List<string>(), classes, Changed);
                Buyback = new PaidFlags(entry?.BuybacksPaid ?? new List<string>(), classes, Changed);
                EntryTotal = new ColumnTotals(i => Dollars(_service.EntriesPaidCount(_classes[i]) * _service.PriceFor(_classes[i]).Entry));
                BuybackTotal = new ColumnTotals(i => Dollars(_service.BuybacksPaidCount(_classes[i]) * _service.PriceFor(_classes[i]).Buyback));
            }

            public static MoneyRow TotalRow(List<string> classes, MoneySheetService service) =>
                new MoneyRow(null, classes, service, null);

            internal MoneySheetEntry Source { get; }
            private bool IsTotal => Source == null;

            public string DriverName => IsTotal ? "Total" : Source.DriverName;
            public string CarName => IsTotal ? "" : Source.CarName;

            public Visibility TickVisibility => IsTotal ? Visibility.Collapsed : Visibility.Visible;
            public Visibility TotalVisibility => IsTotal ? Visibility.Visible : Visibility.Collapsed;

            public bool TrackFee
            {
                get => !IsTotal && MoneySheetService.TrackFeePaid(Source);
                set
                {
                    if (IsTotal || value == TrackFee) return;
                    MoneySheetService.SetTrackFeePaid(Source, value);
                    OnPropertyChanged();
                    Changed();
                }
            }

            public string TrackFeeTotal => Dollars(_service.TrackFees);

            public PaidFlags Entry { get; }
            public PaidFlags Buyback { get; }
            public ColumnTotals EntryTotal { get; }
            public ColumnTotals BuybackTotal { get; }

            public string TotalText => Dollars(IsTotal ? _service.Total : _service.TotalFor(Source));

            public void Refresh()
            {
                OnPropertyChanged(nameof(TotalText));
                if (!IsTotal) return;
                OnPropertyChanged(nameof(TrackFeeTotal));
                EntryTotal.Refresh();
                BuybackTotal.Refresh();
            }

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

        /// <summary>A column's total for the total row, by class index.</summary>
        public sealed class ColumnTotals : INotifyPropertyChanged
        {
            private readonly Func<int, string> _total;

            public ColumnTotals(Func<int, string> total) { _total = total; }

            public string this[int index] => _total(index);

            public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
