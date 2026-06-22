using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data.SQLite;
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
    public void GetAllSessions_UnloadableRowsAreHiddenWithoutBeingDeleted()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new RaceSessionRepository(db.ConnectionString);
        int validId = repository.SaveSession(
            CreateSession("QA Valid Session", new DateTime(2026, 6, 22), "Pro Ladder"));
        int missingDataId = InsertRawSession(db.ConnectionString, "QA Missing Data", "");
        int invalidDataId = InsertRawSession(db.ConnectionString, "QA Invalid Data", "{not-json");

        var sessions = repository.GetAllSessions();

        Assert.AreEqual(1, sessions.Count);
        Assert.AreEqual(validId, sessions[0].Id);
        Assert.AreEqual(3, CountSessionRows(db.ConnectionString),
            "Listing sessions must never delete or overwrite damaged saved data");
        Assert.AreEqual(
            RaceSessionLoadStatus.MissingData,
            repository.TryLoadSession(missingDataId).Status);
        Assert.AreEqual(
            RaceSessionLoadStatus.InvalidData,
            repository.TryLoadSession(invalidDataId).Status);
    }

    [TestMethod]
    public void TryLoadSession_MissingIdReportsNotFound()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new RaceSessionRepository(db.ConnectionString);

        var result = repository.TryLoadSession(999);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(RaceSessionLoadStatus.NotFound, result.Status);
        Assert.IsNull(result.Session);
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

    [TestMethod]
    public void SaveSession_WhenAlreadyPersisted_UpdatesExistingRow()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new RaceSessionRepository(db.ConnectionString);

        var session = CreateSession(
            "QA Update Session",
            new DateTime(2026, 3, 2, 9, 0, 0),
            "Pro Ladder");

        var firstId = repository.SaveSession(session);
        session.EventName = "QA Updated Session";
        session.DriverEntries[0].DialIn = 4.125;

        var secondId = repository.SaveSession(session);
        var all = repository.GetAllSessions();
        var loaded = repository.LoadSession(firstId);

        Assert.AreEqual(firstId, secondId);
        Assert.AreEqual(1, all.Count, "Saving an existing session must not insert a stale duplicate");
        Assert.AreEqual("QA Updated Session", loaded.EventName);
        Assert.AreEqual(4.125, loaded.DriverEntries[0].DialIn);
        Assert.AreEqual(firstId, loaded.Id);
    }

    [TestMethod]
    public void ControllerSaveSession_PreservesDialInsAndEntryMetadata()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new RaceSessionRepository(db.ConnectionString);

        var session = CreateSession(
            "QA Dial-In Preservation",
            new DateTime(2026, 3, 2, 10, 0, 0),
            "Pro Ladder");
        for (int i = 0; i < session.DriverEntries.Count; i++)
            session.DriverEntries[i].DialIn = 4.000 + (i * 0.100);

        var controller = new RCDragManagerProd.Controllers.RaceController(
            session,
            new NoOpStandingsDialogService());
        controller.GenerateBracket("Pro Ladder", session.Drivers);
        controller.UpdateDriverDialIn(session.Drivers[1].Id, 4.555);
        controller.SaveProgress();

        var id = repository.SaveSession(session);
        var loaded = repository.LoadSession(id);

        Assert.AreEqual(4.000, loaded.DriverEntries[0].DialIn);
        Assert.AreEqual(4.555, loaded.DriverEntries[1].DialIn);
        Assert.AreEqual(4.200, loaded.DriverEntries[2].DialIn);
        Assert.AreEqual(4.300, loaded.DriverEntries[3].DialIn);
        Assert.AreEqual(500, loaded.DriverEntries[0].CarID);
        Assert.AreEqual("Car 1", loaded.DriverEntries[0].CarName);
        Assert.AreEqual(1, loaded.DriverEntries[0].Seed);
    }

    [TestMethod]
    public void ControllerSaveSession_PersistsProLadderResultSnapshot()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new RaceSessionRepository(db.ConnectionString);

        var session = CreateSession(
            "QA Result Snapshot",
            new DateTime(2026, 3, 2, 11, 0, 0),
            "Pro Ladder");
        var controller = new RCDragManagerProd.Controllers.RaceController(
            session,
            new NoOpStandingsDialogService());
        controller.GenerateBracket("Pro Ladder", session.Drivers);

        var first = controller.PeekUpcomingMatches(10).First();
        controller.SubmitWinner(first.MatchId, firstOption: true);
        controller.SaveProgress();

        var id = repository.SaveSession(session);
        var loaded = repository.LoadSession(id);
        var phase = loaded.ResultsArchive.Phases
            .Single(p => p.Phase == "Pro Ladder");
        var savedMatch = phase.Matches.Single(m => m.MatchId == first.MatchId);

        Assert.AreEqual(session.Drivers[0].Id, savedMatch.WinnerDriverId);
        Assert.AreEqual(session.Drivers[0].Name, savedMatch.WinnerName);
        Assert.IsTrue(phase.Matches.Count >= 3);
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

    private static int InsertRawSession(string connectionString, string eventName, string sessionData)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();
        using var command = new SQLiteCommand(@"
INSERT INTO RaceSessions (EventName, EventDate, ClassType, RaceType, SessionData)
VALUES (@EventName, @EventDate, 'Open', 'Pro Ladder', @SessionData);
SELECT last_insert_rowid();", connection);
        command.Parameters.AddWithValue("@EventName", eventName);
        command.Parameters.AddWithValue("@EventDate", "2026-06-22 12:00:00");
        command.Parameters.AddWithValue("@SessionData", sessionData);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static int CountSessionRows(string connectionString)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();
        using var command = new SQLiteCommand("SELECT COUNT(*) FROM RaceSessions", connection);
        return Convert.ToInt32(command.ExecuteScalar());
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
