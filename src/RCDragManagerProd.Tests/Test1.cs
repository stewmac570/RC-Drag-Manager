using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.Tests.Helpers;

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
}
