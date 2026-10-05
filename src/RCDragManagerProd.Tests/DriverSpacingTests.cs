using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Race-day test (2026-10-05): drivers with several cars raced back to back, and a
/// driver could run the last race of one round and the first of the next. Races are
/// now ordered so each person gets as many races between runs as possible.
/// </summary>
[TestClass]
[DoNotParallelize]
public class DriverSpacingTests
{
    // 17 cars, like the race-day DYO class: one driver with four cars, three with two.
    private static readonly int[] CarsPerOwner = { 4, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1 };

    [DataTestMethod]
    [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void MultiCar_NoDriverRacesBackToBack(int attempt)
    {
        var controller = MultiCar(out _);
        var order = RaceOrder(controller);

        var clashes = BackToBack(order);
        Assert.AreEqual(0, clashes.Count, $"Attempt {attempt}: back-to-back runs: {string.Join("; ", clashes)}");
    }

    [TestMethod]
    public void SingleCar_NoDriverRunsLastAndFirstAcrossRounds()
    {
        var session = new RaceSession
        {
            EventName = "QA", EventDate = new DateTime(2026, 10, 5),
            RaceType = RaceTypes.RoundRobin, ClassType = "Heads Up",
            RoundRobinVariant = "QMDRA", RoundsToRun = 3
        };
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket(RaceTypes.RoundRobin, TestDriverFactory.CreateRoundRobinPack(7));

        Assert.AreEqual(0, BackToBack(RaceOrder(controller)).Count);
    }

    [TestMethod]
    public void Order_DoesNotChangeAsResultsComeIn()
    {
        var controller = MultiCar(out _);
        var before = RaceOrder(controller);

        foreach (var m in controller.PeekUpcomingMatches(4).ToList())
            controller.SubmitWinner(m.MatchId, firstOption: m.Driver1 != null);

        CollectionAssert.AreEqual(before, RaceOrder(controller), "Results must not reshuffle the race order.");
    }

    [TestMethod]
    public void PushToEnd_StillWins()
    {
        var controller = MultiCar(out _);
        var first = controller.PeekUpcomingMatches(1).Single();

        controller.PushCurrentMatchToEndOfRound();

        Assert.AreNotEqual(first.MatchId, controller.PeekUpcomingMatches(1).Single().MatchId);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Every race in the order the console runs them: owner lists per race.</summary>
    private static List<string> RaceOrder(RaceController c) =>
        c.BuildCurrentBracketRows()
            .Where(r => !r.IsHeader)
            .Select(r => Owner(r.Driver1) + "|" + Owner(r.Driver2))
            .ToList();

    private static string Owner(string name)
    {
        if (string.IsNullOrEmpty(name) || name == "BYE") return "";
        int i = name.IndexOf(" — ", StringComparison.Ordinal);
        return i < 0 ? name : name.Substring(0, i);
    }

    private static List<string> BackToBack(List<string> order)
    {
        var clashes = new List<string>();
        for (int i = 1; i < order.Count; i++)
        {
            var prev = order[i - 1].Split('|').Where(x => x != "");
            var now = order[i].Split('|').Where(x => x != "");
            foreach (var p in prev.Intersect(now))
                clashes.Add($"{p} in races {i} and {i + 1}");
        }
        return clashes;
    }

    private static RaceController MultiCar(out RaceSession session)
    {
        var entries = new List<RaceSessionDriverEntry>();
        int id = 1;
        for (int owner = 0; owner < CarsPerOwner.Length; owner++)
            for (int car = 1; car <= CarsPerOwner[owner]; car++, id++)
                entries.Add(new RaceSessionDriverEntry
                {
                    RaceEntryId = id, DriverID = 100 + owner, DriverName = "Owner " + owner,
                    CarID = id, CarName = "Car " + car
                });

        session = new RaceSession
        {
            EventName = "QA Spacing", EventDate = new DateTime(2026, 10, 5),
            RaceType = RaceTypes.MultiCarRoundRobin, ClassType = "DYO",
            RoundRobinVariant = "QMDRA", RoundsToRun = 3, DriverEntries = entries
        };
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket(RaceTypes.MultiCarRoundRobin,
            entries.Select(e => new Driver { Id = e.RaceEntryId, Name = e.DriverName + " — " + e.CarName }).ToList());
        return controller;
    }
}
