using RCDragManagerProd.AppServices;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers the rules behind the per-event Settings tab (#415): when buybacks may be
/// toggled mid-event, and when a class may be reset. Buybacks are the Round-Robin
/// "Standard" variant; off is "QMDRA", where all drivers advance after a fixed
/// number of rounds.
/// </summary>
[TestClass]
public class EventSettingsServiceTests
{
    private const string RR = "Round Robin";

    // ── Buyback toggle ────────────────────────────────────────────────────────

    [TestMethod]
    public void CanChangeBuybacks_FreshRoundRobinClass_Allowed()
    {
        var r = EventSettingsService.CanChangeBuybacks(
            RR, classComplete: false, roundRobinComplete: false, buybacksAlreadyUsed: false,
            turningOff: false, roundsToRun: null);

        Assert.IsTrue(r.IsAllowed);
        Assert.IsNull(r.Reason);
    }

    [TestMethod]
    public void CanChangeBuybacks_NonRoundRobinClass_Blocked()
    {
        foreach (var raceType in new[] { "Pro Ladder", "Random Draw", "", null })
        {
            var r = EventSettingsService.CanChangeBuybacks(
                raceType, false, false, false, turningOff: false, roundsToRun: 3);

            Assert.IsFalse(r.IsAllowed, $"'{raceType}' should not offer buybacks");
            Assert.IsNotNull(r.Reason);
        }
    }

    [TestMethod]
    public void CanChangeBuybacks_CompletedClass_Blocked()
    {
        var r = EventSettingsService.CanChangeBuybacks(
            RR, classComplete: true, roundRobinComplete: true, buybacksAlreadyUsed: false,
            turningOff: false, roundsToRun: 3);

        Assert.IsFalse(r.IsAllowed);
    }

    [TestMethod]
    public void CanChangeBuybacks_AfterABuybackWasApplied_Blocked()
    {
        // The bracket already contains bought-back drivers; flipping the variant now
        // would leave a shape the engine never produces.
        var r = EventSettingsService.CanChangeBuybacks(
            RR, classComplete: false, roundRobinComplete: true, buybacksAlreadyUsed: true,
            turningOff: true, roundsToRun: 3);

        Assert.IsFalse(r.IsAllowed);
        StringAssert.Contains(r.Reason, "buyback");
    }

    [TestMethod]
    public void CanChangeBuybacks_AfterRoundRobinComplete_Blocked()
    {
        // By this point the variant has already decided how finals get seeded.
        var r = EventSettingsService.CanChangeBuybacks(
            RR, classComplete: false, roundRobinComplete: true, buybacksAlreadyUsed: false,
            turningOff: true, roundsToRun: 3);

        Assert.IsFalse(r.IsAllowed);
    }

    [TestMethod]
    public void CanChangeBuybacks_TurningOffWithoutRoundsToRun_Blocked()
    {
        // QMDRA stops after N rounds; without N the controller refuses to seed finals,
        // so the class would strand at the end of Round Robin.
        foreach (int? rounds in new int?[] { null, 0, -1 })
        {
            var r = EventSettingsService.CanChangeBuybacks(
                RR, false, false, false, turningOff: true, roundsToRun: rounds);

            Assert.IsFalse(r.IsAllowed, $"roundsToRun={rounds} should block turning buybacks off");
            StringAssert.Contains(r.Reason, "rounds");
        }
    }

    [TestMethod]
    public void CanChangeBuybacks_TurningOffWithRoundsToRun_Allowed()
    {
        var r = EventSettingsService.CanChangeBuybacks(
            RR, false, false, false, turningOff: true, roundsToRun: 3);

        Assert.IsTrue(r.IsAllowed);
    }

    [TestMethod]
    public void CanChangeBuybacks_TurningOnNeedsNoRoundsToRun()
    {
        // Standard doesn't use RoundsToRun, so a missing value must not block it.
        var r = EventSettingsService.CanChangeBuybacks(
            RR, false, false, false, turningOff: false, roundsToRun: null);

        Assert.IsTrue(r.IsAllowed);
    }

    // ── Variant mapping ───────────────────────────────────────────────────────

    [TestMethod]
    public void VariantMapping_RoundTrips()
    {
        Assert.AreEqual("Standard", EventSettingsService.VariantFor(true));
        Assert.AreEqual("QMDRA", EventSettingsService.VariantFor(false));

        Assert.IsTrue(EventSettingsService.BuybacksEnabledIn("Standard"));
        Assert.IsFalse(EventSettingsService.BuybacksEnabledIn("QMDRA"));
        Assert.IsFalse(EventSettingsService.BuybacksEnabledIn("qmdra"));
    }

    [TestMethod]
    public void BuybacksEnabledIn_UnsetVariant_DefaultsToOn()
    {
        // RaceSession defaults to Standard, so a null/blank variant means buybacks on.
        Assert.IsTrue(EventSettingsService.BuybacksEnabledIn(null));
        Assert.IsTrue(EventSettingsService.BuybacksEnabledIn(""));
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void CanResetClass_InProgressClass_Allowed()
    {
        Assert.IsTrue(EventSettingsService.CanResetClass(classComplete: false).IsAllowed);
    }

    [TestMethod]
    public void CanResetClass_CompletedClass_Blocked()
    {
        // Its results are already recorded against driver stats.
        var r = EventSettingsService.CanResetClass(classComplete: true);

        Assert.IsFalse(r.IsAllowed);
        Assert.IsNotNull(r.Reason);
    }

    // ── Race type restored after a reset ──────────────────────────────────────

    [TestMethod]
    public void RaceTypeToRestoreOnReset_PrefersTheCapturedOriginal()
    {
        // Mid-event RaceType has mutated to Finals; the class must come back as the
        // mode it was set up with, not the phase it happened to be in.
        Assert.AreEqual("Round Robin",
            EventSettingsService.RaceTypeToRestoreOnReset("Round Robin", "Finals"));
    }

    [TestMethod]
    public void RaceTypeToRestoreOnReset_NeverStartedClass_KeepsItsConfiguredType()
    {
        // OriginalRaceType is only captured when a bracket is generated, so a class
        // configured but never raced only has the current value to go on.
        Assert.AreEqual("Pro Ladder",
            EventSettingsService.RaceTypeToRestoreOnReset(null, "Pro Ladder"));
        Assert.AreEqual("Pro Ladder",
            EventSettingsService.RaceTypeToRestoreOnReset("  ", "Pro Ladder"));
    }

    [TestMethod]
    public void RaceTypeToRestoreOnReset_NothingKnown_ReturnsNull()
    {
        Assert.IsNull(EventSettingsService.RaceTypeToRestoreOnReset(null, null));
        Assert.IsNull(EventSettingsService.RaceTypeToRestoreOnReset("", "   "));
    }

    // ── Status text ───────────────────────────────────────────────────────────

    [TestMethod]
    public void DescribeClassStatus_ReportsTheMostAdvancedState()
    {
        Assert.AreEqual("Not started",
            EventSettingsService.DescribeClassStatus(false, false, false));
        Assert.AreEqual("Racing",
            EventSettingsService.DescribeClassStatus(false, false, true));
        Assert.AreEqual("Round Robin complete",
            EventSettingsService.DescribeClassStatus(false, true, true));
        Assert.AreEqual("Complete",
            EventSettingsService.DescribeClassStatus(true, true, true));
    }
}
