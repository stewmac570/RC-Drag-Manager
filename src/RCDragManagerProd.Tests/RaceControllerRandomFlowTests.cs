using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

[TestClass]
public class RaceControllerRandomFlowTests
{
    [TestMethod]
    public void RandomMode_AfterResolvingVisibleRound_AllowsAdvanceAndRevealsNextRound()
    {
        var session = CreateSession("Random");
        var controller = new RaceController(session, new NoOpStandingsDialogService());

        var advanceSignals = new List<bool>();
        controller.CanAdvanceChanged += canAdvance => advanceSignals.Add(canAdvance);

        controller.GenerateBracket("Random", TestDriverFactory.CreateRoundRobinPack(8));

        Assert.AreEqual("R1", controller.GetActiveRoundLabel());

        var firstRoundMatches = controller.PeekUpcomingMatches(10).ToList();
        Assert.AreEqual(4, firstRoundMatches.Count);

        foreach (var match in firstRoundMatches)
        {
            controller.SubmitWinner(match.MatchId, firstOption: match.Driver1 != null);
            Assert.IsNotNull(controller.GetWinner(match.MatchId));
        }

        Assert.AreEqual(true, advanceSignals.LastOrDefault());

        controller.AdvanceRound();
        Assert.AreEqual("R2", controller.GetActiveRoundLabel());

        var secondRoundMatches = controller.PeekUpcomingMatches(10).ToList();
        Assert.AreEqual(2, secondRoundMatches.Count);
        Assert.IsTrue(secondRoundMatches.All(m => controller.IsMatchInActiveRound(m.MatchId)));
    }

    private static RaceSession CreateSession(string raceType)
    {
        return new RaceSession
        {
            EventName = "QA Random Flow",
            EventDate = new DateTime(2026, 3, 11, 8, 0, 0),
            RaceType = raceType,
            ClassType = "Heads Up"
        };
    }
}
