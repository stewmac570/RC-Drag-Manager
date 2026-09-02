using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers the end-of-event board that replaced the ASCII text dump the multi-class
/// window used to show when the last class finished.
///
/// It reads each class's saved archive rather than the completion summaries the
/// window happened to collect, so a resumed event whose first class finished in an
/// earlier sitting still shows that champion.
/// </summary>
[TestClass]
public class EventCompletionPresentationBuilderTests
{
    [TestMethod]
    public void Build_ListsEveryClassWithItsChampionAndRunnerUp()
    {
        var view = EventCompletionPresentationBuilder.Build(EventWith(
            Class("Pro Mod", "Ava", "Casey"),
            Class("Junior", "Blake", "Drew")));

        Assert.AreEqual("Club Round 4", view.EventName);
        Assert.AreEqual(2, view.Classes.Count);

        Assert.AreEqual("Pro Mod", view.Classes[0].ClassName);
        Assert.AreEqual("Ava", view.Classes[0].ChampionName);
        Assert.AreEqual("Casey", view.Classes[0].RunnerUpName);
        Assert.IsTrue(view.Classes[0].HasResult);

        Assert.AreEqual("Junior", view.Classes[1].ClassName);
        Assert.AreEqual("Blake", view.Classes[1].ChampionName);
    }

    [TestMethod]
    public void Build_ReadsTheSavedArchive_NotLiveSummaries()
    {
        // A class that finished in an earlier sitting has no live summary in the
        // window, only its saved archive. It must still appear with its champion.
        var evt = EventWith(Class("Pro Mod", "Ava", "Casey"));

        var view = EventCompletionPresentationBuilder.Build(evt);

        Assert.IsTrue(view.Classes.Single().HasResult);
        Assert.AreEqual("Ava", view.Classes.Single().ChampionName);
    }

    [TestMethod]
    public void Build_MarksAClassWithNoRecordedChampion()
    {
        var view = EventCompletionPresentationBuilder.Build(EventWith(
            Class("Pro Mod", champion: null, runnerUp: null)));

        var row = view.Classes.Single();
        Assert.IsFalse(row.HasResult);
        Assert.AreEqual("Not recorded", row.ChampionName);
    }

    [TestMethod]
    public void Build_NamesAnUnnamedClassByPosition()
    {
        var view = EventCompletionPresentationBuilder.Build(EventWith(
            Class(null, "Ava", "Casey"),
            Class("  ", "Blake", "Drew")));

        Assert.AreEqual("Class 1", view.Classes[0].ClassName);
        Assert.AreEqual("Class 2", view.Classes[1].ClassName);
    }

    [TestMethod]
    public void SubHeading_CountsClasses()
    {
        var one = EventCompletionPresentationBuilder.Build(EventWith(Class("Pro Mod", "Ava", "Casey")));
        var two = EventCompletionPresentationBuilder.Build(EventWith(
            Class("Pro Mod", "Ava", "Casey"), Class("Junior", "Blake", "Drew")));

        StringAssert.Contains(one.SubHeading, "1 class");
        StringAssert.Contains(two.SubHeading, "2 classes");
    }

    [TestMethod]
    public void CopyText_CarriesEveryClassResult()
    {
        var view = EventCompletionPresentationBuilder.Build(EventWith(
            Class("Pro Mod", "Ava", "Casey"),
            Class("Junior", "Blake", "Drew")));

        StringAssert.Contains(view.CopyText, "Club Round 4");
        StringAssert.Contains(view.CopyText, "Pro Mod");
        StringAssert.Contains(view.CopyText, "Ava");
        StringAssert.Contains(view.CopyText, "Junior");
        StringAssert.Contains(view.CopyText, "Drew");
    }

    [TestMethod]
    public void Build_HandlesAnEventWithNoClasses()
    {
        var view = EventCompletionPresentationBuilder.Build(
            new MultiClassEvent { EventName = "Empty", EventDate = new DateTime(2026, 9, 2) });

        Assert.AreEqual(0, view.Classes.Count);
        Assert.IsNotNull(view.CopyText);
    }

    [TestMethod]
    public void Build_HandlesANullEvent()
    {
        var view = EventCompletionPresentationBuilder.Build(null);

        Assert.AreEqual("Event complete", view.EventName);
        Assert.AreEqual(0, view.Classes.Count);
    }

    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static MultiClassEvent EventWith(params RaceSession[] classes) => new MultiClassEvent
    {
        EventName = "Club Round 4",
        EventDate = new DateTime(2026, 9, 2),
        ClassSessions = classes.ToList()
    };

    private static RaceSession Class(string name, string champion, string runnerUp) => new RaceSession
    {
        ClassType = name,
        ResultsArchive = new RaceResultsArchive
        {
            ChampionName = champion,
            RunnerUpName = runnerUp
        }
    };
}
