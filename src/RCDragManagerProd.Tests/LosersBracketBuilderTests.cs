using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RandomMode;

namespace RCDragManagerProd.Tests;

[TestClass]
public class LosersBracketBuilderTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Driver D(string name) => new Driver { Name = name };

    /// <summary>Creates a list of N uniquely-named drivers.</summary>
    private static List<Driver> Drivers(int count)
    {
        var names = new[] { "Alpha", "Beta", "Gamma", "Delta", "Echo", "Foxt", "Golf", "Hotel" };
        return Enumerable.Range(0, count)
            .Select(i => D(names[i]))
            .ToList();
    }

    private static HashSet<(int, int)> Empty() => new HashSet<(int, int)>();

    /// <summary>Returns (smaller Id, larger Id) for use in a history set.</summary>
    private static (int, int) Pair(Driver a, Driver b) =>
        a.Id < b.Id ? (a.Id, b.Id) : (b.Id, a.Id);

    private static int CountR1(List<RandomMatch> matches) =>
        matches.Count(m => m.RoundLabel == "LB-R1");

    // ── Power-of-two padding → correct R1 slot count ─────────────────────────

    [TestMethod]
    public void EvenDriverCounts_R1MatchCount_ReflectsPaddedSize()
    {
        // 4 drivers → padded = 4 → 2 R1 slots (no BYEs)
        var m4 = LosersBracketBuilder.Build(Drivers(4), Empty());
        Assert.AreEqual(2, CountR1(m4), "4 drivers should produce 2 R1 matches");

        // 8 drivers → padded = 8 → 4 R1 slots (no BYEs)
        var m8 = LosersBracketBuilder.Build(Drivers(8), Empty());
        Assert.AreEqual(4, CountR1(m8), "8 drivers should produce 4 R1 matches");
    }

    [TestMethod]
    public void OddDriverCounts_R1MatchCount_PadsToNextPowerOfTwo()
    {
        // 3 drivers → padded = 4 → 2 R1 slots (one real+real, one real+BYE)
        var m3 = LosersBracketBuilder.Build(Drivers(3), Empty());
        Assert.AreEqual(2, CountR1(m3), "3 drivers padded to 4 should produce 2 R1 matches");

        // 7 drivers → padded = 8 → 4 R1 slots (three real+real, one real+BYE)
        var m7 = LosersBracketBuilder.Build(Drivers(7), Empty());
        Assert.AreEqual(4, CountR1(m7), "7 drivers padded to 8 should produce 4 R1 matches");
    }

    // ── BYE slot placement ────────────────────────────────────────────────────

    [TestMethod]
    public void AllRealDrivers_AppearInAtLeastOneR1Match()
    {
        var drivers = Drivers(4);
        var matches = LosersBracketBuilder.Build(drivers, Empty());

        var r1DriverIds = matches
            .Where(m => m.RoundLabel == "LB-R1")
            .SelectMany(m => new[] { m.Seed1, m.Seed2 })
            .Where(d => d != null)
            .Select(d => d!.Id)
            .ToHashSet();

        foreach (var d in drivers)
            Assert.IsTrue(r1DriverIds.Contains(d.Id),
                $"Driver '{d.Name}' (Id={d.Id}) must appear in an R1 match");
    }

    [TestMethod]
    public void BYEvsNYE_PairsAreSkipped_SixDriversProducesThreeR1Matches()
    {
        // 6 drivers padded to 8: pool = [d1..d6, null, null].
        // The last pair (indices 6,7) is null/null and is skipped entirely.
        var matches = LosersBracketBuilder.Build(Drivers(6), Empty());

        Assert.AreEqual(3, CountR1(matches),
            "6 drivers (padded to 8) should have 3 R1 matches; the null/null pair is skipped");
    }

    [TestMethod]
    public void BYESlotCount_MatchesPaddingRequired()
    {
        // 3 drivers padded to 4 → exactly 1 BYE slot present in R1
        var matches = LosersBracketBuilder.Build(Drivers(3), Empty());

        int byeSlotsInR1 = matches
            .Where(m => m.RoundLabel == "LB-R1")
            .Count(m => ByePolicy.IsBye(m.Seed1) || ByePolicy.IsBye(m.Seed2));

        Assert.AreEqual(1, byeSlotsInR1,
            "3 drivers padded to 4 should have exactly 1 BYE slot across all R1 matches");
    }

    [TestMethod]
    public void NoR1Match_HasBothSlotsBYE()
    {
        // Even though BYE-vs-BYE pairs are skipped, verify none slipped through
        foreach (int count in new[] { 2, 3, 4, 5, 6, 7, 8 })
        {
            var matches = LosersBracketBuilder.Build(Drivers(count), Empty());
            foreach (var m in matches.Where(m => m.RoundLabel == "LB-R1"))
            {
                Assert.IsFalse(ByePolicy.IsBye(m.Seed1) && ByePolicy.IsBye(m.Seed2),
                    $"Count={count}: R1 match {m.MatchId} must not be BYE vs BYE");
            }
        }
    }

    // ── Round labels ──────────────────────────────────────────────────────────

    [TestMethod]
    public void AllR1Matches_HaveLabel_LbR1()
    {
        var matches = LosersBracketBuilder.Build(Drivers(4), Empty());
        Assert.IsTrue(matches.Any(m => m.RoundLabel == "LB-R1"));

        foreach (var m in matches.Where(m => m.Seed1 != null || m.Seed2 != null))
        {
            // All matches that carry real seeds in their initial assignment are R1
            // (subsequent rounds leave Seed1/Seed2 null and use FromMatch references)
            if (m.Seed1 != null || m.Seed2 != null)
                Assert.AreEqual("LB-R1", m.RoundLabel,
                    $"Match {m.MatchId} has real seeds so it must be R1");
        }
    }

    [TestMethod]
    public void FourDriverBracket_LastMatch_HasLabel_LbF()
    {
        // 4 drivers → 2 R1 matches → 1 LB-F match (prevRound.Count == 2)
        var matches = LosersBracketBuilder.Build(Drivers(4), Empty());

        Assert.AreEqual("LB-F", matches.Last().RoundLabel,
            "The last match in a 4-driver bracket must be LB-F");
    }

    [TestMethod]
    public void EightDriverBracket_ContainsAllExpectedRoundLabels()
    {
        // 8 drivers → R1 (4 matches) → LB-R2 (2 matches) → LB-F (1 match)
        var matches = LosersBracketBuilder.Build(Drivers(8), Empty());
        var labels = matches.Select(m => m.RoundLabel).ToHashSet();

        Assert.IsTrue(labels.Contains("LB-R1"), "Must contain LB-R1");
        Assert.IsTrue(labels.Contains("LB-R2"), "Must contain LB-R2");
        Assert.IsTrue(labels.Contains("LB-F"),  "Must contain LB-F");
    }

    [TestMethod]
    public void IntermediateRoundLabels_AreSequential()
    {
        // 8 drivers → LB-R1, LB-R2, LB-F  (LB-R2 appears before LB-F)
        var matches = LosersBracketBuilder.Build(Drivers(8), Empty());

        int r2Count = matches.Count(m => m.RoundLabel == "LB-R2");
        Assert.AreEqual(2, r2Count, "8 drivers should produce exactly 2 LB-R2 matches");

        int lfCount = matches.Count(m => m.RoundLabel == "LB-F");
        Assert.AreEqual(1, lfCount, "There must be exactly one LB-F match");
    }

    // ── FromMatch references ──────────────────────────────────────────────────

    [TestMethod]
    public void PostR1Matches_FromMatch1_PointsToValidMatchId()
    {
        var matches = LosersBracketBuilder.Build(Drivers(8), Empty());
        var validIds = new HashSet<int>(matches.Select(m => m.MatchId));

        foreach (var m in matches.Where(m => m.RoundLabel != "LB-R1"))
        {
            if (m.FromMatch1 > 0)
                Assert.IsTrue(validIds.Contains(m.FromMatch1!.Value),
                    $"Match {m.MatchId} FromMatch1={m.FromMatch1} is not a valid MatchId");
        }
    }

    [TestMethod]
    public void PostR1Matches_FromMatch2_WhenNonZero_PointsToValidMatchId()
    {
        var matches = LosersBracketBuilder.Build(Drivers(8), Empty());
        var validIds = new HashSet<int>(matches.Select(m => m.MatchId));

        foreach (var m in matches.Where(m => m.RoundLabel != "LB-R1"))
        {
            if (m.FromMatch2 > 0)
                Assert.IsTrue(validIds.Contains(m.FromMatch2!.Value),
                    $"Match {m.MatchId} FromMatch2={m.FromMatch2} is not a valid MatchId");
        }
    }

    [TestMethod]
    public void FinalMatch_BothFromMatchReferences_AreWithinRange()
    {
        // Final match must reference the two semifinal matches, which exist in the list
        var matches = LosersBracketBuilder.Build(Drivers(4), Empty());
        var validIds = new HashSet<int>(matches.Select(m => m.MatchId));

        var final = matches.Last();

        Assert.IsTrue(final.FromMatch1 > 0,
            "LB-F match must have a non-zero FromMatch1");
        Assert.IsTrue(validIds.Contains(final.FromMatch1!.Value),
            $"LB-F FromMatch1={final.FromMatch1} does not exist in the output");

        Assert.IsTrue(final.FromMatch2 > 0,
            "LB-F match must have a non-zero FromMatch2");
        Assert.IsTrue(validIds.Contains(final.FromMatch2!.Value),
            $"LB-F FromMatch2={final.FromMatch2} does not exist in the output");
    }

    // ── Rematch avoidance ─────────────────────────────────────────────────────

    [TestMethod]
    public void RematchAvoidance_HistoryPair_IsSplitAcrossAllShuffles()
    {
        // Setup: 3 drivers padded to 4.
        // Pool layout is always [shuffle(d1,d2,d3), null].
        // R1 has exactly one real-vs-real match (positions 0,1) and one real-vs-BYE (positions 2,3).
        //
        // If d1+d2 land at (0,1), the swap target is always d3 at position 2 (never null).
        // Therefore in every possible shuffle, d1 and d2 end up in different R1 matches.
        //
        // Tested over 30 independent Build() calls to exercise all shuffle orderings.

        var d1 = D("One");
        var d2 = D("Two");
        var d3 = D("Three");
        var entrants = new List<Driver> { d1, d2, d3 };
        var history = new HashSet<(int, int)> { Pair(d1, d2) };

        for (int run = 0; run < 30; run++)
        {
            var matches = LosersBracketBuilder.Build(entrants, history);

            bool pairedTogether = matches
                .Where(m => m.RoundLabel == "LB-R1")
                .Any(m =>
                    (m.Seed1?.Id == d1.Id || m.Seed2?.Id == d1.Id) &&
                    (m.Seed1?.Id == d2.Id || m.Seed2?.Id == d2.Id));

            Assert.IsFalse(pairedTogether,
                $"Run {run}: d1 and d2 must not be paired when d3 is available as a swap target");
        }
    }

    [TestMethod]
    public void RematchAvoidance_NoAlternateAvailable_AcceptsRematchAndCompletes()
    {
        // 2 drivers with each other in history: no swap candidate exists.
        // Build() must log the accepted rematch and still return a valid result.
        var d1 = D("Solo");
        var d2 = D("Buddy");
        var history = new HashSet<(int, int)> { Pair(d1, d2) };

        var matches = LosersBracketBuilder.Build(new List<Driver> { d1, d2 }, history);

        Assert.AreEqual(1, matches.Count,
            "2 drivers must produce exactly 1 match even when both are in history");

        var m = matches[0];
        bool containsBoth =
            (m.Seed1?.Id == d1.Id || m.Seed2?.Id == d1.Id) &&
            (m.Seed1?.Id == d2.Id || m.Seed2?.Id == d2.Id);

        Assert.IsTrue(containsBoth,
            "The accepted rematch must still contain both drivers");
    }

    // ── Completes without error for all small counts ──────────────────────────

    [TestMethod]
    public void Build_CompletesWithoutError_ForDriverCounts_2_Through_8()
    {
        for (int count = 2; count <= 8; count++)
        {
            var matches = LosersBracketBuilder.Build(Drivers(count), Empty());

            Assert.IsTrue(matches.Count > 0,
                $"Build({count} drivers) must return at least one match");

            Assert.IsTrue(matches.All(m => !string.IsNullOrEmpty(m.RoundLabel)),
                $"All matches from Build({count} drivers) must have a non-empty RoundLabel");

            Assert.IsTrue(matches.All(m => m.MatchId >= 1000),
                $"All MatchIds from Build({count} drivers) must be >= the default startMatchId (1000)");
        }
    }

    // ── 2-driver edge case ────────────────────────────────────────────────────

    [TestMethod]
    public void TwoDrivers_ProducesExactlyOneMatch_WithBothRealDrivers()
    {
        // 2 drivers → padded to 2 → 1 R1 match → prevRound.Count = 1
        // → subsequent-rounds loop never runs → total = 1 match.
        // Note: the single match is labelled "LB-R1" (not "LB-F"), because it
        // is created in the R1 loop and no subsequent round is generated.
        var matches = LosersBracketBuilder.Build(Drivers(2), Empty());

        Assert.AreEqual(1, matches.Count,
            "2 drivers should produce exactly 1 match (no subsequent rounds needed)");

        var single = matches[0];
        Assert.IsNotNull(single.Seed1, "The single match must have a real driver in Seed1");
        Assert.IsNotNull(single.Seed2, "The single match must have a real driver in Seed2");
    }
}
