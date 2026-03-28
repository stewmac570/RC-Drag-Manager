using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Field-level regression tests for RaceController.Reset() (issue #95).
///
/// Reset() is a single public method that clears a large number of private fields
/// and raises events.  The broad signal-level assertions (HasBracketStarted, RaceType,
/// CanAdvanceChanged, CanPickWinnerChanged, NextMatchReady) are already covered by
/// Reset_ClearsControllerState_AndDisablesProgressSignals in RaceControllerFlowTests.
///
/// These tests cover every private field that Reset() touches but that is NOT visible
/// through the public API — accessed here via System.Reflection — plus the key
/// regression guard: a second GenerateBracket() after Reset() must start cleanly.
/// </summary>
[TestClass]
public class RaceControllerResetTests
{
    // ── Reflection helpers ────────────────────────────────────────────────────

    private static T GetField<T>(RaceController controller, string fieldName)
    {
        var fi = typeof(RaceController).GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingFieldException(nameof(RaceController), fieldName);
        return (T)fi.GetValue(controller)!;
    }

    private static void SetField(RaceController controller, string fieldName, object? value)
    {
        var fi = typeof(RaceController).GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingFieldException(nameof(RaceController), fieldName);
        fi.SetValue(controller, value);
    }

    // ── Session / controller factory ─────────────────────────────────────────

    private static RaceSession NewSession() => new RaceSession
    {
        EventName = "Reset Field Tests",
        EventDate  = new DateTime(2026, 3, 29),
        RaceType   = "Pro Ladder",
        ClassType  = "Heads Up"
    };

    // ── _matchResult cleared ──────────────────────────────────────────────────

    [TestMethod]
    public void MatchResult_IsClearedAfterReset_WhenResultsWerePresent()
    {
        // Start a bracket, submit one winner so _matchResult has at least one
        // entry, then Reset() and confirm no results remain.
        var controller = new RaceController(NewSession());
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        var firstMatch = controller.PeekUpcomingMatches(1)[0];
        controller.SubmitWinner(firstMatch.MatchId, firstOption: true);

        // Precondition: a result was recorded.
        Assert.IsNotNull(controller.GetWinner(firstMatch.MatchId),
            "Precondition: winner must be stored before Reset()");

        controller.Reset();

        // The controller has no public GetAllResults(), but GetWinner() returns
        // null for an unknown matchId — and after Reset() the internal map is cleared,
        // so even the previously recorded matchId returns null.
        Assert.IsNull(controller.GetWinner(firstMatch.MatchId),
            "GetWinner() must return null for every matchId after Reset()");
    }

    // ── _finalsPending ────────────────────────────────────────────────────────

    [TestMethod]
    public void FinalsPending_IsFalseAfterReset_WhenItWasTrue()
    {
        // _finalsPending is not set by any public method we can call cheaply without
        // running an entire tournament, so we set it via reflection and verify the
        // public IsFinalsPending property reads false after Reset().
        var controller = new RaceController(NewSession());
        SetField(controller, "_finalsPending", true);

        Assert.IsTrue(controller.IsFinalsPending, "Precondition: IsFinalsPending must be true");

        controller.Reset();

        Assert.IsFalse(controller.IsFinalsPending,
            "_finalsPending must be false after Reset()");
    }

    // ── _activeRound ──────────────────────────────────────────────────────────

    [TestMethod]
    public void ActiveRound_IsNullAfterReset_WhenBracketWasStarted()
    {
        // GenerateBracket sets _activeRound to the first round label.
        // After Reset() it must be null (or empty) so that a fresh bracket can set it.
        var controller = new RaceController(NewSession());
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        // Precondition: _activeRound was assigned by GenerateBracket.
        var activeAfterGenerate = GetField<string>(controller, "_activeRound");
        Assert.IsFalse(string.IsNullOrEmpty(activeAfterGenerate),
            "Precondition: _activeRound must be non-null/non-empty after GenerateBracket()");

        controller.Reset();

        var activeAfterReset = GetField<string>(controller, "_activeRound");
        Assert.IsTrue(string.IsNullOrEmpty(activeAfterReset),
            "_activeRound must be null or empty after Reset()");
    }

    // ── _rrLoggedRounds ───────────────────────────────────────────────────────

    [TestMethod]
    public void RrLoggedRounds_IsEmptyAfterReset_WhenItWasPopulated()
    {
        var controller = new RaceController(NewSession());

        // Add an entry directly via reflection (avoids running a full RR flow).
        var loggedRounds = GetField<HashSet<string>>(controller, "_rrLoggedRounds");
        loggedRounds.Add("RR1");
        Assert.AreEqual(1, loggedRounds.Count, "Precondition: _rrLoggedRounds must have one entry");

        controller.Reset();

        // The same HashSet instance is reused (it is readonly); Reset() calls .Clear() on it.
        Assert.AreEqual(0, loggedRounds.Count,
            "_rrLoggedRounds must be empty after Reset()");
    }

    // ── RR snapshot fields ────────────────────────────────────────────────────

    [TestMethod]
    public void RrTop3_IsNullAfterReset()
    {
        var controller = new RaceController(NewSession());
        SetField(controller, "_rrTop3", new List<Driver> { new Driver { Name = "Sentinel" } });

        Assert.IsNotNull(GetField<List<Driver>>(controller, "_rrTop3"),
            "Precondition: _rrTop3 must be non-null before Reset()");

        controller.Reset();

        Assert.IsNull(GetField<List<Driver>>(controller, "_rrTop3"),
            "_rrTop3 must be null after Reset()");
    }

    [TestMethod]
    public void RrMatchesSnapshot_IsNullAfterReset()
    {
        var controller = new RaceController(NewSession());
        SetField(controller, "_rrMatchesSnapshot", new List<EngineMatch>());

        Assert.IsNotNull(GetField<List<EngineMatch>>(controller, "_rrMatchesSnapshot"),
            "Precondition: _rrMatchesSnapshot must be non-null before Reset()");

        controller.Reset();

        Assert.IsNull(GetField<List<EngineMatch>>(controller, "_rrMatchesSnapshot"),
            "_rrMatchesSnapshot must be null after Reset()");
    }

    [TestMethod]
    public void RrRoundOrderSnapshot_IsNullAfterReset()
    {
        var controller = new RaceController(NewSession());
        SetField(controller, "_rrRoundOrderSnapshot", new List<string> { "RR1", "RR2" });

        Assert.IsNotNull(GetField<List<string>>(controller, "_rrRoundOrderSnapshot"),
            "Precondition: _rrRoundOrderSnapshot must be non-null before Reset()");

        controller.Reset();

        Assert.IsNull(GetField<List<string>>(controller, "_rrRoundOrderSnapshot"),
            "_rrRoundOrderSnapshot must be null after Reset()");
    }

    [TestMethod]
    public void RrStandingsCardCache_IsNullAfterReset()
    {
        var controller = new RaceController(NewSession());
        SetField(controller, "_rrStandingsCardCache", "cached standings text");

        Assert.IsNotNull(GetField<string>(controller, "_rrStandingsCardCache"),
            "Precondition: _rrStandingsCardCache must be non-null before Reset()");

        controller.Reset();

        Assert.IsNull(GetField<string>(controller, "_rrStandingsCardCache"),
            "_rrStandingsCardCache must be null after Reset()");
    }

    // ── Second GenerateBracket() starts cleanly ───────────────────────────────

    [TestMethod]
    public void SecondGenerateBracket_AfterReset_StartsCleanly_WithCorrectMatchCount()
    {
        // This is the core regression guard for issue #95: stale private state
        // after Reset() caused the second GenerateBracket() to produce an incorrect
        // bracket (wrong match count, wrong round labels, or an exception).
        var drivers = TestDriverFactory.CreateProLadderPack(); // 4 drivers → 2 R1 matches

        var controller = new RaceController(NewSession());

        // First run
        controller.GenerateBracket("Pro Ladder", drivers);
        foreach (var m in controller.PeekUpcomingMatches(10))
            controller.SubmitWinner(m.MatchId, firstOption: true);

        // Reset, then second run with the same drivers
        controller.Reset();
        controller.GenerateBracket("Pro Ladder", drivers);

        Assert.IsTrue(controller.HasBracketStarted,
            "HasBracketStarted must be true after a second GenerateBracket() call");

        var matches = controller.PeekUpcomingMatches(10);
        Assert.AreNotEqual(0, matches.Count,
            "Second GenerateBracket() must produce at least one visible match");

        // 4 drivers → Pro Ladder → R1 has 2 matches
        Assert.AreEqual(2, matches.Count,
            "4 drivers → Pro Ladder → R1 must expose exactly 2 matches on the second run");
    }
}
