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
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.WPF.Dialogs;

namespace RCDragManagerProd.WPF.Views
{
    /// <summary>What the money sheet is being used for.</summary>
    public enum MoneySheetMode
    {
        /// <summary>Setup screen, before racing. Every driver and car is listed; ticking a
        /// class entry takes the money and puts the car into the class.</summary>
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
        private ICollectionView _view;

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

            // In the event the sheet holds whoever paid at setup; add anyone entered
            // since (or an older event with no sheet) so their buybacks can be taken.
            if (!entries) _service.AddEveryoneFromClasses();

            BuildPrices();
            BuildColumns();
            _view = CollectionViewSource.GetDefaultView(_rows);
            _view.Filter = MatchesSearch;
            DgMoney.ItemsSource = _view;
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
            DgMoney.Columns.Add(TickColumn("Track fee", nameof(MoneyRow.TrackFee), nameof(MoneyRow.TrackFeeTotal),
                editable: entries, visibilityPath: nameof(MoneyRow.TrackFeeVisibility)));

            for (int i = 0; i < _classes.Count; i++)
            {
                DgMoney.Columns.Add(TickColumn($"{_classes[i].Name} entry",
                    $"{nameof(MoneyRow.Entry)}[{i}]", $"{nameof(MoneyRow.EntryTotal)}[{i}]", editable: entries));
                if (!entries && _classes[i].HasBuybacks)
                    DgMoney.Columns.Add(TickColumn($"{_classes[i].Name} buyback",
                        $"{nameof(MoneyRow.Buyback)}[{i}]", $"{nameof(MoneyRow.BuybackTotal)}[{i}]", editable: true));
            }

            DgMoney.Columns.Add(new DataGridTextColumn { Header = "Paid", Binding = new Binding(nameof(MoneyRow.TotalText)), IsReadOnly = true, Width = 80 });
        }

        /// <summary>A tick box for each competitor; on the total row, the column's total.</summary>
        private static DataGridTemplateColumn TickColumn(string header, string tickPath, string totalPath, bool editable,
                                                         string visibilityPath = null)
        {
            var root = new FrameworkElementFactory(typeof(Grid));

            var tick = new FrameworkElementFactory(typeof(CheckBox));
            tick.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,
                new Binding(tickPath) { Mode = editable ? BindingMode.TwoWay : BindingMode.OneWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            tick.SetValue(IsEnabledProperty, editable);
            tick.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            tick.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            tick.SetBinding(VisibilityProperty, new Binding(visibilityPath ?? nameof(MoneyRow.TickVisibility)));
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

        // ── Rows, search and totals ───────────────────────────────────────────

        private void Reload()
        {
            _rows.Clear();
            var names = _classes.Select(c => c.Name).ToList();
            foreach (var e in _service.Sheet.Entries
                         .OrderBy(x => x.DriverName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.CarName, StringComparer.OrdinalIgnoreCase))
                _rows.Add(new MoneyRow(e, names, _service, OnRowChanged, ShowError));
            _rows.Add(MoneyRow.TotalRow(names, _service));
            UpdateTotals();
        }

        private bool MatchesSearch(object item)
        {
            var row = item as MoneyRow;
            var text = (TxtSearch?.Text ?? "").Trim();
            if (row == null || row.Source == null || text.Length == 0) return true;
            return (row.DriverName ?? "").IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (row.CarName ?? "").IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => _view?.Refresh();

        private void OnRowChanged()
        {
            LblMessage.Text = "";
            UpdateTotals();
            Save();
        }

        private void ShowError(string message) => LblMessage.Text = message;

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

        // ── New driver ────────────────────────────────────────────────────────

        private void BtnNewDriver_Click(object sender, RoutedEventArgs e)
        {
            if (_drivers == null) return;
            var dlg = new QuickAddDriverDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;

            var car = new Car { CarName = dlg.CarName, ClassType = dlg.ClassType, DefaultDialIn = dlg.DialIn };
            new MultiClassSetupService(_drivers).QuickAddDriver(dlg.DriverName, car);
            _service.AddEveryoneFromDatabase(_drivers.GetAllDrivers());
            Reload();
            TxtSearch.Text = dlg.DriverName;
            LblMessage.Text = $"Added {dlg.DriverName}. Tick what they have paid.";
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

            public MoneyRow(MoneySheetEntry entry, List<string> classes, MoneySheetService service, Action changed,
                            Action<string> error)
            {
                Source = entry;
                _classes = classes;
                _service = service;
                _changed = changed;

                var entriesPaid = entry?.EntriesPaid ?? new List<string>();
                // Entry ticks go through the service: at setup a tick also puts the car
                // into the class, and can be refused (e.g. already in with another car).
                Entry = new PaidFlags(entriesPaid, classes, Changed,
                    (cls, paid) => entry == null ? null : _service.SetEntryPaid(entry, cls, paid), error);
                Buyback = new PaidFlags(entry?.BuybacksPaid ?? new List<string>(), classes, Changed, null, error);
                EntryTotal = new ColumnTotals(i => Dollars(_service.EntriesPaidCount(_classes[i]) * _service.PriceFor(_classes[i]).Entry));
                BuybackTotal = new ColumnTotals(i => Dollars(_service.BuybacksPaidCount(_classes[i]) * _service.PriceFor(_classes[i]).Buyback));
            }

            public static MoneyRow TotalRow(List<string> classes, MoneySheetService service) =>
                new MoneyRow(null, classes, service, null, null);

            internal MoneySheetEntry Source { get; }
            private bool IsTotal => Source == null;

            public string DriverName => IsTotal ? "Total" : Source.DriverName;
            public string CarName => IsTotal ? "" : Source.CarName;

            public Visibility TickVisibility => IsTotal ? Visibility.Collapsed : Visibility.Visible;
            public Visibility TotalVisibility => IsTotal ? Visibility.Visible : Visibility.Collapsed;

            /// <summary>The track fee is per driver: only their first car's row has the tick.</summary>
            public Visibility TrackFeeVisibility =>
                !IsTotal && _service.TakesTrackFee(Source) ? Visibility.Visible : Visibility.Collapsed;

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
            private readonly Func<string, bool, string> _set;
            private readonly Action<string> _error;

            public PaidFlags(List<string> paid, List<string> classes, Action changed,
                             Func<string, bool, string> set, Action<string> error)
            {
                _paid = paid;
                _classes = classes;
                _changed = changed;
                _set = set;
                _error = error;
            }

            public bool this[int index]
            {
                get => index >= 0 && index < _classes.Count && MoneySheetService.IsPaid(_paid, _classes[index]);
                set
                {
                    if (index < 0 || index >= _classes.Count || this[index] == value) return;
                    var problem = _set != null ? _set(_classes[index], value) : null;
                    if (_set == null) MoneySheetService.SetPaid(_paid, _classes[index], value);
                    // Refused ticks spring back.
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                    if (problem != null) { _error?.Invoke(problem); return; }
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
