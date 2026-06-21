using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="LoadSessionService"/> (issue #288) — the UI-independent saved-race
/// loading operations the Load Session screen delegates to. Runs headless against an
/// in-memory database, proving the form needs no repositories of its own.
/// </summary>
[TestClass]
public class LoadSessionServiceTests
{
    private static LoadSessionService NewService(TemporarySqliteDb db)
    {
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        return new LoadSessionService(
            new RaceSessionRepository(db.ConnectionString),
            new MultiClassEventRepository(db.ConnectionString));
    }

    // ── ListSessions ──────────────────────────────────────────────────────────

    [TestMethod]
    public void ListSessions_EmptyDb_ReturnsEmptyList()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);

        var sessions = service.ListSessions();

        Assert.IsNotNull(sessions);
        Assert.AreEqual(0, sessions.Count);
    }

    [TestMethod]
    public void ListSessions_AfterSave_ReturnsSummaryRow()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var repo = new RaceSessionRepository(db.ConnectionString);
        repo.SaveSession(MakeSession("Spring Nats", "Pro Mod", "Round Robin"));

        var sessions = service.ListSessions();

        Assert.AreEqual(1, sessions.Count);
        Assert.AreEqual("Spring Nats", sessions[0].EventName);
        Assert.AreEqual("Pro Mod", sessions[0].ClassType);
        Assert.AreEqual("Round Robin", sessions[0].RaceType);
    }

    // ── ListMultiClassEvents ──────────────────────────────────────────────────

    [TestMethod]
    public void ListMultiClassEvents_EmptyDb_ReturnsEmptyList()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);

        var events = service.ListMultiClassEvents();

        Assert.IsNotNull(events);
        Assert.AreEqual(0, events.Count);
    }

    [TestMethod]
    public void ListMultiClassEvents_AfterSave_ReturnsSummaryRow()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var multiRepo = new MultiClassEventRepository(db.ConnectionString);
        var session1 = MakeSession("Class A", "Open", "Round Robin");
        var session2 = MakeSession("Class B", "Pro", "Round Robin");
        multiRepo.SaveEvent(new MultiClassEvent
        {
            EventName = "Big Weekend",
            EventDate = new DateTime(2026, 3, 1),
            ClassSessions = new List<RaceSession> { session1, session2 }
        });

        var events = service.ListMultiClassEvents();

        Assert.AreEqual(1, events.Count);
        Assert.AreEqual("Big Weekend", events[0].EventName);
        Assert.AreEqual(2, events[0].ClassCount);
    }

    // ── LoadSingleClassSession ────────────────────────────────────────────────

    [TestMethod]
    public void LoadSingleClassSession_ValidId_WrapsInOneClassMultiClassEvent()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var repo = new RaceSessionRepository(db.ConnectionString);
        var session = MakeSession("Summer Shoot-Out", "Sportsman", "Round Robin");
        repo.SaveSession(session);
        int id = service.ListSessions()[0].Id;

        var result = service.LoadSingleClassSession(id);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Event);
        Assert.AreEqual("Summer Shoot-Out", result.Event.EventName);
        Assert.AreEqual(1, result.Event.ClassSessions.Count);
        Assert.AreEqual("Sportsman", result.Event.ClassSessions[0].ClassType);
    }

    [TestMethod]
    public void LoadSingleClassSession_InvalidId_ReturnsFailure()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);

        var result = service.LoadSingleClassSession(999);

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Event);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [TestMethod]
    public void LoadSingleClassSession_SessionWithDefaultDate_UsesNowAsEventDate()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var repo = new RaceSessionRepository(db.ConnectionString);
        var session = MakeSession("No-Date Session", "Open", "Round Robin");
        session.EventDate = default;
        repo.SaveSession(session);
        int id = service.ListSessions()[0].Id;

        var result = service.LoadSingleClassSession(id);

        Assert.IsTrue(result.Success);
        Assert.AreNotEqual(default(DateTime), result.Event.EventDate);
    }

    // ── LoadMultiClassEvent ───────────────────────────────────────────────────

    [TestMethod]
    public void LoadMultiClassEvent_ValidId_ReturnsEvent()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var multiRepo = new MultiClassEventRepository(db.ConnectionString);
        var evt = new MultiClassEvent
        {
            EventName = "Autumn Opener",
            EventDate = new DateTime(2026, 9, 15),
            ClassSessions = new List<RaceSession> { MakeSession("Open A", "Open", "Round Robin") }
        };
        multiRepo.SaveEvent(evt);
        int id = service.ListMultiClassEvents()[0].Id;

        var result = service.LoadMultiClassEvent(id);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Event);
        Assert.AreEqual("Autumn Opener", result.Event.EventName);
    }

    [TestMethod]
    public void LoadMultiClassEvent_InvalidId_ReturnsFailure()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);

        var result = service.LoadMultiClassEvent(999);

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Event);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    // ── DeleteSession ─────────────────────────────────────────────────────────

    [TestMethod]
    public void LoadEvent_SingleClassCard_LoadsWrappedSession()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var repo = new RaceSessionRepository(db.ConnectionString);
        repo.SaveSession(MakeSession("Recent Single", "Sportsman", "Pro Ladder"));
        int id = service.ListSessions()[0].Id;

        var result = service.LoadEvent(id, isMultiClass: false);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("Recent Single", result.Event.EventName);
        Assert.AreEqual(1, result.Event.ClassSessions.Count);
    }

    [TestMethod]
    public void LoadEvent_MultiClassCard_LoadsSavedEvent()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var repo = new MultiClassEventRepository(db.ConnectionString);
        repo.SaveEvent(new MultiClassEvent
        {
            EventName = "Recent Multi",
            EventDate = new DateTime(2026, 6, 22),
            ClassSessions = new List<RaceSession>
            {
                MakeSession("Class A", "Open", "Round Robin"),
                MakeSession("Class B", "Mod", "Round Robin")
            }
        });
        int id = service.ListMultiClassEvents()[0].Id;

        var result = service.LoadEvent(id, isMultiClass: true);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("Recent Multi", result.Event.EventName);
        Assert.AreEqual(2, result.Event.ClassSessions.Count);
    }

    [TestMethod]
    public void LoadEvent_MissingRecentCard_ReturnsFailure()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);

        var result = service.LoadEvent(999, isMultiClass: false);

        Assert.IsFalse(result.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [TestMethod]
    public void DeleteSession_RemovesRowFromList()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var repo = new RaceSessionRepository(db.ConnectionString);
        repo.SaveSession(MakeSession("To Delete", "Open", "Round Robin"));
        int id = service.ListSessions()[0].Id;

        service.DeleteSession(id);

        Assert.AreEqual(0, service.ListSessions().Count);
    }

    // ── DeleteMultiClassEvent ─────────────────────────────────────────────────

    [TestMethod]
    public void DeleteMultiClassEvent_RemovesRowFromList()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var multiRepo = new MultiClassEventRepository(db.ConnectionString);
        multiRepo.SaveEvent(new MultiClassEvent
        {
            EventName = "To Delete",
            EventDate = DateTime.Now,
            ClassSessions = new List<RaceSession> { MakeSession("C1", "Open", "Round Robin") }
        });
        int id = service.ListMultiClassEvents()[0].Id;

        service.DeleteMultiClassEvent(id);

        Assert.AreEqual(0, service.ListMultiClassEvents().Count);
    }

    // ── LoadResult ────────────────────────────────────────────────────────────

    [TestMethod]
    public void LoadResult_Ok_HasSuccessAndEvent()
    {
        var evt = new MultiClassEvent { EventName = "X", ClassSessions = new List<RaceSession>() };

        var result = LoadResult.Ok(evt);

        Assert.IsTrue(result.Success);
        Assert.AreSame(evt, result.Event);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public void LoadResult_Fail_HasErrorAndNoEvent()
    {
        var result = LoadResult.Fail("oops");

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Event);
        Assert.AreEqual("oops", result.ErrorMessage);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RaceSession MakeSession(string eventName, string classType, string raceType) =>
        new RaceSession
        {
            EventName = eventName,
            ClassType = classType,
            RaceType = raceType,
            EventDate = new DateTime(2026, 1, 1),
            Drivers = new List<Driver>()
        };

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-loadsession-svc-tests-{Guid.NewGuid():N}.db");
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
