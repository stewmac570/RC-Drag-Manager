using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Regression tests for issue #96: PairingHistory (HashSet&lt;(int,int)&gt;) cannot be
/// serialized by System.Text.Json directly because ValueTuple-keyed collections are
/// not supported. The fix introduces PairingHistoryRaw (List&lt;int[]&gt;) as the
/// serialization backing store, with PairingHistory as a [JsonIgnore] computed property.
///
/// These tests verify the domain object's serialization contract using
/// System.Text.Json directly — matching the options used by RaceSessionRepository —
/// without touching the database layer.
/// </summary>
[TestClass]
public class PairingHistorySerializationTests
{
    // Options mirror RaceSessionRepository.SaveSession / LoadSession exactly.
    private static readonly JsonSerializerOptions WriteOpts = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions ReadOpts = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serialize then deserialize a RaceSession, returning the rehydrated instance.
    /// </summary>
    private static RaceSession RoundTrip(RaceSession session)
    {
        var json = JsonSerializer.Serialize(session, WriteOpts);
        return JsonSerializer.Deserialize<RaceSession>(json, ReadOpts)!;
    }

    // ── Core regression guard (issue #96) ────────────────────────────────────

    [TestMethod]
    public void NonEmptyPairingHistory_RoundTrips_WithAllPairsIntact()
    {
        // This is the core regression: before the fix, PairingHistory serialized
        // as an empty object (System.Text.Json cannot handle HashSet<(int,int)>),
        // so all pairing history was silently lost on save/load.
        var session = new RaceSession();
        session.PairingHistory = new HashSet<(int, int)> { (1, 2), (3, 4), (5, 6) };

        var loaded = RoundTrip(session);
        var history = loaded.PairingHistory;

        Assert.IsTrue(history.Contains((1, 2)), "Pair (1, 2) must survive round-trip");
        Assert.IsTrue(history.Contains((3, 4)), "Pair (3, 4) must survive round-trip");
        Assert.IsTrue(history.Contains((5, 6)), "Pair (5, 6) must survive round-trip");
    }

    // ── Pair normalization ────────────────────────────────────────────────────

    [TestMethod]
    public void PairNormalization_SmallerIdFirst_RoundTrips_Correctly()
    {
        // Convention: pairs are stored normalized (smaller Id first) by callers
        // such as LosersBracketBuilder.Norm(). The backing store preserves
        // whatever ordering is set; this test confirms (3, 5) stored as (3, 5)
        // comes back as (3, 5) — i.e., the normalized form is not corrupted.
        var session = new RaceSession();
        session.PairingHistory = new HashSet<(int, int)> { (3, 5) };

        var loaded = RoundTrip(session);

        Assert.IsTrue(loaded.PairingHistory.Contains((3, 5)),
            "Normalized pair (3, 5) must round-trip with the same ordering");
        Assert.IsFalse(loaded.PairingHistory.Contains((5, 3)),
            "Reversed pair (5, 3) must not appear — the stored ordering is preserved");
    }

    // ── Empty history ─────────────────────────────────────────────────────────

    [TestMethod]
    public void EmptyPairingHistory_RoundTrips_AsEmpty_NotNull()
    {
        var session = new RaceSession();
        // PairingHistoryRaw is initialized to new List<int[]>() in the ctor;
        // PairingHistory setter is not called — raw stays empty.

        var loaded = RoundTrip(session);

        Assert.IsNotNull(loaded.PairingHistory,
            "PairingHistory must not be null after deserializing a session with no pairs");
        Assert.AreEqual(0, loaded.PairingHistory.Count,
            "PairingHistory must be empty when no pairs were stored");
    }

    [TestMethod]
    public void ExplicitlyEmptyPairingHistory_RoundTrips_AsEmpty()
    {
        var session = new RaceSession();
        session.PairingHistory = new HashSet<(int, int)>(); // explicit empty assign

        var loaded = RoundTrip(session);

        Assert.AreEqual(0, loaded.PairingHistory.Count,
            "Explicitly assigned empty PairingHistory must round-trip as empty");
    }

    // ── Multiple pairs ────────────────────────────────────────────────────────

    [TestMethod]
    public void MultiplePairs_AllSurviveRoundTrip()
    {
        var pairs = new HashSet<(int, int)>
        {
            (1, 2), (1, 3), (1, 4),
            (2, 3), (2, 4),
            (3, 4)
        };

        var session = new RaceSession();
        session.PairingHistory = pairs;

        var loaded = RoundTrip(session);
        var history = loaded.PairingHistory;

        Assert.AreEqual(pairs.Count, history.Count,
            "Loaded PairingHistory must have the same number of pairs as the original");

        foreach (var pair in pairs)
        {
            Assert.IsTrue(history.Contains(pair),
                $"Pair ({pair.Item1}, {pair.Item2}) must be present after round-trip");
        }
    }

    [TestMethod]
    public void RoundTripPairCount_MatchesOriginal()
    {
        var session = new RaceSession();
        session.PairingHistory = new HashSet<(int, int)> { (10, 20), (30, 40), (50, 60) };

        var loaded = RoundTrip(session);

        Assert.AreEqual(3, loaded.PairingHistory.Count,
            "Loaded PairingHistory must contain exactly as many pairs as were saved");
    }

    // ── Contains() works after deserialization ────────────────────────────────

    [TestMethod]
    public void AfterDeserialization_Contains_ReturnsTrueForStoredPairs()
    {
        var d1 = 7;
        var d2 = 13;

        var session = new RaceSession();
        session.PairingHistory = new HashSet<(int, int)> { (d1, d2) };

        var loaded = RoundTrip(session);

        Assert.IsTrue(loaded.PairingHistory.Contains((d1, d2)),
            "Contains() must return true for a pair that was in the original history");
    }

    [TestMethod]
    public void AfterDeserialization_Contains_ReturnsFalseForAbsentPairs()
    {
        var session = new RaceSession();
        session.PairingHistory = new HashSet<(int, int)> { (1, 2) };

        var loaded = RoundTrip(session);

        Assert.IsFalse(loaded.PairingHistory.Contains((3, 4)),
            "Contains() must return false for a pair that was never stored");
    }

    // ── PairingHistoryRaw backing store ───────────────────────────────────────

    [TestMethod]
    public void PairingHistoryRaw_IsNonNullAndPopulated_AfterDeserialization()
    {
        // PairingHistoryRaw is the property that actually serializes.
        // After a round-trip it must be non-null and contain the same data
        // that PairingHistory exposes — confirming the backing store was
        // correctly written to JSON and read back.
        var session = new RaceSession();
        session.PairingHistory = new HashSet<(int, int)> { (2, 8), (4, 6) };

        var loaded = RoundTrip(session);

        Assert.IsNotNull(loaded.PairingHistoryRaw,
            "PairingHistoryRaw must not be null after deserialization");
        Assert.AreEqual(2, loaded.PairingHistoryRaw.Count,
            "PairingHistoryRaw must contain one entry per stored pair");
    }

    [TestMethod]
    public void PairingHistoryRaw_ContainsCorrectArrayValues_AfterDeserialization()
    {
        var session = new RaceSession();
        session.PairingHistory = new HashSet<(int, int)> { (3, 9) };

        var loaded = RoundTrip(session);

        Assert.AreEqual(1, loaded.PairingHistoryRaw.Count,
            "PairingHistoryRaw must have exactly one entry");

        var entry = loaded.PairingHistoryRaw[0];
        Assert.AreEqual(2, entry.Length, "Each PairingHistoryRaw entry must be a 2-element array");

        // The pair (3, 9) must appear in the raw array (order is the stored order)
        bool correctValues = (entry[0] == 3 && entry[1] == 9) ||
                             (entry[0] == 9 && entry[1] == 3);
        Assert.IsTrue(correctValues,
            $"PairingHistoryRaw entry must contain the values 3 and 9, got [{entry[0]}, {entry[1]}]");
    }

    [TestMethod]
    public void PairingHistoryRaw_IsEmpty_WhenPairingHistoryWasNeverSet()
    {
        var session = new RaceSession(); // PairingHistoryRaw defaults to new List<int[]>()

        var loaded = RoundTrip(session);

        Assert.IsNotNull(loaded.PairingHistoryRaw,
            "PairingHistoryRaw must not be null even when never explicitly set");
        Assert.AreEqual(0, loaded.PairingHistoryRaw.Count,
            "PairingHistoryRaw must be empty when PairingHistory was never populated");
    }

    // ── JSON structure sanity ─────────────────────────────────────────────────

    [TestMethod]
    public void SerializedJson_ContainsPairingHistoryRaw_NotPairingHistory()
    {
        // [JsonIgnore] on PairingHistory means the camelCase key "pairingHistory"
        // must NOT appear in JSON; "pairingHistoryRaw" MUST appear.
        var session = new RaceSession();
        session.PairingHistory = new HashSet<(int, int)> { (1, 2) };

        var json = JsonSerializer.Serialize(session, WriteOpts);

        Assert.IsTrue(json.Contains("pairingHistoryRaw"),
            "JSON must contain the backing-store property 'pairingHistoryRaw'");
        Assert.IsFalse(json.Contains("\"pairingHistory\""),
            "JSON must NOT contain the [JsonIgnore] property 'pairingHistory'");
    }
}
