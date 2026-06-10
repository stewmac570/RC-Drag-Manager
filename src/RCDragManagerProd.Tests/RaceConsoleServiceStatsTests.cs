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
/// Covers <see cref="RaceConsoleService.RecordTournamentCompletion"/> and
/// <see cref="RaceConsoleService.RecomputeEventsWon"/> (issue #290) — stat writes
/// extracted from Form1. Runs headless against a temporary SQLite database.
/// </summary>
[TestClass]
public class RaceConsoleServiceStatsTests
{
    private static (RaceConsoleService svc, DriverRepository repo) NewPair(TemporarySqliteDb db)
    {
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);
        var controller = new RaceController(new RaceSession { EventName = "Test", RaceType = "Pro Ladder" });
        return (new RaceConsoleService(controller, null, repo), repo);
    }

    private static Driver AddDriver(DriverRepository repo, string name)
    {
        var d = new Driver { Name = name, Cars = new List<Car> { new Car { CarName = "Car" } } };
        repo.AddDriver(d);
        return repo.GetAllDrivers().Find(x => x.Name == name);
    }

    // ── RecordTournamentCompletion ────────────────────────────────────────────

    [TestMethod]
    public void RecordTournamentCompletion_NullSummary_DoesNotThrow()
    {
        using var db = new TemporarySqliteDb();
        var (svc, _) = NewPair(db);
        svc.RecordTournamentCompletion(null, null);
    }

    [TestMethod]
    public void RecordTournamentCompletion_IncrementsEventsEntered()
    {
        using var db = new TemporarySqliteDb();
        var (svc, repo) = NewPair(db);
        var d = AddDriver(repo, "Alice");

        svc.RecordTournamentCompletion(
            new RaceController.RaceSummary { MatchResults = new List<(int, int)>() },
            new[] { d });

        Assert.AreEqual(1, repo.GetDriverById(d.Id).EventsEntered);
    }

    [TestMethod]
    public void RecordTournamentCompletion_IncrementsWinsAndLosses()
    {
        using var db = new TemporarySqliteDb();
        var (svc, repo) = NewPair(db);
        var winner = AddDriver(repo, "Bob");
        var loser  = AddDriver(repo, "Carol");

        svc.RecordTournamentCompletion(
            new RaceController.RaceSummary
            {
                MatchResults = new List<(int, int)> { (winner.Id, loser.Id) },
                Winner = winner
            },
            new[] { winner, loser });

        Assert.AreEqual(1, repo.GetDriverById(winner.Id).TotalWins);
        Assert.AreEqual(1, repo.GetDriverById(loser.Id).TotalLosses);
    }

    [TestMethod]
    public void RecordTournamentCompletion_IncrementsEventsWon()
    {
        using var db = new TemporarySqliteDb();
        var (svc, repo) = NewPair(db);
        var winner = AddDriver(repo, "Dave");

        svc.RecordTournamentCompletion(
            new RaceController.RaceSummary
            {
                MatchResults = new List<(int, int)>(),
                Winner = winner
            },
            new[] { winner });

        Assert.AreEqual(1, repo.GetDriverById(winner.Id).EventsWon);
    }

    [TestMethod]
    public void RecordTournamentCompletion_NullRepo_DoesNotThrow()
    {
        var controller = new RaceController(new RaceSession { EventName = "Q", RaceType = "Pro Ladder" });
        var svc = new RaceConsoleService(controller, null, null);
        svc.RecordTournamentCompletion(
            new RaceController.RaceSummary { MatchResults = new List<(int, int)>() },
            new[] { new Driver { Id = 1, Name = "Eve" } });
    }

    // ── RecomputeEventsWon ────────────────────────────────────────────────────

    [TestMethod]
    public void RecomputeEventsWon_NullParticipants_DoesNotThrow()
    {
        using var db = new TemporarySqliteDb();
        var (svc, _) = NewPair(db);
        svc.RecomputeEventsWon(null);
    }

    [TestMethod]
    public void RecomputeEventsWon_NullRepo_DoesNotThrow()
    {
        var controller = new RaceController(new RaceSession { EventName = "Q", RaceType = "Pro Ladder" });
        var svc = new RaceConsoleService(controller, null, null);
        svc.RecomputeEventsWon(new[] { new Driver { Id = 1, Name = "Frank" } });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-consolestats-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); } catch { }
        }
    }
}
