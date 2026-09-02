using System;
using System.Linq;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Guards the rule that nothing starts the Finals except the Race Director.
///
/// Round Robin completion used to inject the Finals itself. Because the
/// RoundRobinCompleted event opens a modal standings window, closing that window
/// returned control to the injection call and the RD was already racing the Finals
/// with no way to stop — which is exactly what "No auto-advancement" forbids.
/// Completion now only raises the gate; StartFinals does the work.
/// </summary>
[TestClass]
[DoNotParallelize]
public class RaceControllerFinalsGateTests
{
    // ── QMDRA: every driver advances ──────────────────────────────────────────

    [TestMethod]
    public void Qmdra_RoundRobinComplete_RaisesTheGateWithoutStartingFinals()
    {
        var session = QmdraSession(roundsToRun: 1);
        var controller = NewController(session);
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));

        ResolveVisibleMatches(controller);

        Assert.IsTrue(controller.IsFinalsPending, "Round Robin completion must raise the Finals gate.");
        Assert.AreEqual("Round Robin", session.RaceType,
            "The class must still be in Round Robin until the RD starts the Finals.");
        Assert.AreEqual(RaceController.FinalsReasonRoundRobinAllAdvance, controller.FinalsPendingReason);
    }

    [TestMethod]
    public void Qmdra_ClosingTheStandingsWindow_DoesNotStartFinals()
    {
        var session = QmdraSession(roundsToRun: 1);
        var controller = NewController(session);

        // The console opens a modal standings window from this event. Whatever the
        // handler does, returning from it must not have started the Finals.
        string raceTypeInsideHandler = null;
        controller.RoundRobinCompleted += () => raceTypeInsideHandler = session.RaceType;

        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));
        ResolveVisibleMatches(controller);

        Assert.AreEqual("Round Robin", raceTypeInsideHandler);
        Assert.AreEqual("Round Robin", session.RaceType);
    }

    [TestMethod]
    public void Qmdra_StartFinals_SeedsEveryDriverInRoundRobinOrder()
    {
        var session = QmdraSession(roundsToRun: 1);
        var controller = NewController(session);
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));
        ResolveVisibleMatches(controller);

        controller.StartFinals();

        Assert.AreEqual("Finals", session.RaceType);
        Assert.IsFalse(controller.IsFinalsPending);
        Assert.IsTrue(controller.PeekUpcomingMatches(1).Count > 0,
            "Starting the Finals must put a race on deck.");
    }

    [TestMethod]
    public void Qmdra_GateStaysUpUntilStartFinalsIsCalled()
    {
        var session = QmdraSession(roundsToRun: 1);
        var controller = NewController(session);
        var gateSignals = 0;
        controller.CanStartFinalsChanged += enabled => { if (enabled) gateSignals++; };

        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));
        ResolveVisibleMatches(controller);

        Assert.AreEqual(1, gateSignals, "The gate must be raised exactly once.");

        // Nothing the console does short of StartFinals may move the class on.
        controller.AdvanceRound();
        Assert.AreEqual("Round Robin", session.RaceType);
        Assert.IsTrue(controller.IsFinalsPending);
    }

    // ── Standard RR with too few drivers for a buyback ────────────────────────

    [TestMethod]
    public void StandardRr_TooFewForBuyback_RaisesTheGateInsteadOfAdvancing()
    {
        var session = StandardSession();
        var controller = NewController(session);
        // Four drivers: the Top 3 go through, leaving exactly one driver who is not
        // enough for a buyback round and instead becomes the wildcard finalist.
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));

        ResolveAllRounds(controller);

        Assert.IsTrue(controller.IsFinalsPending,
            "Too few drivers for a buyback must still gate the Finals, not advance into them.");
        Assert.AreEqual("Round Robin", session.RaceType);
        Assert.AreEqual(RaceController.FinalsReasonBuybackSkipped, controller.FinalsPendingReason);
        Assert.IsFalse(string.IsNullOrWhiteSpace(controller.FinalsPendingWildcardName),
            "The RD is told which driver goes through as the wildcard before they commit.");
    }

    [TestMethod]
    public void StandardRr_TooFewForBuyback_StartFinalsRunsTheFinalFour()
    {
        var session = StandardSession();
        var controller = NewController(session);
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));
        ResolveAllRounds(controller);

        controller.StartFinals();

        Assert.AreEqual("Finals", session.RaceType);
        Assert.IsFalse(controller.IsFinalsPending);
        Assert.IsTrue(controller.PeekUpcomingMatches(1).Count > 0);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Reset_ClearsAPendingFinalsGate()
    {
        var session = QmdraSession(roundsToRun: 1);
        var controller = NewController(session);
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));
        ResolveVisibleMatches(controller);
        Assert.IsTrue(controller.IsFinalsPending);

        controller.Reset();

        Assert.IsFalse(controller.IsFinalsPending);
        Assert.IsNull(controller.FinalsPendingReason);
        Assert.IsNull(controller.FinalsPendingWildcardName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ResolveVisibleMatches(RaceController controller)
    {
        foreach (var match in controller.PeekUpcomingMatches(20).ToList())
            controller.SubmitWinner(match.MatchId, firstOption: true);
    }

    /// <summary>
    /// Runs a Standard Round Robin to the end: resolve the active round, then reveal
    /// the next one, walking the round labels the bracket declared at generation.
    /// AdvanceRound is deliberately not called after the last round — that is the
    /// point at which the class decides what happens next.
    /// </summary>
    private static void ResolveAllRounds(RaceController controller)
    {
        var rounds = controller.BuildCurrentBracketRows()
            .Where(r => r.IsHeader)
            .Select(r => r.RoundLabel ?? "")
            .Where(r => r.StartsWith("RR", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        for (var i = 0; i < rounds.Count; i++)
        {
            ResolveVisibleMatches(controller);
            if (i < rounds.Count - 1) controller.AdvanceRound();
        }
    }

    private static RaceSession QmdraSession(int roundsToRun) => new RaceSession
    {
        EventName = "QA Finals Gate (QMDRA)",
        EventDate = new DateTime(2026, 9, 2, 9, 0, 0),
        RaceType = "Round Robin",
        ClassType = "Heads Up",
        RoundRobinVariant = "QMDRA",
        RoundsToRun = roundsToRun
    };

    private static RaceSession StandardSession() => new RaceSession
    {
        EventName = "QA Finals Gate (Standard)",
        EventDate = new DateTime(2026, 9, 2, 9, 0, 0),
        RaceType = "Round Robin",
        ClassType = "Heads Up",
        RoundRobinVariant = "Standard"
    };

    private static RaceController NewController(RaceSession session) =>
        new RaceController(session, new NoOpStandingsDialogService());
}
