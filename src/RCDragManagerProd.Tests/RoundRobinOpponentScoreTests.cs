using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.RoundRobinMode;

namespace RCDragManagerProd.Tests;

/// <summary>
/// The opponent score is the win/loss points of the drivers you <em>beat</em>.
///
/// It used to add every driver you raced, win or lose, which rewarded losing to good
/// drivers: on a real meet sheet the driver who placed 5th with a single win carried
/// the highest opponent score in the class, purely from who had beaten him. Changed
/// 2026-09-04 at the race director's call. This decides placings and the Finals
/// seeding order, not just what is on screen.
/// </summary>
[TestClass]
public class RoundRobinOpponentScoreTests
{
    [TestMethod]
    public void OpponentScore_CountsOnlyTheDriversYouBeat()
    {
        // Ava beats Blake and Casey; Drew beats nobody.
        var result = Rank(
            Match(1, "RR1", Ava, Blake, winner: Ava),
            Match(2, "RR2", Ava, Casey, winner: Ava),
            Match(3, "RR3", Drew, Blake, winner: Blake));

        var ava = Row(result, Ava);
        var blake = Row(result, Blake);

        // Blake: 1 win 1 loss = 5. Casey: 1 loss = 1.
        Assert.AreEqual(5 + 1, ava.OpponentStrength);

        // Blake beat Drew, who scored 1 from his single loss.
        Assert.AreEqual(1, blake.OpponentStrength);
    }

    [TestMethod]
    public void OpponentScore_IsZeroForADriverWhoNeverWon()
    {
        var result = Rank(
            Match(1, "RR1", Ava, Drew, winner: Ava),
            Match(2, "RR2", Blake, Drew, winner: Blake));

        Assert.AreEqual(0, Row(result, Drew).OpponentStrength,
            "Losing to good drivers must not build an opponent score.");
    }

    [TestMethod]
    public void OpponentScore_NoLongerRewardsLosingToStrongDrivers()
    {
        // The shape that started this: Drew loses to the two best drivers and beats
        // nobody. Under the old rule his opponent score was the highest in the class.
        var result = Rank(
            Match(1, "RR1", Ava, Blake, winner: Ava),
            Match(2, "RR2", Ava, Drew, winner: Ava),
            Match(3, "RR3", Blake, Drew, winner: Blake));

        var ava = Row(result, Ava);
        var drew = Row(result, Drew);

        Assert.IsTrue(ava.OpponentStrength > drew.OpponentStrength,
            "The class winner must not sit below a driver who only lost well.");
        Assert.AreEqual(0, drew.OpponentStrength);
    }

    [TestMethod]
    public void OpponentScore_IgnoresByes()
    {
        var result = Rank(
            Match(1, "RR1", Ava, Blake, winner: Ava),
            Bye(2, "RR2", Ava));

        // Only Blake counts; the bye adds points to Ava but has no opponent to add.
        Assert.AreEqual(Row(result, Blake).Points, Row(result, Ava).OpponentStrength);
    }

    [TestMethod]
    public void OpponentScore_UsesTheOpponentsFinalPoints()
    {
        // Blake loses to Ava first, then wins twice. Ava's opponent score must use
        // Blake's end-of-event total, not what he had when they raced.
        var result = Rank(
            Match(1, "RR1", Ava, Blake, winner: Ava),
            Match(2, "RR2", Blake, Casey, winner: Blake),
            Match(3, "RR3", Blake, Drew, winner: Blake));

        var blake = Row(result, Blake);
        Assert.AreEqual(4 + 4 + 1, blake.Points);          // 2 wins + 1 loss
        Assert.AreEqual(blake.Points, Row(result, Ava).OpponentStrength);
    }

    [TestMethod]
    public void OpponentScore_BreaksATieOnlyAfterPointsWinsAndHeadToHead()
    {
        // Ava and Blake both finish 1-1; they never raced each other. Ava beat Casey
        // (who went on to win one), Blake beat Drew (who lost everything).
        var result = Rank(
            Match(1, "RR1", Ava, Casey, winner: Ava),
            Match(2, "RR1", Blake, Drew, winner: Blake),
            Match(3, "RR2", Ava, Drew, winner: Drew),
            Match(4, "RR2", Blake, Casey, winner: Casey));

        var ava = Row(result, Ava);
        var blake = Row(result, Blake);

        Assert.AreEqual(ava.Points, blake.Points, "Fixture must leave them level on points.");
        Assert.AreEqual(ava.Wins, blake.Wins, "and level on wins");
        Assert.AreEqual(Row(result, Casey).Points, ava.OpponentStrength);
        Assert.AreEqual(Row(result, Drew).Points, blake.OpponentStrength);
        Assert.IsTrue(ava.Rank < blake.Rank,
            "Ava beat the stronger driver, so she places higher.");
    }

    // ── The total ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Total_IsPointsPlusHeadToHeadPlusBeatenDriversScaledDown()
    {
        var result = Rank(
            Match(1, "RR1", Ava, Blake, winner: Ava),
            Match(2, "RR2", Ava, Casey, winner: Ava),
            Match(3, "RR3", Drew, Blake, winner: Blake));

        var ava = Row(result, Ava);

        Assert.AreEqual(
            ava.Points + ava.HeadToHeadBonus + (ava.OpponentStrength * RoundRobinRanker.BeatenDriversWeight),
            ava.TotalScore,
            1e-9);
    }

    [TestMethod]
    public void HeadToHeadBonus_IsBankedOnlyAgainstDriversLevelOnPoints()
    {
        // Ava and Blake finish level; Ava beat Blake, so only Ava banks the bonus.
        var result = Rank(
            Match(1, "RR1", Ava, Blake, winner: Ava),
            Match(2, "RR2", Ava, Casey, winner: Casey),
            Match(3, "RR3", Blake, Drew, winner: Blake),
            Match(4, "RR3", Casey, Drew, winner: Casey));

        var ava = Row(result, Ava);
        var blake = Row(result, Blake);

        Assert.AreEqual(ava.Points, blake.Points, "Fixture must leave them level on points.");
        Assert.AreEqual(RoundRobinRanker.HeadToHeadBonus, ava.HeadToHeadBonus);
        Assert.AreEqual(0, blake.HeadToHeadBonus);
        Assert.IsTrue(ava.Rank < blake.Rank);
    }

    [TestMethod]
    public void HeadToHeadBonus_IsNotBankedForBeatingSomeoneOnFewerPoints()
    {
        // Ava beats Drew, who finishes well below her. No bonus — they were never level.
        var result = Rank(
            Match(1, "RR1", Ava, Drew, winner: Ava),
            Match(2, "RR2", Ava, Casey, winner: Ava),
            Match(3, "RR3", Blake, Drew, winner: Blake));

        Assert.AreEqual(0, Row(result, Ava).HeadToHeadBonus);
    }

    [TestMethod]
    public void Total_CanNeverLiftADriverPastSomeoneOnMorePoints()
    {
        // The whole point of the weights. Ava wins everything; Blake loses to her but
        // beats the rest, so Blake carries the bigger beaten-drivers score.
        var result = Rank(
            Match(1, "RR1", Ava, Blake, winner: Ava),
            Match(2, "RR2", Ava, Casey, winner: Ava),
            Match(3, "RR2", Blake, Drew, winner: Blake),
            Match(4, "RR3", Ava, Drew, winner: Ava),
            Match(5, "RR3", Blake, Casey, winner: Blake));

        var ava = Row(result, Ava);
        var blake = Row(result, Blake);

        Assert.IsTrue(ava.Points > blake.Points);
        Assert.IsTrue(ava.TotalScore > blake.TotalScore,
            "A points lead must survive every tiebreak column.");
        Assert.AreEqual(1, ava.Rank);
    }

    [TestMethod]
    public void TheTiebreakColumnsAreTooSmallToCloseAOnePointGap()
    {
        // Arithmetic guard on the weights themselves: three races is the most anyone
        // runs, and the beaten-drivers score tops out well under a whole point.
        const int mostRaces = 3;
        var maxHeadToHead = mostRaces * RoundRobinRanker.HeadToHeadBonus;
        var maxBeaten = 40 * RoundRobinRanker.BeatenDriversWeight;

        Assert.IsTrue(maxHeadToHead + maxBeaten < 1.0,
            "Together the tiebreaks must never overturn a single win/loss point.");
        Assert.IsTrue(maxBeaten < RoundRobinRanker.HeadToHeadBonus,
            "Beaten drivers must never overturn a head-to-head result.");
    }

    // ── Fixture ───────────────────────────────────────────────────────────────

    private const int Ava = 1;
    private const int Blake = 2;
    private const int Casey = 3;
    private const int Drew = 4;

    private static readonly Dictionary<int, string> Names = new Dictionary<int, string>
    {
        [Ava] = "Ava", [Blake] = "Blake", [Casey] = "Casey", [Drew] = "Drew"
    };

    private static DriverRankResult Row(List<DriverRankResult> table, int driverId) =>
        table.Single(r => r.DriverId == driverId);

    private static List<DriverRankResult> Rank(params (RoundRobinMatch Match, Driver Winner)[] races)
    {
        var drivers = Names.Select(kv => new Driver { Id = kv.Key, Name = kv.Value }).ToList();
        var results = new MatchResult();

        foreach (var (match, winner) in races)
        {
            if (winner == null) continue;
            var loser = match.Driver2 != null && match.Driver2.Id == winner.Id
                ? match.Driver1
                : match.Driver2;
            results.SetWinner(match.MatchId, winner, loser);
        }

        return new RoundRobinRanker().Rank(races.Select(r => r.Match).ToList(), drivers, results);
    }

    private static (RoundRobinMatch, Driver) Match(int id, string round, int d1, int d2, int winner) =>
        (new RoundRobinMatch
        {
            MatchId = id,
            RoundLabel = round,
            Driver1 = Driver(d1),
            Driver2 = Driver(d2)
        }, Driver(winner));

    private static (RoundRobinMatch, Driver) Bye(int id, string round, int driverId) =>
        (new RoundRobinMatch
        {
            MatchId = id,
            RoundLabel = round,
            Driver1 = Driver(driverId),
            Driver2 = null
        }, Driver(driverId));

    private static Driver Driver(int id) => new Driver { Id = id, Name = Names[id] };
}
