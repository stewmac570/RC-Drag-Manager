using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// "Push to end of round" — sending the current race to the back of its round when a
/// racer needs more time. Live-session ordering only; engines are never reordered.
/// </summary>
[TestClass]
public class RaceControllerDeferMatchTests
{
    [TestMethod]
    public void PushToEndOfRound_MovesCurrentMatchToBack_AndPromotesTheNextOne()
    {
        var controller = NewRandomEight();

        var before = MatchIds(controller);
        Assert.AreEqual(4, before.Count, "8-driver random R1 should have 4 matches");
        var wasCurrent = before[0];

        controller.PushCurrentMatchToEndOfRound();

        var after = MatchIds(controller);
        CollectionAssert.AreEquivalent(before, after, "no matches gained or lost");
        Assert.AreEqual(wasCurrent, after[after.Count - 1], "pushed match is now last");
        Assert.AreEqual(before[1], after[0], "the next match becomes current");
    }

    [TestMethod]
    public void PushToEndOfRound_Twice_SecondPushGoesBehindTheFirst()
    {
        var controller = NewRandomEight();
        var before = MatchIds(controller); // [a, b, c, d]

        controller.PushCurrentMatchToEndOfRound(); // a → back: [b, c, d, a]
        controller.PushCurrentMatchToEndOfRound(); // b → back: [c, d, a, b]

        var after = MatchIds(controller);
        Assert.AreEqual(before[2], after[0]); // c
        Assert.AreEqual(before[3], after[1]); // d
        Assert.AreEqual(before[0], after[2]); // a
        Assert.AreEqual(before[1], after[3]); // b
    }

    [TestMethod]
    public void PushToEndOfRound_IsNoOp_WhenOnlyOneMatchRemains()
    {
        var controller = NewRandomEight();

        // Resolve all but the last match in the round.
        var matches = controller.PeekUpcomingMatches(10).ToList();
        for (int i = 0; i < matches.Count - 1; i++)
            controller.SubmitWinner(matches[i].MatchId, firstOption: matches[i].Driver1 != null);

        var remaining = controller.PeekUpcomingMatches(10).ToList();
        Assert.AreEqual(1, remaining.Count);
        var onlyId = remaining[0].MatchId;

        controller.PushCurrentMatchToEndOfRound(); // nothing to run ahead of it → no-op

        var after = controller.PeekUpcomingMatches(10).ToList();
        Assert.AreEqual(1, after.Count);
        Assert.AreEqual(onlyId, after[0].MatchId);
    }

    [TestMethod]
    public void CanDeferChanged_TrueWithTwoPlusMatches_FalseWhenOneLeft()
    {
        var session = CreateSession("Random");
        var controller = new RaceController(session, new NoOpStandingsDialogService());

        var signals = new List<bool>();
        controller.CanDeferChanged += v => signals.Add(v);

        controller.GenerateBracket("Random", TestDriverFactory.CreateRoundRobinPack(8));
        Assert.IsTrue(signals.LastOrDefault(), "4 unraced matches → push is actionable");

        var matches = controller.PeekUpcomingMatches(10).ToList();
        for (int i = 0; i < matches.Count - 1; i++)
            controller.SubmitWinner(matches[i].MatchId, firstOption: matches[i].Driver1 != null);

        Assert.IsFalse(signals.LastOrDefault(), "one match left → push no longer actionable");
    }

    [TestMethod]
    public void AdvanceRound_ClearsDeferral_NextRoundIsNaturalOrder()
    {
        var controller = NewRandomEight();

        controller.PushCurrentMatchToEndOfRound(); // defer something in R1

        // Resolve the whole round and advance.
        foreach (var m in controller.PeekUpcomingMatches(10).ToList())
            controller.SubmitWinner(m.MatchId, firstOption: m.Driver1 != null);
        controller.AdvanceRound();

        Assert.AreEqual("R2", controller.GetActiveRoundLabel());
        var r2 = MatchIds(controller);
        var sorted = r2.OrderBy(id => id).ToList();
        CollectionAssert.AreEqual(sorted, r2, "deferral does not carry into the next round");
    }

    private static RaceController NewRandomEight()
    {
        var session = CreateSession("Random");
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Random", TestDriverFactory.CreateRoundRobinPack(8));
        Assert.AreEqual("R1", controller.GetActiveRoundLabel());
        return controller;
    }

    private static List<int> MatchIds(RaceController controller) =>
        controller.PeekUpcomingMatches(10).Select(m => m.MatchId).ToList();

    private static RaceSession CreateSession(string raceType) => new RaceSession
    {
        EventName = "QA Defer Flow",
        EventDate = new DateTime(2026, 3, 11, 8, 0, 0),
        RaceType = raceType,
        ClassType = "Heads Up"
    };
}
