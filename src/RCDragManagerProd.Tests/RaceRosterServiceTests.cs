using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="RaceRosterService"/> — pre-race roster editing on the console:
/// pull an existing driver in from the driver database, create a late entry who is
/// not in it yet, or drop a driver from the race. Runs headless against a temporary
/// database, because the whole point of the service is that roster ids are real
/// database ids.
/// </summary>
[TestClass]
public class RaceRosterServiceTests
{
    private static RaceRosterService NewService(TemporarySqliteDb db)
    {
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        return new RaceRosterService(new DriverRepository(db.ConnectionString));
    }

    private static Driver SeedDriver(TemporarySqliteDb db, string name, double? defaultDialIn = null)
    {
        var repo = new DriverRepository(db.ConnectionString);
        var driver = new Driver
        {
            Name = name,
            State = "VIC",
            Cars = new List<Car> { new Car { CarName = $"{name}'s car", ClassType = "Open", DefaultDialIn = defaultDialIn } }
        };
        repo.AddDriver(driver);
        return repo.GetAllDrivers().First(d => d.Name == name);
    }

    // ── AddFromDatabase ───────────────────────────────────────────────────────

    [TestMethod]
    public void AddFromDatabase_KeepsTheDatabaseId()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var seeded = SeedDriver(db, "Ash Drummond");
        var roster = new List<Driver>();

        var result = svc.AddFromDatabase(seeded, roster);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, roster.Count);
        Assert.AreEqual(seeded.Id, roster[0].Id,
            "A roster driver must keep their database id, or end-of-event stats land on the wrong driver.");
    }

    [TestMethod]
    public void AddFromDatabase_CarriesTheCarSoDialInsCanBeSeeded()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver>();

        svc.AddFromDatabase(SeedDriver(db, "Ash Drummond", 4.500), roster);

        Assert.AreEqual(4.500, roster[0].Cars.Single().DefaultDialIn);
    }

    [TestMethod]
    public void AddFromDatabase_DoesNotSeedTheRaceQualTime()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var seeded = SeedDriver(db, "Ash Drummond");
        seeded.QualTime = 3.100;   // a leftover from some earlier event
        var roster = new List<Driver>();

        svc.AddFromDatabase(seeded, roster);

        Assert.IsNull(roster[0].QualTime, "Qual time is a per-race value, not a database field.");
    }

    [TestMethod]
    public void AddFromDatabase_AlreadyRacing_RejectsAndLeavesRosterUntouched()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var seeded = SeedDriver(db, "Ash Drummond");
        var roster = new List<Driver>();
        svc.AddFromDatabase(seeded, roster);

        var result = svc.AddFromDatabase(seeded, roster);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(1, roster.Count);
        StringAssert.Contains(result.Error, "already in this race");
    }

    [TestMethod]
    public void AddFromDatabase_SameNameDifferentRow_IsStillRejected()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var first = SeedDriver(db, "Ash Drummond");
        var duplicate = SeedDriver(db, "ash drummond");
        var roster = new List<Driver>();
        svc.AddFromDatabase(first, roster);

        var result = svc.AddFromDatabase(duplicate, roster);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(1, roster.Count);
    }

    // ── CreateAndAdd ──────────────────────────────────────────────────────────

    [TestMethod]
    public void CreateAndAdd_NewName_PersistsToTheDatabaseAndUsesThatId()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver>();

        var result = svc.CreateAndAdd("Ash Drummond", "3.220", roster);

        Assert.IsTrue(result.Success);
        var stored = svc.GetDatabaseDrivers().Single(d => d.Name == "Ash Drummond");
        Assert.AreEqual(stored.Id, roster[0].Id);
        Assert.AreEqual(3.220, roster[0].QualTime);
    }

    [TestMethod]
    public void CreateAndAdd_NameAlreadyInDatabase_ReusesTheRowInsteadOfDuplicating()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var seeded = SeedDriver(db, "Ash Drummond");
        var roster = new List<Driver>();

        var result = svc.CreateAndAdd("ash drummond", "", roster);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(seeded.Id, roster[0].Id);
        Assert.AreEqual(1, svc.GetDatabaseDrivers().Count,
            "Typing an existing driver's name must not create a second database row for them.");
    }

    [TestMethod]
    public void CreateAndAdd_NameAlreadyOnRoster_UpdatesQualInsteadOfDuplicating()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver>();
        svc.CreateAndAdd("Ash Drummond", "3.220", roster);

        var result = svc.CreateAndAdd("ASH DRUMMOND", "3.100", roster);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.WasExisting);
        Assert.AreEqual(1, roster.Count);
        Assert.AreEqual(3.100, roster[0].QualTime);
    }

    [TestMethod]
    public void CreateAndAdd_BlankName_FailsAndWritesNothing()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver>();

        var result = svc.CreateAndAdd("   ", "", roster);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, roster.Count);
        Assert.AreEqual(0, svc.GetDatabaseDrivers().Count);
    }

    [TestMethod]
    public void CreateAndAdd_InvalidQualTime_FailsAndWritesNothing()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver>();

        var result = svc.CreateAndAdd("Ash Drummond", "not-a-number", roster);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, roster.Count);
        Assert.AreEqual(0, svc.GetDatabaseDrivers().Count,
            "A rejected entry must not leave a half-created driver behind.");
    }

    [TestMethod]
    public void CreateAndAdd_TwoNewDrivers_GetDistinctIds()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver>();

        svc.CreateAndAdd("Ash Drummond", "", roster);
        svc.CreateAndAdd("Bev Nolan", "", roster);

        Assert.AreEqual(2, roster.Select(d => d.Id).Distinct().Count());
    }

    // ── Rename ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Rename_WritesThroughToTheDatabase()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver>();
        svc.AddFromDatabase(SeedDriver(db, "Ash Drumond"), roster);

        var error = svc.Rename(roster[0], "Ash Drummond", "NSW", roster);

        Assert.IsNull(error);
        Assert.AreEqual("Ash Drummond", roster[0].Name);
        var stored = svc.GetDatabaseDrivers().Single();
        Assert.AreEqual("Ash Drummond", stored.Name);
        Assert.AreEqual("NSW", stored.State);
    }

    [TestMethod]
    public void Rename_ClashWithAnotherRacer_IsRejected()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver>();
        svc.AddFromDatabase(SeedDriver(db, "Ash Drummond"), roster);
        svc.AddFromDatabase(SeedDriver(db, "Bev Nolan"), roster);
        var bev = roster.Single(d => d.Name == "Bev Nolan");

        var error = svc.Rename(bev, "ash drummond", "", roster);

        Assert.IsNotNull(error);
        Assert.AreEqual("Bev Nolan", bev.Name);
    }

    [TestMethod]
    public void Rename_BlankName_IsRejected()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver>();
        svc.AddFromDatabase(SeedDriver(db, "Ash Drummond"), roster);

        Assert.IsNotNull(svc.Rename(roster[0], "  ", "", roster));
        Assert.AreEqual("Ash Drummond", roster[0].Name);
    }

    // ── Remove ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Remove_DropsFromTheRaceButNotFromTheDatabase()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver>();
        svc.AddFromDatabase(SeedDriver(db, "Ash Drummond"), roster);

        Assert.IsTrue(svc.Remove(roster[0], roster));

        Assert.AreEqual(0, roster.Count);
        Assert.AreEqual(1, svc.GetDatabaseDrivers().Count,
            "Taking a driver out of a race must never delete them from the database.");
    }

    [TestMethod]
    public void Remove_DriverNotOnRoster_ReturnsFalse()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver>();

        Assert.IsFalse(svc.Remove(new Driver { Id = 99, Name = "Nobody" }, roster));
    }

    // ── ValidateRoster ────────────────────────────────────────────────────────

    [TestMethod]
    public void ValidateRoster_FewerThanTwoDrivers_ReturnsError()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);

        Assert.IsNotNull(svc.ValidateRoster(new List<Driver>()));
        Assert.IsNotNull(svc.ValidateRoster(new List<Driver> { new Driver { Name = "Solo" } }));
    }

    [TestMethod]
    public void ValidateRoster_TwoDrivers_IsAccepted()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var roster = new List<Driver> { new Driver { Name = "A" }, new Driver { Name = "B" } };

        Assert.IsNull(svc.ValidateRoster(roster));
    }

    // ── Test infrastructure ───────────────────────────────────────────────────

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-raceroster-svc-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); } catch { }
        }
    }
}
