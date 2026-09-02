using System.Collections.Generic;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

/// <summary>
/// The winner board used to exist only in the moment the last race was called: the
/// completion window popped once and could never be reopened. It now lives on the
/// results window's Winner tab, gated by <c>HasWinner</c>, so a finished class can
/// show its champion again for as long as the results are saved.
/// </summary>
[TestClass]
public class RaceResultsWinnerAccessTests
{
    [TestMethod]
    public void HasWinner_IsFalse_WhileTheClassIsStillRacing()
    {
        var view = RaceResultsPresentationBuilder.Build(InProgressSession());

        Assert.IsTrue(view.HasResults, "Part-run classes still have results to show.");
        Assert.IsFalse(view.HasWinner, "No champion recorded yet, so the Winner tab stays shut.");
    }

    [TestMethod]
    public void HasWinner_IsTrue_OnceAChampionIsRecorded()
    {
        var view = RaceResultsPresentationBuilder.Build(CompletedSession());

        Assert.IsTrue(view.HasWinner);
    }

    [TestMethod]
    public void HasWinner_IsFalse_WhenThereIsNoArchiveAtAll()
    {
        var view = RaceResultsPresentationBuilder.Build(new RaceSession { EventName = "Empty" });

        Assert.IsFalse(view.HasWinner);
        Assert.IsFalse(view.HasResults);
    }

    [TestMethod]
    public void CompletedClass_StillBuildsItsWinnerBoardFromTheSavedArchive()
    {
        // This is what the Winner tab binds to. It must survive on saved data alone —
        // the controller that ran the class is long gone by the time the RD reopens it.
        var completion = ClassCompletionPresentationBuilder.Build(CompletedSession());

        Assert.AreEqual("Ava", completion.ChampionName);
        Assert.AreEqual("Casey", completion.RunnerUpName);
        Assert.IsTrue(completion.HasThird, "Semi-final losers give the third-place slot.");
    }

    [TestMethod]
    public void Standings_RemainAvailableAfterCompletion()
    {
        // Backs keeping the Standings button enabled on a finished class: the numbers
        // come from the archive, not from the live in-memory scorecard.
        var session = CompletedSession();
        session.ResultsArchive.RoundRobinStandings = new List<RoundRobinStandingSnapshot>
        {
            new RoundRobinStandingSnapshot { Rank = 1, DriverId = 1, DriverName = "Ava", Wins = 3, Losses = 0 },
            new RoundRobinStandingSnapshot { Rank = 2, DriverId = 2, DriverName = "Casey", Wins = 2, Losses = 1 }
        };

        var view = RaceResultsPresentationBuilder.Build(session);

        Assert.IsTrue(view.HasRoundRobinStandings);
        Assert.AreEqual(2, view.Standings.Count);
    }

    [TestMethod]
    public void WinnerBoard_BuildsWithoutThrowing_ForAClassThatHasNoArchive()
    {
        // The results window builds the Winner tab's context on every open, including
        // for a class that has not raced. It must come back empty, not throw.
        var completion = ClassCompletionPresentationBuilder.Build(
            new RaceSession { EventName = "Fresh", ClassType = "Pro Mod" });

        Assert.IsNotNull(completion);
        Assert.AreEqual("Winner not recorded", completion.ChampionName);
        Assert.AreEqual(0, completion.OtherFinishers.Count);
    }

    [TestMethod]
    public void WinnerBoard_BuildsWithoutThrowing_ForANullSession()
    {
        var completion = ClassCompletionPresentationBuilder.Build(null);

        Assert.IsNotNull(completion);
        Assert.AreEqual("Race complete", completion.EventName);
    }

    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static RaceSession CompletedSession() => new RaceSession
    {
        EventName = "Club Finals",
        ClassType = "Pro Mod",
        ResultsArchive = new RaceResultsArchive
        {
            Phases = new List<RacePhaseResultSnapshot>
            {
                new RacePhaseResultSnapshot
                {
                    Phase = RaceTypes.Finals,
                    Matches = new List<RaceResultMatchSnapshot>
                    {
                        Match(1, "SF", "Ava", "Drew", "Ava", "Drew"),
                        Match(2, "SF", "Blake", "Casey", "Casey", "Blake"),
                        Match(3, "F", "Ava", "Casey", "Ava", "Casey")
                    }
                }
            },
            ChampionName = "Ava",
            RunnerUpName = "Casey"
        }
    };

    private static RaceSession InProgressSession() => new RaceSession
    {
        EventName = "Club Finals",
        ClassType = "Pro Mod",
        ResultsArchive = new RaceResultsArchive
        {
            Phases = new List<RacePhaseResultSnapshot>
            {
                new RacePhaseResultSnapshot
                {
                    Phase = RaceTypes.Finals,
                    Matches = new List<RaceResultMatchSnapshot>
                    {
                        Match(1, "SF", "Ava", "Drew", "Ava", "Drew")
                    }
                }
            }
        }
    };

    private static RaceResultMatchSnapshot Match(
        int id, string round, string d1, string d2, string winner, string loser) =>
        new RaceResultMatchSnapshot
        {
            MatchId = id,
            RoundLabel = round,
            Driver1Name = d1,
            Driver2Name = d2,
            WinnerName = winner,
            LoserName = loser
        };
}
