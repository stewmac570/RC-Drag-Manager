using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="MultiClassSetupService"/> (issue #287) — the UI-independent race setup
/// and class configuration operations the Multi-Class Setup and Config Dialog screens delegate
/// to. Runs headless against an in-memory database.
/// </summary>
[TestClass]
public class MultiClassSetupServiceTests
{
    private static MultiClassSetupService NewService(TemporarySqliteDb db)
    {
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        return new MultiClassSetupService(new DriverRepository(db.ConnectionString));
    }

    // ── GetAllDrivers ─────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAllDrivers_EmptyDb_ReturnsEmptyList()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        Assert.AreEqual(0, svc.GetAllDrivers().Count);
    }

    // ── QuickAddDriver ────────────────────────────────────────────────────────

    [TestMethod]
    public void QuickAddDriver_PersistsDriverWithCar()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        svc.QuickAddDriver("Jake Miller", new Car { CarName = "Rocket", ClassType = "Pro", DefaultDialIn = 3.9 });

        var drivers = svc.GetAllDrivers();
        Assert.AreEqual(1, drivers.Count);
        Assert.AreEqual("Jake Miller", drivers[0].Name);
        Assert.AreEqual(1, drivers[0].Cars.Count);
        Assert.AreEqual("Rocket", drivers[0].Cars[0].CarName);
    }

    // ── ValidateClassName ─────────────────────────────────────────────────────

    [TestMethod]
    public void ValidateClassName_BlankName_ReturnsError()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        Assert.IsNotNull(svc.ValidateClassName("", new[] { "Open" }));
        Assert.IsNotNull(svc.ValidateClassName("   ", new[] { "Open" }));
    }

    [TestMethod]
    public void ValidateClassName_UniqueNonEmpty_ReturnsNull()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        Assert.IsNull(svc.ValidateClassName("Pro Mod", new[] { "Open", "Bracket" }));
    }

    [TestMethod]
    public void ValidateClassName_DuplicateExactMatch_ReturnsError()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        Assert.IsNotNull(svc.ValidateClassName("Open", new[] { "Open", "Bracket" }));
    }

    [TestMethod]
    public void ValidateClassName_DuplicateCaseInsensitive_ReturnsError()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        Assert.IsNotNull(svc.ValidateClassName("open", new[] { "Open", "Bracket" }));
    }

    // ── ValidateCanStart ──────────────────────────────────────────────────────

    [TestMethod]
    public void ValidateCanStart_NoClasses_ReturnsError()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        var error = svc.ValidateCanStart(Array.Empty<ClassConfigDto>());

        Assert.IsNotNull(error);
        StringAssert.Contains(error, "at least one class");
    }

    [TestMethod]
    public void ValidateCanStart_AllClassesHaveDrivers_ReturnsNull()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        var classes = new[]
        {
            new ClassConfigDto { ClassName = "A", DriverEntries = new List<RaceSessionDriverEntry> { MakeEntry(1) } },
            new ClassConfigDto { ClassName = "B", DriverEntries = new List<RaceSessionDriverEntry> { MakeEntry(2) } }
        };

        Assert.IsNull(svc.ValidateCanStart(classes));
    }

    [TestMethod]
    public void ValidateCanStart_ClassWithZeroDrivers_ReturnsError()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        var classes = new[]
        {
            new ClassConfigDto { ClassName = "Empty", DriverEntries = new List<RaceSessionDriverEntry>() }
        };

        Assert.IsNotNull(svc.ValidateCanStart(classes));
    }

    [TestMethod]
    public void ValidateCanStart_SingleClassWithDrivers_ReturnsNull()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        var classes = new[]
        {
            new ClassConfigDto { ClassName = "Pro", DriverEntries = new List<RaceSessionDriverEntry> { MakeEntry(1) } }
        };

        Assert.IsNull(svc.ValidateCanStart(classes));
    }

    // ── BuildDriverEntries ────────────────────────────────────────────────────

    [TestMethod]
    public void BuildDriverEntries_HeadsUp_SetsDialInToNull()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        var drivers = new List<Driver>
        {
            new Driver { Id = 1, Name = "Ann", Cars = new List<Car> { new Car { CarID = 10, DefaultDialIn = 4.5 } } }
        };

        var entries = svc.BuildDriverEntries(new[] { 1 }, drivers, "Heads Up", null, null, "HU");

        Assert.AreEqual(1, entries.Count);
        Assert.IsNull(entries[0].DialIn);
    }

    [TestMethod]
    public void BuildDriverEntries_BracketClass_AppliesFixedDialIn()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        var drivers = new List<Driver>
        {
            new Driver { Id = 1, Name = "Bob", Cars = new List<Car> { new Car { CarID = 11, DefaultDialIn = 4.0 } } }
        };

        var entries = svc.BuildDriverEntries(new[] { 1 }, drivers, "Bracket Class", 3.85, null, "BK");

        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(3.85, entries[0].DialIn);
    }

    [TestMethod]
    public void BuildDriverEntries_DialIn_UsesPerDriverOverride()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        var drivers = new List<Driver>
        {
            new Driver { Id = 1, Name = "Carol", Cars = new List<Car> { new Car { CarID = 12, DefaultDialIn = 4.1 } } }
        };
        var overrides = new Dictionary<int, double?> { [1] = 3.92 };

        var entries = svc.BuildDriverEntries(new[] { 1 }, drivers, "Dial-In", null, overrides, "DI");

        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(3.92, entries[0].DialIn);
    }

    [TestMethod]
    public void BuildDriverEntries_DialIn_FallsBackToCarDefault_WhenNoOverride()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        var drivers = new List<Driver>
        {
            new Driver { Id = 2, Name = "Dan", Cars = new List<Car> { new Car { CarID = 13, DefaultDialIn = 4.25 } } }
        };

        var entries = svc.BuildDriverEntries(new[] { 2 }, drivers, "Dial-In", null, null, "DI");

        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(4.25, entries[0].DialIn);
    }

    [TestMethod]
    public void BuildDriverEntries_SetsClassNameOnEachEntry()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        var drivers = new List<Driver>
        {
            new Driver { Id = 1, Name = "Eve", Cars = new List<Car> { new Car { CarID = 14 } } }
        };

        var entries = svc.BuildDriverEntries(new[] { 1 }, drivers, "Heads Up", null, null, "Pro Mod");

        Assert.AreEqual("Pro Mod", entries[0].ClassType);
    }

    [TestMethod]
    public void BuildDriverEntries_SkipsIdNotInDriverList()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        var drivers = new List<Driver>
        {
            new Driver { Id = 1, Name = "Fred", Cars = new List<Car> { new Car { CarID = 15 } } }
        };

        var entries = svc.BuildDriverEntries(new[] { 1, 99 }, drivers, "Heads Up", null, null, "X");

        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(1, entries[0].DriverID);
    }

    // ── StartEvent ────────────────────────────────────────────────────────────

    [TestMethod]
    public void StartEvent_BuildsMultiClassEventWithOneSectionPerClass()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var repo = new DriverRepository(db.ConnectionString);
        var driver = repo.AddDriverAndReturn("Gina");

        var classes = new[]
        {
            new ClassConfigDto
            {
                ClassName    = "Open A",
                RaceType     = RaceTypes.RoundRobin,
                ClassType    = "Heads Up",
                Variant      = "Standard",
                RoundsToRun  = 3,
                DriverEntries = new List<RaceSessionDriverEntry> { MakeEntry(driver.Id) }
            },
            new ClassConfigDto
            {
                ClassName    = "Bracket B",
                RaceType     = RaceTypes.RoundRobin,
                ClassType    = "Bracket Class",
                FixedDialIn  = 4.0,
                Variant      = "QMDRA",
                RoundsToRun  = 2,
                DriverEntries = new List<RaceSessionDriverEntry> { MakeEntry(driver.Id) }
            }
        };

        var result = svc.StartEvent("Summer Nats", new DateTime(2026, 8, 10), classes);

        Assert.AreEqual("Summer Nats", result.EventName);
        Assert.AreEqual(new DateTime(2026, 8, 10), result.EventDate);
        Assert.AreEqual(2, result.ClassSessions.Count);
        Assert.AreEqual("Open A", result.ClassSessions[0].ClassType);
        Assert.AreEqual("Bracket B", result.ClassSessions[1].ClassType);
    }

    [TestMethod]
    public void StartEvent_MapsRoundRobinVariantAndRounds()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var repo = new DriverRepository(db.ConnectionString);
        var driver = repo.AddDriverAndReturn("Harry");

        var classes = new[]
        {
            new ClassConfigDto
            {
                ClassName    = "RR Class",
                RaceType     = RaceTypes.RoundRobin,
                ClassType    = "Heads Up",
                Variant      = "QMDRA",
                RoundsToRun  = 4,
                DriverEntries = new List<RaceSessionDriverEntry> { MakeEntry(driver.Id) }
            }
        };

        var result = svc.StartEvent("Test Event", DateTime.Today, classes);

        var session = result.ClassSessions[0];
        Assert.AreEqual("QMDRA", session.RoundRobinVariant);
        Assert.AreEqual(4, session.RoundsToRun);
    }

    [TestMethod]
    public void StartEvent_IncrementsEventsEnteredForEachDriver()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var repo = new DriverRepository(db.ConnectionString);
        var d1 = repo.AddDriverAndReturn("Ivan");
        var d2 = repo.AddDriverAndReturn("Julia");

        var classes = new[]
        {
            new ClassConfigDto
            {
                ClassName    = "Class A",
                RaceType     = RaceTypes.RoundRobin,
                Variant      = "Standard",
                RoundsToRun  = 3,
                DriverEntries = new List<RaceSessionDriverEntry> { MakeEntry(d1.Id), MakeEntry(d2.Id) }
            }
        };

        svc.StartEvent("Event", DateTime.Today, classes);

        Assert.AreEqual(1, repo.GetDriverById(d1.Id).EventsEntered);
        Assert.AreEqual(1, repo.GetDriverById(d2.Id).EventsEntered);
    }

    // ── RoundRobin variant mapping ─────────────────────────────────────────────

    [TestMethod]
    public void StartEvent_NonRRClass_ClearsVariantAndRounds()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var repo = new DriverRepository(db.ConnectionString);
        var driver = repo.AddDriverAndReturn("Karl");

        var classes = new[]
        {
            new ClassConfigDto
            {
                ClassName    = "Pro Ladder",
                RaceType     = "Pro Ladder",
                ClassType    = "Heads Up",
                Variant      = "Standard",
                RoundsToRun  = 3,
                DriverEntries = new List<RaceSessionDriverEntry> { MakeEntry(driver.Id) }
            }
        };

        var result = svc.StartEvent("Event", DateTime.Today, classes);
        var session = result.ClassSessions[0];

        Assert.IsNull(session.RoundRobinVariant);
        Assert.IsNull(session.RoundsToRun);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RaceSessionDriverEntry MakeEntry(int driverId) =>
        new RaceSessionDriverEntry { DriverID = driverId, DriverName = "Driver " + driverId };

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"rcdragmanager-multiclasssetup-svc-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); } catch { }
        }
    }
}

/// <summary>Extension to DriverRepository for test helpers only.</summary>
internal static class DriverRepositoryTestExtensions
{
    public static Driver AddDriverAndReturn(this DriverRepository repo, string name)
    {
        var driver = new Driver
        {
            Name  = name,
            Cars  = new List<Car> { new Car { CarName = "Car", ClassType = "Open" } }
        };
        repo.AddDriver(driver);
        return repo.GetAllDrivers().First(d => d.Name == name);
    }
}
