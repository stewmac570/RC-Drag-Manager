using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="SessionRosterService"/> (issue #290) — the validation and
/// driver-construction helpers extracted from <c>Form1.btnAddDriver_Click</c>.
/// Pure in-memory, no database required.
/// </summary>
[TestClass]
public class SessionRosterServiceTests
{
    private readonly SessionRosterService _svc = new SessionRosterService();

    // ── Validate ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Validate_BlankName_ReturnsError()
    {
        Assert.IsNotNull(_svc.Validate("", ""));
        Assert.IsNotNull(_svc.Validate("   ", ""));
    }

    [TestMethod]
    public void Validate_ValidName_NoTime_ReturnsNull()
    {
        Assert.IsNull(_svc.Validate("Alice", ""));
        Assert.IsNull(_svc.Validate("Bob", null));
    }

    [TestMethod]
    public void Validate_ValidName_ValidTime_ReturnsNull()
    {
        Assert.IsNull(_svc.Validate("Carol", "3.85"));
        Assert.IsNull(_svc.Validate("Dave", "4.125"));
    }

    [TestMethod]
    public void Validate_ValidName_InvalidTime_ReturnsError()
    {
        Assert.IsNotNull(_svc.Validate("Eve", "abc"));
        Assert.IsNotNull(_svc.Validate("Frank", "3.8x"));
    }

    // ── ParseQualTime ─────────────────────────────────────────────────────────

    [TestMethod]
    public void ParseQualTime_BlankOrNull_ReturnsNull()
    {
        Assert.IsNull(_svc.ParseQualTime(""));
        Assert.IsNull(_svc.ParseQualTime("   "));
        Assert.IsNull(_svc.ParseQualTime(null));
    }

    [TestMethod]
    public void ParseQualTime_ValidNumber_ReturnsValue()
    {
        Assert.AreEqual(3.85, _svc.ParseQualTime("3.85"));
        Assert.AreEqual(4.125, _svc.ParseQualTime("4.125"));
    }

    // ── BuildNewDriver ────────────────────────────────────────────────────────

    [TestMethod]
    public void BuildNewDriver_EmptyRoster_AssignsId1()
    {
        var driver = _svc.BuildNewDriver("Alice", null, new List<Driver>());
        Assert.AreEqual(1, driver.Id);
        Assert.AreEqual("Alice", driver.Name);
        Assert.IsNull(driver.QualTime);
    }

    [TestMethod]
    public void BuildNewDriver_ExistingRoster_AssignsMaxPlusOne()
    {
        var roster = new List<Driver>
        {
            new Driver { Id = 1, Name = "Alice" },
            new Driver { Id = 3, Name = "Bob" }   // gap at 2
        };
        var driver = _svc.BuildNewDriver("Carol", 4.0, roster);
        Assert.AreEqual(4, driver.Id);
        Assert.AreEqual(4.0, driver.QualTime);
    }

    [TestMethod]
    public void BuildNewDriver_NullRoster_AssignsId1()
    {
        var driver = _svc.BuildNewDriver("Dave", null, null);
        Assert.AreEqual(1, driver.Id);
    }

    [TestMethod]
    public void BuildNewDriver_SetsQualTime()
    {
        var driver = _svc.BuildNewDriver("Eve", 3.92, new List<Driver>());
        Assert.AreEqual(3.92, driver.QualTime);
    }

    [TestMethod]
    public void SyncSession_AddsNewDriverSoDialInCanBeStored()
    {
        var session = new RaceSession();
        var roster = new List<Driver>
        {
            new Driver { Id = 1, Name = "Existing" },
            new Driver { Id = 2, Name = "Added after reset" }
        };

        _svc.SyncSession(session, roster);
        var controller = new Controllers.RaceController(session);
        controller.UpdateDriverDialIn(2, 2.850);

        Assert.AreEqual(2, session.DriverEntries.Count);
        Assert.AreEqual(2.850, session.DriverEntries.Single(e => e.DriverID == 2).DialIn);
    }

    [TestMethod]
    public void SyncSession_PreservesExistingDialInAndCarMetadata()
    {
        var session = new RaceSession
        {
            DriverEntries = new List<RaceSessionDriverEntry>
            {
                new RaceSessionDriverEntry
                {
                    DriverID = 7,
                    DriverName = "Old name",
                    DialIn = 3.125,
                    CarID = 44,
                    CarName = "Blue car",
                    Seed = 2
                }
            }
        };

        _svc.SyncSession(session, new List<Driver>
        {
            new Driver { Id = 7, Name = "New name", QualTime = 3.010 }
        });

        var entry = session.DriverEntries.Single();
        Assert.AreEqual("New name", entry.DriverName);
        Assert.AreEqual(3.010, entry.QualifyingTime);
        Assert.AreEqual(3.125, entry.DialIn);
        Assert.AreEqual(44, entry.CarID);
        Assert.AreEqual("Blue car", entry.CarName);
        Assert.AreEqual(2, entry.Seed);
    }
}
