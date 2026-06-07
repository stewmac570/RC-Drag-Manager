using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RandomMode;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Tests for the static BYE tracker in RandomBracket (byeGiven / ResetByeTracker).
///
/// byeGiven is a private static HashSet&lt;int&gt; shared across all calls. Without a call
/// to ResetByeTracker() between events, a driver who received a BYE in one event is
/// silently excluded from BYE eligibility in the next, which is the bug these tests
/// document and guard against.
///
/// [TestInitialize] calls ResetByeTracker() before every test to prevent inter-test
/// contamination; the static-state-leak test deliberately omits the mid-test reset.
///
/// [DoNotParallelize]: every test here mutates the process-wide static byeGiven set, so
/// running them concurrently (assembly default is method-level parallel) lets one test's
/// ResetByeTracker()/GenerateFirstRound() corrupt another's state mid-run. Serialize the
/// class so the static setup/teardown is reliable.
/// </summary>
[TestClass]
[DoNotParallelize]
public class RandomBracketByeTrackerTests
{
    // ── Test isolation ────────────────────────────────────────────────────────

    [TestInitialize]
    public void ResetStaticByeState()
    {
        // Must be called before every test because byeGiven is static.
        RandomBracket.ResetByeTracker();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Driver D(int id, string name) =>
        new Driver { Id = id, Name = name };

    private static bool IsByeMatch(RandomMatch m) => m.Seed2 == null;

    private static List<Driver> ThreeDrivers() => new List<Driver>
    {
        D(1, "Alpha"),
        D(2, "Beta"),
        D(3, "Gamma")
    };

    private static List<Driver> FourDrivers() => new List<Driver>
    {
        D(1, "Alpha"),
        D(2, "Beta"),
        D(3, "Gamma"),
        D(4, "Delta")
    };

    // ── Odd field → exactly one BYE in R1 ────────────────────────────────────

    [TestMethod]
    public void GenerateFirstRound_OddDriverCount_ProducesExactlyOneByeMatch()
    {
        var matches = RandomBracket.GenerateFirstRound(ThreeDrivers());

        int byeCount = matches.Count(IsByeMatch);
        Assert.AreEqual(1, byeCount,
            "An odd-field R1 must produce exactly one BYE match (Seed2 == null)");
    }

    [TestMethod]
    public void GenerateFirstRound_OddDriverCount_ByeMatchHasRealDriverInSeed1()
    {
        var matches = RandomBracket.GenerateFirstRound(ThreeDrivers());

        var byeMatch = matches.Single(IsByeMatch);
        Assert.IsNotNull(byeMatch.Seed1,
            "The BYE match must have a real driver in Seed1");
        Assert.IsNull(byeMatch.Seed2,
            "The BYE match must have null in Seed2");
    }

    // ── Even field → no BYE in R1 ────────────────────────────────────────────

    [TestMethod]
    public void GenerateFirstRound_EvenDriverCount_ProducesNoByeMatch()
    {
        var matches = RandomBracket.GenerateFirstRound(FourDrivers());

        int byeCount = matches.Count(IsByeMatch);
        Assert.AreEqual(0, byeCount,
            "An even-field R1 must produce no BYE matches");
    }

    [TestMethod]
    public void GenerateFirstRound_EvenDriverCount_AllMatchesHaveRealDriversInBothSeeds()
    {
        var matches = RandomBracket.GenerateFirstRound(FourDrivers());

        foreach (var m in matches)
        {
            Assert.IsNotNull(m.Seed1, $"Match {m.MatchId}: Seed1 must be a real driver");
            Assert.IsNotNull(m.Seed2, $"Match {m.MatchId}: Seed2 must be a real driver");
        }
    }

    // ── BYE avoidance: driver with prior BYE is skipped ──────────────────────

    [TestMethod]
    public void GenerateFirstRound_DriverWithPriorBye_DoesNotReceiveByeAgain()
    {
        // Mark driver 1 as having already received a BYE by running a single-driver
        // round — the only possible outcome puts driver 1 into byeGiven.
        var driverA = D(1, "Alpha");
        RandomBracket.GenerateFirstRound(new List<Driver> { driverA });
        // byeGiven = { 1 }

        // Now run with all three drivers. Driver 1 is ineligible; the BYE must
        // go to driver 2 or driver 3, regardless of how the list is shuffled.
        var drivers = new List<Driver> { driverA, D(2, "Beta"), D(3, "Gamma") };
        var matches = RandomBracket.GenerateFirstRound(drivers);

        var byeMatch = matches.Single(IsByeMatch);
        Assert.AreNotEqual(driverA.Id, byeMatch.Seed1.Id,
            "Driver 1 already received a BYE and must not receive another in the same event");
    }

    // ── ResetByeTracker restores eligibility ──────────────────────────────────

    [TestMethod]
    public void ResetByeTracker_ClearsState_SoDriverCanReceiveByeAgain()
    {
        // Give the only driver a BYE in the first round.
        var driver = D(1, "Solo");
        RandomBracket.GenerateFirstRound(new List<Driver> { driver });
        // byeGiven = { 1 }

        // Reset: byeGiven should be empty again.
        RandomBracket.ResetByeTracker();

        // Second run with the same single driver — the only possible BYE recipient
        // is driver 1, confirming eligibility was restored.
        var matches = RandomBracket.GenerateFirstRound(new List<Driver> { driver });

        var byeMatch = matches.Single(IsByeMatch);
        Assert.AreEqual(driver.Id, byeMatch.Seed1.Id,
            "After ResetByeTracker(), driver 1 must be eligible for a BYE again");
    }

    [TestMethod]
    public void ResetByeTracker_AllowsTwoConsecutiveRuns_BothProduceValidByeAssignment()
    {
        // Run 1
        var matchesRun1 = RandomBracket.GenerateFirstRound(ThreeDrivers());
        Assert.AreEqual(1, matchesRun1.Count(IsByeMatch),
            "Run 1 must produce exactly one BYE match");
        Assert.IsNotNull(matchesRun1.Single(IsByeMatch).Seed1,
            "Run 1 BYE match must have a real driver in Seed1");

        // Reset between runs (simulates starting a new event)
        RandomBracket.ResetByeTracker();

        // Run 2 — after reset, all three drivers are eligible again
        var matchesRun2 = RandomBracket.GenerateFirstRound(ThreeDrivers());
        Assert.AreEqual(1, matchesRun2.Count(IsByeMatch),
            "Run 2 (after reset) must produce exactly one BYE match");
        Assert.IsNotNull(matchesRun2.Single(IsByeMatch).Seed1,
            "Run 2 BYE match must have a real driver in Seed1");
    }

    // ── GenerateNextRound BYE with odd survivor count ─────────────────────────

    [TestMethod]
    public void GenerateNextRound_OddSurvivorCount_ProducesExactlyOneByeMatch()
    {
        var survivors = ThreeDrivers();
        var history   = new HashSet<(int, int)>();

        var matches = RandomBracket.GenerateNextRound(survivors, history);

        int byeCount = matches.Count(IsByeMatch);
        Assert.AreEqual(1, byeCount,
            "GenerateNextRound with an odd survivor count must produce exactly one BYE match");
    }

    [TestMethod]
    public void GenerateNextRound_OddSurvivorCount_ByeDriverIsEligible_AndRemainingPaired()
    {
        var d1 = D(1, "Alpha");
        var d2 = D(2, "Beta");
        var d3 = D(3, "Gamma");
        var survivors = new List<Driver> { d1, d2, d3 };
        var history   = new HashSet<(int, int)>();

        var matches = RandomBracket.GenerateNextRound(survivors, history);

        // Exactly 2 matches: 1 BYE match + 1 real-vs-real match
        Assert.AreEqual(2, matches.Count,
            "3 survivors must produce 2 matches: 1 BYE and 1 real pair");

        var byeMatch  = matches.Single(IsByeMatch);
        var realMatch = matches.Single(m => !IsByeMatch(m));

        Assert.IsNotNull(byeMatch.Seed1,
            "The BYE match must have a real driver in Seed1");
        Assert.IsNotNull(realMatch.Seed1,
            "The real match must have a real driver in Seed1");
        Assert.IsNotNull(realMatch.Seed2,
            "The real match must have a real driver in Seed2");
    }

    [TestMethod]
    public void GenerateNextRound_DriverWithPriorBye_IsDeprioritised_ForNextRoundBye()
    {
        // Give driver 1 a BYE in R1 by using a single-driver round.
        var d1 = D(1, "Alpha");
        RandomBracket.GenerateFirstRound(new List<Driver> { d1 });
        // byeGiven = { 1 }

        // Now GenerateNextRound with 3 survivors including d1. Drivers 2 and 3 have
        // not had BYEs, so d1 must NOT receive the BYE in this round.
        var d2 = D(2, "Beta");
        var d3 = D(3, "Gamma");
        var survivors = new List<Driver> { d1, d2, d3 };
        var history   = new HashSet<(int, int)>();

        var matches = RandomBracket.GenerateNextRound(survivors, history);

        var byeMatch = matches.Single(IsByeMatch);
        Assert.AreNotEqual(d1.Id, byeMatch.Seed1.Id,
            "Driver 1 already received a BYE and must not receive another when eligible drivers remain");
    }

    // ── Static state leak: without reset, consecutive runs differ ────────────

    [TestMethod]
    public void StaticStateLeak_WithoutReset_SecondRunByeDiffersFromFirst()
    {
        // This test documents the static-state-carry-over behaviour that
        // ResetByeTracker() exists to correct.
        //
        // [TestInitialize] already called ResetByeTracker(), so the slate is clean.
        //
        // Run 1: BYE assigned to some driver X (recorded in static byeGiven).
        var drivers = ThreeDrivers();
        var matchesRun1 = RandomBracket.GenerateFirstRound(drivers);
        int byeDriverIdRun1 = matchesRun1.Single(IsByeMatch).Seed1.Id;

        // Run 2: NO reset between runs.
        // byeGiven still contains X, so the avoidance logic steers the BYE
        // to one of the remaining two drivers — the BYE MUST differ from run 1.
        var matchesRun2 = RandomBracket.GenerateFirstRound(drivers);
        int byeDriverIdRun2 = matchesRun2.Single(IsByeMatch).Seed1.Id;

        Assert.AreNotEqual(byeDriverIdRun1, byeDriverIdRun2,
            "Without ResetByeTracker(), the second run must assign the BYE to a different " +
            "driver — proving static state is carrying over between runs. " +
            "Call ResetByeTracker() at the start of each new event to prevent this.");
    }
}
