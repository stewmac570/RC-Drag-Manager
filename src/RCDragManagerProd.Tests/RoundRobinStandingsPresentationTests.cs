using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

/// <summary>
/// The standings table is now the only place Round Robin scores are shown, so it has
/// to be readable on its own.
///
/// The scorecard popup it replaced scored rounds on a decaying scale (R1 4/1/2, R2
/// 3.5/0.75/1.5, R3 3/0.5/1) while <c>RoundRobinRanker</c> — the only thing that
/// decides rank and the Finals seeding — scores every round 4/1/2. The two disagreed
/// on points and on finishing order. These tests pin the surviving table to the
/// ranker's arithmetic.
/// </summary>
[TestClass]
public class RoundRobinStandingsPresentationTests
{
    [TestMethod]
    public void Byes_AreCountedFromTheSavedRoundRobinMatches()
    {
        var view = RaceResultsPresentationBuilder.Build(SessionWithByes());

        var aaron = view.Standings.Single(s => s.Driver == "Aaron Deluca");
        Assert.AreEqual(1, aaron.Byes, "Aaron sat out one round.");

        var ava = view.Standings.Single(s => s.Driver == "Ava");
        Assert.AreEqual(0, ava.Byes, "Ava raced every round.");
    }

    [TestMethod]
    public void Byes_ExplainAPointsTotalThatOtherwiseLooksWrong()
    {
        // 1 win + 1 loss + 1 bye = 4 + 1 + 2 = 7. Without the bye column a reader
        // sees "1 win, 1 loss, 7 points" and assumes the table is broken.
        var view = RaceResultsPresentationBuilder.Build(SessionWithByes());
        var aaron = view.Standings.Single(s => s.Driver == "Aaron Deluca");

        Assert.AreEqual(1, aaron.Wins);
        Assert.AreEqual(1, aaron.Losses);
        Assert.AreEqual(1, aaron.Byes);
        Assert.AreEqual("7.00", aaron.Points);
    }

    [TestMethod]
    public void ByeCounting_IgnoresNonRoundRobinPhases()
    {
        var session = SessionWithByes();
        session.ResultsArchive.Phases.Add(new RacePhaseResultSnapshot
        {
            Phase = RaceTypes.Finals,
            Matches = new List<RaceResultMatchSnapshot>
            {
                // A Finals bye must not inflate a Round Robin bye tally.
                new RaceResultMatchSnapshot
                {
                    MatchId = 90, RoundLabel = "SF",
                    Driver1Id = 1, Driver1Name = "Ava",
                    Driver2Id = null, Driver2Name = "BYE",
                    WinnerDriverId = 1, WinnerName = "Ava"
                }
            }
        });

        var view = RaceResultsPresentationBuilder.Build(session);

        Assert.AreEqual(0, view.Standings.Single(s => s.Driver == "Ava").Byes);
    }

    [TestMethod]
    public void ByeCounting_RecognisesAByeWrittenAsAName()
    {
        // Older saves recorded the empty side as the name "BYE" rather than a null id.
        var session = SessionWithByes();
        var byeMatch = session.ResultsArchive.Phases
            .Single(p => p.Phase == RaceTypes.RoundRobin)
            .Matches.Single(m => m.MatchId == 3);
        byeMatch.Driver2Id = 0;
        byeMatch.Driver2Name = "BYE";

        var view = RaceResultsPresentationBuilder.Build(session);

        Assert.AreEqual(1, view.Standings.Single(s => s.Driver == "Aaron Deluca").Byes);
    }

    [TestMethod]
    public void ScoringNote_StatesWhatEachResultIsWorth()
    {
        var view = RaceResultsPresentationBuilder.Build(SessionWithByes());

        // Whole numbers, not "4.00" — nobody reads that as four.
        Assert.AreEqual("Every race scores: win 4, bye 2, loss 1. Every round is worth the same.",
            view.ScoringNote);
    }

    [TestMethod]
    public void PointsWorking_WritesTheSumOut()
    {
        var view = RaceResultsPresentationBuilder.Build(SessionWithByes());

        // 1 win + 1 loss + 1 bye = 7.00, and the row says so rather than asking for trust.
        Assert.AreEqual("1 win (4) + 1 bye (2) + 1 loss (1)",
            view.Standings.Single(s => s.Driver == "Aaron Deluca").PointsWorking);

        Assert.AreEqual("2 wins (8) + 1 loss (1)",
            view.Standings.Single(s => s.Driver == "Ava").PointsWorking);
    }

    [TestMethod]
    public void PointsWorking_OmitsWhatDidNotHappen()
    {
        var session = SessionWithByes();
        session.ResultsArchive.RoundRobinStandings = new List<RoundRobinStandingSnapshot>
        {
            new RoundRobinStandingSnapshot
            {
                Rank = 1, DriverId = 9, DriverName = "Unbeaten",
                Wins = 3, Losses = 0, Points = 12.00
            }
        };

        var view = RaceResultsPresentationBuilder.Build(session);

        Assert.AreEqual("3 wins (12)", view.Standings.Single().PointsWorking);
    }

    [TestMethod]
    public void PointsWorking_HandlesADriverWithNoRacesYet()
    {
        var session = SessionWithByes();
        session.ResultsArchive.RoundRobinStandings = new List<RoundRobinStandingSnapshot>
        {
            new RoundRobinStandingSnapshot { Rank = 1, DriverId = 9, DriverName = "Late entry" }
        };

        var view = RaceResultsPresentationBuilder.Build(session);

        Assert.AreEqual("No races yet", view.Standings.Single().PointsWorking);
    }

    [TestMethod]
    public void TieNotes_AreEmptyWhenNobodyFinishedLevel()
    {
        var view = RaceResultsPresentationBuilder.Build(SessionWithByes());

        Assert.IsFalse(view.HasTieNotes,
            "The tiebreak rules stay out of sight until one actually decides a place.");
    }

    [TestMethod]
    public void TieNotes_ExplainAHeadToHeadDecision()
    {
        var view = RaceResultsPresentationBuilder.Build(TiedOnPointsAndWins());

        Assert.IsTrue(view.HasTieNotes);
        var note = view.TieNotes.Single();
        StringAssert.Contains(note, "both finished on 9");
        StringAssert.Contains(note, "Ava");
        StringAssert.Contains(note, "winning their");
        StringAssert.Contains(note, "Tyler Nguyen");
    }

    [TestMethod]
    public void TieNotes_ExplainAWinCountDecision()
    {
        var session = TiedOnPointsAndWins();
        // Same points, different win counts: 2W/1L against 1W/1BYE/1L both make 9.
        session.ResultsArchive.RoundRobinStandings[1].Wins = 1;
        session.ResultsArchive.RoundRobinStandings[1].Losses = 1;

        var view = RaceResultsPresentationBuilder.Build(session);

        StringAssert.Contains(view.TieNotes.Single(), "more wins");
    }

    [TestMethod]
    public void TieNotes_FallBackToOpponentStrength()
    {
        var session = TiedOnPointsAndWins();
        // Strip the head-to-head race so only opponent strength can separate them.
        session.ResultsArchive.Phases.Single().Matches.Clear();

        var view = RaceResultsPresentationBuilder.Build(session);

        var note = view.TieNotes.Single();
        StringAssert.Contains(note, "stronger field");
        StringAssert.Contains(note, "20");
    }

    [TestMethod]
    public void ScoringNote_MatchesWhatTheRankerActuallyDoes()
    {
        // Guards the note against drifting from RoundRobinRanker.PointsForRound, which
        // is deliberately constant across rounds. If someone reintroduces per-round
        // weighting, this fails and the note gets fixed with it.
        var r1 = RCDragManagerProd.RoundRobinMode.RoundRobinRanker.PointsForRound("R1");
        var r3 = RCDragManagerProd.RoundRobinMode.RoundRobinRanker.PointsForRound("R3");

        Assert.AreEqual(4.0, r1.Win);
        Assert.AreEqual(1.0, r1.Loss);
        Assert.AreEqual(2.0, r1.Bye);
        Assert.AreEqual(r1, r3, "Every round is worth the same — the note says so.");
    }

    /// <summary>Ava and Tyler both on 9.00 with two wins each; Ava won their RR1 race.</summary>
    private static RaceSession TiedOnPointsAndWins() => new RaceSession
    {
        EventName = "Club Round 4",
        ClassType = "Pro Mod",
        ResultsArchive = new RaceResultsArchive
        {
            Phases = new List<RacePhaseResultSnapshot>
            {
                new RacePhaseResultSnapshot
                {
                    Phase = RaceTypes.RoundRobin,
                    Matches = new List<RaceResultMatchSnapshot>
                    {
                        new RaceResultMatchSnapshot
                        {
                            MatchId = 1, RoundLabel = "RR1",
                            Driver1Id = 1, Driver1Name = "Ava",
                            Driver2Id = 3, Driver2Name = "Tyler Nguyen",
                            WinnerDriverId = 1, WinnerName = "Ava",
                            LoserDriverId = 3, LoserName = "Tyler Nguyen"
                        }
                    }
                }
            },
            RoundRobinStandings = new List<RoundRobinStandingSnapshot>
            {
                new RoundRobinStandingSnapshot
                {
                    Rank = 1, DriverId = 1, DriverName = "Ava",
                    Wins = 2, Losses = 1, Points = 9.00, OpponentStrength = 20.00
                },
                new RoundRobinStandingSnapshot
                {
                    Rank = 2, DriverId = 3, DriverName = "Tyler Nguyen",
                    Wins = 2, Losses = 1, Points = 9.00, OpponentStrength = 19.00
                }
            }
        }
    };

    // ── Fixture ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Three rounds. Aaron takes a bye in RR3, Ava races throughout. Points match the
    /// ranker: Ava 2 wins + 1 loss = 9.00, Aaron 1 win + 1 loss + 1 bye = 7.00.
    /// </summary>
    private static RaceSession SessionWithByes() => new RaceSession
    {
        EventName = "Club Round 4",
        ClassType = "Pro Mod",
        ResultsArchive = new RaceResultsArchive
        {
            Phases = new List<RacePhaseResultSnapshot>
            {
                new RacePhaseResultSnapshot
                {
                    Phase = RaceTypes.RoundRobin,
                    Matches = new List<RaceResultMatchSnapshot>
                    {
                        new RaceResultMatchSnapshot
                        {
                            MatchId = 1, RoundLabel = "RR1",
                            Driver1Id = 1, Driver1Name = "Ava",
                            Driver2Id = 2, Driver2Name = "Aaron Deluca",
                            WinnerDriverId = 1, WinnerName = "Ava",
                            LoserDriverId = 2, LoserName = "Aaron Deluca"
                        },
                        new RaceResultMatchSnapshot
                        {
                            MatchId = 2, RoundLabel = "RR2",
                            Driver1Id = 2, Driver1Name = "Aaron Deluca",
                            Driver2Id = 1, Driver2Name = "Ava",
                            WinnerDriverId = 2, WinnerName = "Aaron Deluca",
                            LoserDriverId = 1, LoserName = "Ava"
                        },
                        new RaceResultMatchSnapshot
                        {
                            MatchId = 3, RoundLabel = "RR3",
                            Driver1Id = 2, Driver1Name = "Aaron Deluca",
                            Driver2Id = null, Driver2Name = null,
                            WinnerDriverId = 2, WinnerName = "Aaron Deluca"
                        }
                    }
                }
            },
            RoundRobinStandings = new List<RoundRobinStandingSnapshot>
            {
                new RoundRobinStandingSnapshot
                {
                    Rank = 1, DriverId = 1, DriverName = "Ava",
                    Wins = 2, Losses = 1, Points = 9.00, OpponentStrength = 20.00
                },
                new RoundRobinStandingSnapshot
                {
                    Rank = 2, DriverId = 2, DriverName = "Aaron Deluca",
                    Wins = 1, Losses = 1, Points = 7.00, OpponentStrength = 18.00
                }
            }
        }
    };
}
