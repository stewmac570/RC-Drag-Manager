using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers the cheap list-time validation introduced for issue #384.
/// GetAllSessions used to fully deserialize every row's SessionData just to
/// decide loadability; it now only parses. The observable contract: rows whose
/// JSON parses are listed even if full deserialization would fail (the load
/// path reports those with a friendly error), while empty/unparseable rows are
/// still hidden exactly as before (#373 behaviour).
/// </summary>
[TestClass]
public class RaceSessionListValidationTests
{
    [TestMethod]
    public void GetAllSessions_ListsParseableRowsWithoutFullDeserialization()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new RaceSessionRepository(db.ConnectionString);

        // Valid JSON object, but full RaceSession binding would throw
        // (driverEntries must be an array). If GetAllSessions still bound the
        // POCO, this row would be skipped — listing it proves the cheap path.
        int shapeInvalidId = InsertRawSession(db.ConnectionString,
            "QA Shape Invalid", @"{""driverEntries"": ""not-an-array""}");

        var sessions = repository.GetAllSessions();

        Assert.IsTrue(sessions.Any(s => s.Id == shapeInvalidId),
            "Parseable rows must be listed without binding the full RaceSession.");
        Assert.AreEqual(
            RaceSessionLoadStatus.InvalidData,
            repository.TryLoadSession(shapeInvalidId).Status,
            "The load path still reports the row as invalid, surfacing a friendly error.");
    }

    [TestMethod]
    public void GetAllSessions_StillHidesEmptyAndUnparseableRows()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new RaceSessionRepository(db.ConnectionString);

        int validId = repository.SaveSession(new RaceSession
        {
            EventName = "QA Valid",
            EventDate = new DateTime(2026, 7, 11, 12, 0, 0),
            ClassType = "Open",
            RaceType = "Pro Ladder"
        });
        InsertRawSession(db.ConnectionString, "QA Empty", "");
        InsertRawSession(db.ConnectionString, "QA Broken", "{not-json");
        InsertRawSession(db.ConnectionString, "QA Not Object", "[1,2,3]");

        var sessions = repository.GetAllSessions();

        Assert.AreEqual(1, sessions.Count);
        Assert.AreEqual(validId, sessions[0].Id);
    }

    private static int InsertRawSession(string connectionString, string eventName, string sessionData)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();
        using var command = new SQLiteCommand(@"
INSERT INTO RaceSessions (EventName, EventDate, ClassType, RaceType, SessionData)
VALUES (@EventName, '2026-07-11 12:00:00', 'Open', 'Pro Ladder', @SessionData);
SELECT last_insert_rowid();", connection);
        command.Parameters.AddWithValue("@EventName", eventName);
        command.Parameters.AddWithValue("@SessionData", sessionData);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-listvalidation-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); } catch { }
        }
    }
}
