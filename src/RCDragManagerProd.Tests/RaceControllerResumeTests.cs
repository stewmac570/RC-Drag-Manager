using System;
using System.Collections.Generic;
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
/// Button-free regression tests for issue #294 — resume of an interrupted event.
///
/// Each test drives a controller to a save point, persists the session through the
/// REAL repository (SQLite round-trip, exactly as the app does), constructs a fresh
/// controller from the loaded session, calls <see cref="RaceController.RestoreFromSave"/>,
/// and asserts the restored observable state (bracket structure, recorded winners,
/// active round, pending matches, finals-pending flag) matches the pre-save state.
///
/// State is compared by (round label + unordered driver-name pair) rather than raw
/// MatchId, because restored engines regenerate/re-inject and may assign new MatchIds.
///
/// The Random-mode tests are the critical ones: Random brackets are NON-deterministic
/// (RandomBracket shuffles), so a faithful resume MUST re-inject the saved pairings.
/// If restore regenerated instead, the pairings would reshuffle and the assertions fail.
/// </summary>
[TestClass]
[DoNotParallelize]
public class RaceControllerResumeTests
{
    [TestInitialize]
    public void ResetStatics() => RandomBracket.ResetByeTracker();

    // ── Save point 1: setup (no bracket generated) ───────────────────────────

    [TestMethod]
    public void Setup_NoBracketYet_RestoreIsNoOp()
    {
        var session = NewSession("Pro Ladder");
        var controller = new RaceController(session, new NoOpStandingsDialogService());

        var restored = SaveRestore(session, controller);

        // No engine existed at save time, so no resume snapshot is written and the
        // fresh controller has no bracket — restore is a clean no-op (must not throw).
        Assert.IsNull(session.Resume, "No snapshot should be captured before a bracket exists");
        Assert.IsFalse(restored.HasBracketStarted, "Restore must not fabricate a bracket from an empty snapshot");
        Assert.AreEqual(0, restored.BuildCurrentBracketRows().Count);
    }

    // ── Save point 2: post-bracket, no winners (Pro Ladder) ──────────────────

    [TestMethod]
    public void ProLadder_PostBracket_RestoresStructure()
    {
        var session = NewSession("Pro Ladder");
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        var before = Capture(controller);
        var restored = SaveRestore(session, controller);

        AssertSameState(before, Capture(restored));
    }

    // ── Save point 3: mid-round, partial results (Pro Ladder) ────────────────

    [TestMethod]
    public void ProLadder_MidRound_RestoresPartialResults()
    {
        var session = NewSession("Pro Ladder");
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        // Resolve exactly one of the two semifinal matches.
        var first = controller.PeekUpcomingMatches(10).First();
        controller.SubmitWinner(first.MatchId, firstOption: true);

        var before = Capture(controller);
        Assert.AreEqual(1, before.Results.Count, "Exactly one match should be resolved at this save point");

        var restored = SaveRestore(session, controller);
        AssertSameState(before, Capture(restored));
    }

    // ── Save point 4: between rounds (Pro Ladder, finals pending) ────────────

    [TestMethod]
    public void ProLadder_BetweenRounds_RestoresState()
    {
        var session = NewSession("Pro Ladder");
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        // Resolve the whole semifinal round and advance into the final.
        foreach (var m in controller.PeekUpcomingMatches(10).ToList())
            controller.SubmitWinner(m.MatchId, firstOption: true);
        controller.AdvanceRound();

        var before = Capture(controller);
        var restored = SaveRestore(session, controller);

        AssertSameState(before, Capture(restored));
    }

    // ── Save point 5: post-finals (Pro Ladder, champion decided) ─────────────

    [TestMethod]
    public void ProLadder_PostFinals_RestoresChampion()
    {
        var session = NewSession("Pro Ladder");
        var controller = new RaceController(session, new NoOpStandingsDialogService());

        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());
        RunOut(controller);

        var before = Capture(controller);
        Assert.IsTrue(before.Results.Count > 0, "All matches should be resolved at the post-finals save point");
        Assert.AreEqual(0, before.Upcoming.Count, "No matches should remain after the final");

        var restored = SaveRestore(session, controller);
        AssertSameState(before, Capture(restored));
    }

    // ── Random mode: re-injection must preserve EXACT pairings (critical) ─────

    [TestMethod]
    public void Random_PostBracket_ReInjectionPreservesExactPairings()
    {
        var session = NewSession("Random");
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Random", TestDriverFactory.CreateRoundRobinPack(8));

        var before = Capture(controller);
        Assert.AreEqual(4, before.Upcoming.Count, "Random R1 with 8 drivers must have 4 matches");

        var restored = SaveRestore(session, controller);
        var after = Capture(restored);

        // The exact R1 pairings must survive the round-trip. Random regeneration would
        // reshuffle these; equality here is the proof that restore re-injected.
        AssertSameState(before, after);
        CollectionAssert.AreEqual(before.Upcoming, after.Upcoming,
            "Restored Random R1 pairings must be identical to the saved pairings (re-injection, not reshuffle)");
    }

    [TestMethod]
    public void Random_BetweenRounds_RestoresState()
    {
        var session = NewSession("Random");
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Random", TestDriverFactory.CreateRoundRobinPack(8));

        foreach (var m in controller.PeekUpcomingMatches(10).ToList())
            controller.SubmitWinner(m.MatchId, firstOption: true);
        controller.AdvanceRound();
        Assert.AreEqual("R2", controller.GetActiveRoundLabel());

        var before = Capture(controller);
        var restored = SaveRestore(session, controller);

        AssertSameState(before, Capture(restored));
    }

    // ── Round Robin mid-event ────────────────────────────────────────────────

    [TestMethod]
    public void RoundRobin_MidEvent_RestoresState()
    {
        var session = NewSession("Round Robin");
        session.RoundRobinVariant = "Standard";
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(6));

        // Resolve RR1 and advance into RR2.
        foreach (var m in controller.PeekUpcomingMatches(20).ToList())
            controller.SubmitWinner(m.MatchId, firstOption: true);
        controller.AdvanceRound();

        var before = Capture(controller);
        var restored = SaveRestore(session, controller);

        AssertSameState(before, Capture(restored));
    }

    // ── Full multi-phase: RR → Buyback/LB and RR → … → Finals ────────────────

    [TestMethod]
    public void MultiPhase_LosersBracketPhase_RestoresState()
    {
        var (session, controller) = RunToLosersBracket();

        var before = Capture(controller);
        Assert.AreEqual("Losers Bracket", session.RaceType);
        Assert.IsTrue(before.Upcoming.Count > 0, "LB phase must have pending matches");

        var restored = SaveRestore(session, controller);
        AssertSameState(before, Capture(restored));
    }

    [TestMethod]
    public void MultiPhase_FinalsPhase_RestoresState()
    {
        var (session, controller) = RunToLosersBracket();

        // Drive the LB to completion so finals become pending, then start finals.
        var startFinals = false;
        controller.CanStartFinalsChanged += v => { if (v) startFinals = true; };
        int guard = 40;
        while (!startFinals && guard-- > 0)
        {
            var matches = controller.PeekUpcomingMatches(20).ToList();
            if (matches.Count == 0) controller.AdvanceRound();
            else foreach (var m in matches) controller.SubmitWinner(m.MatchId, firstOption: true);
        }
        Assert.IsTrue(controller.IsFinalsPending, "Finals must be pending after LB completes");
        controller.StartFinals();
        Assert.AreEqual("Finals", session.RaceType);

        var before = Capture(controller);
        var restored = SaveRestore(session, controller);

        AssertSameState(before, Capture(restored));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (RaceSession, RaceController) RunToLosersBracket()
    {
        var session = NewSession("Round Robin");
        session.RoundRobinVariant = "Standard";
        var controller = new RaceController(session, new NoOpStandingsDialogService());

        var canBuyback = false;
        controller.CanOfferBuybackChanged += v => { if (v) canBuyback = true; };

        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(6));

        int guard = 40;
        while (!canBuyback && guard-- > 0)
        {
            var matches = controller.PeekUpcomingMatches(20).ToList();
            if (matches.Count == 0) controller.AdvanceRound();
            else foreach (var m in matches) controller.SubmitWinner(m.MatchId, firstOption: true);
        }

        Assert.IsTrue(canBuyback, "Buyback must be offered after all RR rounds complete");
        var eligible = controller.GetEligibleBuybackDrivers();
        controller.GenerateLosersBracket(eligible);
        return (session, controller);
    }

    /// <summary>Resolves every match and advances until the bracket can advance no further.</summary>
    private static void RunOut(RaceController controller)
    {
        int guard = 40;
        while (guard-- > 0)
        {
            var matches = controller.PeekUpcomingMatches(20).ToList();
            if (matches.Count > 0)
            {
                foreach (var m in matches) controller.SubmitWinner(m.MatchId, firstOption: true);
                continue;
            }

            var before = controller.GetActiveRoundLabel();
            controller.AdvanceRound();
            if (string.Equals(controller.GetActiveRoundLabel(), before, StringComparison.OrdinalIgnoreCase))
                break; // no further round to reveal — bracket is complete
        }
    }

    private static RaceSession NewSession(string raceType) => new RaceSession
    {
        EventName = "QA Resume " + raceType,
        EventDate = new DateTime(2026, 6, 8, 9, 0, 0),
        RaceType = raceType,
        ClassType = "Heads Up"
    };

    /// <summary>
    /// Persists the controller's session through the real repository, loads it back into a
    /// fresh controller, and runs the restore — the exact path the app takes on resume.
    /// </summary>
    private static RaceController SaveRestore(RaceSession session, RaceController controller)
    {
        controller.SaveSession();

        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repo = new RaceSessionRepository(db.ConnectionString);

        var id = repo.SaveSession(session);
        var loaded = repo.LoadSession(id);
        Assert.IsNotNull(loaded, "Session must load back from the repository");

        var restored = new RaceController(loaded, new NoOpStandingsDialogService());
        restored.RestoreFromSave();
        return restored;
    }

    private sealed class StateSnapshot
    {
        public string ActiveRound = "";
        public bool FinalsPending;
        public List<string> Structure = new List<string>();
        public List<string> Results = new List<string>();
        public List<string> Upcoming = new List<string>();
    }

    private static StateSnapshot Capture(RaceController c)
    {
        var snap = new StateSnapshot
        {
            ActiveRound = c.GetActiveRoundLabel() ?? "",
            FinalsPending = c.IsFinalsPending
        };

        foreach (var r in c.BuildCurrentBracketRows().Where(r => !r.IsHeader))
        {
            var pair = NormPair(r.Driver1, r.Driver2, r.RoundLabel);
            snap.Structure.Add(pair);
            var w = c.GetWinner(r.MatchId);
            if (w != null) snap.Results.Add(pair + "|" + w.Name);
        }

        foreach (var m in c.PeekUpcomingMatches(50))
            snap.Upcoming.Add(NormPair(m.Driver1?.Name, m.Driver2?.Name, m.RoundLabel));

        snap.Structure.Sort(StringComparer.Ordinal);
        snap.Results.Sort(StringComparer.Ordinal);
        snap.Upcoming.Sort(StringComparer.Ordinal);
        return snap;
    }

    private static string NormPair(string a, string b, string round)
    {
        a ??= ""; b ??= "";
        var swap = string.CompareOrdinal(a, b) > 0;
        var lo = swap ? b : a;
        var hi = swap ? a : b;
        return $"{(round ?? "").Trim().ToUpperInvariant()}|{lo}|{hi}";
    }

    private static void AssertSameState(StateSnapshot before, StateSnapshot after)
    {
        Assert.AreEqual(before.ActiveRound, after.ActiveRound, "Active round label must match after restore");
        Assert.AreEqual(before.FinalsPending, after.FinalsPending, "Finals-pending flag must match after restore");
        CollectionAssert.AreEqual(before.Structure, after.Structure,
            $"Bracket structure must match.\n  before: [{string.Join("; ", before.Structure)}]\n  after:  [{string.Join("; ", after.Structure)}]");
        CollectionAssert.AreEqual(before.Results, after.Results,
            $"Recorded results must match.\n  before: [{string.Join("; ", before.Results)}]\n  after:  [{string.Join("; ", after.Results)}]");
        CollectionAssert.AreEqual(before.Upcoming, after.Upcoming,
            $"Pending matches must match.\n  before: [{string.Join("; ", before.Upcoming)}]\n  after:  [{string.Join("; ", after.Upcoming)}]");
    }

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(Path.GetTempPath(), $"rcdragmanager-resume-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); }
            catch { /* best-effort */ }
        }
    }
}
