using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers the UI-independent race-console state contract (issue #284): the
/// <see cref="RaceConsoleViewModel"/> produced by <see cref="RaceConsoleViewModelBuilder"/>.
/// These prove the builder derives the right state straight from a
/// <see cref="RaceController"/> with no WinForms involvement, so the same snapshot can
/// drive the existing console (after rewiring) and a future WPF view.
/// </summary>
[TestClass]
[DoNotParallelize]
public class RaceConsoleViewModelTests
{
    // ── Primary-action resolution (pure decision, no controller needed) ───────────

    [TestMethod]
    public void PrimaryAction_BeforeAnyPhase_BuildsBracket()
    {
        var action = RaceConsoleViewModelBuilder.ResolvePrimaryAction(isFinalsPending: false, isInLosersBracketPhase: false);

        Assert.AreEqual(RaceConsolePrimaryAction.BuildBracket, action);
        Assert.AreEqual("Build Bracket", RaceConsoleViewModelBuilder.LabelFor(action));
    }

    [TestMethod]
    public void PrimaryAction_WhenLosersBracketPending_StartsLosersBracket()
    {
        var action = RaceConsoleViewModelBuilder.ResolvePrimaryAction(isFinalsPending: false, isInLosersBracketPhase: true);

        Assert.AreEqual(RaceConsolePrimaryAction.StartLosersBracket, action);
        Assert.AreEqual("Start Losers Bracket", RaceConsoleViewModelBuilder.LabelFor(action));
    }

    [TestMethod]
    public void PrimaryAction_WhenFinalsPending_StartsFinals_EvenIfLosersPhaseFlagSet()
    {
        // Finals pending takes precedence — matches the console's Build/Start handler order.
        var action = RaceConsoleViewModelBuilder.ResolvePrimaryAction(isFinalsPending: true, isInLosersBracketPhase: true);

        Assert.AreEqual(RaceConsolePrimaryAction.StartFinals, action);
        Assert.AreEqual("Start Finals", RaceConsoleViewModelBuilder.LabelFor(action));
    }

    // ── Builder against a live controller ─────────────────────────────────────────

    [TestMethod]
    public void Build_BeforeBracket_ReportsSetupState()
    {
        var controller = new RaceController(TestSessionFactory.ProLadder(eventName: "Spring Shootout"));

        var vm = RaceConsoleViewModelBuilder.Build(controller);

        Assert.AreEqual("Event: Spring Shootout", vm.EventTitle);
        Assert.IsFalse(vm.HasBracketStarted);
        Assert.AreEqual(string.Empty, vm.ActiveRoundLabel, "Active round must be empty before the bracket starts");
        Assert.AreEqual(0, vm.PairingRows.Count);
        Assert.AreEqual("No current match.", vm.CurrentMatchText);
        Assert.AreEqual(string.Empty, vm.OnDeckText);
        Assert.AreEqual(string.Empty, vm.InTheHoleText);
        Assert.AreEqual(RaceConsolePrimaryAction.BuildBracket, vm.PrimaryAction);
        Assert.AreEqual("Build Bracket", vm.PrimaryActionLabel);
    }

    [TestMethod]
    public void Build_AfterProLadderBracket_ReportsActiveState()
    {
        var controller = new RaceController(TestSessionFactory.ProLadder());
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        var vm = RaceConsoleViewModelBuilder.Build(controller);

        Assert.IsTrue(vm.HasBracketStarted);
        Assert.IsFalse(string.IsNullOrWhiteSpace(vm.ActiveRoundLabel), "Active round label must be set once started");
        Assert.IsTrue(vm.PairingRows.Count > 0, "A started bracket must expose pairing rows");
        Assert.IsTrue(vm.PairingRows.Any(r => !r.IsHeader), "Pairing rows must include at least one match row");
        Assert.AreNotEqual("No current match.", vm.CurrentMatchText, "A started bracket has a current match");
        StringAssert.Contains(vm.CurrentMatchText, " vs ", "Current match text must read as 'Name vs Name'");

        // Bracket is live, no buyback/finals phase yet → primary action is still Build Bracket.
        Assert.AreEqual(RaceConsolePrimaryAction.BuildBracket, vm.PrimaryAction);
    }
}
