using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

[TestClass]
public class RaceSessionRepositoryTests
{
    [TestMethod]
    public void SaveAndLoadSession_RoundTripsCoreSessionState()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new RaceSessionRepository(db.ConnectionString);

        var session = CreateSession(
            eventName: "QA SaveLoad RoundTrip",
            eventDate: new DateTime(2026, 3, 1, 10, 30, 0),
            raceType: "Round Robin");

        session.SavedRevealedRounds = new List<string> { "RR1", "RR2" };
        session.SavedResults = new List<MatchResultSave>
        {
            new MatchResultSave { MatchId = 101, WinnerDriverId = 1, LoserDriverId = 2 },
            new MatchResultSave { MatchId = 102, WinnerDriverId = 3, LoserDriverId = 4 }
        };

        var savedId = repository.SaveSession(session);
        var loaded = repository.LoadSession(savedId);

        Assert.IsTrue(savedId > 0);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(savedId, session.Id);
        Assert.AreEqual("QA SaveLoad RoundTrip", loaded.EventName);
        Assert.AreEqual(new DateTime(2026, 3, 1, 10, 30, 0), loaded.EventDate);
        Assert.AreEqual("Round Robin", loaded.RaceType);
        Assert.AreEqual("Heads Up", loaded.ClassType);
        Assert.AreEqual("QMDRA", loaded.RoundRobinVariant);
        Assert.AreEqual(4, loaded.Drivers.Count);
        Assert.AreEqual(4, loaded.DriverEntries.Count);
        CollectionAssert.AreEqual(new[] { "RR1", "RR2" }, loaded.SavedRevealedRounds);
        Assert.AreEqual(2, loaded.SavedResults.Count);
        Assert.AreEqual(101, loaded.SavedResults[0].MatchId);
        Assert.AreEqual(1, loaded.SavedResults[0].WinnerDriverId);
        Assert.AreEqual(2, loaded.SavedResults[0].LoserDriverId);
    }

    [TestMethod]
    public void GetAllSessions_ReturnsNewestSessionFirst()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new RaceSessionRepository(db.ConnectionString);

        var older = CreateSession("QA Older Session", new DateTime(2026, 2, 10, 9, 0, 0), "Pro Ladder");
        var newer = CreateSession("QA Newer Session", new DateTime(2026, 2, 10, 9, 5, 0), "Random");

        var olderId = repository.SaveSession(older);
        var newerId = repository.SaveSession(newer);

        var sessions = repository.GetAllSessions();

        Assert.AreEqual(2, sessions.Count);
        Assert.AreEqual(newerId, sessions[0].Id);
        Assert.AreEqual("QA Newer Session", sessions[0].EventName);
        Assert.AreEqual(olderId, sessions[1].Id);
    }

    [TestMethod]
    public void DeleteSession_RemovesSessionFromLoadAndSummaryList()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new RaceSessionRepository(db.ConnectionString);

        var session = CreateSession("QA Delete Session", new DateTime(2026, 2, 12, 14, 0, 0), "Round Robin");
        var savedId = repository.SaveSession(session);

        repository.DeleteSession(savedId);

        var loadedAfterDelete = repository.LoadSession(savedId);
        var summariesAfterDelete = repository.GetAllSessions();

        Assert.IsNull(loadedAfterDelete);
        Assert.IsFalse(summariesAfterDelete.Any(s => s.Id == savedId));
    }

    private static RaceSession CreateSession(string eventName, DateTime eventDate, string raceType)
    {
        var drivers = TestDriverFactory.CreateRoundRobinPack(4);

        return new RaceSession
        {
            EventName = eventName,
            EventDate = eventDate,
            RaceType = raceType,
            ClassType = "Heads Up",
            RoundRobinVariant = "QMDRA",
            RoundsToRun = 3,
            Drivers = drivers,
            DriverEntries = drivers.Select((d, i) => new RaceSessionDriverEntry
            {
                DriverID = d.Id,
                DriverName = d.Name,
                CarID = 500 + i,
                CarName = $"Car {i + 1}",
                ClassType = "Heads Up",
                Seed = i + 1
            }).ToList()
        };
    }

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try
            {
                if (File.Exists(DatabasePath))
                {
                    File.Delete(DatabasePath);
                }
            }
            catch
            {
                // Best-effort cleanup; test assertions should not depend on file deletion.
            }
        }
    }
}
