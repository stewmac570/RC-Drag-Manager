using System.IO;
using System.Text.Json;
using RCDragManagerProd.Integration;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="LiveLocalFeed"/>, the file the stream overlay reads for
/// driver names when the race-day laptop has no internet.
/// </summary>
[TestClass]
public class LiveLocalFeedTests
{
    private string _dir = "";
    private LiveLocalFeed _feed = null!;

    [TestInitialize]
    public void Setup()
    {
        _dir = Path.Combine(Path.GetTempPath(), "LiveLocalFeedTests_" + Path.GetRandomFileName());
        _feed = new LiveLocalFeed(Path.Combine(_dir, "live.json"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private static LiveRaceUpdateDto Dto(string eventId, string eventName, string classType, string nextUp) =>
        new LiveRaceUpdateDto
        {
            EventId = eventId,
            EventName = eventName,
            ClassType = classType,
            CurrentRound = "RR1",
            NextUp = nextUp,
            Matches = new List<LiveMatchDto>
            {
                new LiveMatchDto { RoundLabel = "RR1", LeftDriver = "Amy", RightDriver = "Ben", LeftDriverId = 1, RightDriverId = 2 }
            }
        };

    private JsonElement ReadFile() =>
        JsonDocument.Parse(File.ReadAllText(_feed.FilePath)).RootElement;

    [TestMethod]
    public void Update_WritesTheSameCamelCaseShapeAsTheLiveSite()
    {
        _feed.Update(Dto("e1", "Club Night", "DYO", "Amy vs Ben"));

        var root = ReadFile();
        Assert.AreEqual(JsonValueKind.Array, root.ValueKind);
        var cls = root[0];
        Assert.AreEqual("DYO", cls.GetProperty("classType").GetString());
        Assert.AreEqual("Amy vs Ben", cls.GetProperty("nextUp").GetString());
        Assert.AreEqual("Amy", cls.GetProperty("matches")[0].GetProperty("leftDriver").GetString());
    }

    [TestMethod]
    public void Update_KeepsOneEntryPerClass_LatestWins()
    {
        _feed.Update(Dto("e1", "Club Night", "DYO", "Amy vs Ben"));
        _feed.Update(Dto("e1", "Club Night", "Street", "Cal vs Dee"));
        _feed.Update(Dto("e1", "Club Night", "DYO", "Ben vs Amy"));

        var root = ReadFile();
        Assert.AreEqual(2, root.GetArrayLength());
        var dyo = root.EnumerateArray().Single(c => c.GetProperty("classType").GetString() == "DYO");
        Assert.AreEqual("Ben vs Amy", dyo.GetProperty("nextUp").GetString());
    }

    [TestMethod]
    public void Reset_ByEventName_RemovesThatEventOnly()
    {
        _feed.Update(Dto("e1", "Club Night", "DYO", "Amy vs Ben"));
        _feed.Update(Dto("e2", "Big Meet", "DYO", "Cal vs Dee"));

        _feed.Reset("", "Club Night");

        var root = ReadFile();
        Assert.AreEqual(1, root.GetArrayLength());
        Assert.AreEqual("Big Meet", root[0].GetProperty("eventName").GetString());
    }

    [TestMethod]
    public void Reset_ByEventId_RemovesThatEventOnly()
    {
        _feed.Update(Dto("e1", "Club Night", "DYO", "Amy vs Ben"));
        _feed.Update(Dto("e2", "Big Meet", "DYO", "Cal vs Dee"));

        _feed.Reset("e2", null!);

        var root = ReadFile();
        Assert.AreEqual(1, root.GetArrayLength());
        Assert.AreEqual("e1", root[0].GetProperty("eventId").GetString());
    }

    [TestMethod]
    public void Update_OverwritesAnExistingFileWithoutLeavingATempFile()
    {
        _feed.Update(Dto("e1", "Club Night", "DYO", "Amy vs Ben"));
        _feed.Update(Dto("e1", "Club Night", "DYO", "Ben vs Amy"));

        Assert.IsFalse(File.Exists(_feed.FilePath + ".tmp"));
        Assert.AreEqual("Ben vs Amy", ReadFile()[0].GetProperty("nextUp").GetString());
    }
}
