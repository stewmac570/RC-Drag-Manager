using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// Rules for the event money sheet, so the Money tab holds no logic: filling it
    /// from the classes, adding a driver, totals, and saving it with the event.
    /// </summary>
    public sealed class MoneySheetService
    {
        private readonly MultiClassEvent _event;
        private readonly MultiClassEventRepository _repo;

        public MoneySheetService(MultiClassEvent evt, MultiClassEventRepository repo)
        {
            _event = evt ?? throw new ArgumentNullException(nameof(evt));
            _repo = repo;
            if (_event.MoneySheet == null) _event.MoneySheet = new EventMoneySheet();
        }

        public EventMoneySheet Sheet => _event.MoneySheet;

        /// <summary>The event's class names, in tab order.</summary>
        public List<string> ClassNames =>
            _event.ClassSessions.Select((s, i) => string.IsNullOrWhiteSpace(s.ClassType) ? $"Class {i + 1}" : s.ClassType)
                                .ToList();

        /// <summary>
        /// Adds a row for every driver and car entered in the event's classes that is
        /// not on the sheet yet. Returns how many rows were added.
        /// </summary>
        public int AddEveryoneFromClasses()
        {
            int added = 0;
            foreach (var session in _event.ClassSessions)
                foreach (var e in session.DriverEntries ?? new List<RaceSessionDriverEntry>())
                {
                    if (e == null || e.DriverID <= 0) continue;
                    var car = e.CarName ?? "";
                    var name = MultiCarNaming.OwnerName(e.DriverName, car);
                    if (Find(e.DriverID, car) != null) continue;
                    Sheet.Entries.Add(new MoneySheetEntry { DriverId = e.DriverID, DriverName = name, CarName = car });
                    added++;
                }
            Logger.Log($"[MONEY] Added {added} row(s) from the event's classes.");
            return added;
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

        public decimal TotalFor(MoneySheetEntry e) =>
            e.TrackDaysPaid * Sheet.TrackFee +
            (e.EntriesPaid?.Count ?? 0) * Sheet.EntryFee +
            (e.BuybacksPaid?.Count ?? 0) * Sheet.BuybackFee;

        public decimal Total => Sheet.Entries.Sum(TotalFor);

        /// <summary>Saves the sheet with the event. Returns an operator-facing error, or null.</summary>
        public string Save()
        {
            if (_repo == null) return null;
            try
            {
                _repo.SaveEvent(_event);
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
}
