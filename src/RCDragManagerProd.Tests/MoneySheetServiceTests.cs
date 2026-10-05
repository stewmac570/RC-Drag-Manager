using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>The event money sheet: fill from the classes, totals, and save with the event.</summary>
[TestClass]
public class MoneySheetServiceTests
{
    [TestMethod]
    public void AddEveryone_OneRowPerDriverAndCar_NoDuplicates()
    {
        var evt = Event();
        var svc = new MoneySheetService(evt, null);

        Assert.AreEqual(3, svc.AddEveryoneFromClasses());
        Assert.AreEqual(0, svc.AddEveryoneFromClasses(), "A second fill must not duplicate rows.");
        Assert.IsTrue(svc.Sheet.Entries.Any(e => e.DriverName == "Ben" && e.CarName == "Truck"));
    }

    [TestMethod]
    public void Total_CountsTrackDaysEntriesAndBuybacks()
    {
        var svc = new MoneySheetService(Event(), null);
        svc.Sheet.BuybackFee = 50m;
        svc.AddEveryoneFromClasses();
        var ben = svc.Sheet.Entries.First();
        ben.TrackDaysPaid = 2;
        MoneySheetService.SetPaid(ben.EntriesPaid, "DYO", true);
        MoneySheetService.SetPaid(ben.BuybacksPaid, "DYO", true);
        MoneySheetService.SetPaid(ben.BuybacksPaid, "dyo", true);   // same class, any case: counted once

        Assert.AreEqual(2 * 10m + 100m + 50m, svc.TotalFor(ben));
        Assert.AreEqual(170m, svc.Total);
    }

    [TestMethod]
    public void Pot_IsEntriesPlusBuybacks_AndNeverTheTrackFee()
    {
        var svc = new MoneySheetService(Event(), null);
        svc.Sheet.BuybackFee = 50m;
        svc.AddEveryoneFromClasses();
        foreach (var e in svc.Sheet.Entries)
        {
            e.TrackDaysPaid = 2;
            MoneySheetService.SetPaid(e.EntriesPaid, "DYO", true);
        }
        MoneySheetService.SetPaid(svc.Sheet.Entries[0].BuybacksPaid, "DYO", true);

        Assert.AreEqual(3 * 100m + 50m, svc.PotFor("DYO"), "Pot = 3 entries + 1 buyback.");
        Assert.AreEqual(3 * 2 * 10m, svc.TrackFees, "Track fees are counted separately.");
        Assert.AreEqual(svc.PotFor("DYO") + svc.TrackFees, svc.Total);
    }

    [TestMethod]
    public void EachClass_HasItsOwnPrices()
    {
        var evt = Event();
        evt.ClassSessions.Add(new RaceSession
        {
            EventName = "QA Money", EventDate = new DateTime(2026, 10, 5),
            RaceType = RaceTypes.RoundRobin, ClassType = "Outlaw",
            DriverEntries = new List<RaceSessionDriverEntry>
            {
                new RaceSessionDriverEntry { DriverID = 4, DriverName = "Chris", CarName = "Outlaw" }
            }
        });
        var svc = new MoneySheetService(evt, null);
        svc.AddEveryoneFromClasses();
        svc.PriceFor("DYO").Entry = 100m;
        svc.PriceFor("Outlaw").Entry = 60m;
        svc.PriceFor("Outlaw").Buyback = 30m;

        var chris = svc.Sheet.Entries.Single(e => e.CarName == "Outlaw");
        MoneySheetService.SetTrackFeePaid(chris, true);
        MoneySheetService.SetPaid(chris.EntriesPaid, "DYO", true);
        MoneySheetService.SetPaid(chris.EntriesPaid, "Outlaw", true);
        MoneySheetService.SetPaid(chris.BuybacksPaid, "Outlaw", true);

        Assert.AreEqual(100m, svc.PotFor("DYO"));
        Assert.AreEqual(60m + 30m, svc.PotFor("Outlaw"));
        Assert.AreEqual(10m + 100m + 60m + 30m, svc.TotalFor(chris));
        Assert.AreEqual(1, svc.TrackFeesPaidCount);
    }

    [TestMethod]
    public void SetupSheet_FillsFromConfiguredClasses_BeforeTheEventExists()
    {
        var sheet = new EventMoneySheet();
        var classes = Event().ClassSessions
            .Select(s => new MoneySheetClass(s.ClassType, s.DriverEntries)).ToList();
        var svc = new MoneySheetService(sheet, () => classes);

        Assert.AreEqual(3, svc.AddEveryoneFromClasses());
        Assert.IsTrue(svc.IsEnteredIn(sheet.Entries.First(e => e.CarName == "Outlaw"), "DYO"));
        Assert.IsNull(svc.Save(), "Nothing to save before the event starts.");
    }

    [TestMethod]
    public void Sheet_SavesAndLoadsWithTheEvent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rcdm-money-{Guid.NewGuid():N}.db");
        var cs = $"Data Source={path};Version=3;";
        try
        {
            DatabaseInitializer.InitializeDatabase(cs);
            var repo = new MultiClassEventRepository(cs);
            var evt = Event();
            repo.SaveEvent(evt);

            var svc = new MoneySheetService(evt, repo);
            svc.AddEveryoneFromClasses();
            svc.Sheet.Entries[0].TrackDaysPaid = 1;
            Assert.IsNull(svc.Save());

            var loaded = repo.LoadEvent(evt.Id);
            Assert.IsNotNull(loaded?.MoneySheet);
            Assert.AreEqual(3, loaded.MoneySheet.Entries.Count);
            Assert.AreEqual(1, loaded.MoneySheet.Entries[0].TrackDaysPaid);
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
        }
    }

    private static MultiClassEvent Event() => new MultiClassEvent
    {
        EventName = "QA Money", EventDate = new DateTime(2026, 10, 5),
        ClassSessions = new List<RaceSession>
        {
            new RaceSession
            {
                EventName = "QA Money", EventDate = new DateTime(2026, 10, 5),
                RaceType = RaceTypes.MultiCarRoundRobin, ClassType = "DYO",
                DriverEntries = new List<RaceSessionDriverEntry>
                {
                    new RaceSessionDriverEntry { RaceEntryId = 1, DriverID = 55, DriverName = "Ben — Truck", CarName = "Truck" },
                    new RaceSessionDriverEntry { RaceEntryId = 2, DriverID = 55, DriverName = "Ben", CarName = "Pinto" },
                    new RaceSessionDriverEntry { RaceEntryId = 3, DriverID = 4, DriverName = "Chris", CarName = "Outlaw" }
                }
            }
        }
    };
}
