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
}
