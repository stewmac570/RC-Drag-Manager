using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RoundRobinMode;

namespace RCDragManagerProd.Tests;

[TestClass]
public class MultiCarRoundRobinSchedulerTests
{
    [TestMethod]
    public void Build_TwoCarsForOneDriver_PostponesTheirRaceUntilAfterExternalOpponents()
    {
        var schedule = new MultiCarRoundRobinScheduler().Build(new[]
        {
            Entry(1, 10, "A1"), Entry(2, 10, "A2"),
            Entry(3, 20, "B"), Entry(4, 30, "C")
        }, roundsToRun: 3);

        var sameDriver = schedule
            .Where(m => m.Driver1 != null && m.Driver2 != null)
            .Where(m => new[] { 1, 2 }.Contains(m.Driver1.Id) && new[] { 1, 2 }.Contains(m.Driver2.Id))
            .Single();

        Assert.AreEqual("RR3", sameDriver.RoundLabel);
        Assert.IsFalse(schedule.Where(m => m.RoundLabel != "RR3")
            .Any(m => m.Driver1 != null && m.Driver2 != null &&
                      new[] { 1, 2 }.Contains(m.Driver1.Id) && new[] { 1, 2 }.Contains(m.Driver2.Id)));
    }

    [TestMethod]
    public void Build_OnlyTwoCarsForOneDriver_AllowsTheForcedRace()
    {
        var schedule = new MultiCarRoundRobinScheduler().Build(new[]
        {
            Entry(1, 10, "A1"), Entry(2, 10, "A2")
        }, roundsToRun: 1);

        Assert.AreEqual(1, schedule.Count);
        Assert.AreEqual(1, schedule[0].Driver1.Id);
        Assert.AreEqual(2, schedule[0].Driver2.Id);
    }

    [TestMethod]
    public void Build_FourCarsForOneDriver_AllowsEveryCarAsAnIndependentEntry()
    {
        var schedule = new MultiCarRoundRobinScheduler().Build(new[]
        {
            Entry(1, 10, "A1"), Entry(2, 10, "A2"),
            Entry(3, 10, "A3"), Entry(4, 10, "A4")
        }, roundsToRun: 3);

        Assert.AreEqual(6, schedule.Count);
        Assert.IsTrue(schedule.All(m => m.Driver1 != null && m.Driver2 != null));
    }

    [TestMethod]
    public void Build_OddField_UsesBothExternalOpponentsBeforeTheForcedSameDriverRace()
    {
        var schedule = new MultiCarRoundRobinScheduler().Build(new[]
        {
            Entry(1, 10, "A1"), Entry(2, 10, "A2"), Entry(3, 20, "B")
        }, roundsToRun: 3);

        var sameDriver = schedule.Single(m => m.Driver1 != null && m.Driver2 != null &&
            new[] { 1, 2 }.Contains(m.Driver1.Id) && new[] { 1, 2 }.Contains(m.Driver2.Id));

        Assert.AreEqual("RR3", sameDriver.RoundLabel);
        Assert.AreEqual(3, schedule.Count(m => m.Driver2 == null));
    }

    private static MultiCarRaceEntry Entry(int entryId, int driverId, string carName) => new()
    {
        RaceEntryId = entryId,
        DriverId = driverId,
        DriverName = "Driver " + driverId,
        CarId = entryId,
        CarName = carName
    };
}
