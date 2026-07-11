using System;
using System.Data.SQLite;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Regression tests for issue #385 — SaveSession/SaveEvent used to INSERT a new
/// row and then immediately UPDATE the same row with identical data, serializing
/// the JSON blob twice per save. These tests pin the contract the split insert/
/// update paths must keep: ids round-trip on first save, re-saves update in
/// place (no extra rows), and loads return the latest data.
/// </summary>
[TestClass]
public class RepositorySaveRoundTripTests
{
    [TestMethod]
    public void SaveSession_NewThenResave_KeepsOneRowAndLatestData()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new RaceSessionRepository(db.ConnectionString);

        var session = new RaceSession
        {
            EventName = "First Save",
            EventDate = new DateTime(2026, 7, 11, 18, 0, 0),
            ClassType = "Open",
            RaceType = "Pro Ladder"
        };

        var savedId = repo.SaveSession(session);
        Assert.IsTrue(savedId > 0);
        Assert.AreEqual(savedId, session.Id, "First save must assign the row id.");

        session.EventName = "Renamed Event";
        var resavedId = repo.SaveSession(session);

        Assert.AreEqual(savedId, resavedId, "Re-save must update in place, not insert.");
        Assert.AreEqual(1, CountRows(db.ConnectionString, "RaceSessions"));

        var loaded = repo.LoadSession(savedId);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(savedId, loaded.Id, "Load must overwrite the embedded id with the row id.");
        Assert.AreEqual("Renamed Event", loaded.EventName);
    }

    [TestMethod]
    public void SaveEvent_NewThenResave_KeepsOneRowAndLatestData()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new MultiClassEventRepository(db.ConnectionString);

        var evt = new MultiClassEvent
        {
            EventName = "First Save",
            EventDate = new DateTime(2026, 7, 11, 18, 0, 0)
        };

        repo.SaveEvent(evt);
        var savedId = evt.Id;
        Assert.IsTrue(savedId > 0, "First save must assign the row id.");

        evt.EventName = "Renamed Event";
        repo.SaveEvent(evt);

        Assert.AreEqual(savedId, evt.Id, "Re-save must update in place, not insert.");
        Assert.AreEqual(1, CountRows(db.ConnectionString, "MultiClassEvents"));

        var loaded = repo.LoadEvent(savedId);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(savedId, loaded.Id, "Load must overwrite the embedded id with the row id.");
        Assert.AreEqual("Renamed Event", loaded.EventName);
    }

    private static int CountRows(string connectionString, string table)
    {
        using var cn = new SQLiteConnection(connectionString);
        cn.Open();
        using var cmd = new SQLiteCommand($"SELECT COUNT(*) FROM {table}", cn);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-saveroundtrip-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); } catch { }
        }
    }
}
