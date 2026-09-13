using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RandomMode;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Guards the auto-checkpoint (issue #404): every state-changing console command
/// persists through the session store without the operator pressing Save Progress.
/// Winner entry, result edits, buyback decisions, and round/phase transitions all
/// route through <see cref="RaceConsoleService"/>, which now checkpoints after each
/// one; a service created without a store stays usable headless.
/// </summary>
[TestClass]
[DoNotParallelize]
public class RaceConsoleServiceAutoCheckpointTests
{
    [TestInitialize]
    public void ResetStatics() => RandomBracket.ResetByeTracker();

    // ── Service wiring: each mutating command checkpoints exactly once ───────────

    [TestMethod]
    public void ExecutePrimaryAction_WithStore_Checkpoints()
    {
        var store = new RecordingSessionStore();
        var service = new RaceConsoleService(
            new RaceController(TestSessionFactory.ProLadder()), store);

        service.ExecutePrimaryAction(TestDriverFactory.CreateProLadderPack(), "Pro Ladder");

        Assert.AreEqual(1, store.PersistCount,
            "Building the bracket must checkpoint without the operator pressing Save Progress");
    }

    [TestMethod]
    public void SubmitWinner_WithStore_Checkpoints()
    {
        var store = new RecordingSessionStore();
        var controller = new RaceController(TestSessionFactory.ProLadder());
        var service = new RaceConsoleService(controller, store);
        service.ExecutePrimaryAction(TestDriverFactory.CreateProLadderPack(), "Pro Ladder");
        int baseline = store.PersistCount;

        var match = controller.PeekUpcomingMatches(1).First();   // two real drivers, no BYE

        Assert.IsTrue(service.SubmitWinnerFromButton(match.MatchId, uiFirstOption: true).Accepted);
        Assert.AreEqual(baseline + 1, store.PersistCount,
            "Every recorded winner must be checkpointed");
    }

    [TestMethod]
    public void ApplyEditResult_WithStore_Checkpoints()
    {
        var store = new RecordingSessionStore();
        var controller = new RaceController(TestSessionFactory.ProLadder());
        var service = new RaceConsoleService(controller, store);
        service.ExecutePrimaryAction(TestDriverFactory.CreateProLadderPack(), "Pro Ladder");
        var match = controller.PeekUpcomingMatches(1).First();
        controller.SubmitWinner(match.MatchId, firstOption: true);
        int baseline = store.PersistCount;

        Assert.IsTrue(service.ApplyEditResult(match.MatchId, engineFirstOption: false));
        Assert.AreEqual(baseline + 1, store.PersistCount,
            "An edited result must be checkpointed");
    }

    [TestMethod]
    public void AdvanceRound_WithStore_Checkpoints()
    {
        var store = new RecordingSessionStore();
        var controller = new RaceController(TestSessionFactory.ProLadder());
        var service = new RaceConsoleService(controller, store);
        service.ExecutePrimaryAction(TestDriverFactory.CreateProLadderPack(), "Pro Ladder");
        foreach (var m in controller.PeekUpcomingMatches(10).ToList())
            controller.SubmitWinner(m.MatchId, firstOption: true);
        int baseline = store.PersistCount;

        service.AdvanceRound();

        Assert.AreEqual("F", controller.GetActiveRoundLabel(), "Pro Ladder advances from SF to the Final");
        Assert.AreEqual(baseline + 1, store.PersistCount,
            "Advancing a round must checkpoint the transition");
    }

    [TestMethod]
    public void ApplyBuybackSelection_WithStore_Checkpoints()
    {
        var store = new RecordingSessionStore();
        var service = new RaceConsoleService(
            new RaceController(TestSessionFactory.RoundRobin()), store);

        var outcome = service.ApplyBuybackSelection(TestDriverFactory.CreateRoundRobinPack(2));

        Assert.AreEqual(BuybackSelectionOutcome.Stored, outcome);
        Assert.AreEqual(1, store.PersistCount,
            "Storing the buyback selection must checkpoint");
    }

    [TestMethod]
    public void SkipBuybacks_WithStore_Checkpoints()
    {
        var store = new RecordingSessionStore();
        var controller = RunRoundRobinToBuybackOffer();
        var service = new RaceConsoleService(controller, store);

        Assert.IsTrue(service.SkipBuybacks());
        Assert.AreEqual(1, store.PersistCount,
            "Skipping the buyback and opening the Finals must checkpoint");
    }

    // ── End to end: the checkpoint must survive a kill-and-reload ───────────────

    [TestMethod]
    public void SubmitWinner_AutoCheckpoint_ResultSurvivesReload()
    {
        using var db = new TempDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var session = NewSession("Pro Ladder");
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        var store = new RepositorySessionStore(new RaceSessionRepository(db.ConnectionString), session);
        var service = new RaceConsoleService(controller, store);

        // Both actions checkpoint through the real repository; the second is an
        // UPDATE of the row the first INSERTed.
        service.ExecutePrimaryAction(TestDriverFactory.CreateProLadderPack(), "Pro Ladder");
        var match = controller.PeekUpcomingMatches(1).First();
        Assert.IsTrue(service.SubmitWinnerFromButton(match.MatchId, uiFirstOption: true).Accepted);

        var loaded = new RaceSessionRepository(db.ConnectionString).LoadSession(session.Id);
        Assert.IsNotNull(loaded, "Checkpoints must write the session row without Save Progress");

        var saved = loaded.SavedResults.FirstOrDefault(r => r.MatchId == match.MatchId);
        Assert.IsNotNull(saved, "The recorded winner must be on disk");
        Assert.AreEqual(controller.GetWinner(match.MatchId).Id, saved.WinnerDriverId,
            "A kill-and-reload must find the result the operator just entered");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Drives a Standard Round Robin through every round to the buyback offer,
    /// the state the console is in when the operator declines the buyback.</summary>
    private static RaceController RunRoundRobinToBuybackOffer()
    {
        var session = new RaceSession
        {
            EventName = "Auto Checkpoint RR",
            EventDate = new DateTime(2026, 9, 5),
            RaceType = "Round Robin",
            ClassType = "Open",
            RoundRobinVariant = "Standard"
        };
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(6));

        var rrRoundLabels = controller.BuildCurrentBracketRows()
            .Where(r => r.IsHeader)
            .Select(r => r.RoundLabel ?? "")
            .Where(r => r.StartsWith("RR", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        for (int ri = 0; ri < rrRoundLabels.Count; ri++)
        {
            foreach (var match in controller.PeekUpcomingMatches(20).ToList())
                controller.SubmitWinner(match.MatchId, firstOption: true);

            if (ri < rrRoundLabels.Count - 1)
                controller.AdvanceRound();
        }

        return controller;
    }

    private static RaceSession NewSession(string raceType) => new RaceSession
    {
        EventName = "QA AutoCheckpoint " + raceType,
        EventDate = new DateTime(2026, 9, 13, 9, 0, 0),
        RaceType = raceType,
        ClassType = "Heads Up"
    };

    /// <summary>The persistence path the app uses: writes the live session through
    /// <see cref="RaceSessionRepository"/>, just like <c>RaceConsoleView</c>.</summary>
    private sealed class RepositorySessionStore : IRaceSessionStore
    {
        private readonly RaceSessionRepository _repo;
        private readonly RaceSession _session;

        public RepositorySessionStore(RaceSessionRepository repo, RaceSession session)
        {
            _repo = repo;
            _session = session;
        }

        public void Persist() => _repo.SaveSession(_session);
    }

    private sealed class TempDb : IDisposable
    {
        public TempDb() =>
            DatabasePath = Path.Combine(Path.GetTempPath(), $"rcdragmanager-autocheckpoint-{Guid.NewGuid():N}.db");

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); }
            catch { /* best-effort */ }
        }
    }
}
