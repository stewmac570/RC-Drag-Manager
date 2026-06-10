using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="MultiClassRaceService"/> (issue #289) — stat persistence
/// extracted from MultiClassRaceForm. Runs headless against an in-memory database.
/// </summary>
[TestClass]
public class MultiClassRaceServiceTests
{
    private static (MultiClassRaceService svc, DriverRepository repo) NewPair(TemporarySqliteDb db)
    {
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);
        return (new MultiClassRaceService(repo), repo);
    }

    private static Driver AddDriver(DriverRepository repo, string name)
    {
        var d = new Driver { Name = name, Cars = new System.Collections.Generic.List<Car> { new Car { CarName = "Car" } } };
        repo.AddDriver(d);
        return repo.GetAllDrivers().Find(x => x.Name == name);
    }

    // ── Null / empty guard ────────────────────────────────────────────────────

    [TestMethod]
    public void RecordClassCompletion_NullSummary_DoesNotThrow()
    {
        using var db = new TemporarySqliteDb();
        var (svc, _) = NewPair(db);
        svc.RecordClassCompletion(null);
    }

    [TestMethod]
    public void RecordClassCompletion_NullMatchResultsAndNullWinner_DoesNotThrow()
    {
        using var db = new TemporarySqliteDb();
        var (svc, _) = NewPair(db);
        svc.RecordClassCompletion(new RaceController.RaceSummary { MatchResults = null, Winner = null });
    }

    // ── Win / loss stats ──────────────────────────────────────────────────────

    [TestMethod]
    public void RecordClassCompletion_WithMatchResults_IncrementsWinsAndLosses()
    {
        using var db = new TemporarySqliteDb();
        var (svc, repo) = NewPair(db);
        var winner = AddDriver(repo, "Alice");
        var loser  = AddDriver(repo, "Bob");

        svc.RecordClassCompletion(new RaceController.RaceSummary
        {
            MatchResults = new List<(int, int)> { (winner.Id, loser.Id) },
            Winner = winner
        });

        Assert.AreEqual(1, repo.GetDriverById(winner.Id).TotalWins);
        Assert.AreEqual(1, repo.GetDriverById(loser.Id).TotalLosses);
    }

    [TestMethod]
    public void RecordClassCompletion_MultipleMatchResults_IncrementsEachPair()
    {
        using var db = new TemporarySqliteDb();
        var (svc, repo) = NewPair(db);
        var d1 = AddDriver(repo, "Carol");
        var d2 = AddDriver(repo, "Dave");
        var d3 = AddDriver(repo, "Eve");

        svc.RecordClassCompletion(new RaceController.RaceSummary
        {
            MatchResults = new List<(int, int)>
            {
                (d1.Id, d2.Id),
                (d1.Id, d3.Id)
            },
            Winner = d1
        });

        Assert.AreEqual(2, repo.GetDriverById(d1.Id).TotalWins);
        Assert.AreEqual(1, repo.GetDriverById(d2.Id).TotalLosses);
        Assert.AreEqual(1, repo.GetDriverById(d3.Id).TotalLosses);
    }

    // ── Events won ────────────────────────────────────────────────────────────

    [TestMethod]
    public void RecordClassCompletion_WithWinner_IncrementsEventsWon()
    {
        using var db = new TemporarySqliteDb();
        var (svc, repo) = NewPair(db);
        var winner = AddDriver(repo, "Frank");

        svc.RecordClassCompletion(new RaceController.RaceSummary
        {
            MatchResults = new List<(int, int)>(),
            Winner = winner
        });

        Assert.AreEqual(1, repo.GetDriverById(winner.Id).EventsWon);
    }

    [TestMethod]
    public void RecordClassCompletion_NoWinner_DoesNotIncrementEventsWon()
    {
        using var db = new TemporarySqliteDb();
        var (svc, repo) = NewPair(db);
        var d = AddDriver(repo, "Gina");

        svc.RecordClassCompletion(new RaceController.RaceSummary
        {
            MatchResults = new List<(int, int)> { (d.Id, d.Id) },
            Winner = null
        });

        Assert.AreEqual(0, repo.GetDriverById(d.Id).EventsWon);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-multiclassrace-svc-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); } catch { }
        }
    }
}
