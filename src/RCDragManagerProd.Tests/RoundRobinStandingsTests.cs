using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.RoundRobinMode;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

[TestClass]
public class RoundRobinStandingsTests
{
    [TestMethod]
    public void Rank_ProducesExpectedOrder_ForDeterministicResults()
    {
        var a = new Driver { Id = 1, Name = "Ava Stone" };
        var b = new Driver { Id = 2, Name = "Blake Turner" };
        var c = new Driver { Id = 3, Name = "Casey Reed" };

        var drivers = new List<Driver> { a, b, c };
        var matches = new List<RoundRobinMatch>
        {
            new RoundRobinMatch { MatchId = 1, RoundLabel = "R1", Driver1 = a, Driver2 = b },
            new RoundRobinMatch { MatchId = 2, RoundLabel = "R2", Driver1 = a, Driver2 = c },
            new RoundRobinMatch { MatchId = 3, RoundLabel = "R3", Driver1 = b, Driver2 = c }
        };

        var results = new MatchResult();
        results.SetWinner(1, a, b);
        results.SetWinner(2, a, c);
        results.SetWinner(3, b, c);

        var ranker = new RoundRobinRanker();
        var ranked = ranker.Rank(matches, drivers, results);

        Assert.AreEqual(3, ranked.Count);
        Assert.AreEqual(1, ranked[0].DriverId);
        Assert.AreEqual(2, ranked[0].Wins);
        Assert.AreEqual(0, ranked[0].Losses);
        Assert.AreEqual(2, ranked[1].DriverId);
        Assert.AreEqual(3, ranked[2].DriverId);
    }

    [TestMethod]
    public void Rank_UsesHeadToHead_WhenPointsAndWinsTie()
    {
        var a = new Driver { Id = 1, Name = "Ava Stone" };
        var b = new Driver { Id = 2, Name = "Blake Turner" };
        var c = new Driver { Id = 3, Name = "Casey Reed" };
        var d = new Driver { Id = 4, Name = "Drew Cole" };

        var drivers = new List<Driver> { a, b, c, d };
        var matches = new List<RoundRobinMatch>
        {
            new RoundRobinMatch { MatchId = 1, RoundLabel = "R1", Driver1 = a, Driver2 = b }, // A beats B
            new RoundRobinMatch { MatchId = 2, RoundLabel = "R1", Driver1 = a, Driver2 = c }, // C beats A
            new RoundRobinMatch { MatchId = 3, RoundLabel = "R1", Driver1 = b, Driver2 = d }, // B beats D
            new RoundRobinMatch { MatchId = 4, RoundLabel = "R1", Driver1 = c, Driver2 = d }  // C beats D
        };

        var results = new MatchResult();
        results.SetWinner(1, a, b);
        results.SetWinner(2, c, a);
        results.SetWinner(3, b, d);
        results.SetWinner(4, c, d);

        var ranker = new RoundRobinRanker();
        var ranked = ranker.Rank(matches, drivers, results);

        Assert.AreEqual(4, ranked.Count);
        Assert.AreEqual(3, ranked[0].DriverId); // C top (2 wins)
        Assert.AreEqual(1, ranked[1].DriverId); // A and B tied; A beat B head-to-head
        Assert.AreEqual(2, ranked[2].DriverId);
        Assert.AreEqual(4, ranked[3].DriverId);
    }

    [TestMethod]
    public void Adapter_StandingsAndTopRanked_AreStructurallyConsistent_AfterResults()
    {
        var adapter = new RoundRobinEngineAdapter();
        adapter.SetRoundsToRun(3);
        adapter.LoadDrivers(TestDriverFactory.CreateRoundRobinPack(4));
        adapter.GenerateBracket();

        var matches = adapter.GetMatches();
        foreach (var m in matches)
        {
            var winner = m.Driver1!.Id <= m.Driver2!.Id ? m.Driver1 : m.Driver2;
            adapter.SubmitWinner(m.MatchId, winner);
        }

        var standings = adapter.GetStandings();
        var detailed = adapter.GetRankedStandings();
        var topRanked = adapter.GetTopRankedDrivers(4);

        Assert.IsTrue(standings.Count > 0);
        Assert.AreEqual(matches.Count, standings.Sum(s => s.Wins));
        for (var i = 0; i < standings.Count - 1; i++)
        {
            Assert.IsTrue(standings[i].Wins >= standings[i + 1].Wins);
        }

        Assert.AreEqual(4, topRanked.Count);
        Assert.AreEqual(4, topRanked.Select(d => d.Id).Distinct().Count());
        Assert.AreEqual(4, detailed.Count);
        Assert.IsTrue(detailed.All(r => r.Points > 0));
        Assert.IsTrue(detailed.All(r => r.Rank > 0));
    }
}
