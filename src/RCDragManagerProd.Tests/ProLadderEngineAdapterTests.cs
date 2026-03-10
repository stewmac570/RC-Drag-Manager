using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

[TestClass]
public class ProLadderEngineAdapterTests
{
    [TestMethod]
    public void GenerateBracket_AllowsWinnerSubmission_ForSemifinalMatch()
    {
        var adapter = new ProLadderEngineAdapter();
        var drivers = TestDriverFactory.CreateProLadderPack();

        adapter.LoadDrivers(drivers);
        adapter.GenerateBracket();

        var semifinal = adapter.GetMatches()
            .Where(m => string.Equals(m.RoundLabel, "SF", StringComparison.OrdinalIgnoreCase))
            .First(m => m.Driver1 != null && m.Driver2 != null);

        var winner = semifinal.Driver1!;
        adapter.SubmitWinner(semifinal.MatchId, winner);

        Assert.IsTrue(adapter.HasWinner(semifinal.MatchId));
        Assert.AreEqual(winner.Id, adapter.GetWinner(semifinal.MatchId)?.Id);
    }

    [TestMethod]
    public void FiveDriverBracket_ByeMatchWinner_PropagatesToSemifinal()
    {
        var adapter = new ProLadderEngineAdapter();
        adapter.LoadDrivers(TestDriverFactory.CreateProLadderByePack());
        adapter.GenerateBracket();

        var r1ByeMatch = adapter.GetRoundMatches("R1")
            .Single(m => m.Driver1 != null && m.Driver2 == null);
        var byeRecipient = r1ByeMatch.Driver1!;

        adapter.SubmitWinner(r1ByeMatch.MatchId, byeRecipient);

        var semifinal = adapter.GetRoundMatches("SF")
            .Single(m => m.MatchId == 4);

        Assert.AreEqual(byeRecipient.Id, semifinal.Driver1?.Id);
    }

    [TestMethod]
    public void FiveDriverBracket_CompletesDeterministically_WithChampionAndRunnerUp()
    {
        var adapter = new ProLadderEngineAdapter();
        adapter.LoadDrivers(TestDriverFactory.CreateProLadderByePack());
        adapter.GenerateBracket();

        foreach (var match in adapter.GetRoundMatches("R1"))
        {
            adapter.SubmitWinner(match.MatchId, match.Driver1 ?? match.Driver2!);
        }

        foreach (var match in adapter.GetRoundMatches("SF"))
        {
            adapter.SubmitWinner(match.MatchId, match.Driver1 ?? match.Driver2!);
        }

        var final = adapter.GetRoundMatches("F").Single();
        adapter.SubmitWinner(final.MatchId, final.Driver1!);

        Assert.IsTrue(adapter.IsComplete);
        Assert.AreEqual(final.Driver1!.Id, adapter.GetChampion()?.Id);
        Assert.AreEqual(final.Driver2!.Id, adapter.GetRunnerUp()?.Id);
    }
}
