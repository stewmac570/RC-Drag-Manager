using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

[TestClass]
public class RoundLabelsTests
{
    // ── CompareKey broad ordering: RR < Winners < SF < F < LB rounds < LB-F ──

    [TestMethod]
    public void CompareKey_RrRound_SortsBeforeWinnersRound()
    {
        Assert.IsTrue(RoundLabels.CompareKey("RR1") < RoundLabels.CompareKey("R1"),
            "RR round must sort before a Winners bracket round");
    }

    [TestMethod]
    public void CompareKey_WinnersRound_SortsBeforeSF()
    {
        Assert.IsTrue(RoundLabels.CompareKey("R1") < RoundLabels.CompareKey("SF"),
            "Winners round must sort before SF");
    }

    [TestMethod]
    public void CompareKey_SF_SortsBeforeF()
    {
        Assert.IsTrue(RoundLabels.CompareKey("SF") < RoundLabels.CompareKey("F"),
            "SF must sort before F");
    }

    [TestMethod]
    public void CompareKey_F_SortsBeforeLbRound()
    {
        Assert.IsTrue(RoundLabels.CompareKey("F") < RoundLabels.CompareKey("LB-R1"),
            "Finals (F) must sort before any Losers Bracket round");
    }

    [TestMethod]
    public void CompareKey_LbRound_SortsBeforeLbF()
    {
        Assert.IsTrue(RoundLabels.CompareKey("LB-R1") < RoundLabels.CompareKey("LB-F"),
            "LB round must sort before LB-F");
    }

    // ── RR labels sort in numeric order among themselves ──────────────────────

    [TestMethod]
    public void CompareKey_MultipleRrLabels_SortInNumericOrder()
    {
        int key1 = RoundLabels.CompareKey("RR1");
        int key2 = RoundLabels.CompareKey("RR2");
        int key3 = RoundLabels.CompareKey("RR3");

        Assert.IsTrue(key1 < key2, "RR1 must sort before RR2");
        Assert.IsTrue(key2 < key3, "RR2 must sort before RR3");
    }

    // ── LB labels sort in numeric order and LB-F comes last ──────────────────

    [TestMethod]
    public void CompareKey_MultipleLbLabels_SortInNumericOrder()
    {
        int key1 = RoundLabels.CompareKey("LB-R1");
        int key2 = RoundLabels.CompareKey("LB-R2");
        int keyF = RoundLabels.CompareKey("LB-F");

        Assert.IsTrue(key1 < key2, "LB-R1 must sort before LB-R2");
        Assert.IsTrue(key2 < keyF, "LB-R2 must sort before LB-F");
    }

    // ── Normalize: Round Robin labels ─────────────────────────────────────────

    [TestMethod]
    public void Normalize_RrLabel_IsCaseInsensitive_AndPreservesNumber()
    {
        Assert.AreEqual("RR1", RoundLabels.Normalize("rr1"));
        Assert.AreEqual("RR3", RoundLabels.Normalize("RR3"));
        Assert.AreEqual("RR5", RoundLabels.Normalize("rR5"));
    }

    // ── Normalize: Losers Bracket round labels ────────────────────────────────

    [TestMethod]
    public void Normalize_LbRoundLabel_IsCaseInsensitive_AndPreservesNumber()
    {
        Assert.AreEqual("LB-R1", RoundLabels.Normalize("lb-r1"));
        Assert.AreEqual("LB-R2", RoundLabels.Normalize("LB-R2"));
    }

    [TestMethod]
    public void Normalize_LegacyLosersBracketRound_MapsToLbRCanonical()
    {
        Assert.AreEqual("LB-R1", RoundLabels.Normalize("LOSERS BRACKET R1"));
        Assert.AreEqual("LB-R3", RoundLabels.Normalize("losers bracket r3"));
    }

    // ── Normalize: Losers Bracket Final ──────────────────────────────────────

    [TestMethod]
    public void Normalize_AllLbFVariants_ReturnLbF()
    {
        Assert.AreEqual("LB-F", RoundLabels.Normalize("LB-F"));
        Assert.AreEqual("LB-F", RoundLabels.Normalize("LBF"));
        Assert.AreEqual("LB-F", RoundLabels.Normalize("LOSERS BRACKET FINAL"));
        Assert.AreEqual("LB-F", RoundLabels.Normalize("losers bracket final"));
    }

    // ── Normalize: Semi-Final ─────────────────────────────────────────────────

    [TestMethod]
    public void Normalize_SemiFinalVariants_ReturnSF()
    {
        Assert.AreEqual("SF", RoundLabels.Normalize("SF"));
        Assert.AreEqual("SF", RoundLabels.Normalize("sf"));
        Assert.AreEqual("SF", RoundLabels.Normalize("Semi Final"));
        Assert.AreEqual("SF", RoundLabels.Normalize("SEMIFINAL"));
    }

    // ── Normalize: Final ──────────────────────────────────────────────────────

    [TestMethod]
    public void Normalize_FinalVariants_ReturnF()
    {
        Assert.AreEqual("F", RoundLabels.Normalize("F"));
        Assert.AreEqual("F", RoundLabels.Normalize("FINAL"));
        Assert.AreEqual("F", RoundLabels.Normalize("final"));
    }

    // ── Normalize: Winners bracket rounds ────────────────────────────────────

    [TestMethod]
    public void Normalize_WinnersRound_IsCaseInsensitive_AndPreservesNumber()
    {
        Assert.AreEqual("R1", RoundLabels.Normalize("R1"));
        Assert.AreEqual("R2", RoundLabels.Normalize("r2"));
    }

    // ── Normalize: empty / null / whitespace ──────────────────────────────────

    [TestMethod]
    public void Normalize_NullOrEmpty_ReturnsEmptyString()
    {
        Assert.AreEqual(string.Empty, RoundLabels.Normalize(null!));
        Assert.AreEqual(string.Empty, RoundLabels.Normalize(""));
        Assert.AreEqual(string.Empty, RoundLabels.Normalize("   "));
    }

    // ── Compare ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Compare_RrBeforeWinners_ReturnsNegative()
    {
        Assert.IsTrue(RoundLabels.Compare("RR1", "R1") < 0);
    }

    [TestMethod]
    public void Compare_SameLabelDifferentCase_ReturnsZero()
    {
        Assert.AreEqual(0, RoundLabels.Compare("SF", "SF"));
        Assert.AreEqual(0, RoundLabels.Compare("RR2", "rr2"),
            "Compare must be case-insensitive after normalization");
    }

    [TestMethod]
    public void Compare_LbAfterFinals_ReturnsPositive()
    {
        Assert.IsTrue(RoundLabels.Compare("LB-R1", "F") > 0);
    }
}
