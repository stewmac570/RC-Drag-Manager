using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="SessionRosterService.AddOrUpdate"/> — the commit step behind
/// the bulk add-driver dialog (#417). The dialog stays open across entries, so
/// each call must either apply cleanly or reject without touching the roster.
/// </summary>
[TestClass]
public class SessionRosterAddOrUpdateTests
{
    private readonly SessionRosterService _svc = new SessionRosterService();

    [TestMethod]
    public void AddOrUpdate_NewName_AddsDriver()
    {
        var roster = new List<Driver>();

        var result = _svc.AddOrUpdate("Ash Drummond", "3.220", roster);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.WasExisting);
        Assert.AreEqual(1, roster.Count);
        Assert.AreEqual("Ash Drummond", roster[0].Name);
        Assert.AreEqual(3.220, roster[0].QualTime);
    }

    [TestMethod]
    public void AddOrUpdate_BlankQualTime_AddsDriverWithNoQual()
    {
        var roster = new List<Driver>();

        var result = _svc.AddOrUpdate("Ash Drummond", "", roster);

        Assert.IsTrue(result.Success);
        Assert.IsNull(roster[0].QualTime);
    }

    [TestMethod]
    public void AddOrUpdate_TrimsSurroundingWhitespace()
    {
        var roster = new List<Driver>();

        _svc.AddOrUpdate("  Ash Drummond  ", "  3.220  ", roster);

        Assert.AreEqual("Ash Drummond", roster[0].Name);
        Assert.AreEqual(3.220, roster[0].QualTime);
    }

    [TestMethod]
    public void AddOrUpdate_ExistingNameDifferentCase_UpdatesQualInsteadOfDuplicating()
    {
        var roster = new List<Driver>();
        _svc.AddOrUpdate("Ash Drummond", "3.220", roster);

        var result = _svc.AddOrUpdate("ASH DRUMMOND", "3.100", roster);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.WasExisting);
        Assert.AreEqual(1, roster.Count, "same driver must not be added twice");
        Assert.AreEqual(3.100, roster[0].QualTime);
    }

    [TestMethod]
    public void AddOrUpdate_ExistingNameBlankQual_LeavesQualUnchanged()
    {
        var roster = new List<Driver>();
        _svc.AddOrUpdate("Ash Drummond", "3.220", roster);

        _svc.AddOrUpdate("Ash Drummond", "", roster);

        Assert.AreEqual(3.220, roster[0].QualTime,
            "re-entering a name without a time must not wipe the existing qual time");
    }

    [TestMethod]
    public void AddOrUpdate_BlankName_FailsAndLeavesRosterUntouched()
    {
        var roster = new List<Driver>();
        _svc.AddOrUpdate("Ash Drummond", "", roster);

        var result = _svc.AddOrUpdate("   ", "", roster);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual(1, roster.Count);
    }

    [TestMethod]
    public void AddOrUpdate_InvalidQualTime_FailsAndLeavesRosterUntouched()
    {
        var roster = new List<Driver>();

        var result = _svc.AddOrUpdate("Ash Drummond", "not-a-number", roster);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual(0, roster.Count, "a rejected entry must not be added");
    }

    [TestMethod]
    public void AddOrUpdate_BulkRun_AssignsUniqueSequentialIds()
    {
        var roster = new List<Driver>();
        var names = new[] { "Ash", "Ben", "Corey", "Damo", "Jake" };

        foreach (var n in names)
            Assert.IsTrue(_svc.AddOrUpdate(n, "", roster).Success);

        Assert.AreEqual(names.Length, roster.Count);
        CollectionAssert.AllItemsAreUnique(roster.Select(d => d.Id).ToList());
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, roster.Select(d => d.Id).ToList());
    }

    [TestMethod]
    public void AddOrUpdate_AfterRejectedEntry_NextEntryStillSucceeds()
    {
        var roster = new List<Driver>();

        _svc.AddOrUpdate("", "", roster);
        var result = _svc.AddOrUpdate("Ash Drummond", "", roster);

        Assert.IsTrue(result.Success, "a rejected entry must not poison the next one");
        Assert.AreEqual(1, roster.Count);
        Assert.AreEqual(1, roster[0].Id);
    }
}
