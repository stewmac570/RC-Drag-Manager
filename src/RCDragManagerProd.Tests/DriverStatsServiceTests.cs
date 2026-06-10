using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="DriverStatsService"/> (issue #291) — match history queries
/// extracted from DriverStatsForm. Runs headless against a temporary SQLite database.
/// </summary>
[TestClass]
public class DriverStatsServiceTests
{
    private static DriverStatsService NewService(TemporarySqliteDb db)
    {
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        return new DriverStatsService(new RaceSessionRepository(db.ConnectionString));
    }

    [TestMethod]
    public void GetMatchHistory_NoSessions_ReturnsEmptyList()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var result = svc.GetMatchHistory(new Driver { Id = 1, Name = "Alice" });
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetMatchHistory_DriverNotInSession_ReturnsEmptyList()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var repo = new RaceSessionRepository(db.ConnectionString);

        var session = MakeSession(driverId: 2, winnerId: 2, loserId: 3);
        repo.SaveSession(session);

        var result = svc.GetMatchHistory(new Driver { Id = 1, Name = "Alice" });
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetMatchHistory_DriverWonMatch_ReturnsWinRow()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var repo = new RaceSessionRepository(db.ConnectionString);

        var session = MakeSession(driverId: 1, winnerId: 1, loserId: 2);
        session.EventName = "Spring Nats";
        repo.SaveSession(session);

        var result = svc.GetMatchHistory(new Driver { Id = 1, Name = "Alice" });

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Win", result[0].Result);
        Assert.AreEqual("Spring Nats", result[0].EventName);
    }

    [TestMethod]
    public void GetMatchHistory_DriverLostMatch_ReturnsLossRow()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var repo = new RaceSessionRepository(db.ConnectionString);

        var session = MakeSession(driverId: 1, winnerId: 2, loserId: 1);
        repo.SaveSession(session);

        var result = svc.GetMatchHistory(new Driver { Id = 1, Name = "Alice" });

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Loss", result[0].Result);
    }

    [TestMethod]
    public void GetMatchHistory_NullSavedResults_DoesNotThrow()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        var repo = new RaceSessionRepository(db.ConnectionString);

        var session = MakeSession(driverId: 1, winnerId: 1, loserId: 2);
        session.SavedResults = null;
        repo.SaveSession(session);

        var result = svc.GetMatchHistory(new Driver { Id = 1, Name = "Alice" });
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetMatchHistory_NullDriver_Throws()
    {
        using var db = new TemporarySqliteDb();
        var svc = NewService(db);
        Assert.ThrowsExactly<ArgumentNullException>(() => svc.GetMatchHistory(null));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RaceSession MakeSession(int driverId, int winnerId, int loserId)
    {
        return new RaceSession
        {
            EventName = "Test Event",
            EventDate = new DateTime(2026, 1, 1),
            RaceType  = "Pro Ladder",
            ClassType = "Heads Up",
            DriverEntries = new List<RaceSessionDriverEntry>
            {
                new RaceSessionDriverEntry { DriverID = 1, DriverName = "Alice" },
                new RaceSessionDriverEntry { DriverID = 2, DriverName = "Bob" }
            },
            SavedResults = new List<MatchResultSave>
            {
                new MatchResultSave { MatchId = 1, WinnerDriverId = winnerId, LoserDriverId = loserId }
            }
        };
    }

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-driverstats-svc-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); } catch { }
        }
    }
}
