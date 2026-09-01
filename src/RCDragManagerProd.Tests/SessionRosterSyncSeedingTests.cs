using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers how <see cref="SessionRosterService.SyncSession"/> seeds the race entry for a
/// driver who was not in the class before — a late entry added at the console. The entry
/// must come out the same shape setup would have produced, or the new driver races with
/// no car and no dial-in while everyone around them has both.
/// </summary>
[TestClass]
public class SessionRosterSyncSeedingTests
{
    private readonly SessionRosterService _svc = new SessionRosterService();

    private static Driver WithCar(int id, string name, double? defaultDialIn) => new Driver
    {
        Id = id,
        Name = name,
        Cars = new List<Car>
        {
            new Car { CarID = id * 10, CarName = $"{name}'s car", ClassType = "Open", DefaultDialIn = defaultDialIn }
        }
    };

    private static RaceSession SessionWith(string className, double? fixedDialIn,
                                           params RaceSessionDriverEntry[] entries) => new RaceSession
    {
        ClassType = className,
        FixedDialIn = fixedDialIn,
        DriverEntries = entries.ToList()
    };

    private static RaceSessionDriverEntry Entry(int id, string name, double? dialIn) =>
        new RaceSessionDriverEntry { DriverID = id, DriverName = name, DialIn = dialIn };

    [TestMethod]
    public void NewEntry_CopiesTheDriverCarAndClassName()
    {
        var session = SessionWith("Pro Mod", null, Entry(1, "Ash", 4.100));
        var roster = new List<Driver> { WithCar(1, "Ash", 4.100), WithCar(2, "Bev", 4.400) };

        _svc.SyncSession(session, roster);

        var added = session.DriverEntries.Single(e => e.DriverID == 2);
        Assert.AreEqual("Bev's car", added.CarName);
        Assert.AreEqual(20, added.CarID);
        Assert.AreEqual("Pro Mod", added.ClassType);
    }

    [TestMethod]
    public void NewEntry_DialInClass_StartsOnTheCarDefault()
    {
        // At least one existing entry has a dial-in, so this is a Dial-In class.
        var session = SessionWith("Pro Mod", null, Entry(1, "Ash", 4.100));
        var roster = new List<Driver> { WithCar(1, "Ash", 4.100), WithCar(2, "Bev", 4.400) };

        _svc.SyncSession(session, roster);

        Assert.AreEqual(4.400, session.DriverEntries.Single(e => e.DriverID == 2).DialIn);
    }

    [TestMethod]
    public void NewEntry_BracketClass_TakesTheClassFixedDialIn()
    {
        var session = SessionWith("Junior", 5.000, Entry(1, "Ash", 5.000));
        var roster = new List<Driver> { WithCar(1, "Ash", 5.000), WithCar(2, "Bev", 4.400) };

        _svc.SyncSession(session, roster);

        Assert.AreEqual(5.000, session.DriverEntries.Single(e => e.DriverID == 2).DialIn,
            "A Bracket Class gives every driver the same dial-in, not their car default.");
    }

    [TestMethod]
    public void NewEntry_HeadsUpClass_GetsNoDialIn()
    {
        // No existing entry has a dial-in and there is no fixed dial-in: Heads Up.
        var session = SessionWith("Open", null, Entry(1, "Ash", null));
        var roster = new List<Driver> { WithCar(1, "Ash", null), WithCar(2, "Bev", 4.400) };

        _svc.SyncSession(session, roster);

        Assert.IsNull(session.DriverEntries.Single(e => e.DriverID == 2).DialIn,
            "A Heads Up class has no dial-ins, so a car default must not leak into one.");
    }

    [TestMethod]
    public void ExistingEntry_KeepsItsDialInCarAndSeed()
    {
        var session = SessionWith("Pro Mod", null, new RaceSessionDriverEntry
        {
            DriverID = 1, DriverName = "Ash", CarID = 77, CarName = "Old car",
            ClassType = "Pro Mod", DialIn = 4.100, Seed = 3
        });
        var roster = new List<Driver> { WithCar(1, "Ash", 9.999) };

        _svc.SyncSession(session, roster);

        var kept = session.DriverEntries.Single();
        Assert.AreEqual(4.100, kept.DialIn, "An existing entry's live dial-in must survive a roster edit.");
        Assert.AreEqual(77, kept.CarID);
        Assert.AreEqual(3, kept.Seed);
    }

    [TestMethod]
    public void RemovedDriver_DropsOutOfTheSessionEntries()
    {
        var session = SessionWith("Pro Mod", null, Entry(1, "Ash", 4.100), Entry(2, "Bev", 4.400));
        var roster = new List<Driver> { WithCar(1, "Ash", 4.100) };

        _svc.SyncSession(session, roster);

        Assert.AreEqual(1, session.DriverEntries.Count);
        Assert.AreEqual(1, session.DriverEntries[0].DriverID);
        Assert.AreEqual(1, session.Drivers.Count);
    }

    [TestMethod]
    public void NewEntry_DriverWithNoCar_IsStillAdded()
    {
        var session = SessionWith("Open", null, Entry(1, "Ash", null));
        var roster = new List<Driver> { WithCar(1, "Ash", null), new Driver { Id = 2, Name = "Bev" } };

        _svc.SyncSession(session, roster);

        var added = session.DriverEntries.Single(e => e.DriverID == 2);
        Assert.AreEqual("Bev", added.DriverName);
        Assert.AreEqual(0, added.CarID);
        Assert.AreEqual("", added.CarName);
    }
}
