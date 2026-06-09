using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Covers <see cref="DriverManagerService"/> (issue #286) — the UI-independent driver/car
/// operations the Driver Manager screen delegates to. Runs headless against an in-memory
/// database, proving the form needs no repository or persistence logic of its own.
/// </summary>
[TestClass]
public class DriverManagerServiceTests
{
    private static DriverManagerService NewService(TemporarySqliteDb db)
    {
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        return new DriverManagerService(new DriverRepository(db.ConnectionString));
    }

    [TestMethod]
    public void AddDriverWithCar_PersistsDriverAndCar()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);

        var created = service.AddDriverWithCar("Ava Stone",
            new Car { CarName = "Falcon", ClassType = "Heads Up", DefaultDialIn = 4.01 });

        var reloaded = service.GetDriverById(created.Id);
        Assert.IsNotNull(reloaded);
        Assert.AreEqual("Ava Stone", reloaded.Name);
        Assert.AreEqual(1, reloaded.Cars.Count);
        Assert.AreEqual("Falcon", reloaded.Cars[0].CarName);
        Assert.AreEqual(1, service.GetAllDrivers().Count(d => d.Id == created.Id));
    }

    [TestMethod]
    public void UpdateDriverDetails_PersistsNameAndState()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var driver = service.AddDriverWithCar("Old Name", new Car { CarName = "C1", ClassType = "Open" });

        service.UpdateDriverDetails(driver, "New Name", "NSW");

        var reloaded = service.GetDriverById(driver.Id);
        Assert.AreEqual("New Name", reloaded.Name);
        Assert.AreEqual("NSW", reloaded.State);
    }

    [TestMethod]
    public void DeleteDriver_RemovesFromStore()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var driver = service.AddDriverWithCar("Temp", new Car { CarName = "C1", ClassType = "Open" });

        service.DeleteDriver(driver.Id);

        Assert.IsFalse(service.GetAllDrivers().Any(d => d.Id == driver.Id));
    }

    [TestMethod]
    public void AddCar_AppendsSecondCar()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var driver = service.AddDriverWithCar("Driver", new Car { CarName = "First", ClassType = "Open" });

        service.AddCar(driver, new Car { CarName = "Second", ClassType = "Heads Up", DefaultDialIn = 3.9 });

        var reloaded = service.GetDriverById(driver.Id);
        Assert.AreEqual(2, reloaded.Cars.Count);
        CollectionAssert.Contains(reloaded.Cars.Select(c => c.CarName).ToList(), "Second");
    }

    [TestMethod]
    public void ApplyCarEdit_UpdatesNamedCar_AndRejectsUnknown()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var driver = service.AddDriverWithCar("Driver", new Car { CarName = "Before", ClassType = "Open", DefaultDialIn = 4.0 });
        var carId = service.GetDriverById(driver.Id).Cars[0].CarID;
        driver = service.GetDriverById(driver.Id);

        var ok = service.ApplyCarEdit(driver, carId,
            new Car { CarName = "After", ClassType = "Heads Up", DefaultDialIn = 3.8 });
        Assert.IsTrue(ok);
        Assert.IsFalse(service.ApplyCarEdit(driver, carId: -1, new Car { CarName = "X" }));

        var reloaded = service.GetDriverById(driver.Id);
        Assert.AreEqual("After", reloaded.Cars[0].CarName);
        Assert.AreEqual("Heads Up", reloaded.Cars[0].ClassType);
        Assert.AreEqual(3.8, reloaded.Cars[0].DefaultDialIn);
    }

    [TestMethod]
    public void DeleteCar_RemovesNamedCar_AndRejectsUnknown()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var driver = service.AddDriverWithCar("Driver", new Car { CarName = "Keep", ClassType = "Open" });
        service.AddCar(driver, new Car { CarName = "Drop", ClassType = "Open" });
        driver = service.GetDriverById(driver.Id);
        var dropId = driver.Cars.First(c => c.CarName == "Drop").CarID;

        Assert.IsFalse(service.DeleteCar(driver, carId: -1));
        Assert.IsTrue(service.DeleteCar(driver, dropId));

        var reloaded = service.GetDriverById(driver.Id);
        Assert.AreEqual(1, reloaded.Cars.Count);
        Assert.AreEqual("Keep", reloaded.Cars[0].CarName);
    }

    [TestMethod]
    public void SetQualifyingTime_PersistsAndReturnsRefreshedDriver()
    {
        using var db = new TemporarySqliteDb();
        var service = NewService(db);
        var driver = service.AddDriverWithCar("Driver", new Car { CarName = "C1", ClassType = "Open" });

        var refreshed = service.SetQualifyingTime(driver.Id, 3.915);

        Assert.AreEqual(3.915, refreshed.QualTime);
        Assert.AreEqual(3.915, service.GetDriverById(driver.Id).QualTime);
    }

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-drivermgr-svc-tests-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }
        public string ConnectionString => $"Data Source={DatabasePath};Version=3;";

        public void Dispose()
        {
            try
            {
                if (File.Exists(DatabasePath))
                    File.Delete(DatabasePath);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
