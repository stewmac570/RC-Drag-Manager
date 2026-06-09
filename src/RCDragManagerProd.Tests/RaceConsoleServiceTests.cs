using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
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

}
