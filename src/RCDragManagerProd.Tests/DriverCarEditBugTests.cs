using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

[TestClass]
public class DriverCarEditBugTests
{
    [TestMethod]
    public void EditCarName_UpdateDriver_ReloadByIdShowsUpdatedCarName()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new DriverRepository(db.ConnectionString);

        var driver = CreateDriverWithSingleCar("Ava Stone", "Old Car Name");
        repository.AddDriver(driver);

        var loaded = repository.GetDriverById(driver.Id);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(1, loaded!.Cars.Count);

        loaded.Cars[0].CarName = "Updated Car Name";
        repository.UpdateDriver(loaded);

        var reloaded = repository.GetDriverById(driver.Id);
        Assert.IsNotNull(reloaded);
        Assert.AreEqual("Updated Car Name", reloaded!.Cars[0].CarName);
    }

    [TestMethod]
    public void BugRepro_EditCarName_ChangesCarId_AndBreaksIdBasedPropagation()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new DriverRepository(db.ConnectionString);

        var driver = CreateDriverWithSingleCar("Blake Turner", "Original Car");
        repository.AddDriver(driver);

        var beforeEdit = repository.GetDriverById(driver.Id);
        Assert.IsNotNull(beforeEdit);
        var originalCarId = beforeEdit!.Cars.Single().CarID;

        beforeEdit.Cars[0].CarName = "Edited Car";
        repository.UpdateDriver(beforeEdit);

        var afterEdit = repository.GetDriverById(driver.Id);
        Assert.IsNotNull(afterEdit);
        Assert.AreEqual("Edited Car", afterEdit!.Cars.Single().CarName);

        var sameCarIdentity = afterEdit.Cars.SingleOrDefault(c => c.CarID == originalCarId);

        // Expected bug-repro assertion:
        // editing a car should keep the same car identity for downstream ID-based flows.
        // Current implementation deletes and reinserts cars, so this assertion fails.
        Assert.IsNotNull(
            sameCarIdentity,
            "Bug repro: car edit changed CarID, which can break ID-based propagation across views/workflows.");
    }

    [TestMethod]
    public void EditCarName_UpdateDriver_GetAllDriversReflectsUpdatedCarName()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new DriverRepository(db.ConnectionString);

        var driver = CreateDriverWithSingleCar("Casey Reed", "Race Car A");
        repository.AddDriver(driver);

        var loaded = repository.GetDriverById(driver.Id);
        Assert.IsNotNull(loaded);
        loaded!.Cars[0].CarName = "Race Car A (Edited)";
        repository.UpdateDriver(loaded);

        var allDrivers = repository.GetAllDrivers();
        var edited = allDrivers.Single(d => d.Id == driver.Id);

        Assert.AreEqual(1, edited.Cars.Count);
        Assert.AreEqual("Race Car A (Edited)", edited.Cars[0].CarName);
    }

    private static Driver CreateDriverWithSingleCar(string driverName, string carName)
    {
        return new Driver
        {
            Name = driverName,
            Notes = string.Empty,
            State = string.Empty,
            Cars =
            {
                new Car
                {
                    CarName = carName,
                    ClassType = "Heads Up",
                    DefaultDialIn = null
                }
            }
        };
    }

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-driver-car-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try
            {
                if (File.Exists(DatabasePath))
                {
                    File.Delete(DatabasePath);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
