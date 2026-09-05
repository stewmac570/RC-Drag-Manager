using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

[TestClass]
public class RaceResultsPresentationBuilderTests
{
    [TestMethod]
    public void Build_SeparatesLadderRoundsAndBuildsResultList()
    {
        var session = new RaceSession
        {
            EventName = "Club Finals",
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

        var view = RaceResultsPresentationBuilder.Build(session);

        Assert.IsTrue(view.HasResults);
        Assert.AreEqual("Champion: Ava  •  Runner-up: Casey", view.Summary);
        Assert.AreEqual(1, view.Phases.Count);
        Assert.AreEqual(2, view.Phases[0].Rounds.Count);
        Assert.AreEqual("Semi-Final", view.Phases[0].Rounds[0].RoundLabel);
        Assert.AreEqual("Final", view.Phases[0].Rounds[1].RoundLabel);
        Assert.AreEqual(3, view.ResultRows.Count);
    }

    [TestMethod]
    public void Build_IncludesRoundRobinStandings()
    {
        var session = new RaceSession
        {
            ResultsArchive = new RaceResultsArchive
            {
                RoundRobinStandings = new List<RoundRobinStandingSnapshot>
                {
                    new RoundRobinStandingSnapshot
                    {
                        Rank = 2, DriverName = "Blake", Wins = 2, Losses = 1,
                        Points = 9, OpponentStrength = 18
                    },
                    new RoundRobinStandingSnapshot
                    {
                        Rank = 1, DriverName = "Ava", Wins = 3, Losses = 0,
                        Points = 12, OpponentStrength = 16
                    }
                }
            }
        };

        var view = RaceResultsPresentationBuilder.Build(session);

        Assert.IsTrue(view.HasResults);
        Assert.IsTrue(view.HasRoundRobinStandings);
        Assert.AreEqual("Ava", view.Standings[0].Driver);
        // Whole numbers now: a race director reads "12", not "12.00".
        Assert.AreEqual("12", view.Standings[0].Points);
        // The tiebreak column shows what it contributes, so the row adds up.
        Assert.AreEqual("0.016", view.Standings[0].Beaten);
    }

    [TestMethod]
    public void ClassCompletionBuild_CreatesPodiumAndSmallerRemainingResults()
    {
        var session = new RaceSession
        {
            EventName = "Summer Meet",
            ClassType = "Pro Mod",
            ResultsArchive = new RaceResultsArchive
            {
                ChampionDriverId = 1,
                ChampionName = "Ava",
                RunnerUpDriverId = 2,
                RunnerUpName = "Casey",
                RoundRobinStandings = new List<RoundRobinStandingSnapshot>
                {
                    Standing(1, 1, "Ava"),
                    Standing(2, 3, "Blake"),
                    Standing(3, 2, "Casey"),
                    Standing(4, 4, "Drew")
                },
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
                }
            }
        };

        var view = ClassCompletionPresentationBuilder.Build(session);

        Assert.AreEqual("Ava", view.ChampionName);
        Assert.AreEqual("Casey", view.RunnerUpName);
        Assert.AreEqual("Blake", view.ThirdName);
        Assert.AreEqual("3rd place", view.ThirdLabel);
        Assert.AreEqual(1, view.OtherFinishers.Count);
        Assert.AreEqual("Drew", view.OtherFinishers[0].Driver);
        Assert.AreEqual(3, view.FinalsResults.Count);
    }

    [TestMethod]
    public void ClassCompletionBuild_UsesSemiFinalistsWhenNoRoundRobinRankingExists()
    {
        var session = new RaceSession
        {
            ResultsArchive = new RaceResultsArchive
            {
                ChampionName = "Ava",
                RunnerUpName = "Casey",
                Phases = new List<RacePhaseResultSnapshot>
                {
                    new RacePhaseResultSnapshot
                    {
                        Phase = RaceTypes.Finals,
                        Matches = new List<RaceResultMatchSnapshot>
                        {
                            Match(1, "SF", "Ava", "Drew", "Ava", "Drew"),
                            Match(2, "SF", "Blake", "Casey", "Casey", "Blake")
                        }
                    }
                }
            }
        };

        var view = ClassCompletionPresentationBuilder.Build(session);

        Assert.AreEqual("Semi-finalists", view.ThirdLabel);
        StringAssert.Contains(view.ThirdName, "Drew");
        StringAssert.Contains(view.ThirdName, "Blake");
    }

    private static RoundRobinStandingSnapshot Standing(int rank, int id, string name) =>
        new RoundRobinStandingSnapshot
        {
            Rank = rank,
            DriverId = id,
            DriverName = name,
            Wins = 3,
            Losses = 1,
            Points = 10 - rank,
            OpponentStrength = 20 - rank
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
