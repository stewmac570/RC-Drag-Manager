using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RandomMode;
using RCDragManagerProd.RoundRobinMode;

namespace RCDragManagerProd.Tests;

[TestClass]
public class MultiCarOpeningRoundPlannerTests
{
    [TestMethod]
    public void Arrange_AvoidsSameDriverCars_WhenOutsidePairingsExist()
    {
        var a1 = new Driver { Id = 1, Name = "A1" };
        var a2 = new Driver { Id = 2, Name = "A2" };
        var b = new Driver { Id = 3, Name = "B" };
        var c = new Driver { Id = 4, Name = "C" };
        var matches = FirstRoundMatches(2);

        new MultiCarOpeningRoundPlanner().Arrange(matches, new[] { a1, a2, b, c }, Owner, new HashSet<(int, int)>());

        Assert.IsFalse(matches.Any(m => Owner(m.Seed1) == Owner(m.Seed2)));
    }

    [TestMethod]
    public void Arrange_UsesSameDriverPair_WhenItIsTheOnlyRace()
    {
        var a1 = new Driver { Id = 1, Name = "A1" };
        var a2 = new Driver { Id = 2, Name = "A2" };
        var matches = FirstRoundMatches(1);

        new MultiCarOpeningRoundPlanner().Arrange(matches, new[] { a1, a2 }, Owner, new HashSet<(int, int)>());

        Assert.AreEqual(1, Owner(matches[0].Seed1));
        Assert.AreEqual(1, Owner(matches[0].Seed2));
    }

    private static List<RandomMatch> FirstRoundMatches(int count) => Enumerable.Range(0, count)
        .Select(i => new RandomMatch
        {
            MatchId = 1000 + i,
            RoundLabel = RoundLabels.Normalize("LB-R1"),
            FromMatch1 = 0,
            FromMatch2 = 0
        })
        .ToList();

    private static int Owner(Driver driver) => driver != null && driver.Id <= 2 ? 1 : driver?.Id ?? 0;
}
