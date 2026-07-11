using System;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Regression tests for issue #382 — EventDate strings were written with
/// culture-sensitive ToString (custom format separators are substituted per
/// culture) and read back with culture-sensitive parsing; a malformed value
/// crashed MultiClassEventRepository.GetAllEvents via bare DateTime.Parse.
/// </summary>
[TestClass]
public class RepositoryDateCultureTests
{
    private static readonly DateTime SampleDate = new DateTime(2026, 7, 11, 18, 30, 45);

    private static T RunUnderCulture<T>(string cultureName, Func<T> action)
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);
            return action();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [TestMethod]
    public void DbDate_RoundTrips_UnderCultureWithNonColonTimeSeparator()
    {
        var roundTripped = RunUnderCulture("fi-FI", () => DbDate.ParseOrMinValue(DbDate.ToDbString(SampleDate)));
        Assert.AreEqual(SampleDate, roundTripped);
    }

    [TestMethod]
    public void DbDate_ToDbString_IsInvariantAcrossCultures()
    {
        var fi = RunUnderCulture("fi-FI", () => DbDate.ToDbString(SampleDate));
        var us = RunUnderCulture("en-US", () => DbDate.ToDbString(SampleDate));
        Assert.AreEqual("2026-07-11 18:30:45", fi);
        Assert.AreEqual(fi, us);
    }

    [TestMethod]
    public void DbDate_ParseOrMinValue_ReturnsMinValueForGarbage()
    {
        Assert.AreEqual(DateTime.MinValue, DbDate.ParseOrMinValue(null));
        Assert.AreEqual(DateTime.MinValue, DbDate.ParseOrMinValue(""));
        Assert.AreEqual(DateTime.MinValue, DbDate.ParseOrMinValue("not-a-date"));
    }

    [TestMethod]
    public void RaceSession_EventDate_RoundTripsUnderForeignCulture()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new RaceSessionRepository(db.ConnectionString);

        var summaries = RunUnderCulture("fi-FI", () =>
        {
            repo.SaveSession(new RaceSession
            {
                EventName = "Culture Night",
                EventDate = SampleDate,
                ClassType = "Open",
                RaceType = "Pro Ladder"
            });
            return repo.GetAllSessions();
        });

        Assert.AreEqual(1, summaries.Count);
        Assert.AreEqual(SampleDate, summaries[0].EventDate);
    }

    [TestMethod]
    public void MultiClassEvent_EventDate_RoundTripsUnderForeignCulture()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new MultiClassEventRepository(db.ConnectionString);

        var events = RunUnderCulture("fi-FI", () =>
        {
            repo.SaveEvent(new MultiClassEvent
            {
                EventName = "Culture Cup",
                EventDate = SampleDate
            });
            return repo.GetAllEvents();
        });

        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(SampleDate, events[0].EventDate);
    }

    [TestMethod]
    public void GetAllEvents_MalformedEventDateRow_DoesNotThrow()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new MultiClassEventRepository(db.ConnectionString);

        using (var cn = new SQLiteConnection(db.ConnectionString))
        {
            cn.Open();
            using var cmd = new SQLiteCommand(@"
INSERT INTO MultiClassEvents (EventName, EventDate, ClassCount, EventData)
VALUES ('Damaged', 'garbage-date', 1, '{}');", cn);
            cmd.ExecuteNonQuery();
        }

        var events = repo.GetAllEvents();

        Assert.AreEqual(1, events.Count, "The damaged row must still be listed, not crash the screen.");
        Assert.AreEqual(DateTime.MinValue, events[0].EventDate);
    }

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-dateculture-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); } catch { }
        }
    }
}
