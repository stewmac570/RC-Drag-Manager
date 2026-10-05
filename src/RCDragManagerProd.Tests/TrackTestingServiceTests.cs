using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Integration;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Track testing: times on the stream with no event. Starting must clear an earlier
/// event's names out of the overlay's feed; stopping must leave the feed empty.
/// </summary>
[TestClass]
public class TrackTestingServiceTests
{
    private string _dir = "";
    private LiveLocalFeed _feed = null!;

    [TestInitialize]
    public void Setup()
    {
        _dir = Path.Combine(Path.GetTempPath(), "TrackTestingServiceTests_" + Path.GetRandomFileName());
        _feed = new LiveLocalFeed(Path.Combine(_dir, "live.json"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    [TestMethod]
    public void Start_ReplacesLastNightsEvent_WithATestingStateAndNoRaces()
    {
        _feed.Update(new LiveRaceUpdateDto { EventId = "old", EventName = "test", ClassType = "tet", CurrentRound = "RR1" });

        var testing = new TrackTestingService(_feed);
        testing.Start();

        var classes = ReadFeed();
        Assert.AreEqual(1, classes.Length, "Only the testing state may be in the feed.");
        Assert.AreEqual(TrackTestingService.TestingRaceType, classes[0].GetProperty("raceType").GetString());
        Assert.AreEqual(0, classes[0].GetProperty("matches").GetArrayLength());
        Assert.IsTrue(testing.IsRunning);
    }

    [TestMethod]
    public void Stop_LeavesTheFeedEmpty()
    {
        var testing = new TrackTestingService(_feed);
        testing.Start();
        testing.Stop();

        Assert.AreEqual(0, ReadFeed().Length);
        Assert.IsFalse(testing.IsRunning);
    }

    private JsonElement[] ReadFeed() =>
        JsonDocument.Parse(File.ReadAllText(_feed.FilePath)).RootElement.EnumerateArray().ToArray();
}
