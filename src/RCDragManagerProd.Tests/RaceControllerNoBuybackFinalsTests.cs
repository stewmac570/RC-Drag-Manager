using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Race day 2026-10-03. With buybacks off, a Round Robin class sends EVERY driver to
/// the Finals, and the Finals ladder is seeded on Round Robin ranking (1st v last,
/// 2nd v second-last, and so on). Supersedes the short-lived "top four" rule.
///
/// The seeding tests give each competitor a qualifying time that runs against the
/// ranking: the ladder used to re-sort finalists on qualifying time, then name, which
/// threw the ranking away.
/// </summary>
[TestClass]
[DoNotParallelize]
public class RaceControllerNoBuybackFinalsTests
{
    [DataTestMethod]
    [DataRow(RaceTypes.RoundRobin)]
    [DataRow(RaceTypes.MultiCarRoundRobin)]
    public void BuybacksOff_StartFinals_FieldsEveryDriver(string raceType)
    {
        var session = NoBuybackSession(raceType);
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket(raceType, EightCompetitors(session));

        ResolveRoundRobin(controller, rounds: 3);
        Assert.IsTrue(controller.IsFinalsPending, "Round Robin completion must raise the Finals gate.");
        Assert.AreEqual(RaceController.FinalsReasonRoundRobinAllAdvance, controller.FinalsPendingReason,
            "Buybacks off must never offer a losers bracket.");

        controller.StartFinals();

        Assert.AreEqual(RaceTypes.Finals, session.RaceType);
        Assert.AreEqual(8, FinalsDriverIds(controller).Count,
            "Buybacks off must send every driver to the Finals.");
    }

    [DataTestMethod]
    [DataRow(RaceTypes.RoundRobin, 8)]
    [DataRow(RaceTypes.MultiCarRoundRobin, 8)]
    [DataRow(RaceTypes.RoundRobin, 4)]
    public void BuybacksOff_FinalsLadderIsSeededOnRanking(string raceType, int field)
    {
        var session = NoBuybackSession(raceType);
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket(raceType, Competitors(session, field));
        ResolveRoundRobin(controller, rounds: 3);

        var ranked = RankedIds(session);
        controller.StartFinals();

        AssertSeededOn(ranked, controller.PeekUpcomingMatches(50).ToList());
    }

    [TestMethod]
    public void FourDriverFinals_PairsFirstWithFourth_AndSecondWithThird()
    {
        var session = NoBuybackSession(RaceTypes.RoundRobin);
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket(RaceTypes.RoundRobin, Competitors(session, 4));
        ResolveRoundRobin(controller, rounds: 3);

        var r = RankedIds(session);
        controller.StartFinals();

        var semis = controller.PeekUpcomingMatches(10)
            .Select(m => Pair(m.Driver1?.Id ?? 0, m.Driver2?.Id ?? 0))
            .ToList();
        CollectionAssert.Contains(semis, Pair(r[0], r[3]), "1st must race 4th.");
        CollectionAssert.Contains(semis, Pair(r[1], r[2]), "2nd must race 3rd.");
    }

    [TestMethod]
    public void BuybacksOff_FinalsSeeding_SurvivesSaveAndResume()
    {
        var session = NoBuybackSession(RaceTypes.RoundRobin);
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket(RaceTypes.RoundRobin, Competitors(session, 8));
        ResolveRoundRobin(controller, rounds: 3);
        var ranked = RankedIds(session);
        controller.StartFinals();

        var before = PairsOf(controller.PeekUpcomingMatches(50));
        var restored = SaveRestore(session, controller);
        var after = PairsOf(restored.PeekUpcomingMatches(50));

        CollectionAssert.AreEqual(before, after, "Resumed Finals must keep the same pairings.");
        AssertSeededOn(ranked, restored.PeekUpcomingMatches(50).ToList());
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Every slot in the ladder template that takes a seed must hold the
    /// driver ranked at that seed.</summary>
    private static void AssertSeededOn(List<int> ranked, List<EngineMatch> firstRound)
    {
        var template = ProLadder.GetLadder(ranked.Count);
        Assert.IsTrue(template.Count > 0, "No ladder template for " + ranked.Count);
        var byId = firstRound.ToDictionary(m => m.MatchId);
        var checkedSlots = 0;
        foreach (var slot in template.Where(s => s.Seed1 > 0 && s.Seed2 > 0))
        {
            if (!byId.TryGetValue(slot.MatchId, out var m)) continue;
            Assert.AreEqual(ranked[slot.Seed1.Value - 1], m.Driver1?.Id, $"Match {slot.MatchId} seed {slot.Seed1}");
            if (slot.Seed2.Value <= ranked.Count)
                Assert.AreEqual(ranked[slot.Seed2.Value - 1], m.Driver2?.Id, $"Match {slot.MatchId} seed {slot.Seed2}");
            checkedSlots++;
        }
        Assert.IsTrue(checkedSlots > 0, "No seeded first-round matches were checked.");
    }

    private static List<int> RankedIds(RaceSession session) =>
        session.ResultsArchive.RoundRobinStandings.OrderBy(s => s.Rank).Select(s => s.DriverId).ToList();

    private static List<int> FinalsDriverIds(RaceController controller) =>
        controller.PeekUpcomingMatches(50)
            .SelectMany(m => new[] { m.Driver1, m.Driver2 })
            .Where(d => d != null).Select(d => d.Id).Distinct().ToList();

    private static List<string> PairsOf(IEnumerable<EngineMatch> matches) =>
        matches.Select(m => (m.RoundLabel ?? "") + "|" + Pair(m.Driver1?.Id ?? 0, m.Driver2?.Id ?? 0))
               .OrderBy(s => s, StringComparer.Ordinal).ToList();

    private static string Pair(int a, int b) => a < b ? a + "v" + b : b + "v" + a;

    private static List<Driver> EightCompetitors(RaceSession session) => Competitors(session, 8);

    /// <summary>
    /// Competitors with qualifying times that run backwards against name order, so a
    /// ladder that re-sorts on time or name cannot match the ranking by accident.
    /// Multi-Car Round Robin races car entries: two cars per driver.
    /// </summary>
    private static List<Driver> Competitors(RaceSession session, int count)
    {
        List<Driver> list;
        if (session.RaceType != RaceTypes.MultiCarRoundRobin)
        {
            list = TestDriverFactory.CreateRoundRobinPack(count);
        }
        else
        {
            var owners = TestDriverFactory.CreateRoundRobinPack((count + 1) / 2);
            session.DriverEntries = Enumerable.Range(1, count).Select(i => new RaceSessionDriverEntry
            {
                RaceEntryId = i,
                DriverID = (i + 1) / 2,
                DriverName = owners[(i + 1) / 2 - 1].Name,
                CarID = i,
                CarName = "Car " + i
            }).ToList();
            list = session.DriverEntries
                .Select(e => new Driver { Id = e.RaceEntryId, Name = e.DriverName + " — " + e.CarName })
                .ToList();
        }
        for (var i = 0; i < list.Count; i++)
            list[i].QualTime = 5.0 - i * 0.1;
        return list;
    }

    /// <summary>Alternating winners so the ranking is not simply entry order.</summary>
    private static void ResolveRoundRobin(RaceController controller, int rounds)
    {
        for (var i = 0; i < rounds; i++)
        {
            var flip = false;
            foreach (var match in controller.PeekUpcomingMatches(50).ToList())
            {
                controller.SubmitWinner(match.MatchId, firstOption: flip);
                flip = !flip;
            }
            if (i < rounds - 1) controller.AdvanceRound();
        }
    }

    private static RaceSession NoBuybackSession(string raceType) => new RaceSession
    {
        EventName = "QA No-Buyback Finals",
        EventDate = new DateTime(2026, 10, 5, 9, 0, 0),
        RaceType = raceType,
        ClassType = "Heads Up",
        RoundRobinVariant = "QMDRA",
        RoundsToRun = 3
    };

    private static RaceController SaveRestore(RaceSession session, RaceController controller)
    {
        controller.SaveSession();
        var path = Path.Combine(Path.GetTempPath(), $"rcdm-nobuyback-{Guid.NewGuid():N}.db");
        var cs = $"Data Source={path};Version=3;";
        try
        {
            DatabaseInitializer.InitializeDatabase(cs);
            var repo = new RaceSessionRepository(cs);
            var loaded = repo.LoadSession(repo.SaveSession(session));
            Assert.IsNotNull(loaded, "Session must load back from the repository");
            var restored = new RaceController(loaded, new NoOpStandingsDialogService());
            restored.RestoreFromSave();
            return restored;
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
        }
    }
}
