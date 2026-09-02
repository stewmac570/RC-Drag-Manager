using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.Tests;

[TestClass]
[DoNotParallelize]
public class RaceControllerQmdraFlowTests
{
    [TestMethod]
    public void Qmdra_InitializesRoundRobinFlow_WithoutBuybackSignals()
    {
        var drivers = TestDriverFactory.CreateRoundRobinPack(4);
        var session = CreateQmdraSession(roundsToRun: 1);
        var controller = CreateController(session);

        var canOfferBuybackSignals = new List<bool>();
        controller.CanOfferBuybackChanged += value => canOfferBuybackSignals.Add(value);

        controller.GenerateBracket("Round Robin", drivers);

        var nextMatches = controller.PeekUpcomingMatches(10);
        Assert.AreEqual("Round Robin", session.RaceType);
        Assert.AreEqual("RR1", controller.GetActiveRoundLabel());
        Assert.IsFalse(controller.IsFinalsPending);
        Assert.IsTrue(nextMatches.Count > 0);
        Assert.IsTrue(nextMatches.All(m => m.RoundLabel.StartsWith("RR", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(canOfferBuybackSignals.Any(v => v));
    }

    [TestMethod]
    public void Qmdra_WaitsForConfiguredRoundCount_BeforeFinalsTransition()
    {
        var session = CreateQmdraSession(roundsToRun: 2);
        var controller = CreateController(session);
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));

        ResolveVisibleMatches(controller);

        Assert.AreEqual("Round Robin", session.RaceType);
        Assert.AreEqual("RR1", controller.GetActiveRoundLabel());

        controller.AdvanceRound();
        Assert.AreEqual("RR2", controller.GetActiveRoundLabel());

        ResolveVisibleMatches(controller);

        // The Finals are gated on the RD now: the class stays in Round Robin until
        // StartFinals is called, so closing the standings window cannot drop the
        // operator into the Finals.
        Assert.IsTrue(controller.IsFinalsPending);
        Assert.AreEqual("Round Robin", session.RaceType);

        controller.StartFinals();

        Assert.AreEqual("Finals", session.RaceType);
        Assert.IsTrue(controller.PeekUpcomingMatches(1).Count > 0);
    }

    [TestMethod]
    public void Qmdra_AllAdvanceFinalsInjection_CanReachTournamentCompletedSummary()
    {
        var drivers = TestDriverFactory.CreateRoundRobinPack(4);
        var session = CreateQmdraSession(roundsToRun: 1);
        var controller = CreateController(session);
        RaceController.RaceSummary? completion = null;
        controller.TournamentCompleted += summary => completion = summary;

        controller.GenerateBracket("Round Robin", drivers);
        InjectFinalsAllAdvance(controller, drivers);

        ResolveVisibleMatches(controller); // Resolve first revealed finals round.
        controller.AdvanceRound();         // Reveal final round.
        ResolveVisibleMatches(controller); // Resolve final.

        Assert.IsNotNull(completion);
        Assert.AreEqual("Finals (Pro Ladder)", completion!.Bracket);
        Assert.IsNotNull(completion.Winner);
        Assert.IsFalse(string.IsNullOrWhiteSpace(completion.Winner.Name));

        var currentRows = controller.BuildCurrentBracketRows().Where(r => !r.IsHeader).ToList();
        foreach (var name in drivers.Select(d => d.Name))
        {
            Assert.IsTrue(currentRows.Any(r => r.Driver1 == name || r.Driver2 == name));
        }
    }

    [TestMethod]
    public void Qmdra_AfterConfiguredRounds_FinalsFlow_CompletesTournamentSummary()
    {
        var session = CreateQmdraSession(roundsToRun: 1);
        var controller = CreateController(session);
        RaceController.RaceSummary? completion = null;
        controller.TournamentCompleted += summary => completion = summary;

        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));

        ResolveVisibleMatches(controller); // Completes RR and raises the Finals gate.
        controller.StartFinals();          // The RD starts the Finals.
        Assert.AreEqual("Finals", session.RaceType);
        Assert.AreEqual("SF", controller.GetActiveRoundLabel());

        ResolveVisibleMatches(controller); // Resolve SF
        controller.AdvanceRound();         // Reveal F
        Assert.AreEqual("F", controller.GetActiveRoundLabel());

        ResolveVisibleMatches(controller); // Resolve final

        Assert.IsNotNull(completion);
        Assert.AreEqual("Finals (Pro Ladder)", completion!.Bracket);
        Assert.IsNotNull(completion.Winner);
        Assert.IsNotNull(completion.RunnerUp);
        Assert.IsFalse(string.IsNullOrWhiteSpace(completion.Winner.Name));
        Assert.IsFalse(string.IsNullOrWhiteSpace(completion.RunnerUp.Name));
    }

    [TestMethod]
    public void Qmdra_Completion_DoesNotAdvanceFurther_AndDoesNotFireDuplicateTournamentCompleted()
    {
        var session = CreateQmdraSession(roundsToRun: 1);
        var controller = CreateController(session);
        var completionCount = 0;
        controller.TournamentCompleted += _ => completionCount++;

        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));

        ResolveVisibleMatches(controller); // RR complete -> Finals gate raised
        controller.StartFinals();          // RD starts the Finals
        ResolveVisibleMatches(controller); // SF complete
        controller.AdvanceRound();         // Reveal F
        ResolveVisibleMatches(controller); // F complete -> tournament complete

        var activeAtCompletion = controller.GetActiveRoundLabel();
        Assert.AreEqual(1, completionCount);
        Assert.AreEqual(0, controller.PeekUpcomingMatches(10).Count);

        controller.AdvanceRound();

        Assert.AreEqual(1, completionCount);
        Assert.AreEqual(activeAtCompletion, controller.GetActiveRoundLabel());
        Assert.AreEqual(0, controller.PeekUpcomingMatches(10).Count);
    }

    [TestMethod]
    public void Qmdra_AfterFinalsTransition_BracketGridKeepsRoundRobinHistory()
    {
        // Regression: QMDRA "all advance" used to clear the match-up grid when moving to
        // Finals (it captured no RR snapshot and swapped engines), leaving only the injected
        // Finals round. The grid must keep the full match-up list in race order — every
        // Round Robin round first, then the Finals round.
        var session = CreateQmdraSession(roundsToRun: 2);
        var controller = CreateController(session);
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));

        ResolveVisibleMatches(controller); // RR1
        controller.AdvanceRound();         // reveal RR2
        ResolveVisibleMatches(controller); // RR2 complete -> Finals gate raised
        controller.StartFinals();          // RD starts the Finals

        Assert.AreEqual("Finals", session.RaceType);

        var headers = controller.BuildCurrentBracketRows()
            .Where(r => r.IsHeader)
            .Select(r => r.RoundLabel)
            .ToList();

        CollectionAssert.Contains(headers, "RR1", "RR1 must remain in the grid after moving to Finals");
        CollectionAssert.Contains(headers, "RR2", "RR2 must remain in the grid after moving to Finals");
        Assert.IsTrue(headers.Any(h => string.Equals(h, "SF", StringComparison.OrdinalIgnoreCase)),
            "The Finals round (SF) must be appended after the Round Robin rounds");

        // Race order: all RR rounds must be listed before the Finals round.
        int lastRrIndex = headers.FindLastIndex(h => h != null && h.StartsWith("RR", StringComparison.OrdinalIgnoreCase));
        int sfIndex = headers.FindIndex(h => string.Equals(h, "SF", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(lastRrIndex >= 0 && sfIndex > lastRrIndex,
            "Round Robin rounds must be listed before the Finals round (race order)");
    }

    private static void ResolveVisibleMatches(RaceController controller)
    {
        foreach (var match in controller.PeekUpcomingMatches(20).ToList())
        {
            controller.SubmitWinner(match.MatchId, firstOption: true);
        }
    }

    private static RaceSession CreateQmdraSession(int roundsToRun)
    {
        return new RaceSession
        {
            EventName = "QA QMDRA Flow",
            EventDate = new DateTime(2026, 3, 10, 9, 0, 0),
            RaceType = "Round Robin",
            ClassType = "Heads Up",
            RoundRobinVariant = "QMDRA",
            RoundsToRun = roundsToRun
        };
    }

    private static void InjectFinalsAllAdvance(RaceController controller, List<Driver> rankedDrivers)
    {
        var method = typeof(RaceController).GetMethod(
            "InjectFinalsAllAdvance",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        method!.Invoke(controller, new object[] { rankedDrivers });
    }

    private static RaceController CreateController(RaceSession session)
    {
        return new RaceController(session, new NoOpStandingsDialogService());
    }
}
