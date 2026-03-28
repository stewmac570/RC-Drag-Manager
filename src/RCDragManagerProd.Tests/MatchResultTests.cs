using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

[TestClass]
public class MatchResultTests
{
    private static Driver MakeDriver(int id, string name = null!) =>
        new Driver { Id = id, Name = name ?? $"Driver{id}" };

    // ── SetWinner / GetWinner / GetLoser ──────────────────────────────────────

    [TestMethod]
    public void SetWinner_GetWinner_ReturnsExpectedDriver()
    {
        var mr = new MatchResult();
        var winner = MakeDriver(1);
        var loser  = MakeDriver(2);

        mr.SetWinner(10, winner, loser);

        Assert.AreEqual(winner, mr.GetWinner(10));
    }

    [TestMethod]
    public void SetWinner_GetLoser_ReturnsExpectedDriver()
    {
        var mr = new MatchResult();
        var winner = MakeDriver(1);
        var loser  = MakeDriver(2);

        mr.SetWinner(10, winner, loser);

        Assert.AreEqual(loser, mr.GetLoser(10));
    }

    [TestMethod]
    public void GetWinner_UnknownMatchId_ReturnsNull()
    {
        var mr = new MatchResult();

        Assert.IsNull(mr.GetWinner(99));
    }

    [TestMethod]
    public void GetLoser_UnknownMatchId_ReturnsNull()
    {
        var mr = new MatchResult();

        Assert.IsNull(mr.GetLoser(99));
    }

    // ── HasResult / IsMatchResolved ───────────────────────────────────────────

    [TestMethod]
    public void HasResult_ReturnsFalseBeforeSet_TrueAfterSet()
    {
        var mr = new MatchResult();

        Assert.IsFalse(mr.HasResult(1));

        mr.SetWinner(1, MakeDriver(1), MakeDriver(2));

        Assert.IsTrue(mr.HasResult(1));
    }

    [TestMethod]
    public void IsMatchResolved_ReturnsFalseBeforeSet_TrueAfterSet()
    {
        var mr = new MatchResult();

        Assert.IsFalse(mr.IsMatchResolved(5));

        mr.SetWinner(5, MakeDriver(3), MakeDriver(4));

        Assert.IsTrue(mr.IsMatchResolved(5));
    }

    // ── ClearFromMatch ────────────────────────────────────────────────────────

    [TestMethod]
    public void ClearFromMatch_RemovesResultsAtAndAboveMatchId()
    {
        var mr = new MatchResult();
        mr.SetWinner(1, MakeDriver(1), MakeDriver(2));
        mr.SetWinner(2, MakeDriver(3), MakeDriver(4));
        mr.SetWinner(3, MakeDriver(5), MakeDriver(6));

        mr.ClearFromMatch(2);

        Assert.IsTrue(mr.HasResult(1),  "match 1 should survive ClearFromMatch(2)");
        Assert.IsFalse(mr.HasResult(2), "match 2 should be cleared");
        Assert.IsFalse(mr.HasResult(3), "match 3 should be cleared");
    }

    [TestMethod]
    public void ClearFromMatch_PreservesResultsBelowMatchId()
    {
        var mr = new MatchResult();
        mr.SetWinner(1,  MakeDriver(1), MakeDriver(2));
        mr.SetWinner(5,  MakeDriver(3), MakeDriver(4));
        mr.SetWinner(10, MakeDriver(5), MakeDriver(6));

        mr.ClearFromMatch(5);

        Assert.IsTrue(mr.HasResult(1),   "match 1 is below the cut-point and must survive");
        Assert.IsFalse(mr.HasResult(5),  "match 5 is at the cut-point and must be cleared");
        Assert.IsFalse(mr.HasResult(10), "match 10 is above the cut-point and must be cleared");
    }

    // ── IsTournamentComplete ──────────────────────────────────────────────────

    [TestMethod]
    public void IsTournamentComplete_ReturnsFalseWhenFinalMatchHasNoResult()
    {
        var mr = new MatchResult();
        var bracket = new List<ProLadder.LadderMatch>
        {
            new ProLadder.LadderMatch { MatchId = 1, RoundLabel = "R1" },
            new ProLadder.LadderMatch { MatchId = 2, RoundLabel = "F"  }
        };

        Assert.IsFalse(mr.IsTournamentComplete(bracket));
    }

    [TestMethod]
    public void IsTournamentComplete_ReturnsTrueWhenFinalMatchHasResult()
    {
        var mr = new MatchResult();
        var bracket = new List<ProLadder.LadderMatch>
        {
            new ProLadder.LadderMatch { MatchId = 1, RoundLabel = "R1" },
            new ProLadder.LadderMatch { MatchId = 2, RoundLabel = "F"  }
        };

        mr.SetWinner(2, MakeDriver(1), MakeDriver(2));

        Assert.IsTrue(mr.IsTournamentComplete(bracket));
    }

    [TestMethod]
    public void IsTournamentComplete_ReturnsFalseWhenBracketContainsNoFinalMatch()
    {
        var mr = new MatchResult();
        var bracket = new List<ProLadder.LadderMatch>
        {
            new ProLadder.LadderMatch { MatchId = 1, RoundLabel = "R1" }
        };

        mr.SetWinner(1, MakeDriver(1), MakeDriver(2));

        Assert.IsFalse(mr.IsTournamentComplete(bracket),
            "No 'F' match in the bracket means tournament is never complete");
    }

    // ── GetAllPairings ────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAllPairings_NormalizesSmallerIdFirst()
    {
        var mr = new MatchResult();
        // Winner Id=5, Loser Id=2 → stored pair should be (2, 5)
        mr.SetWinner(1, MakeDriver(5), MakeDriver(2));

        var pairs = mr.GetAllPairings();

        Assert.AreEqual(1, pairs.Count);
        Assert.IsTrue(pairs.Contains((2, 5)));
    }

    [TestMethod]
    public void GetAllPairings_SkipsEntriesWithNullDriver()
    {
        var mr = new MatchResult();
        mr.SetWinner(1, null!, MakeDriver(2));
        mr.SetWinner(2, MakeDriver(3), null!);

        var pairs = mr.GetAllPairings();

        Assert.AreEqual(0, pairs.Count,
            "Pairings with a null winner or loser must be skipped");
    }

    [TestMethod]
    public void GetAllPairings_ReturnsOneNormalizedPairPerResult()
    {
        var mr = new MatchResult();
        mr.SetWinner(1, MakeDriver(1), MakeDriver(2));
        mr.SetWinner(2, MakeDriver(3), MakeDriver(4));

        var pairs = mr.GetAllPairings();

        Assert.AreEqual(2, pairs.Count);
        Assert.IsTrue(pairs.Contains((1, 2)));
        Assert.IsTrue(pairs.Contains((3, 4)));
    }

    // ── GetAllResults ─────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAllResults_ReturnsCorrectWinnerLoserTuples()
    {
        var mr = new MatchResult();
        mr.SetWinner(1, MakeDriver(1), MakeDriver(2));
        mr.SetWinner(2, MakeDriver(3), MakeDriver(4));

        var results = mr.GetAllResults();

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.Any(r => r.WinnerId == 1 && r.LoserId == 2));
        Assert.IsTrue(results.Any(r => r.WinnerId == 3 && r.LoserId == 4));
    }

    [TestMethod]
    public void GetAllResults_SkipsEntriesWithNullDriver()
    {
        // Driver.Id setter ignores zero (constructor always assigns a non-zero runtime Id),
        // so the Id > 0 guard is really a null-safety filter.
        var mr = new MatchResult();
        mr.SetWinner(1, null!, MakeDriver(2)); // null winner — must be excluded
        mr.SetWinner(2, MakeDriver(3), MakeDriver(4));

        var results = mr.GetAllResults();

        Assert.AreEqual(1, results.Count,
            "Entry with a null Winner must be excluded from GetAllResults");
        Assert.AreEqual(3, results[0].WinnerId);
        Assert.AreEqual(4, results[0].LoserId);
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Clear_RemovesAllResults()
    {
        var mr = new MatchResult();
        mr.SetWinner(1, MakeDriver(1), MakeDriver(2));
        mr.SetWinner(2, MakeDriver(3), MakeDriver(4));

        mr.Clear();

        Assert.IsFalse(mr.HasResult(1));
        Assert.IsFalse(mr.HasResult(2));
        Assert.AreEqual(0, mr.GetAllResults().Count);
    }
}
