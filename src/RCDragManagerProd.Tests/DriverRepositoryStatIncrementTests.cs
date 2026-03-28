using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Regression tests for the stat increment methods and the _allowedStatColumns whitelist
/// in DriverRepository (issue #101 fix).
///
/// ExecuteStatIncrement is private, so whitelist-rejection is tested via reflection.
/// All other tests use the public Increment* methods against an in-memory SQLite database
/// to verify correct column targeting and column isolation (no other stat column changes).
/// </summary>
[TestClass]
public class DriverRepositoryStatIncrementTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Driver AddFreshDriver(DriverRepository repo, string name = "Test Driver")
    {
        var driver = new Driver
        {
            Name = name,
            Notes = string.Empty,
            State = string.Empty,
            TotalWins = 0,
            TotalLosses = 0,
            EventsEntered = 0,
            EventsWon = 0
        };
        repo.AddDriver(driver);
        return driver;
    }

    // ── IncrementWins ─────────────────────────────────────────────────────────

    [TestMethod]
    public void IncrementWins_IncrementsTotalWins_AndLeavesOtherStatColumnsUnchanged()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);
        repo.IncrementWins(driver.Id);

        var reloaded = repo.GetDriverById(driver.Id)!;
        Assert.AreEqual(1, reloaded.TotalWins,     "TotalWins must be incremented by 1");
        Assert.AreEqual(0, reloaded.TotalLosses,   "TotalLosses must be unchanged");
        Assert.AreEqual(0, reloaded.EventsEntered, "EventsEntered must be unchanged");
        Assert.AreEqual(0, reloaded.EventsWon,     "EventsWon must be unchanged");
    }

    [TestMethod]
    public void IncrementWins_WithCustomDelta_IncrementsCorrectly()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);
        repo.IncrementWins(driver.Id, delta: 3);

        var reloaded = repo.GetDriverById(driver.Id)!;
        Assert.AreEqual(3, reloaded.TotalWins,
            "TotalWins must reflect the custom delta of 3");
    }

    // ── IncrementLosses ───────────────────────────────────────────────────────

    [TestMethod]
    public void IncrementLosses_IncrementsTotalLosses_AndLeavesOtherStatColumnsUnchanged()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);
        repo.IncrementLosses(driver.Id);

        var reloaded = repo.GetDriverById(driver.Id)!;
        Assert.AreEqual(0, reloaded.TotalWins,     "TotalWins must be unchanged");
        Assert.AreEqual(1, reloaded.TotalLosses,   "TotalLosses must be incremented by 1");
        Assert.AreEqual(0, reloaded.EventsEntered, "EventsEntered must be unchanged");
        Assert.AreEqual(0, reloaded.EventsWon,     "EventsWon must be unchanged");
    }

    [TestMethod]
    public void IncrementLosses_WithCustomDelta_IncrementsCorrectly()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);
        repo.IncrementLosses(driver.Id, delta: 2);

        var reloaded = repo.GetDriverById(driver.Id)!;
        Assert.AreEqual(2, reloaded.TotalLosses,
            "TotalLosses must reflect the custom delta of 2");
    }

    // ── IncrementEventsEntered ────────────────────────────────────────────────

    [TestMethod]
    public void IncrementEventsEntered_IncrementsEventsEntered_AndLeavesOtherStatColumnsUnchanged()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);
        repo.IncrementEventsEntered(driver.Id);

        var reloaded = repo.GetDriverById(driver.Id)!;
        Assert.AreEqual(0, reloaded.TotalWins,     "TotalWins must be unchanged");
        Assert.AreEqual(0, reloaded.TotalLosses,   "TotalLosses must be unchanged");
        Assert.AreEqual(1, reloaded.EventsEntered, "EventsEntered must be incremented by 1");
        Assert.AreEqual(0, reloaded.EventsWon,     "EventsWon must be unchanged");
    }

    [TestMethod]
    public void IncrementEventsEntered_WithCustomDelta_IncrementsCorrectly()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);
        repo.IncrementEventsEntered(driver.Id, delta: 5);

        var reloaded = repo.GetDriverById(driver.Id)!;
        Assert.AreEqual(5, reloaded.EventsEntered,
            "EventsEntered must reflect the custom delta of 5");
    }

    // ── IncrementEventsWon ────────────────────────────────────────────────────

    [TestMethod]
    public void IncrementEventsWon_IncrementsEventsWon_AndLeavesOtherStatColumnsUnchanged()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);
        repo.IncrementEventsWon(driver.Id);

        var reloaded = repo.GetDriverById(driver.Id)!;
        Assert.AreEqual(0, reloaded.TotalWins,     "TotalWins must be unchanged");
        Assert.AreEqual(0, reloaded.TotalLosses,   "TotalLosses must be unchanged");
        Assert.AreEqual(0, reloaded.EventsEntered, "EventsEntered must be unchanged");
        Assert.AreEqual(1, reloaded.EventsWon,     "EventsWon must be incremented by 1");
    }

    [TestMethod]
    public void IncrementEventsWon_WithCustomDelta_IncrementsCorrectly()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);
        repo.IncrementEventsWon(driver.Id, delta: 4);

        var reloaded = repo.GetDriverById(driver.Id)!;
        Assert.AreEqual(4, reloaded.EventsWon,
            "EventsWon must reflect the custom delta of 4");
    }

    // ── IncrementWinsAndLosses (atomic) ───────────────────────────────────────

    [TestMethod]
    public void IncrementWinsAndLosses_AtomicallyUpdates_WinnerAndLoser()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var winner = AddFreshDriver(repo, "Winner Driver");
        var loser  = AddFreshDriver(repo, "Loser Driver");

        repo.IncrementWinsAndLosses(winner.Id, loser.Id);

        var reloadedWinner = repo.GetDriverById(winner.Id)!;
        var reloadedLoser  = repo.GetDriverById(loser.Id)!;

        Assert.AreEqual(1, reloadedWinner.TotalWins,
            "Winner's TotalWins must be incremented by 1");
        Assert.AreEqual(0, reloadedWinner.TotalLosses,
            "Winner's TotalLosses must be unchanged");

        Assert.AreEqual(0, reloadedLoser.TotalWins,
            "Loser's TotalWins must be unchanged");
        Assert.AreEqual(1, reloadedLoser.TotalLosses,
            "Loser's TotalLosses must be incremented by 1");
    }

    [TestMethod]
    public void IncrementWinsAndLosses_WithCustomDeltas_IncrementsCorrectly()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var winner = AddFreshDriver(repo, "Alpha");
        var loser  = AddFreshDriver(repo, "Beta");

        repo.IncrementWinsAndLosses(winner.Id, loser.Id, winDelta: 2, lossDelta: 3);

        var reloadedWinner = repo.GetDriverById(winner.Id)!;
        var reloadedLoser  = repo.GetDriverById(loser.Id)!;

        Assert.AreEqual(2, reloadedWinner.TotalWins,
            "Winner's TotalWins must reflect winDelta=2");
        Assert.AreEqual(3, reloadedLoser.TotalLosses,
            "Loser's TotalLosses must reflect lossDelta=3");
    }

    // ── All four public methods do not throw for valid column names ───────────

    [TestMethod]
    public void AllFourIncrementMethods_DoNotThrow_ForValidColumnNames()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);

        // None of these must throw — the column names are all in the whitelist.
        repo.IncrementWins(driver.Id);
        repo.IncrementLosses(driver.Id);
        repo.IncrementEventsEntered(driver.Id);
        repo.IncrementEventsWon(driver.Id);

        // Reaching this line proves all four calls completed without exception.
    }

    // ── ExecuteStatIncrement whitelist rejection ──────────────────────────────

    [TestMethod]
    public void ExecuteStatIncrement_ThrowsArgumentException_ForColumnNotInWhitelist()
    {
        // ExecuteStatIncrement is private; call it via reflection.
        // Reflection wraps thrown exceptions in TargetInvocationException, so the lambda
        // catches TIE and rethrows the inner ArgumentException for Assert.ThrowsException.
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);

        var method = typeof(DriverRepository).GetMethod(
            "ExecuteStatIncrement",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMethodException(nameof(DriverRepository), "ExecuteStatIncrement");

        // Try a column name that is not in the whitelist.
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            try { method.Invoke(repo, new object[] { driver.Id, "Name", 1 }); }
            catch (TargetInvocationException e) when (e.InnerException is ArgumentException argEx)
            { throw argEx; }
        }, "ExecuteStatIncrement must throw ArgumentException for a column not in the whitelist");
    }

    [TestMethod]
    public void ExecuteStatIncrement_ThrowsArgumentException_ForSqlInjectionAttempt()
    {
        // Verify that a SQL-injection-style column name is also rejected by the whitelist
        // before it ever reaches the database — the core security guarantee of issue #101.
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);

        var method = typeof(DriverRepository).GetMethod(
            "ExecuteStatIncrement",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMethodException(nameof(DriverRepository), "ExecuteStatIncrement");

        const string injected = "TotalWins; DROP TABLE Drivers; --";

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            try { method.Invoke(repo, new object[] { driver.Id, injected, 1 }); }
            catch (TargetInvocationException e) when (e.InnerException is ArgumentException argEx)
            { throw argEx; }
        }, "ExecuteStatIncrement must throw ArgumentException for a SQL-injection-style column name");

        // The Drivers table must still exist — the injection never reached the database.
        var allDrivers = repo.GetAllDrivers();
        Assert.AreEqual(1, allDrivers.Count,
            "Drivers table must be intact after a rejected injection attempt");
    }

    [TestMethod]
    public void ExecuteStatIncrement_ThrowsArgumentException_ForEmptyColumnName()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var driver = AddFreshDriver(repo);

        var method = typeof(DriverRepository).GetMethod(
            "ExecuteStatIncrement",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMethodException(nameof(DriverRepository), "ExecuteStatIncrement");

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            try { method.Invoke(repo, new object[] { driver.Id, string.Empty, 1 }); }
            catch (TargetInvocationException e) when (e.InnerException is ArgumentException argEx)
            { throw argEx; }
        }, "ExecuteStatIncrement must throw ArgumentException for an empty column name");
    }

    // ── TemporarySqliteDb ─────────────────────────────────────────────────────

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-stat-increment-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try
            {
                if (File.Exists(DatabasePath))
                    File.Delete(DatabasePath);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
