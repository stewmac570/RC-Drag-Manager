using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.Tests.Helpers;
using System.Linq;

namespace RCDragManagerProd.Tests;

[TestClass]
public class RandomEngineAdapterTests
{
    [TestMethod]
    public void GenerateBracket_WithEightDrivers_ProducesMatchesAndRounds()
    {
        var adapter = new RandomEngineAdapter();
        adapter.LoadDrivers(TestDriverFactory.CreateRoundRobinPack(8));

        adapter.GenerateBracket();

        var matches = adapter.GetMatches();
        var rounds = adapter.GetRoundOrder();

        Assert.IsTrue(matches.Count > 0);
        Assert.IsTrue(rounds.Count > 0);
    }

    [TestMethod]
    public void GenerateBracket_WithEightDrivers_AllowsWinnerProgression_ToTournamentComplete()
    {
        var adapter = new RandomEngineAdapter();
        adapter.LoadDrivers(TestDriverFactory.CreateRoundRobinPack(8));
        adapter.GenerateBracket();

        foreach (var match in adapter.GetRoundMatches("R1"))
        {
            adapter.SubmitWinner(match.MatchId, match.Driver1 ?? match.Driver2!);
        }

        var roundTwo = adapter.GetRoundMatches("R2");
        Assert.AreEqual(2, roundTwo.Count);
        Assert.IsTrue(roundTwo.All(m => m.Driver1 != null && m.Driver2 != null));

        foreach (var match in roundTwo)
        {
            adapter.SubmitWinner(match.MatchId, match.Driver1!);
        }

        var final = adapter.GetRoundMatches("F").Single();
        Assert.IsNotNull(final.Driver1);
        Assert.IsNotNull(final.Driver2);

        adapter.SubmitWinner(final.MatchId, final.Driver1!);

        Assert.IsTrue(adapter.IsComplete);
        Assert.AreEqual(final.Driver1!.Id, adapter.GetChampion()?.Id);
        Assert.AreEqual(final.Driver2!.Id, adapter.GetRunnerUp()?.Id);
    }

    [TestMethod]
    public void GenerateBracket_WithFiveDrivers_ContainsByeAndAdvancesRealDriver()
    {
        var adapter = new RandomEngineAdapter();
        adapter.LoadDrivers(TestDriverFactory.CreateRoundRobinPack(5));
        adapter.GenerateBracket();

        var roundOne = adapter.GetRoundMatches("R1");
        var byeMatch = roundOne.Single(m => (m.Driver1 == null) ^ (m.Driver2 == null));
        var advancingDriver = byeMatch.Driver1 ?? byeMatch.Driver2!;

        adapter.SubmitWinner(byeMatch.MatchId, advancingDriver);

        var roundTwo = adapter.GetRoundMatches("R2");
        Assert.IsTrue(roundTwo.Any(m => m.Driver1?.Id == advancingDriver.Id || m.Driver2?.Id == advancingDriver.Id));
    }

}
