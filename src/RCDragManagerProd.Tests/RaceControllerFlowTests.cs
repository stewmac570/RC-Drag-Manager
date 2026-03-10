using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.Tests;

[TestClass]
public class RaceControllerFlowTests
{
    [TestMethod]
    public void GenerateBracket_WithProLadderPack_StartsControllerFlow()
    {
        var session = CreateSession("Pro Ladder");
        var controller = new RaceController(session);

        IReadOnlyList<PairingRow>? latestRows = null;
        PairingRow? latestNextMatch = null;

        controller.BracketRedrawn += rows => latestRows = rows;
        controller.NextMatchReady += row => latestNextMatch = row;

        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        Assert.IsTrue(controller.HasBracketStarted);
        Assert.AreEqual("Pro Ladder", session.RaceType);
        Assert.IsNotNull(latestRows);
        Assert.IsTrue(latestRows!.Count > 0);
        Assert.IsNotNull(latestNextMatch);
    }

    [TestMethod]
    public void SubmitWinner_TracksResults_AndEnablesRoundAdvanceAfterVisibleRound()
    {
        var session = CreateSession("Pro Ladder");
        var controller = new RaceController(session);
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        var advanceSignals = new List<bool>();
        controller.CanAdvanceChanged += canAdvance => advanceSignals.Add(canAdvance);

        var visibleMatches = controller.PeekUpcomingMatches(10).ToList();
        Assert.IsTrue(visibleMatches.Count >= 2);

        var firstMatch = visibleMatches[0];
        controller.SubmitWinner(firstMatch.MatchId, firstOption: true);

        Assert.IsNotNull(controller.GetWinner(firstMatch.MatchId));
        Assert.IsNotNull(controller.GetLoser(firstMatch.MatchId));
        Assert.IsTrue(advanceSignals.Count > 0);
        Assert.IsFalse(advanceSignals.Last());

        var secondMatch = visibleMatches[1];
        controller.SubmitWinner(secondMatch.MatchId, firstOption: true);

        Assert.IsNotNull(controller.GetWinner(secondMatch.MatchId));
        Assert.AreEqual(true, advanceSignals.LastOrDefault());
    }

    [TestMethod]
    public void AdvanceRound_AfterCurrentRoundResolved_RevealsNextRound()
    {
        var session = CreateSession("Pro Ladder");
        var controller = new RaceController(session);
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        foreach (var match in controller.PeekUpcomingMatches(10))
        {
            controller.SubmitWinner(match.MatchId, firstOption: true);
        }

        var activeBefore = controller.GetActiveRoundLabel();
        controller.AdvanceRound();
        var activeAfter = controller.GetActiveRoundLabel();

        Assert.IsFalse(string.IsNullOrWhiteSpace(activeBefore));
        Assert.IsFalse(string.IsNullOrWhiteSpace(activeAfter));
        Assert.AreNotEqual(activeBefore, activeAfter);

        var nextMatches = controller.PeekUpcomingMatches(1);
        Assert.AreEqual(1, nextMatches.Count);
        Assert.IsTrue(controller.IsMatchInActiveRound(nextMatches[0].MatchId));
    }

    [TestMethod]
    public void Reset_ClearsControllerState_AndDisablesProgressSignals()
    {
        var session = CreateSession("Pro Ladder");
        var controller = new RaceController(session);
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        bool? canAdvance = null;
        bool? canPickWinner = null;
        PairingRow? nextMatch = new PairingRow();

        controller.CanAdvanceChanged += value => canAdvance = value;
        controller.CanPickWinnerChanged += value => canPickWinner = value;
        controller.NextMatchReady += row => nextMatch = row;

        controller.Reset();

        Assert.IsFalse(controller.HasBracketStarted);
        Assert.AreEqual(string.Empty, session.RaceType);
        Assert.AreEqual(0, controller.BuildCurrentBracketRows().Count);
        Assert.AreEqual(false, canAdvance);
        Assert.AreEqual(false, canPickWinner);
        Assert.IsNull(nextMatch);
    }

    [TestMethod]
    public void ProLadderFiveDriverFlow_ResolvesByeMatches_AndAdvancesRounds()
    {
        var session = CreateSession("Pro Ladder");
        var controller = new RaceController(session);
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderByePack());

        Assert.AreEqual("R1", controller.GetActiveRoundLabel());

        var r1Matches = controller.PeekUpcomingMatches(10).ToList();
        Assert.AreEqual(3, r1Matches.Count);
        Assert.AreEqual(1, r1Matches.Count(m => m.Driver1 == null || m.Driver2 == null));

        var advanceSignals = new List<bool>();
        controller.CanAdvanceChanged += canAdvance => advanceSignals.Add(canAdvance);

        foreach (var match in r1Matches)
        {
            controller.SubmitWinner(match.MatchId, firstOption: match.Driver1 != null);
            Assert.IsNotNull(controller.GetWinner(match.MatchId));
        }

        Assert.AreEqual(true, advanceSignals.LastOrDefault());

        controller.AdvanceRound();
        Assert.AreEqual("SF", controller.GetActiveRoundLabel());

        var sfMatches = controller.PeekUpcomingMatches(10).ToList();
        Assert.AreEqual(2, sfMatches.Count);
        Assert.AreEqual(1, sfMatches.Count(m => m.Driver1 == null || m.Driver2 == null));

        foreach (var match in sfMatches)
        {
            controller.SubmitWinner(match.MatchId, firstOption: match.Driver1 != null);
        }

        Assert.AreEqual(true, advanceSignals.LastOrDefault());

        controller.AdvanceRound();
        Assert.AreEqual("F", controller.GetActiveRoundLabel());

        var finalMatches = controller.PeekUpcomingMatches(10).ToList();
        Assert.AreEqual(1, finalMatches.Count);
        Assert.IsNotNull(finalMatches[0].Driver1);
        Assert.IsNotNull(finalMatches[0].Driver2);
    }

    [TestMethod]
    public void SubmitWinner_DuplicateSubmission_DoesNotOverwriteExistingWinner()
    {
        var session = CreateSession("Pro Ladder");
        var controller = new RaceController(session);
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderByePack());

        var targetMatch = controller.PeekUpcomingMatches(10)
            .First(m => m.Driver1 != null && m.Driver2 != null);

        controller.SubmitWinner(targetMatch.MatchId, firstOption: true);
        var originalWinner = controller.GetWinner(targetMatch.MatchId);

        controller.SubmitWinner(targetMatch.MatchId, firstOption: false);
        var winnerAfterDuplicateSubmit = controller.GetWinner(targetMatch.MatchId);

        Assert.IsNotNull(originalWinner);
        Assert.IsNotNull(winnerAfterDuplicateSubmit);
        Assert.AreEqual(originalWinner!.Id, winnerAfterDuplicateSubmit!.Id);
    }

    private static RaceSession CreateSession(string raceType)
    {
        return new RaceSession
        {
            EventName = "QA Controller Flow",
            EventDate = new DateTime(2026, 3, 10, 8, 0, 0),
            RaceType = raceType,
            ClassType = "Heads Up"
        };
    }
}
