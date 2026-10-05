using System;
using System.Linq;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Race day 2026-10-03: the live feed did not follow a race pushed to the end of
/// its round, did not show byes, and sent no standings for a Multi-Car class.
/// </summary>
[TestClass]
[DoNotParallelize]
public class LiveFeedRaceOrderTests
{
    [TestMethod]
    public void PushedToEnd_RaceMovesToTheBackOfItsRound_InTheFeed()
    {
        var controller = RoundRobin(6, out _);
        var before = RoundOne(controller);
        var pushed = before[0];

        controller.PushCurrentMatchToEndOfRound();

        var after = RoundOne(controller);
        Assert.AreEqual(pushed, after.Last(), "The pushed race must be last in its round on the feed.");
        Assert.AreEqual(before.Count, after.Count);
    }

    [TestMethod]
    public void ByeResult_IsSentWithByeAsTheLoser()
    {
        var controller = RoundRobin(5, out _);   // odd field: one bye a round
        foreach (var m in controller.PeekUpcomingMatches(50).ToList())
            controller.SubmitWinner(m.MatchId, firstOption: m.Driver1 != null);

        var dto = controller.BuildLiveState();
        Assert.IsTrue(dto.Winners.Any(w => w.LoserName == "BYE"), "A bye run must appear in the feed's results.");
    }

    [TestMethod]
    public void MultiCarClass_SendsStandings()
    {
        var session = new RaceSession
        {
            EventName = "QA", EventDate = new DateTime(2026, 10, 5),
            RaceType = RaceTypes.MultiCarRoundRobin, ClassType = "DYO",
            RoundRobinVariant = "QMDRA", RoundsToRun = 2,
            DriverEntries = Enumerable.Range(1, 6).Select(i => new RaceSessionDriverEntry
            {
                RaceEntryId = i, DriverID = 100 + i, DriverName = "Owner " + i, CarID = i, CarName = "Car " + i
            }).ToList()
        };
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket(RaceTypes.MultiCarRoundRobin,
            session.DriverEntries.Select(e => new Driver { Id = e.RaceEntryId, Name = e.DriverName + " — " + e.CarName }).ToList());
        var first = controller.PeekUpcomingMatches(10).First();
        controller.SubmitWinner(first.MatchId, firstOption: true);

        Assert.IsFalse(string.IsNullOrWhiteSpace(controller.BuildLiveState().RRStandings),
            "A Multi-Car Round Robin class must send standings like a Round Robin.");
    }

    [TestMethod]
    public void FeedCarriesWhenItWasBuilt()
    {
        var controller = RoundRobin(4, out _);
        var stamp = controller.BuildLiveState().PublishedAtUtc;
        Assert.IsTrue(DateTime.TryParse(stamp, out _), "PublishedAtUtc must be a timestamp: " + stamp);
    }

    private static System.Collections.Generic.List<string> RoundOne(RaceController c) =>
        c.BuildLiveState().Matches
            .Where(m => m.RoundLabel == c.BuildLiveState().CurrentRound)
            .Select(m => m.Driver1 + "|" + m.Driver2)
            .ToList();

    private static RaceController RoundRobin(int drivers, out RaceSession session)
    {
        session = new RaceSession
        {
            EventName = "QA", EventDate = new DateTime(2026, 10, 5),
            RaceType = RaceTypes.RoundRobin, ClassType = "Heads Up",
            RoundRobinVariant = "QMDRA", RoundsToRun = 2
        };
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket(RaceTypes.RoundRobin, TestDriverFactory.CreateRoundRobinPack(drivers));
        return controller;
    }
}
