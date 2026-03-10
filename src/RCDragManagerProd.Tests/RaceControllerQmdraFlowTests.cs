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
    public void QmdraCompletion_UsesInjectedStandingsDialogService_WithoutModalUi()
    {
        var drivers = TestDriverFactory.CreateRoundRobinPack(4);
        var session = CreateQmdraSession(roundsToRun: 1);
        var dialog = new CaptureStandingsDialogService();
        var controller = new RaceController(session, dialog);

        controller.GenerateBracket("Round Robin", drivers);
        ResolveVisibleMatches(controller);

        Assert.AreEqual(1, dialog.ShowCallCount);
        StringAssert.Contains(dialog.LastTitle!, "Round Robin");
        Assert.IsTrue(controller.TryShowRoundRobinStandings());
        Assert.AreEqual(2, dialog.ShowCallCount);
    }

    [TestMethod]
    public void Qmdra_InitializesRoundRobinFlow_WithoutBuybackSignals()
    {
        var drivers = TestDriverFactory.CreateRoundRobinPack(4);
        var session = CreateQmdraSession(roundsToRun: 1);
        var controller = new RaceController(session);

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
        var controller = new RaceController(session);
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));

        ResolveVisibleMatches(controller);

        Assert.AreEqual("Round Robin", session.RaceType);
        Assert.AreEqual("RR1", controller.GetActiveRoundLabel());

        controller.AdvanceRound();
        Assert.AreEqual("RR2", controller.GetActiveRoundLabel());

        ResolveVisibleMatches(controller);

        Assert.AreEqual("Finals", session.RaceType);
        Assert.IsTrue(controller.PeekUpcomingMatches(1).Count > 0);
    }

    [TestMethod]
    public void Qmdra_AllAdvanceFinalsInjection_CanReachTournamentCompletedSummary()
    {
        var drivers = TestDriverFactory.CreateRoundRobinPack(4);
        var session = CreateQmdraSession(roundsToRun: 1);
        var controller = new RaceController(session);
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

    private sealed class CaptureStandingsDialogService : IStandingsDialogService
    {
        public int ShowCallCount { get; private set; }
        public string? LastTitle { get; private set; }

        public void Show(string title, string content)
        {
            ShowCallCount++;
            LastTitle = title;
        }
    }
}
