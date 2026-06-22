using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Exercises the shared headless test foundation (issue #290): the reusable
/// <see cref="TestSessionFactory"/> builders and the <see cref="RecordingStandingsDialogService"/>
/// double. These tests both prove the helpers produce valid inputs and add genuine new
/// coverage of the standings dialog seam that backs the race-console "Standings" button —
/// previously untested. Everything runs under <c>dotnet test</c> with no WinForms windows.
/// </summary>
[TestClass]
[DoNotParallelize]
public class TestFoundationTests
{
    // ── Session builders produce valid controller inputs ──────────────────────────

    [TestMethod]
    public void ProLadderBuilder_ProducesSessionThatStartsBracket()
    {
        var session = TestSessionFactory.ProLadder();
        var controller = new RaceController(session);

        controller.GenerateBracket("Pro Ladder", TestDriverFactory.CreateProLadderPack());

        Assert.IsTrue(controller.HasBracketStarted, "Pro Ladder builder must yield a startable session");
        Assert.AreEqual("Pro Ladder", session.RaceType);
        Assert.IsFalse(string.IsNullOrWhiteSpace(controller.GetActiveRoundLabel()));
        Assert.IsTrue(controller.PeekUpcomingMatches(10).Count > 0);
    }

    [TestMethod]
    public void RoundRobinBuilder_ProducesQmdraSession_WithExpectedConfig()
    {
        var session = TestSessionFactory.RoundRobin(variant: "QMDRA", roundsToRun: 2);
        var controller = new RaceController(session, new NoOpStandingsDialogService());

        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));

        Assert.AreEqual(RaceTypes.RoundRobin, session.RaceType);
        Assert.AreEqual("QMDRA", session.RoundRobinVariant);
        Assert.AreEqual(2, session.RoundsToRun);
        Assert.AreEqual("RR1", controller.GetActiveRoundLabel());
    }

    [TestMethod]
    public void WithDriverEntries_SeedsDialInsReadableThroughController()
    {
        var pack = TestDriverFactory.CreateProLadderPack();
        var session = TestSessionFactory.ProLadder().WithDriverEntries(pack);
        var controller = new RaceController(session);

        Assert.AreEqual(pack.Count, session.DriverEntries.Count);
        foreach (var driver in pack)
        {
            Assert.AreEqual(driver.QualTime, controller.GetDriverDialIn(driver.Id),
                $"Dial-in for driver {driver.Id} must be seeded from the qualifying time");
        }
    }

    [TestMethod]
    public void MultiClassEventBuilder_WrapsClassSessions_AndPropagatesEventName()
    {
        var pro = TestSessionFactory.ProLadder(classType: "Pro");
        var sportsman = TestSessionFactory.RoundRobin(classType: "Sportsman");

        var evt = TestSessionFactory.MultiClassEvent("Spring Shootout", pro, sportsman);

        Assert.AreEqual("Spring Shootout", evt.EventName);
        Assert.AreEqual(2, evt.ClassSessions.Count);
        Assert.IsTrue(evt.ClassSessions.All(s => s.EventName == "Spring Shootout"),
            "Each class session must inherit the parent event name");
        Assert.IsTrue(evt.ClassSessions.All(s => s.EventDate == evt.EventDate));
    }

    // ── Standings dialog seam (backs the "Standings" button) ──────────────────────

    [TestMethod]
    public void Standings_NotShown_BeforeRoundRobinCompletes()
    {
        var recorder = new RecordingStandingsDialogService();
        var controller = new RaceController(
            TestSessionFactory.RoundRobin(variant: "Standard"), recorder);

        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(4));

        Assert.IsFalse(controller.TryShowRoundRobinStandings(),
            "Standings must report unavailable before any RR round completes");
        Assert.AreEqual(0, recorder.ShowCount, "No standings dialog should be surfaced yet");
    }

    [TestMethod]
    public void Standings_CompletionEventRaisedOnce_AndReShownOnDemand()
    {
        var recorder = new RecordingStandingsDialogService();
        var controller = new RaceController(
            TestSessionFactory.RoundRobin(variant: "Standard"), recorder);
        int completionEvents = 0;
        controller.RoundRobinCompleted += () => completionEvents++;

        // 6 drivers (not 4): completion then leaves >=2 buyback-eligible drivers, so the
        // controller takes the buyback path. The standings auto-show fires on RR completion
        // regardless; this loop stops the instant that happens, before the buyback gate — so
        // it never reaches the <2-eligible "wildcard" branch, which surfaces a modal
        // MessageBox that would block a headless test host.
        controller.GenerateBracket("Round Robin", TestDriverFactory.CreateRoundRobinPack(6));

        // Resolve every revealed match and advance until RR completion is announced.
        for (int i = 0; i < 40 && completionEvents == 0; i++)
        {
            var matches = controller.PeekUpcomingMatches(50).ToList();
            if (matches.Count > 0)
                foreach (var m in matches)
                    controller.SubmitWinner(m.MatchId, firstOption: true);
            else
                controller.AdvanceRound();
        }

        Assert.AreEqual(1, completionEvents,
            "RR completion must raise exactly one event for the results UI");
        Assert.AreEqual(0, recorder.ShowCount,
            "RR completion must not open the legacy text standings dialog automatically");

        // The "Standings" button re-shows the cached card on demand.
        Assert.IsTrue(controller.TryShowRoundRobinStandings(),
            "Standings must be available on demand once RR is complete");
        Assert.AreEqual(1, recorder.ShowCount,
            "On-demand Standings must route exactly one more call through the dialog service");
        Assert.IsFalse(string.IsNullOrWhiteSpace(recorder.LastShow?.Content),
            "Standings content must be non-empty");
    }
}
