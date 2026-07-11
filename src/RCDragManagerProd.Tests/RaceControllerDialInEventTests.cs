using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers the <see cref="RaceController.DialInsChanged"/> event added for issue #381 —
/// the race console relies on it to re-render dial-ins the background live-site poll
/// (or a local edit) applies to the session.
/// </summary>
[TestClass]
public class RaceControllerDialInEventTests
{
    private static RaceController NewControllerWithDriver(int driverId)
    {
        var session = new RaceSession
        {
            EventName = "DialIn Test",
            RaceType = "Pro Ladder",
            DriverEntries = new List<RaceSessionDriverEntry>
            {
                new RaceSessionDriverEntry { DriverID = driverId, DriverName = "Alice" }
            }
        };
        return new RaceController(session);
    }

    [TestMethod]
    public void UpdateDriverDialIn_KnownDriver_RaisesDialInsChangedAndStoresValue()
    {
        var controller = NewControllerWithDriver(1);
        int raised = 0;
        controller.DialInsChanged += () => raised++;

        controller.UpdateDriverDialIn(1, 5.123);

        Assert.AreEqual(1, raised, "DialInsChanged must fire once per applied change.");
        Assert.AreEqual(5.123, controller.GetDriverDialIn(1));
    }

    [TestMethod]
    public void UpdateDriverDialIn_UnknownDriver_DoesNotRaise()
    {
        var controller = NewControllerWithDriver(1);
        int raised = 0;
        controller.DialInsChanged += () => raised++;

        controller.UpdateDriverDialIn(99, 5.123);

        Assert.AreEqual(0, raised, "No event when no entry was updated.");
        Assert.IsNull(controller.GetDriverDialIn(99));
    }

    [TestMethod]
    public void UpdateDriverDialIn_ClearingValue_RaisesAndClears()
    {
        var controller = NewControllerWithDriver(1);
        controller.UpdateDriverDialIn(1, 4.500);

        int raised = 0;
        controller.DialInsChanged += () => raised++;
        controller.UpdateDriverDialIn(1, null);

        Assert.AreEqual(1, raised);
        Assert.IsNull(controller.GetDriverDialIn(1));
    }
}
