using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// With buybacks off, a Round Robin class sends its top four to the Finals.
///
/// It used to seed every driver in the class into the Finals ladder, so an
/// eight-driver no-buyback class raced all eight through the Finals. Both
/// Round Robin forms share the same engine and completion path, so both are
/// driven here with a field larger than four.
/// </summary>
[TestClass]
[DoNotParallelize]
public class RaceControllerNoBuybackFinalsFieldTests
{
    [DataTestMethod]
    [DataRow(RaceTypes.RoundRobin)]
    [DataRow(RaceTypes.MultiCarRoundRobin)]
    public void BuybacksOff_StartFinals_FieldsOnlyTheTopFour(string raceType)
    {
        var session = NoBuybackSession(raceType, roundsToRun: 3);
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket(raceType, EightCompetitors(session));

        ResolveRoundRobin(controller, rounds: 3);
        Assert.IsTrue(controller.IsFinalsPending, "Round Robin completion must raise the Finals gate.");

        controller.StartFinals();

        Assert.AreEqual(RaceTypes.Finals, session.RaceType);
        var finalists = FinalsDriverNames(controller);
        Assert.AreEqual(4, finalists.Count,
            "Buybacks off must send the top four to the Finals, not every driver. Got: " +
            string.Join(", ", finalists));
    }

    [DataTestMethod]
    [DataRow(RaceTypes.RoundRobin)]
    [DataRow(RaceTypes.MultiCarRoundRobin)]
    public void BuybacksOff_FinalsAreTheTopFourOnRanking(string raceType)
    {
        var session = NoBuybackSession(raceType, roundsToRun: 3);
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket(raceType, EightCompetitors(session));
        ResolveRoundRobin(controller, rounds: 3);
        Assert.IsTrue(controller.IsFinalsPending, "Round Robin completion must raise the Finals gate.");

        var expected = session.ResultsArchive.RoundRobinStandings
            .OrderBy(s => s.Rank).Take(4).Select(s => s.DriverId).OrderBy(id => id).ToList();

        controller.StartFinals();

        var actual = controller.PeekUpcomingMatches(20)
            .SelectMany(m => new[] { m.Driver1, m.Driver2 })
            .Where(d => d != null)
            .Select(d => d.Id).Distinct().OrderBy(id => id).ToList();
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Eight competitors. Multi-Car Round Robin races car entries, so that form gets
    /// four drivers with two cars each, handed over one competitor per car the way
    /// the race console builds its roster.
    /// </summary>
    private static List<Driver> EightCompetitors(RaceSession session)
    {
        var drivers = TestDriverFactory.CreateRoundRobinPack(8);
        if (session.RaceType != RaceTypes.MultiCarRoundRobin) return drivers;

        session.DriverEntries = Enumerable.Range(1, 8).Select(i => new RaceSessionDriverEntry
        {
            RaceEntryId = i,
            DriverID = (i + 1) / 2,
            DriverName = drivers[(i + 1) / 2 - 1].Name,
            CarID = i,
            CarName = "Car " + i
        }).ToList();
        return session.DriverEntries
            .Select(e => new Driver { Id = e.RaceEntryId, Name = e.DriverName + " — " + e.CarName })
            .ToList();
    }

    private static List<string> FinalsDriverNames(RaceController controller) =>
        controller.PeekUpcomingMatches(20)
            .SelectMany(m => new[] { m.Driver1?.Name, m.Driver2?.Name })
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList();

    private static void ResolveRoundRobin(RaceController controller, int rounds)
    {
        for (var i = 0; i < rounds; i++)
        {
            foreach (var match in controller.PeekUpcomingMatches(20).ToList())
                controller.SubmitWinner(match.MatchId, firstOption: true);
            if (i < rounds - 1) controller.AdvanceRound();
        }
    }

    private static RaceSession NoBuybackSession(string raceType, int roundsToRun) => new RaceSession
    {
        EventName = "QA No-Buyback Finals Field",
        EventDate = new DateTime(2026, 9, 24, 9, 0, 0),
        RaceType = raceType,
        ClassType = "Heads Up",
        RoundRobinVariant = "QMDRA",
        RoundsToRun = roundsToRun
    };
}
