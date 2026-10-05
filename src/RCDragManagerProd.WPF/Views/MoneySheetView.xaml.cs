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
    /// The money sheet. Prices at the top (track fee, and an entry and buyback price per
    /// class), tick boxes per competitor in the middle, totals at the bottom: track fees
    /// on their own, and each class's pot (entries plus buybacks). All rules live in
    /// <see cref="MoneySheetService"/>.
    /// </summary>
    public partial class MoneySheetView : UserControl
    {
        private readonly MoneySheetService _service;
        private readonly DriverRepository _drivers;
        private readonly MoneySheetMode _mode;
        private readonly ObservableCollection<MoneyRow> _rows = new ObservableCollection<MoneyRow>();

        public MoneySheetView(MoneySheetService service, DriverRepository drivers, MoneySheetMode mode)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _drivers = drivers;
            _mode = mode;
            InitializeComponent();

            bool entries = mode == MoneySheetMode.Entries;
            LblTitle.Text = entries ? "Entry money" : "Buybacks and pots";
            LblHint.Text = entries
                ? "Set the prices, then tick what each competitor has paid before racing starts."
                : "Entries were taken at setup. Tick each buyback as it is paid; it goes into that class's pot.";
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

            PricePanel.Children.Add(PriceBox("Track fee", sheet.TrackFee, readOnly: !entries,
                v => sheet.TrackFee = v));

            foreach (var cls in _service.ClassNames)
            {
                var price = _service.PriceFor(cls);
                PricePanel.Children.Add(PriceBox($"{cls} entry", price.Entry, readOnly: !entries, v => price.Entry = v));
                // Buyback price stays editable in the event: it may only be agreed on the day.
                PricePanel.Children.Add(PriceBox($"{cls} buyback", price.Buyback, readOnly: false, v => price.Buyback = v));
            }
        }

        private FrameworkElement PriceBox(string label, decimal value, bool readOnly, Action<decimal> apply)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 22, 8) };
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
                Width = 70,
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
            DgMoney.Columns.Add(new DataGridTextColumn { Header = "Car", Binding = new Binding(nameof(MoneyRow.CarName)), IsReadOnly = true, Width = 120 });
            DgMoney.Columns.Add(CheckColumn("Track fee", nameof(MoneyRow.TrackFee), readOnly: !entries));

            var classes = _service.ClassNames;
            for (int i = 0; i < classes.Count; i++)
            {
                DgMoney.Columns.Add(CheckColumn($"{classes[i]} entry", $"{nameof(MoneyRow.Entry)}[{i}]", readOnly: !entries));
                if (!entries)
                    DgMoney.Columns.Add(CheckColumn($"{classes[i]} buyback", $"{nameof(MoneyRow.Buyback)}[{i}]", readOnly: false));
            }

            DgMoney.Columns.Add(new DataGridTextColumn { Header = "Paid", Binding = new Binding(nameof(MoneyRow.TotalText)), IsReadOnly = true, Width = 80 });

            if (entries)
            {
                var remove = new DataGridTemplateColumn { Header = "", Width = 80 };
                var button = new FrameworkElementFactory(typeof(Button));
                button.SetValue(ContentProperty, "Remove");
                button.SetValue(StyleProperty, FindResource("Style.Button.Toolbar.Danger"));
                button.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler(BtnRemove_Click));
                remove.CellTemplate = new DataTemplate { VisualTree = button };
                DgMoney.Columns.Add(remove);
            }
        }

        private static DataGridCheckBoxColumn CheckColumn(string header, string path, bool readOnly) => new DataGridCheckBoxColumn
        {
            Header = header,
            Binding = new Binding(path) { Mode = readOnly ? BindingMode.OneWay : BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
            IsReadOnly = readOnly,
            Width = DataGridLength.Auto
        };

        // ── Rows and totals ───────────────────────────────────────────────────

        private void Reload()
        {
            _rows.Clear();
            var classes = _service.ClassNames;
            foreach (var e in _service.Sheet.Entries
                         .OrderBy(x => x.DriverName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.CarName, StringComparer.OrdinalIgnoreCase))
                _rows.Add(new MoneyRow(e, classes, _service, OnRowChanged));
            UpdateTotals();
        }

        private void OnRowChanged()
        {
            UpdateTotals();
            Save();
        }

        private void UpdateTotals()
        {
            foreach (var r in _rows) r.RefreshTotal();

            TotalsPanel.Children.Clear();
            TotalsPanel.Children.Add(TotalLine(
                $"Track fees: {_service.TrackFeesPaidCount} paid = {Dollars(_service.TrackFees)}  (not in any pot)"));
            foreach (var cls in _service.ClassNames)
            {
                var price = _service.PriceFor(cls);
                int entries = _service.EntriesPaidCount(cls), buybacks = _service.BuybacksPaidCount(cls);
                TotalsPanel.Children.Add(TotalLine(
                    $"{cls} pot: {entries} {(entries == 1 ? "entry" : "entries")} {Dollars(entries * price.Entry)}" +
                    $" + {buybacks} {(buybacks == 1 ? "buyback" : "buybacks")} {Dollars(buybacks * price.Buyback)}" +
                    $" = {Dollars(_service.PotFor(cls))}", strong: true));
            }
            TotalsPanel.Children.Add(TotalLine($"Total taken: {Dollars(_service.Total)}"));
        }

        private TextBlock TotalLine(string text, bool strong = false) => new TextBlock
        {
            Text = text,
            FontSize = (double)FindResource(strong ? "FontSize.Md" : "FontSize.Sm"),
            FontWeight = strong ? FontWeights.Medium : FontWeights.Normal,
            Foreground = (System.Windows.Media.Brush)FindResource(strong ? "Brush.TextPrimary" : "Brush.TextMuted"),
            Margin = new Thickness(0, 1, 0, 1)
        };

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
            if (!((sender as FrameworkElement)?.DataContext is MoneyRow row)) return;
            _service.Remove(row.Source);
            Reload();
            Save();
        }

        private static string Money(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);
        private static string Dollars(decimal v) => "$" + Money(v);

        // ── Row model ─────────────────────────────────────────────────────────

        /// <summary>One sheet row as the grid sees it.</summary>
        public sealed class MoneyRow : INotifyPropertyChanged
        {
            private readonly MoneySheetService _service;
            private readonly Action _changed;

            public MoneyRow(MoneySheetEntry entry, List<string> classes, MoneySheetService service, Action changed)
            {
                Source = entry;
                _service = service;
                _changed = changed;
                Entry = new PaidFlags(entry.EntriesPaid, classes, Changed);
                Buyback = new PaidFlags(entry.BuybacksPaid, classes, Changed);
            }

            internal MoneySheetEntry Source { get; }

            public string DriverName => Source.DriverName;
            public string CarName => Source.CarName;

            public bool TrackFee
            {
                get => MoneySheetService.TrackFeePaid(Source);
                set
                {
                    if (value == TrackFee) return;
                    MoneySheetService.SetTrackFeePaid(Source, value);
                    OnPropertyChanged();
                    Changed();
                }
            }

            public PaidFlags Entry { get; }
            public PaidFlags Buyback { get; }

            public string TotalText => Dollars(_service.TotalFor(Source));

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
