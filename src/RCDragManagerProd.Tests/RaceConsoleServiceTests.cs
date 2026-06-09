using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="RaceConsoleService"/> (issue #284) — the UI-independent command +
/// state seam the race console calls instead of branching in its button handler. These
/// prove the primary Build/Start dispatch picks and runs the right action per phase, all
/// headless under <c>dotnet test</c>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class RaceConsoleServiceTests
{
    [TestMethod]
    public void GetState_DelegatesToBuilder_AndReportsEventTitle()
    {
        var service = new RaceConsoleService(
            new RaceController(TestSessionFactory.ProLadder(eventName: "Spring Shootout")));

        var state = service.GetState();

        Assert.AreEqual("Event: Spring Shootout", state.EventTitle);
        Assert.IsFalse(state.HasBracketStarted);
        Assert.AreEqual(RaceConsolePrimaryAction.BuildBracket, state.PrimaryAction);
    }

    [TestMethod]
    public void ExecutePrimaryAction_BeforeBracket_BuildsBracket()
    {
        var controller = new RaceController(TestSessionFactory.ProLadder());
        var service = new RaceConsoleService(controller);

        var action = service.ExecutePrimaryAction(TestDriverFactory.CreateProLadderPack(), "Pro Ladder");

        Assert.AreEqual(RaceConsolePrimaryAction.BuildBracket, action);
        Assert.IsTrue(controller.HasBracketStarted, "Building the bracket must start the session");
        Assert.IsTrue(controller.PeekUpcomingMatches(10).Count > 0);
    }

    [TestMethod]
    public void AdvanceRound_LocksDialIns_AndRevealsNextRound()
    {
        var controller = new RaceController(TestSessionFactory.ProLadder());
        var service = new RaceConsoleService(controller);
        service.ExecutePrimaryAction(TestDriverFactory.CreateProLadderPack(), "Pro Ladder");

        Assert.AreEqual("SF", controller.GetActiveRoundLabel(), "Pro Ladder opens on the SF round");
        foreach (var m in controller.PeekUpcomingMatches(10).ToList())
            controller.SubmitWinner(m.MatchId, firstOption: true);

        service.AdvanceRound();

        Assert.AreEqual("F", controller.GetActiveRoundLabel(), "Advancing must reveal the Final round");
        Assert.IsTrue(controller.DialInLocked, "Advancing locks the dial-ins for the committed round");
    }

    [TestMethod]
    public void TryShowStandings_BeforeRoundRobinComplete_ReportsUnavailable()
    {
        var controller = new RaceController(
            TestSessionFactory.RoundRobin(variant: "Standard"), new NoOpStandingsDialogService());
        var service = new RaceConsoleService(controller);
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));

        Assert.IsFalse(service.TryShowStandings(),
            "Standings must report unavailable before any RR round completes");
    }

    [TestMethod]
    public void ApplyBuybackSelection_NoDrivers_IsRejected()
    {
        var service = new RaceConsoleService(new RaceController(TestSessionFactory.RoundRobin()));

        Assert.AreEqual(BuybackSelectionOutcome.Invalid, service.ApplyBuybackSelection(new List<Driver>()));
        Assert.AreEqual(BuybackSelectionOutcome.Invalid, service.ApplyBuybackSelection(null));
    }

    [TestMethod]
    public void ApplyBuybackSelection_SingleDriver_PromotesStraightToFinals()
    {
        var controller = new RaceController(TestSessionFactory.RoundRobin());
        var service = new RaceConsoleService(controller);
        var one = TestDriverFactory.CreateRoundRobinPack(1);

        var outcome = service.ApplyBuybackSelection(one);

        Assert.AreEqual(BuybackSelectionOutcome.SingleToFinals, outcome);
        Assert.IsTrue(controller.IsFinalsPending, "A single buyback skips the LB and raises the Finals gate");
    }

    [TestMethod]
    public void ApplyBuybackSelection_TwoOrMore_StoredForLosersBracket()
    {
        var controller = new RaceController(TestSessionFactory.RoundRobin());
        var service = new RaceConsoleService(controller);
        var two = TestDriverFactory.CreateRoundRobinPack(2);

        var outcome = service.ApplyBuybackSelection(two);

        Assert.AreEqual(BuybackSelectionOutcome.Stored, outcome);
        Assert.IsTrue(controller.IsInLosersBracketPhase, "Two or more buyback drivers enter the Losers Bracket phase");
    }
}
