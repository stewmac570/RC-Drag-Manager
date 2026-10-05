using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Money comes first at setup: the classes exist with no drivers, the money sheet
/// lists every driver and car, and ticking a class entry puts that car into the class.
/// </summary>
[TestClass]
public class MoneyFirstSetupTests
{
    [TestMethod]
    public void TickingEntries_FillsTheClasses()
    {
        using var db = new TempDb();
        var ben = db.Add("Ben", "Truck", "Pinto");
        var chris = db.Add("Chris", "Outlaw");
        var setup = new MultiClassSetupService(db.Drivers);
        var multi = new ClassConfigDto { ClassName = "DYO", RaceType = RaceTypes.MultiCarRoundRobin, ClassType = "Dial-In", DriverEntries = new List<RaceSessionDriverEntry>() };
        var single = new ClassConfigDto { ClassName = "Outlaw", RaceType = RaceTypes.RoundRobin, ClassType = "Heads Up", DriverEntries = new List<RaceSessionDriverEntry>() };
        var configs = new List<ClassConfigDto> { multi, single };

        var money = Sheet(setup, configs, db);
        Assert.AreEqual(3, money.Sheet.Entries.Count, "One row per car: Ben x2, Chris x1.");

        foreach (var row in money.Sheet.Entries.Where(r => r.DriverName == "Ben"))
            Assert.IsNull(money.SetEntryPaid(row, "DYO", true));
        Assert.IsNull(money.SetEntryPaid(money.Sheet.Entries.Single(r => r.DriverName == "Chris"), "Outlaw", true));

        Assert.AreEqual(2, multi.DriverEntries.Count, "Both of Ben's cars are in the multi-car class.");
        Assert.IsTrue(multi.DriverEntries.All(e => e.RaceEntryId > 0));
        Assert.AreEqual(1, single.DriverEntries.Count);
        Assert.AreEqual(2, money.EntriesPaidCount("DYO"));
    }

    [TestMethod]
    public void SingleCarClass_RefusesASecondCar_AndUntickingTakesTheDriverOut()
    {
        using var db = new TempDb();
        db.Add("Ben", "Truck", "Pinto");
        var setup = new MultiClassSetupService(db.Drivers);
        var single = new ClassConfigDto { ClassName = "Outlaw", RaceType = RaceTypes.RoundRobin, ClassType = "Heads Up", DriverEntries = new List<RaceSessionDriverEntry>() };
        var money = Sheet(setup, new List<ClassConfigDto> { single }, db);
        var truck = money.Sheet.Entries.Single(r => r.CarName == "Truck");
        var pinto = money.Sheet.Entries.Single(r => r.CarName == "Pinto");

        Assert.IsNull(money.SetEntryPaid(truck, "Outlaw", true));
        var refused = money.SetEntryPaid(pinto, "Outlaw", true);

        Assert.IsNotNull(refused, "A driver can enter a single-car class once.");
        Assert.IsFalse(MoneySheetService.IsPaid(pinto.EntriesPaid, "Outlaw"), "A refused tick is not paid.");
        Assert.AreEqual("Truck", single.DriverEntries.Single().CarName, "The ticked car is the one entered.");

        Assert.IsNull(money.SetEntryPaid(truck, "Outlaw", false));
        Assert.AreEqual(0, single.DriverEntries.Count);
    }

    [TestMethod]
    public void TrackFee_IsOncePerDriver_AndUnpaidRowsAreDroppedAtStart()
    {
        using var db = new TempDb();
        db.Add("Ben", "Truck", "Pinto");
        db.Add("Chris", "Outlaw");
        var money = Sheet(new MultiClassSetupService(db.Drivers), new List<ClassConfigDto>(), db);

        Assert.AreEqual(1, money.Sheet.Entries.Count(r => r.DriverName == "Ben" && money.TakesTrackFee(r)));
        MoneySheetService.SetTrackFeePaid(money.Sheet.Entries.First(r => r.DriverName == "Ben"), true);

        Assert.AreEqual(2, money.DropUnpaidRows());
        Assert.AreEqual("Ben", money.Sheet.Entries.Single().DriverName);
    }

    private static MoneySheetService Sheet(MultiClassSetupService setup, List<ClassConfigDto> configs, TempDb db)
    {
        var money = new MoneySheetService(new EventMoneySheet(), () => configs
            .Select(c => new MoneySheetClass(c.ClassName, c.DriverEntries, false)).ToList());
        money.AddEveryoneFromDatabase(db.Drivers.GetAllDrivers());
        money.EntryChanging = (row, cls, entered) =>
        {
            var all = setup.GetAllDrivers();
            var driver = all.First(d => d.Id == row.DriverId);
            var car = driver.Cars.FirstOrDefault(c => c.CarID == row.CarId);
            return setup.SetEntered(configs.First(c => c.ClassName == cls), driver, car, entered, all);
        };
        return money;
    }

    private sealed class TempDb : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), $"rcdm-moneyfirst-{Guid.NewGuid():N}.db");
        public DriverRepository Drivers { get; }

        public TempDb()
        {
            var cs = $"Data Source={_path};Version=3;";
            DatabaseInitializer.InitializeDatabase(cs);
            Drivers = new DriverRepository(cs);
        }

        public Driver Add(string name, params string[] cars)
        {
            Drivers.AddDriver(new Driver
            {
                Name = name, Notes = "", State = "",
                Cars = cars.Select(c => new Car { CarName = c, ClassType = "DYO", DefaultDialIn = 2.5 }).ToList()
            });
            return Drivers.GetAllDrivers().Single(d => d.Name == name);
        }

        public void Dispose()
        {
            try { if (File.Exists(_path)) File.Delete(_path); } catch { /* best-effort */ }
        }
    }
}
