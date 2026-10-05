using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RCDragManagerProd.AppServices;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.Tests.Helpers;

namespace RCDragManagerProd.Tests;

/// <summary>
/// Race day 2026-10-03: dial-ins were typed into a class that then had to be set up
/// again, and the new class started with none. A dial-in set at the console now also
/// becomes the car's default, so the next class seeds from it.
/// </summary>
[TestClass]
[DoNotParallelize]
public class DialInRememberedOnCarTests
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConsoleDialIn_BecomesTheCarDefault_AndSeedsTheNextClass(bool multiCar)
    {
        var path = Path.Combine(Path.GetTempPath(), $"rcdm-dialin-{Guid.NewGuid():N}.db");
        var cs = $"Data Source={path};Version=3;";
        try
        {
            DatabaseInitializer.InitializeDatabase(cs);
            var repo = new DriverRepository(cs);
            repo.AddDriver(new Driver
            {
                Name = "Keff", Notes = "", State = "",
                Cars = new List<Car> { new Car { CarName = "Outlaw", ClassType = "DYO", DefaultDialIn = 3.0 } }
            });
            var driver = repo.GetAllDrivers().Single();
            var car = driver.Cars.Single();

            var session = new RaceSession
            {
                EventName = "QA", EventDate = new DateTime(2026, 10, 5),
                RaceType = multiCar ? RaceTypes.MultiCarRoundRobin : RaceTypes.RoundRobin,
                ClassType = "Dial-In",
                DriverEntries = new List<RaceSessionDriverEntry>
                {
                    new RaceSessionDriverEntry
                    {
                        RaceEntryId = multiCar ? 7 : 0, DriverID = driver.Id, DriverName = driver.Name,
                        CarID = car.CarID, CarName = car.CarName, DialIn = 3.0
                    }
                }
            };
            var controller = new RaceController(session, new NoOpStandingsDialogService());
            var console = new RaceConsoleService(controller, null, repo);

            console.SetDialIn(multiCar ? 7 : driver.Id, 2.48);

            Assert.AreEqual(2.48, session.DriverEntries[0].DialIn, "The class dial-in must be set.");
            Assert.AreEqual(2.48, repo.GetDriverById(driver.Id).Cars.Single().DefaultDialIn,
                "The dial-in must be remembered on the car.");

            var next = new MultiClassSetupService(repo)
                .BuildDriverEntries(new[] { driver.Id }, repo.GetAllDrivers(), "Dial-In", null, null, "DYO");
            Assert.AreEqual(2.48, next.Single().DialIn, "A new class must start from the remembered dial-in.");
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
        }
    }
}
