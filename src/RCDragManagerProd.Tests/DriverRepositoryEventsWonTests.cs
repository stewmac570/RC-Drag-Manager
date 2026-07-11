using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Regression tests for issue #378 — EventsWon was wiped on race close because
/// <see cref="DriverRepository.ComputeEventsWonFromSavedSessions"/> looked up
/// PascalCase property names ("SavedResults", "WinnerDriverId") in session JSON
/// that <see cref="RaceSessionRepository"/> serializes camelCase, and
/// JsonElement.TryGetProperty is case-sensitive. These tests round-trip through
/// the real serializer so the casing mismatch can never regress silently again.
/// </summary>
[TestClass]
public class DriverRepositoryEventsWonTests
{
    [TestMethod]
    public void ComputeEventsWon_CountsWinnerFromSessionSavedViaRealSerializer()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var driverRepo = new DriverRepository(db.ConnectionString);
        var sessionRepo = new RaceSessionRepository(db.ConnectionString);

        // Driver 1 wins every match; driver 2 and 3 each lose one.
        sessionRepo.SaveSession(CreateCompletedSession("Event A", new[]
        {
            (MatchId: 1, WinnerId: 1, LoserId: 2),
            (MatchId: 2, WinnerId: 1, LoserId: 3)
        }));

        Assert.AreEqual(1, driverRepo.ComputeEventsWonFromSavedSessions(1),
            "Champion (undefeated winner) must be counted from camelCase session JSON.");
        Assert.AreEqual(0, driverRepo.ComputeEventsWonFromSavedSessions(2));
        Assert.AreEqual(0, driverRepo.ComputeEventsWonFromSavedSessions(3));
    }

    [TestMethod]
    public void ComputeEventsWon_CountsWinnerFromLegacyPascalCaseJson()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var driverRepo = new DriverRepository(db.ConnectionString);

        const string legacyJson = @"{
  ""EventName"": ""Legacy Event"",
  ""SavedResults"": [
    { ""MatchId"": 1, ""WinnerDriverId"": 7, ""LoserDriverId"": 8 },
    { ""MatchId"": 2, ""WinnerDriverId"": 7, ""LoserDriverId"": 9 }
  ]
}";
        InsertRawSession(db.ConnectionString, "Legacy Event", legacyJson);

        Assert.AreEqual(1, driverRepo.ComputeEventsWonFromSavedSessions(7),
            "Sessions saved before camelCase serialization must still be counted.");
        Assert.AreEqual(0, driverRepo.ComputeEventsWonFromSavedSessions(8));
    }

    [TestMethod]
    public void ComputeEventsWon_CountsMultipleEventsAcrossSessions()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var driverRepo = new DriverRepository(db.ConnectionString);
        var sessionRepo = new RaceSessionRepository(db.ConnectionString);

        sessionRepo.SaveSession(CreateCompletedSession("Event A", new[] { (1, 10, 20) }));
        sessionRepo.SaveSession(CreateCompletedSession("Event B", new[] { (1, 10, 30) }));
        sessionRepo.SaveSession(CreateCompletedSession("Event C", new[] { (1, 20, 10) }));

        Assert.AreEqual(2, driverRepo.ComputeEventsWonFromSavedSessions(10));
        Assert.AreEqual(1, driverRepo.ComputeEventsWonFromSavedSessions(20));
    }

    [TestMethod]
    public void RecomputeEventsWon_RestoresCorrectCountAfterRaceClose()
    {
        // The issue #378 end-to-end scenario: close race recomputes EventsWon from
        // saved sessions and must NOT wipe the champion's tally to zero.
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var driverRepo = new DriverRepository(db.ConnectionString);
        var sessionRepo = new RaceSessionRepository(db.ConnectionString);

        var champion = AddDriver(driverRepo, "Alice");
        var runnerUp = AddDriver(driverRepo, "Bob");

        sessionRepo.SaveSession(CreateCompletedSession("Club Night", new[]
        {
            (MatchId: 1, WinnerId: champion.Id, LoserId: runnerUp.Id)
        }));

        // Simulate a stale/incremented value that the recompute overwrites.
        champion.EventsWon = 5;
        driverRepo.UpdateDriver(champion);

        var controller = new RaceController(new RaceSession { EventName = "Club Night", RaceType = "Pro Ladder" });
        var svc = new RaceConsoleService(controller, null, driverRepo);
        svc.RecomputeEventsWon(new[] { champion, runnerUp });

        Assert.AreEqual(1, driverRepo.GetDriverById(champion.Id).EventsWon,
            "Recompute must derive EventsWon from saved sessions, not wipe it to zero.");
        Assert.AreEqual(0, driverRepo.GetDriverById(runnerUp.Id).EventsWon);
    }

    [TestMethod]
    public void RecomputeEventsWon_NullParticipantEntry_DoesNotThrow()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var driverRepo = new DriverRepository(db.ConnectionString);
        var driver = AddDriver(driverRepo, "Carol");

        var controller = new RaceController(new RaceSession { EventName = "Q", RaceType = "Pro Ladder" });
        var svc = new RaceConsoleService(controller, null, driverRepo);

        // BYE slots are represented as null drivers; a null entry must be skipped.
        svc.RecomputeEventsWon(new Driver[] { null, driver });

        Assert.AreEqual(0, driverRepo.GetDriverById(driver.Id).EventsWon);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RaceSession CreateCompletedSession(
        string eventName, IEnumerable<(int MatchId, int WinnerId, int LoserId)> results)
    {
        var saved = new List<MatchResultSave>();
        foreach (var (matchId, winnerId, loserId) in results)
            saved.Add(new MatchResultSave { MatchId = matchId, WinnerDriverId = winnerId, LoserDriverId = loserId });

        return new RaceSession
        {
            EventName = eventName,
            EventDate = new DateTime(2026, 7, 11, 18, 0, 0),
            ClassType = "Heads Up",
            RaceType = "Pro Ladder",
            SavedResults = saved
        };
    }

    private static Driver AddDriver(DriverRepository repo, string name)
    {
        var d = new Driver { Name = name, Cars = new List<Car>() };
        repo.AddDriver(d);
        return repo.GetDriverById(d.Id);
    }

    private static void InsertRawSession(string connectionString, string eventName, string sessionData)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();
        using var command = new SQLiteCommand(@"
INSERT INTO RaceSessions (EventName, EventDate, ClassType, RaceType, SessionData)
VALUES (@EventName, '2026-07-11 18:00:00', 'Open', 'Pro Ladder', @SessionData);", connection);
        command.Parameters.AddWithValue("@EventName", eventName);
        command.Parameters.AddWithValue("@SessionData", sessionData);
        command.ExecuteNonQuery();
    }

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-eventswon-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); } catch { }
        }
    }
}
