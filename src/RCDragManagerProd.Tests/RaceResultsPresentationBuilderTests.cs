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
                    new RoundRobinStandingSnapshot { Rank = 2, DriverName = "Blake", Wins = 2, Losses = 1 },
                    new RoundRobinStandingSnapshot { Rank = 1, DriverName = "Ava", Wins = 3, Losses = 0 }
                }
            }
        };

        var view = RaceResultsPresentationBuilder.Build(session);

        Assert.IsTrue(view.HasResults);
        Assert.IsTrue(view.HasRoundRobinStandings);
        Assert.AreEqual("Ava", view.Standings[0].Driver);
    }

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
