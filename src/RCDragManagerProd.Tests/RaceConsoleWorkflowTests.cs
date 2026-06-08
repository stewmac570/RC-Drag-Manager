using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Maps each major race-console button to its <see cref="RaceController"/> command and
/// asserts the operator-observable outcome — exercising the controller directly, never
/// WinForms controls, so the whole suite runs under <c>dotnet test</c> (issue #285).
///
/// Buttons whose full lifecycle already has a dedicated suite are not re-run here to avoid
/// churn: Build Bracket / Winner / Advance Round (RaceControllerFlowTests), Open Buybacks +
/// finals transition (RaceControllerRRStandardFlowTests, RaceControllerQmdraFlowTests),
/// Save Progress / Close Race (RaceControllerSaveCloseTests), Resume (RaceControllerResumeTests),
/// Reset Race setup-vs-active prevention (RaceControllerResetTests). The net-new coverage below
/// is the "Edit Match Result" and "Set Dial-In" buttons, neither of which had a service test.
/// A single core-loop mapping test anchors the Build → Winner → Advance trio in one place.
/// </summary>
[TestClass]
public class RaceConsoleWorkflowTests
{
    // ── Build Bracket / Winner / Advance Round (core operator loop) ───────────────

    [TestMethod]
    public void CoreLoop_BuildBracket_PickWinner_AdvanceRound_MapToControllerCommands()
    {
        var controller = new RaceController(CreateProLadderSession());

        // "Build Bracket" → GenerateBracket
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());
        Assert.IsTrue(controller.HasBracketStarted, "Build Bracket must start the controller flow");

        var firstRound = controller.PeekUpcomingMatches(10).ToList();
        Assert.IsTrue(firstRound.Count >= 2, "Pro Ladder R1 must surface at least two matches");
        var activeBefore = controller.GetActiveRoundLabel();

        // Winner picker buttons → SubmitWinner
        foreach (var match in firstRound)
        {
            controller.SubmitWinner(match.MatchId, firstOption: true);
            Assert.IsNotNull(controller.GetWinner(match.MatchId), "Winner pick must record the winner");
            Assert.IsNotNull(controller.GetLoser(match.MatchId), "Winner pick must record the loser");
        }

        // "Advance Round" → AdvanceRound
        controller.AdvanceRound();
        var activeAfter = controller.GetActiveRoundLabel();
        Assert.AreNotEqual(activeBefore, activeAfter, "Advance Round must move to the next round once resolved");
        Assert.IsTrue(controller.PeekUpcomingMatches(1).Count > 0, "Next round must expose its match after advancing");
    }

    // ── Edit Match Result → EditWinnerInActiveRound ───────────────────────────────

    [TestMethod]
    public void EditMatchResult_InActiveRound_SwapsWinnerAndLoser()
    {
        var controller = new RaceController(CreateProLadderSession());
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        var match = controller.PeekUpcomingMatches(10)
            .First(m => m.Driver1 != null && m.Driver2 != null);

        controller.SubmitWinner(match.MatchId, firstOption: true);
        Assert.AreEqual(match.Driver1.Id, controller.GetWinner(match.MatchId)?.Id,
            "Initial pick must make Driver1 the winner");

        bool edited = controller.EditWinnerInActiveRound(match.MatchId, firstOption: false);

        Assert.IsTrue(edited, "Editing a match in the active round must succeed");
        Assert.AreEqual(match.Driver2.Id, controller.GetWinner(match.MatchId)?.Id,
            "Edit must promote Driver2 to winner");
        Assert.AreEqual(match.Driver1.Id, controller.GetLoser(match.MatchId)?.Id,
            "Edit must demote the previous winner to loser");
    }

    [TestMethod]
    public void EditMatchResult_OutsideActiveRound_IsRejected()
    {
        var controller = new RaceController(CreateProLadderSession());
        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        var r1Matches = controller.PeekUpcomingMatches(10).ToList();
        var r1Target = r1Matches.First(m => m.Driver1 != null && m.Driver2 != null);
        var r1WinnerId = (int?)null;

        foreach (var match in r1Matches)
            controller.SubmitWinner(match.MatchId, firstOption: true);

        controller.AdvanceRound();
        Assert.IsFalse(controller.IsMatchInActiveRound(r1Target.MatchId),
            "After advancing, the R1 match must no longer be in the active round");

        r1WinnerId = controller.GetWinner(r1Target.MatchId)?.Id;

        bool edited = controller.EditWinnerInActiveRound(r1Target.MatchId, firstOption: false);

        Assert.IsFalse(edited, "Editing a match outside the active round must be rejected");
        Assert.AreEqual(r1WinnerId, controller.GetWinner(r1Target.MatchId)?.Id,
            "A rejected edit must leave the original result untouched");
    }

    // ── Set Dial-In → dial-in edit + lock/unlock commands ─────────────────────────

    [TestMethod]
    public void SetDialIn_ReadsAndUpdatesDriverDialIn()
    {
        var controller = new RaceController(CreateProLadderSession());

        Assert.AreEqual(3.910, controller.GetDriverDialIn(1),
            "GetDriverDialIn must return the configured entry value");

        controller.UpdateDriverDialIn(1, 3.870);

        Assert.AreEqual(3.870, controller.GetDriverDialIn(1),
            "UpdateDriverDialIn must round-trip the new value");
    }

    [TestMethod]
    public void DialInLock_DefaultsUnlocked_AndTogglesWithLockUnlock()
    {
        var controller = new RaceController(CreateProLadderSession());

        Assert.IsFalse(controller.DialInLocked, "Dial-in must start unlocked");

        controller.LockDialIn();
        Assert.IsTrue(controller.DialInLocked, "LockDialIn must lock dial-in");

        controller.UnlockDialIn();
        Assert.IsFalse(controller.DialInLocked, "UnlockDialIn must unlock dial-in");
    }

    [TestMethod]
    public void SetDialIn_ManualEdit_AppliesEvenWhenLocked()
    {
        // The lock only gates the live-broadcast poll from overwriting dial-ins; the operator's
        // manual "Set Dial-In" edit must still apply while locked. Guards the UpdateDriverDialIn
        // contract that PollDialInUpdatesAsync — not the edit path — honors _dialInLocked.
        var controller = new RaceController(CreateProLadderSession());

        controller.LockDialIn();
        controller.UpdateDriverDialIn(2, 4.123);

        Assert.IsTrue(controller.DialInLocked, "Editing must not change the lock state");
        Assert.AreEqual(4.123, controller.GetDriverDialIn(2),
            "Manual dial-in edits must apply even while dial-in is locked");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static RaceSession CreateProLadderSession()
    {
        return new RaceSession
        {
            EventName = "QA Console Workflow",
            EventDate = new DateTime(2026, 3, 24, 9, 0, 0),
            RaceType = "Pro Ladder",
            ClassType = "Heads Up",
            DriverEntries = new List<RaceSessionDriverEntry>
            {
                new RaceSessionDriverEntry { DriverID = 1, DriverName = "Ava Stone",    DialIn = 3.910 },
                new RaceSessionDriverEntry { DriverID = 2, DriverName = "Blake Turner", DialIn = 3.955 },
                new RaceSessionDriverEntry { DriverID = 3, DriverName = "Casey Reed",   DialIn = 4.005 },
                new RaceSessionDriverEntry { DriverID = 4, DriverName = "Drew Cole",    DialIn = 4.080 }
            }
        };
    }
}
