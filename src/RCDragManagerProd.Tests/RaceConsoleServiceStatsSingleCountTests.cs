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
/// Regression tests for issue #379 — win/loss and events-won stats were persisted
/// per winner click AND again at tournament completion in the single-class WPF
/// console, double-counting every match. Completion-time recording
/// (<see cref="RaceConsoleService.RecordTournamentCompletion"/>) is now the only
/// write path; these tests pin the exact totals a completed event must produce.
/// </summary>
[TestClass]
public class RaceConsoleServiceStatsSingleCountTests
{
    [TestMethod]
    public void RecordTournamentCompletion_MultiMatchEvent_CountsEachMatchExactlyOnce()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var champ = AddDriver(repo, "Champ");
        var second = AddDriver(repo, "Second");
        var third = AddDriver(repo, "Third");

        // Semifinals + final: Champ beats Third, Second beats no one, Champ beats Second.
        var summary = new RaceController.RaceSummary
        {
            Winner = champ,
            RunnerUp = second,
            MatchResults = new List<(int, int)>
            {
                (champ.Id, third.Id),
                (champ.Id, second.Id)
            }
        };

        var controller = new RaceController(new RaceSession { EventName = "Finals Night", RaceType = "Pro Ladder" });
        var svc = new RaceConsoleService(controller, null, repo);
        svc.RecordTournamentCompletion(summary, new[] { champ, second, third });

        var champDb = repo.GetDriverById(champ.Id);
        var secondDb = repo.GetDriverById(second.Id);
        var thirdDb = repo.GetDriverById(third.Id);

        Assert.AreEqual(2, champDb.TotalWins, "Champion won exactly two matches.");
        Assert.AreEqual(0, champDb.TotalLosses);
        Assert.AreEqual(1, champDb.EventsWon, "Events won must increment exactly once.");
        Assert.AreEqual(1, champDb.EventsEntered);

        Assert.AreEqual(0, secondDb.TotalWins);
        Assert.AreEqual(1, secondDb.TotalLosses);
        Assert.AreEqual(0, secondDb.EventsWon);
        Assert.AreEqual(1, secondDb.EventsEntered);

        Assert.AreEqual(0, thirdDb.TotalWins);
        Assert.AreEqual(1, thirdDb.TotalLosses);
        Assert.AreEqual(1, thirdDb.EventsEntered);
    }

    [TestMethod]
    public void RecordTournamentCompletion_CalledOnce_DoesNotDependOnPerMatchWrites()
    {
        // Drivers start at zero: no per-match write path may have run before completion.
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new DriverRepository(db.ConnectionString);

        var winner = AddDriver(repo, "Winner");
        var loser = AddDriver(repo, "Loser");

        Assert.AreEqual(0, repo.GetDriverById(winner.Id).TotalWins);

        var controller = new RaceController(new RaceSession { EventName = "Single Final", RaceType = "Pro Ladder" });
        var svc = new RaceConsoleService(controller, null, repo);
        svc.RecordTournamentCompletion(
            new RaceController.RaceSummary
            {
                Winner = winner,
                MatchResults = new List<(int, int)> { (winner.Id, loser.Id) }
            },
            new[] { winner, loser });

        Assert.AreEqual(1, repo.GetDriverById(winner.Id).TotalWins);
        Assert.AreEqual(1, repo.GetDriverById(winner.Id).EventsWon);
        Assert.AreEqual(1, repo.GetDriverById(loser.Id).TotalLosses);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Driver AddDriver(DriverRepository repo, string name)
    {
        var d = new Driver { Name = name, Cars = new List<Car>() };
        repo.AddDriver(d);
        return repo.GetDriverById(d.Id);
    }

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-singlecount-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); } catch { }
        }
    }
}
