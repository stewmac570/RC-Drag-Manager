using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// Rules for the event money sheet, so the money screens hold no logic.
    ///
    /// Entry money (track fee and class entries) is taken on the setup screen before
    /// racing starts. Only buybacks are paid during the event. Each class's pot is its
    /// entry fees plus its buybacks; the track fee is kept separate and is never in a pot.
    /// </summary>
    public sealed class MoneySheetService
    {
        private readonly Func<List<MoneySheetClass>> _classes;
        private readonly Func<string> _save;

        /// <summary>For a running event: classes come from the event, saves go to the database.</summary>
        public MoneySheetService(MultiClassEvent evt, MultiClassEventRepository repo)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));
            if (evt.MoneySheet == null) evt.MoneySheet = new EventMoneySheet();
            Sheet = evt.MoneySheet;
            _classes = () => evt.ClassSessions
                .Select((s, i) => new MoneySheetClass(
                    string.IsNullOrWhiteSpace(s.ClassType) ? $"Class {i + 1}" : s.ClassType,
                    s.DriverEntries,
                    HasBuybacks(s.OriginalRaceType ?? s.RaceType, s.RoundRobinVariant)))
                .ToList();
            _save = () => SaveEvent(evt, repo);
        }

        /// <summary>For the setup screen: the event does not exist yet, so the classes
        /// are whatever has been configured so far and nothing is saved until it starts.</summary>
        public MoneySheetService(EventMoneySheet sheet, Func<List<MoneySheetClass>> classes)
        {
            Sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
            _classes = classes ?? throw new ArgumentNullException(nameof(classes));
            _save = () => null;
        }

        public EventMoneySheet Sheet { get; }

        /// <summary>The event's classes, in order.</summary>
        public List<MoneySheetClass> Classes => _classes();

        /// <summary>The event's class names, in order.</summary>
        public List<string> ClassNames => _classes().Select(c => c.Name).ToList();

        /// <summary>Only a Round Robin class with buybacks switched on takes buyback money.</summary>
        public static bool HasBuybacks(string raceType, string roundRobinVariant) =>
            RaceTypes.IsRoundRobinFormat(raceType) && EventSettingsService.BuybacksEnabledIn(roundRobinVariant);

        public bool ClassHasBuybacks(string className) =>
            _classes().Any(c => c.HasBuybacks && string.Equals(c.Name, className, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Adds a row for every driver and car entered in the classes that is not on the
        /// sheet yet. Returns how many rows were added.
        /// </summary>
        public int AddEveryoneFromClasses()
        {
            int added = 0;
            foreach (var cls in _classes())
                foreach (var e in cls.Entries ?? Enumerable.Empty<RaceSessionDriverEntry>())
                {
                    if (e == null || e.DriverID <= 0) continue;
                    var car = e.CarName ?? "";
                    if (Find(e.DriverID, car) != null) continue;
                    Sheet.Entries.Add(new MoneySheetEntry
                    {
                        DriverId = e.DriverID,
                        DriverName = MultiCarNaming.OwnerName(e.DriverName, car),
                        CarName = car,
                        CarId = e.CarID
                    });
                    added++;
                }
            if (added > 0) Logger.Log($"[MONEY] Added {added} row(s) from the classes.");
            return added;
        }

        /// <summary>
        /// Setup screen: one row for every car of every driver in the database (one row
        /// with no car for a driver who has none), so money can be taken before anyone
        /// is in a class. Returns how many rows were added.
        /// </summary>
        public int AddEveryoneFromDatabase(IEnumerable<Driver> drivers)
        {
            int added = 0;
            foreach (var d in drivers ?? Enumerable.Empty<Driver>())
            {
                if (d == null || d.Id <= 0) continue;
                var cars = (d.Cars ?? new List<Car>()).Where(c => c != null).ToList();
                if (cars.Count == 0) cars.Add(new Car { CarName = "" });
                foreach (var car in cars)
                {
                    var name = car.CarName ?? "";
                    if (Find(d.Id, name) != null) continue;
                    Sheet.Entries.Add(new MoneySheetEntry { DriverId = d.Id, DriverName = d.Name ?? "", CarName = name, CarId = car.CarID });
                    added++;
                }
            }
            return added;
        }

        /// <summary>
        /// Called before an entry tick changes, with the row, the class and the new state.
        /// On the setup screen this puts the car into the class (or takes it out); an
        /// error string stops the tick.
        /// </summary>
        public Func<MoneySheetEntry, string, bool, string> EntryChanging { get; set; }

        /// <summary>Ticks or unticks a class entry. Returns an operator-facing error, or null.</summary>
        public string SetEntryPaid(MoneySheetEntry row, string className, bool paid)
        {
            if (IsPaid(row.EntriesPaid, className) == paid) return null;
            var error = EntryChanging?.Invoke(row, className, paid);
            if (error != null) return error;
            SetPaid(row.EntriesPaid, className, paid);
            return null;
        }

        /// <summary>The track fee is per driver, not per car: only a driver's first row takes it.</summary>
        public bool TakesTrackFee(MoneySheetEntry row) =>
            Sheet.Entries
                .Where(e => e.DriverId == row.DriverId)
                .OrderBy(e => e.CarName ?? "", StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() == row;

        /// <summary>Drops rows with nothing paid, so the saved sheet holds only the people
        /// who paid, not the whole driver database.</summary>
        public int DropUnpaidRows() =>
            Sheet.Entries.RemoveAll(e => e.TrackDaysPaid == 0 &&
                                         (e.EntriesPaid?.Count ?? 0) == 0 &&
                                         (e.BuybacksPaid?.Count ?? 0) == 0);

        /// <summary>Whether this row's driver and car are entered in the named class.</summary>
        public bool IsEnteredIn(MoneySheetEntry row, string className)
        {
            var cls = _classes().FirstOrDefault(c => string.Equals(c.Name, className, StringComparison.OrdinalIgnoreCase));
            return cls?.Entries != null && cls.Entries.Any(e => e != null && e.DriverID == row.DriverId &&
                string.Equals(e.CarName ?? "", row.CarName ?? "", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Adds one driver and car. Returns an operator-facing error, or null.</summary>
        public string AddDriver(Driver driver, string carName)
        {
            if (driver == null) return "Select a driver to add.";
            carName = carName ?? "";
            if (Find(driver.Id, carName) != null)
                return $"{MultiCarNaming.Display(driver.Name, carName)} is already on the money sheet.";
            Sheet.Entries.Add(new MoneySheetEntry { DriverId = driver.Id, DriverName = driver.Name ?? "", CarName = carName });
            return null;
        }

        public void Remove(MoneySheetEntry entry) => Sheet.Entries.Remove(entry);

        public static void SetPaid(List<string> paid, string className, bool isPaid)
        {
            paid.RemoveAll(c => string.Equals(c, className, StringComparison.OrdinalIgnoreCase));
            if (isPaid) paid.Add(className);
        }

        public static bool IsPaid(List<string> paid, string className) =>
            paid != null && paid.Any(c => string.Equals(c, className, StringComparison.OrdinalIgnoreCase));

        // ── Prices ────────────────────────────────────────────────────────────

        /// <summary>The class's own prices, created from the sheet defaults the first
        /// time the class is priced.</summary>
        public MoneySheetClassPrice PriceFor(string className)
        {
            var price = Sheet.ClassPrices.FirstOrDefault(p =>
                string.Equals(p.ClassName, className, StringComparison.OrdinalIgnoreCase));
            if (price == null)
            {
                price = new MoneySheetClassPrice { ClassName = className, Entry = Sheet.EntryFee, Buyback = Sheet.BuybackFee };
                Sheet.ClassPrices.Add(price);
            }
            return price;
        }

        public static bool TrackFeePaid(MoneySheetEntry e) => e.TrackDaysPaid > 0;

        public static void SetTrackFeePaid(MoneySheetEntry e, bool paid) => e.TrackDaysPaid = paid ? 1 : 0;

        // ── Totals ────────────────────────────────────────────────────────────

        /// <summary>Everything this row has paid, track fee included.</summary>
        public decimal TotalFor(MoneySheetEntry e) =>
            e.TrackDaysPaid * Sheet.TrackFee +
            (e.EntriesPaid ?? new List<string>()).Sum(c => PriceFor(c).Entry) +
            (e.BuybacksPaid ?? new List<string>()).Where(ClassHasBuybacks).Sum(c => PriceFor(c).Buyback);

        public decimal Total => Sheet.Entries.Sum(TotalFor);

        /// <summary>Track fees taken. Kept separate: never part of a pot.</summary>
        public decimal TrackFees => Sheet.Entries.Sum(e => e.TrackDaysPaid) * Sheet.TrackFee;

        public int TrackFeesPaidCount => Sheet.Entries.Count(TrackFeePaid);

        public int EntriesPaidCount(string className) => Sheet.Entries.Count(e => IsPaid(e.EntriesPaid, className));

        public int BuybacksPaidCount(string className) =>
            ClassHasBuybacks(className) ? Sheet.Entries.Count(e => IsPaid(e.BuybacksPaid, className)) : 0;

        /// <summary>A class's pot: its paid entries plus its paid buybacks, at its own prices.</summary>
        public decimal PotFor(string className)
        {
            var price = PriceFor(className);
            return EntriesPaidCount(className) * price.Entry + BuybacksPaidCount(className) * price.Buyback;
        }

        /// <summary>Saves the sheet. Returns an operator-facing error, or null.</summary>
        public string Save() => _save();

        private static string SaveEvent(MultiClassEvent evt, MultiClassEventRepository repo)
        {
            if (repo == null) return null;
            try
            {
                repo.SaveEvent(evt);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log("[MONEY][FAIL] Save: " + ex);
                return "The money sheet could not be saved: " + ex.Message;
            }
        }

        private MoneySheetEntry Find(int driverId, string carName) =>
            Sheet.Entries.FirstOrDefault(x => x.DriverId == driverId &&
                string.Equals(x.CarName ?? "", carName ?? "", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>One class as the money sheet sees it: its name and who is entered.</summary>
    public sealed class MoneySheetClass
    {
        public MoneySheetClass(string name, IEnumerable<RaceSessionDriverEntry> entries, bool hasBuybacks = false)
        {
            Name = name ?? "";
            Entries = entries?.ToList() ?? new List<RaceSessionDriverEntry>();
            HasBuybacks = hasBuybacks;
        }

        public string Name { get; }

        /// <summary>True for a Round Robin class with buybacks on: it takes buyback money.</summary>
        public bool HasBuybacks { get; }
        public List<RaceSessionDriverEntry> Entries { get; }
    }
}
