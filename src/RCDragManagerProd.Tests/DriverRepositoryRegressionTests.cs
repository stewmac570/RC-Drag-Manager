using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.Tests;

[TestClass]
public class DriverRepositoryRegressionTests
{
    [TestMethod]
    public void AddDriver_WithSingleCar_ReloadByIdReturnsPersistedDriverAndCar()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new DriverRepository(db.ConnectionString);

        var driver = CreateDriver(
            "Ava Stone",
            new Car { CarName = "Falcon", ClassType = "Heads Up", DefaultDialIn = 4.01 });

        repository.AddDriver(driver);

        var reloaded = repository.GetDriverById(driver.Id);

        Assert.IsNotNull(reloaded);
        Assert.AreEqual("Ava Stone", reloaded!.Name);
        Assert.AreEqual(1, reloaded.Cars.Count);
        Assert.AreEqual("Falcon", reloaded.Cars[0].CarName);
        Assert.AreEqual("Heads Up", reloaded.Cars[0].ClassType);
        Assert.AreEqual(4.01, reloaded.Cars[0].DefaultDialIn);
        Assert.IsTrue(reloaded.Cars[0].CarID > 0);
    }

    [TestMethod]
    public void UpdateDriver_AddSecondCar_ReloadIncludesBothCars()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new DriverRepository(db.ConnectionString);

        var driver = CreateDriver(
            "Blake Turner",
            new Car { CarName = "Rocket", ClassType = "Heads Up", DefaultDialIn = 3.95 });

        repository.AddDriver(driver);

        var loaded = repository.GetDriverById(driver.Id);
        Assert.IsNotNull(loaded);

        loaded!.Cars.Add(new Car { CarName = "Comet", ClassType = "Heads Up", DefaultDialIn = 4.12 });
        repository.UpdateDriver(loaded);

        var reloaded = repository.GetDriverById(driver.Id);

        Assert.IsNotNull(reloaded);
        Assert.AreEqual(2, reloaded!.Cars.Count);
        CollectionAssert.AreEquivalent(
            new[] { "Rocket", "Comet" },
            reloaded.Cars.Select(c => c.CarName).ToArray());
        Assert.IsTrue(reloaded.Cars.All(c => c.CarID > 0));
    }

    [TestMethod]
    public void UpdateDriver_EditExistingCar_PreservesCarIdentityAndUpdatesValues()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new DriverRepository(db.ConnectionString);

        var driver = CreateDriver(
            "Casey Reed",
            new Car { CarName = "Old Name", ClassType = "Super Pro", DefaultDialIn = 4.33 });

        repository.AddDriver(driver);

        var loaded = repository.GetDriverById(driver.Id);
        Assert.IsNotNull(loaded);

        var originalCarId = loaded!.Cars.Single().CarID;
        loaded.Cars[0].CarName = "Updated Name";
        loaded.Cars[0].ClassType = "Pro";
        loaded.Cars[0].DefaultDialIn = 4.22;

        repository.UpdateDriver(loaded);

        var reloaded = repository.GetDriverById(driver.Id);
        Assert.IsNotNull(reloaded);

        var editedCar = reloaded!.Cars.Single();
        Assert.AreEqual(originalCarId, editedCar.CarID);
        Assert.AreEqual("Updated Name", editedCar.CarName);
        Assert.AreEqual("Pro", editedCar.ClassType);
        Assert.AreEqual(4.22, editedCar.DefaultDialIn);
    }

    [TestMethod]
    public void UpdateDriver_RemoveCar_DeletedCarDoesNotAppearAfterReload()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new DriverRepository(db.ConnectionString);

        var driver = CreateDriver(
            "Drew Cole",
            new Car { CarName = "Alpha", ClassType = "Heads Up", DefaultDialIn = 4.00 },
            new Car { CarName = "Beta", ClassType = "Heads Up", DefaultDialIn = 4.05 });

        repository.AddDriver(driver);

        var loaded = repository.GetDriverById(driver.Id);
        Assert.IsNotNull(loaded);

        var removedCarId = loaded!.Cars.Single(c => c.CarName == "Beta").CarID;
        loaded.Cars = loaded.Cars.Where(c => c.CarName != "Beta").ToList();

        repository.UpdateDriver(loaded);

        var reloaded = repository.GetDriverById(driver.Id);

        Assert.IsNotNull(reloaded);
        Assert.AreEqual(1, reloaded!.Cars.Count);
        Assert.AreEqual("Alpha", reloaded.Cars[0].CarName);
        Assert.IsFalse(reloaded.Cars.Any(c => c.CarID == removedCarId));
    }

    [TestMethod]
    public void GetAllDrivers_AfterUpdates_ReturnsLatestCarDataForDownstreamUse()
    {
        using var db = new TemporarySqliteDb();
        DatabaseInitializer.InitializeDatabase(db.ConnectionString);
        var repository = new DriverRepository(db.ConnectionString);

        var driverA = CreateDriver(
            "Evan Hart",
            new Car { CarName = "Storm", ClassType = "Heads Up", DefaultDialIn = 4.10 });
        var driverB = CreateDriver(
            "Flynn Ward",
            new Car { CarName = "Flash", ClassType = "Super Pro", DefaultDialIn = 4.20 });

        repository.AddDriver(driverA);
        repository.AddDriver(driverB);

        var loadedA = repository.GetDriverById(driverA.Id)!;
        loadedA.Cars[0].CarName = "Storm MkII";
        repository.UpdateDriver(loadedA);

        var loadedB = repository.GetDriverById(driverB.Id)!;
        loadedB.Cars.Add(new Car { CarName = "Flash Backup", ClassType = "Super Pro", DefaultDialIn = 4.18 });
        repository.UpdateDriver(loadedB);

        var allDrivers = repository.GetAllDrivers();

        var reloadedA = allDrivers.Single(d => d.Id == driverA.Id);
        var reloadedB = allDrivers.Single(d => d.Id == driverB.Id);

        Assert.AreEqual("Storm MkII", reloadedA.Cars.Single().CarName);
        Assert.AreEqual(2, reloadedB.Cars.Count);
        CollectionAssert.AreEquivalent(
            new[] { "Flash", "Flash Backup" },
            reloadedB.Cars.Select(c => c.CarName).ToArray());
        Assert.IsTrue(reloadedA.Cars.All(c => c.CarID > 0));
        Assert.IsTrue(reloadedB.Cars.All(c => c.CarID > 0));
    }

    private static Driver CreateDriver(string name, params Car[] cars)
    {
        return new Driver
        {
            Name = name,
            Notes = string.Empty,
            State = string.Empty,
            Cars = cars.ToList()
        };
    }

    private sealed class TemporarySqliteDb : IDisposable
    {
        public TemporarySqliteDb()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"rcdragmanager-driver-repo-tests-{Guid.NewGuid():N}.db");
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
