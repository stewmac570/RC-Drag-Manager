using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RandomMode;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Button-free coverage for issue #304 — the Save Progress vs Close Race distinction
/// (issue #255). These prove the two operator actions are NOT interchangeable:
///
///   • Save Progress persists a resumable checkpoint and leaves the race OPEN
///     (IsClosed == false, a resume snapshot is written, and the state restores).
///   • Close Race marks the event FINISHED (IsClosed == true) without, by itself,
///     capturing a resumable in-progress checkpoint.
///
/// Each test drives a controller to an identical mid-event state, applies the action,
/// round-trips the session through the REAL SQLite repository, and inspects the loaded
/// session — exactly the persistence path the app uses.
///
/// The remaining #304 acceptance criteria (save at every event stage + full resume of
/// round/bracket/driver/results/next-action) are covered by RaceControllerResumeTests.
/// </summary>
[TestClass]
[DoNotParallelize]
public class RaceControllerSaveCloseTests
{
    [TestInitialize]
    public void ResetStatics() => RandomBracket.ResetByeTracker();

    [TestMethod]
    public void SaveProgress_OnInProgressRace_PersistsResumableCheckpoint()
    {
        var (session, controller) = NewMidEventRace();

        controller.SaveProgress();
        var loaded = Persist(session);

        Assert.IsFalse(loaded.IsClosed, "Save Progress must leave the race open (resumable)");
        Assert.IsNotNull(loaded.Resume, "Save Progress must capture a resume snapshot");

        var restored = new RaceController(loaded, new NoOpStandingsDialogService());
        restored.RestoreFromSave();
        Assert.IsTrue(restored.HasBracketStarted, "Saved in-progress race must restore its bracket");
        Assert.IsTrue(restored.PeekUpcomingMatches(10).Count > 0,
            "A mid-event Save Progress must restore with pending matches still to run");
    }

    [TestMethod]
    public void CloseRace_MarksEventComplete()
    {
        var (session, controller) = NewMidEventRace();

        controller.CloseRace();
        var loaded = Persist(session);

        Assert.IsTrue(loaded.IsClosed, "Close Race must mark the event finished");
    }

    [TestMethod]
    public void RestoreFromSave_ClosedRace_RemainsResultsOnly()
    {
        var (session, controller) = NewMidEventRace();
        controller.SaveProgress();
        controller.CloseRace();
        var loaded = Persist(session);

        var restored = new RaceController(loaded, new NoOpStandingsDialogService());
        restored.RestoreFromSave();

        Assert.IsTrue(restored.IsCompleted);
        Assert.IsFalse(restored.HasBracketStarted,
            "A closed race must not reconstruct a live engine that can be rerun");

        restored.GenerateBracket(loaded.RaceType, loaded.Drivers);
        Assert.IsFalse(restored.HasBracketStarted,
            "GenerateBracket must reject completed races");
    }

    [TestMethod]
    public void RestoreFromSave_RaceWithCompletedResults_RemainsResultsOnly()
    {
        var (session, controller) = NewMidEventRace();
        controller.SaveProgress();
        session.ResultsArchive.CompletedAt = new DateTime(2026, 6, 8, 10, 0, 0);
        var loaded = Persist(session);

        var restored = new RaceController(loaded, new NoOpStandingsDialogService());
        restored.RestoreFromSave();

        Assert.IsTrue(restored.IsCompleted);
        Assert.IsFalse(restored.HasBracketStarted);
    }

    [TestMethod]
    public void GenerateBracket_WhenEngineAlreadyExists_RejectsPhaseNameWithoutCrashing()
    {
        var session = NewSession("Round Robin");
        var drivers = TestDriverFactory.CreateRoundRobinPack(6);
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Round Robin", drivers);
        var before = controller.PeekUpcomingMatches(10)
            .Select(m => m.MatchId)
            .ToArray();

        session.RaceType = "Finals";
        controller.GenerateBracket(session.RaceType, drivers);

        CollectionAssert.AreEqual(before, controller.PeekUpcomingMatches(10)
            .Select(m => m.MatchId)
            .ToArray());
    }

    [TestMethod]
    public void SaveProgress_AndCloseRace_AreNotTheSameAction()
    {
        var (saveSession, saveController) = NewMidEventRace();
        var (closeSession, closeController) = NewMidEventRace();

        saveController.SaveProgress();
        closeController.CloseRace();

        var saved = Persist(saveSession);
        var closed = Persist(closeSession);

        // The defining difference: Save keeps the race resumable/open; Close ends it.
        Assert.AreNotEqual(saved.IsClosed, closed.IsClosed,
            "Save Progress and Close Race must not record the same race state");
        Assert.IsFalse(saved.IsClosed, "Save Progress leaves the race open");
        Assert.IsTrue(closed.IsClosed, "Close Race marks the race finished");
        Assert.IsNotNull(saved.Resume, "Only Save Progress writes a resumable checkpoint");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Pro Ladder generated and one semifinal resolved — a genuine in-progress event.</summary>
    private static (RaceSession session, RaceController controller) NewMidEventRace()
    {
        var session = NewSession("Pro Ladder");
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        var first = controller.PeekUpcomingMatches(10).First();
        controller.SubmitWinner(first.MatchId, firstOption: true);
        return (session, controller);
    }

    private static RaceSession NewSession(string raceType) => new RaceSession
    {
        EventName = "QA SaveClose " + raceType,
        EventDate = new DateTime(2026, 6, 8, 9, 0, 0),
        RaceType = raceType,
        ClassType = "Heads Up"
    };

    /// <summary>Round-trips the session through the real repository and returns the loaded copy.</summary>
    private static RaceSession Persist(RaceSession session)
    {
        using var db = new TempDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new RaceSessionRepository(db.ConnectionString);

        var id = repo.SaveSession(session);
        var loaded = repo.LoadSession(id);
        Assert.IsNotNull(loaded, "Session must load back from the repository");
        return loaded;
    }

    private sealed class TempDb : IDisposable
    {
        public TempDb() =>
            DatabasePath = Path.Combine(Path.GetTempPath(), $"rcdragmanager-saveclose-{Guid.NewGuid():N}.db");

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); }
            catch { /* best-effort */ }
        }
    }
}
