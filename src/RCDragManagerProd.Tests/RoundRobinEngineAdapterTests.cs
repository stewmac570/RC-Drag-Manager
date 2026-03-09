using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

[TestClass]
public class RoundRobinEngineAdapterTests
{
    [TestMethod]
    public void GenerateBracket_UsesConfiguredQmdraRoundCount()
    {
        var adapter = new RoundRobinEngineAdapter();
        adapter.SetRoundsToRun(5);
        adapter.LoadDrivers(TestDriverFactory.CreateRoundRobinPack(4));

        adapter.GenerateBracket();

        var roundOrder = adapter.GetRoundOrder();
        Assert.AreEqual(5, roundOrder.Count);
        Assert.AreEqual("RR1", roundOrder[0]);
        Assert.AreEqual("RR5", roundOrder[4]);
    }
}
