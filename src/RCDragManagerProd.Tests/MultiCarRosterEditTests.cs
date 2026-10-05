using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Race day 2026-10-03: a driver was added to a Multi-Car Round Robin class from the
/// console's roster dialog. The roster sync keyed car entries by the person's database
/// id, so most cars lost their race entry id, the scheduler saw two cars, and the
/// class collapsed to one race. These tests drive the same path the console takes:
/// the roster as the console builds it, the roster service in car-entry mode, the
/// session sync, then bracket generation.
/// </summary>
[TestClass]
[DoNotParallelize]
public class MultiCarRosterEditTests
{
    private const int Cars = 16;

    [TestMethod]
    public void AddingACar_KeepsEveryEntryId_AndTheFullSchedule()
    {
        using var db = new TempDb();
        var newcomer = db.AddDriver("jeff", "Outlaw", 2.34);
        var session = MultiCarSession(Cars);
        var roster = ConsoleRoster(session);

        var service = new RaceRosterService(db.Drivers) { CarEntries = true };
        var added = service.AddFromDatabase(newcomer, roster);
        Assert.IsTrue(added.Success, added.Error);

        new SessionRosterService().SyncSession(session, roster);

        Assert.AreEqual(Cars + 1, session.DriverEntries.Count);
        Assert.IsTrue(session.DriverEntries.All(e => e.RaceEntryId > 0), "Every car must keep a race entry id.");
        Assert.AreEqual(Cars + 1, session.DriverEntries.Select(e => e.RaceEntryId).Distinct().Count());

        var jeff = session.DriverEntries.Single(e => e.RaceEntryId == added.Driver.Id);
        Assert.AreEqual(newcomer.Id, jeff.DriverID, "The new car must credit the real driver.");
        Assert.AreEqual("jeff", jeff.DriverName);
        Assert.AreEqual("Outlaw", jeff.CarName);

        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket(RaceTypes.MultiCarRoundRobin, roster);

        // 17 cars, 3 rounds: 8 races and a bye each round.
        var races = controller.BuildCurrentBracketRows()
            .Count(r => !r.IsHeader && !string.IsNullOrEmpty(r.Driver1) && !string.IsNullOrEmpty(r.Driver2) &&
                        r.Driver1 != "BYE" && r.Driver2 != "BYE");
        Assert.AreEqual(24, races, "A 17-car, 3-round class must schedule 24 races, not collapse.");
    }

    [TestMethod]
    public void RosterSync_KeepsDialIns_AndDoesNotStackCarNames()
    {
        var session = MultiCarSession(Cars);
        session.DriverEntries[0].DialIn = 2.22;
        var roster = ConsoleRoster(session);

        new SessionRosterService().SyncSession(session, roster);
        new SessionRosterService().SyncSession(session, ConsoleRoster(session));

        Assert.AreEqual(2.22, session.DriverEntries[0].DialIn);
        Assert.AreEqual("Owner 1", session.DriverEntries[0].DriverName);
    }

    [TestMethod]
    public void CarMode_AddsADriverWhoseIdMatchesAnEntryId()
    {
        using var db = new TempDb();
        var session = MultiCarSession(Cars);
        var roster = ConsoleRoster(session);
        var dbDriver = db.AddDriver("Lowday", "Truck", null);
        Assert.IsTrue(roster.Any(r => r.Id == dbDriver.Id), "Test needs a database id that matches an entry id.");

        var result = new RaceRosterService(db.Drivers) { CarEntries = true }.AddFromDatabase(dbDriver, roster);

        Assert.IsTrue(result.Success, result.Error);
        Assert.AreEqual("Lowday — Truck", result.Driver.Name);
    }

    [TestMethod]
    public void CarMode_RefusesAWholeDriverWhenAllTheirCarsAreRacing()
    {
        using var db = new TempDb();
        var dbDriver = db.AddDriver("Ben", "Pinto", null);
        var roster = new List<Driver>();
        var service = new RaceRosterService(db.Drivers) { CarEntries = true };

        Assert.IsTrue(service.AddFromDatabase(dbDriver, roster).Success);
        var second = service.AddFromDatabase(dbDriver, roster);

        Assert.IsFalse(second.Success);
        Assert.AreEqual(1, roster.Count);
    }

    [TestMethod]
    public void CarMode_BlocksRename_SoTheWrongDatabaseDriverIsNeverRenamed()
    {
        using var db = new TempDb();
        var roster = new List<Driver> { new Driver { Id = 1, Name = "Owner 1 — Car 1" } };

        var error = new RaceRosterService(db.Drivers) { CarEntries = true }.Rename(roster[0], "New", "", roster);

        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void OwnerName_StripsStackedCarSuffixes()
    {
        Assert.AreEqual("Adam Briar", MultiCarNaming.OwnerName("Adam Briar — DYO — DYO", "DYO"));
        Assert.AreEqual("Adam Briar", MultiCarNaming.OwnerName("Adam Briar", "DYO"));
        Assert.AreEqual("Solo", MultiCarNaming.OwnerName("Solo", ""));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static RaceSession MultiCarSession(int cars) => new RaceSession
    {
        EventName = "QA Multi-Car Roster",
        EventDate = new DateTime(2026, 10, 5),
        RaceType = RaceTypes.MultiCarRoundRobin,
        ClassType = "DYO",
        RoundRobinVariant = "QMDRA",
        RoundsToRun = 3,
        DriverEntries = Enumerable.Range(1, cars).Select(i => new RaceSessionDriverEntry
        {
            RaceEntryId = i,
            DriverID = 100 + (i + 1) / 2,
            DriverName = "Owner " + ((i + 1) / 2),
            CarID = 500 + i,
            CarName = "Car " + i
        }).ToList()
    };

    /// <summary>The roster exactly as RaceConsoleView builds it for a Multi-Car class.</summary>
    private static List<Driver> ConsoleRoster(RaceSession session) =>
        session.DriverEntries.Select(e => new Driver
        {
            Id = e.RaceEntryId,
            Name = MultiCarNaming.Display(MultiCarNaming.OwnerName(e.DriverName, e.CarName), e.CarName),
            QualTime = e.QualifyingTime
        }).ToList();

    private sealed class TempDb : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), $"rcdm-mcroster-{Guid.NewGuid():N}.db");
        public DriverRepository Drivers { get; }

        public TempDb()
        {
            var cs = $"Data Source={_path};Version=3;";
            DatabaseInitializer.InitializeDatabase(cs);
            Drivers = new DriverRepository(cs);
        }

        public Driver AddDriver(string name, string carName, double? dialIn)
        {
            Drivers.AddDriver(new Driver
            {
                Name = name, Notes = "", State = "",
                Cars = new List<Car> { new Car { CarName = carName, ClassType = "DYO", DefaultDialIn = dialIn } }
            });
            return Drivers.GetAllDrivers().Single(d => d.Name == name);
        }

        public void Dispose()
        {
            try { if (File.Exists(_path)) File.Delete(_path); } catch { /* best-effort */ }
        }
    }
}
