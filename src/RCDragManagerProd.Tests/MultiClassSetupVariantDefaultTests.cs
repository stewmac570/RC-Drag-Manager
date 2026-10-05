using System;
using System.Collections.Generic;
using System.IO;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Race day 2026-10-03: a Multi-Car Round Robin class left setup with no variant, so it
/// fell back to buybacks on and ran a losers bracket with the box unticked. A Round
/// Robin class must always leave setup with a variant and a round count.
/// </summary>
[TestClass]
public class MultiClassSetupVariantDefaultTests
{
    [DataTestMethod]
    [DataRow(RaceTypes.RoundRobin)]
    [DataRow(RaceTypes.MultiCarRoundRobin)]
    public void RoundRobinClass_WithNoVariant_StartsWithBuybacksOff(string raceType)
    {
        var session = StartOne(new ClassConfigDto { ClassName = "DYO", RaceType = raceType });

        Assert.AreEqual("QMDRA", session.RoundRobinVariant);
        Assert.AreEqual(MultiClassSetupService.DefaultRoundRobinRounds, session.RoundsToRun);
    }

    [DataTestMethod]
    [DataRow(RaceTypes.RoundRobin)]
    [DataRow(RaceTypes.MultiCarRoundRobin)]
    public void RoundRobinClass_KeepsTheVariantItWasGiven(string raceType)
    {
        var session = StartOne(new ClassConfigDto
        {
            ClassName = "Chicago", RaceType = raceType, Variant = "Standard", RoundsToRun = 5
        });

        Assert.AreEqual("Standard", session.RoundRobinVariant);
        Assert.AreEqual(5, session.RoundsToRun);
    }

    [TestMethod]
    public void ProLadderClass_GetsNoRoundRobinSettings()
    {
        var session = StartOne(new ClassConfigDto { ClassName = "Pro", RaceType = "Pro Ladder" });

        Assert.IsNull(session.RoundRobinVariant);
        Assert.IsNull(session.RoundsToRun);
    }

    private static RaceSession StartOne(ClassConfigDto cc)
    {
        cc.DriverEntries = new List<RaceSessionDriverEntry>();
        var path = Path.Combine(Path.GetTempPath(), $"rcdm-variant-{Guid.NewGuid():N}.db");
        var cs = $"Data Source={path};Version=3;";
        try
        {
            DatabaseInitializer.InitializeDatabase(cs);
            var svc = new MultiClassSetupService(new DriverRepository(cs));
            return svc.StartEvent("QA", new DateTime(2026, 10, 5), new[] { cc }).ClassSessions[0];
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
        }
    }
}
