using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Regression tests for Standard Round Robin round clamping (issue #99 fix).
///
/// Before the fix, Standard RR always ran 3 rounds regardless of field size, causing
/// duplicate rematches for 2- and 3-driver fields. The fix adds this calculation in
/// RaceController.RoundFlow.Core.cs before calling SetRoundsToRun():
///
///     int naturalMax     = Math.Max(1, drivers.Count - 1);
///     int standardRounds = Math.Min(3, naturalMax);
///
/// Result: Standard RR is capped at min(3, n-1) rounds.
/// QMDRA is unaffected — it always runs the explicitly configured round count.
///
/// Round labels are read via BuildCurrentBracketRows() header rows.
/// GenerateBracket() pre-reveals all RR rounds upfront, so every round produces
/// a header row immediately — no AdvanceRound() calls needed.
/// </summary>
[TestClass]
public class RoundRobinRoundClampingTests
{
    // ── Session / controller factories ────────────────────────────────────────

    private static RaceController BuildAndGenerateStandard(int driverCount)
    {
        var session = new RaceSession
        {
            EventName         = $"Clamping Test — {driverCount} drivers",
            EventDate         = new DateTime(2026, 3, 29),
            RaceType          = "Round Robin",
            ClassType         = "Open",
            RoundRobinVariant = "Standard"
            // RoundsToRun left null — Standard mode derives its count from driver count
        };
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(driverCount));
        return controller;
    }

    private static RaceController BuildAndGenerateQmdra(int driverCount, int roundsToRun)
    {
        var session = new RaceSession
        {
            EventName         = $"QMDRA Clamping Test — {driverCount} drivers, {roundsToRun} rounds",
            EventDate         = new DateTime(2026, 3, 29),
            RaceType          = "Round Robin",
            ClassType         = "Open",
            RoundRobinVariant = "QMDRA",
            RoundsToRun       = roundsToRun
        };
        var controller = new RaceController(session, new NoOpStandingsDialogService());
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(driverCount));
        return controller;
    }

    /// <summary>
    /// Returns the ordered list of distinct round labels that appear as header rows in
    /// BuildCurrentBracketRows(). GenerateBracket() pre-reveals all RR rounds, so every
    /// generated round produces a header immediately.
    /// </summary>
    private static List<string> GetRoundLabels(RaceController controller) =>
        controller.BuildCurrentBracketRows()
                  .Where(r => r.IsHeader)
                  .Select(r => r.RoundLabel)
                  .ToList();

    // ── Standard RR clamping: correct round count ─────────────────────────────

    [TestMethod]
    public void StandardRR_TwoDriverField_ProducesExactlyOneRound()
    {
        // n=2 → naturalMax = max(1, 2-1) = 1 → standardRounds = min(3, 1) = 1
        var controller = BuildAndGenerateStandard(driverCount: 2);
        var rounds = GetRoundLabels(controller);

        Assert.AreEqual(1, rounds.Count,
            "2-driver Standard RR must produce exactly 1 round (n-1=1, capped at 3 → 1)");
    }

    [TestMethod]
    public void StandardRR_ThreeDriverField_ProducesExactlyTwoRounds()
    {
        // n=3 → naturalMax = max(1, 3-1) = 2 → standardRounds = min(3, 2) = 2
        var controller = BuildAndGenerateStandard(driverCount: 3);
        var rounds = GetRoundLabels(controller);

        Assert.AreEqual(2, rounds.Count,
            "3-driver Standard RR must produce exactly 2 rounds (n-1=2, capped at 3 → 2)");
    }

    [TestMethod]
    public void StandardRR_FourDriverField_ProducesExactlyThreeRounds()
    {
        // n=4 → naturalMax = max(1, 4-1) = 3 → standardRounds = min(3, 3) = 3
        var controller = BuildAndGenerateStandard(driverCount: 4);
        var rounds = GetRoundLabels(controller);

        Assert.AreEqual(3, rounds.Count,
            "4-driver Standard RR must produce exactly 3 rounds (n-1=3, capped at 3 → 3)");
    }

    [TestMethod]
    public void StandardRR_FiveDriverField_ProducesExactlyThreeRounds()
    {
        // n=5 → naturalMax = max(1, 5-1) = 4 → standardRounds = min(3, 4) = 3
        // This is the core regression: pre-fix the engine defaulted to 3 regardless,
        // but without the explicit cap the formula was absent and easy to break.
        var controller = BuildAndGenerateStandard(driverCount: 5);
        var rounds = GetRoundLabels(controller);

        Assert.AreEqual(3, rounds.Count,
            "5-driver Standard RR must produce exactly 3 rounds (n-1=4, capped at 3 → 3)");
    }

    [TestMethod]
    public void StandardRR_EightDriverField_ProducesExactlyThreeRounds()
    {
        // n=8 → naturalMax = max(1, 8-1) = 7 → standardRounds = min(3, 7) = 3
        var controller = BuildAndGenerateStandard(driverCount: 8);
        var rounds = GetRoundLabels(controller);

        Assert.AreEqual(3, rounds.Count,
            "8-driver Standard RR must produce exactly 3 rounds (n-1=7, capped at 3 → 3)");
    }

    // ── Standard RR clamping: round labels are RR1, RR2, … RRn ──────────────

    [TestMethod]
    public void StandardRR_TwoDriverField_RoundLabel_IsRR1()
    {
        var controller = BuildAndGenerateStandard(driverCount: 2);
        var rounds = GetRoundLabels(controller);

        Assert.AreEqual("RR1", rounds[0],
            "The only round in a 2-driver Standard RR must be labelled 'RR1'");
    }

    [TestMethod]
    public void StandardRR_ThreeDriverField_RoundLabels_AreRR1_RR2_NoGaps()
    {
        var controller = BuildAndGenerateStandard(driverCount: 3);
        var rounds = GetRoundLabels(controller);

        Assert.AreEqual("RR1", rounds[0], "First round must be 'RR1'");
        Assert.AreEqual("RR2", rounds[1], "Second round must be 'RR2'");
    }

    [TestMethod]
    public void StandardRR_FourDriverField_RoundLabels_AreRR1_RR2_RR3_NoGaps()
    {
        var controller = BuildAndGenerateStandard(driverCount: 4);
        var rounds = GetRoundLabels(controller);

        Assert.AreEqual("RR1", rounds[0], "First round must be 'RR1'");
        Assert.AreEqual("RR2", rounds[1], "Second round must be 'RR2'");
        Assert.AreEqual("RR3", rounds[2], "Third round must be 'RR3'");
    }

    [TestMethod]
    public void StandardRR_RoundLabels_AreSequential_NoGaps_ForAllClampedCounts()
    {
        // Parameterised sweep: for each Standard field size verify round labels are
        // RR1, RR2, …, RRn with no gaps and no unexpected labels.
        var cases = new[]
        {
            (drivers: 2, expectedRounds: 1),
            (drivers: 3, expectedRounds: 2),
            (drivers: 4, expectedRounds: 3),
            (drivers: 5, expectedRounds: 3),
            (drivers: 8, expectedRounds: 3),
        };

        foreach (var (driverCount, expectedRounds) in cases)
        {
            var controller = BuildAndGenerateStandard(driverCount);
            var rounds = GetRoundLabels(controller);

            Assert.AreEqual(expectedRounds, rounds.Count,
                $"{driverCount} drivers: expected {expectedRounds} round(s), got {rounds.Count}");

            for (int i = 0; i < rounds.Count; i++)
            {
                string expected = $"RR{i + 1}";
                Assert.AreEqual(expected, rounds[i],
                    $"{driverCount} drivers: round at index {i} must be '{expected}', got '{rounds[i]}'");
            }
        }
    }

    // ── QMDRA is unaffected by Standard clamping ──────────────────────────────

    [TestMethod]
    public void QmdraMode_FourDriverField_SetRoundsToRun5_ProducesFiveRounds()
    {
        // QMDRA uses session.RoundsToRun; the controller passes it straight through
        // to SetRoundsToRun() without applying the Standard min(3, n-1) cap.
        // 4 drivers + 5 rounds is a normal QMDRA configuration (rematches expected
        // after round 3 — that is by design).
        var controller = BuildAndGenerateQmdra(driverCount: 4, roundsToRun: 5);
        var rounds = GetRoundLabels(controller);

        Assert.AreEqual(5, rounds.Count,
            "QMDRA with RoundsToRun=5 and 4 drivers must produce exactly 5 rounds; Standard clamping must NOT apply");

        Assert.AreEqual("RR1", rounds[0], "First QMDRA round must be 'RR1'");
        Assert.AreEqual("RR5", rounds[4], "Fifth QMDRA round must be 'RR5'");
    }
}
