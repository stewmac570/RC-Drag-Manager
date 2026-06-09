using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// UI-independent driver/car management operations for the Driver Manager screen
    /// (issue #286). Wraps <see cref="DriverRepository"/> so the form holds no repository
    /// and performs no persistence itself — it gathers input, shows dialogs, and renders the
    /// objects this service returns. No WinForms references, so the same operations can back
    /// a future UI and are covered by headless tests against an in-memory database.
    /// </summary>
    public sealed class DriverManagerService
    {
        private readonly DriverRepository _repository;

        public DriverManagerService(DriverRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        // ── Reads ─────────────────────────────────────────────────────────────────

        public List<Driver> GetAllDrivers() => _repository.GetAllDrivers() ?? new List<Driver>();

        public Driver GetDriverById(int driverId) => _repository.GetDriverById(driverId);

        // ── Driver CRUD ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new driver (with zeroed stats) carrying <paramref name="firstCar"/> and
        /// persists it. Returns the created driver.
        /// </summary>
        public Driver AddDriverWithCar(string driverName, Car firstCar)
        {
            if (firstCar == null) throw new ArgumentNullException(nameof(firstCar));

            var driver = new Driver
            {
                Name = driverName,
                Notes = "",
                TotalWins = 0,
                TotalLosses = 0,
                EventsEntered = 0,
                EventsWon = 0,
                State = "",
                Cars = new List<Car> { firstCar }
            };

            _repository.AddDriver(driver);
            return driver;
        }

        /// <summary>Updates a driver's name and state, then persists.</summary>
        public void UpdateDriverDetails(Driver driver, string name, string state)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            driver.Name = name;
            driver.State = state;
            _repository.UpdateDriver(driver);
        }

        public void DeleteDriver(int driverId) => _repository.DeleteDriver(driverId);

        // ── Car CRUD ────────────────────────────────────────────────────────────────

        /// <summary>Adds a car to the driver and persists the driver.</summary>
        public void AddCar(Driver driver, Car car)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (car == null) throw new ArgumentNullException(nameof(car));

            driver.Cars ??= new List<Car>();
            driver.Cars.Add(car);
            _repository.UpdateDriver(driver);
        }

        /// <summary>
        /// Applies edited values onto the driver's car identified by <paramref name="carId"/>
        /// and persists. Returns false if the car is not found.
        /// </summary>
        public bool ApplyCarEdit(Driver driver, int carId, Car editedValues)
        {
            if (editedValues == null) return false;
            var target = driver?.Cars?.FirstOrDefault(c => c.CarID == carId);
            if (target == null) return false;

            target.CarName = editedValues.CarName;
            target.ClassType = editedValues.ClassType;
            target.DefaultDialIn = editedValues.DefaultDialIn;
            _repository.UpdateDriver(driver);
            return true;
        }

        /// <summary>
        /// Removes the driver's car identified by <paramref name="carId"/> and persists.
        /// Returns false if the car is not found.
        /// </summary>
        public bool DeleteCar(Driver driver, int carId)
        {
            var car = driver?.Cars?.FirstOrDefault(c => c.CarID == carId);
            if (car == null) return false;

            driver.Cars.Remove(car);
            _repository.UpdateDriver(driver);
            return true;
        }

        // ── Qualifying time ───────────────────────────────────────────────────────

        /// <summary>
        /// Sets a driver's qualifying time and returns the reloaded driver (so the UI can
        /// refresh from the persisted state).
        /// </summary>
        public Driver SetQualifyingTime(int driverId, double qualifyingTime)
        {
            _repository.UpdateQualifyingTime(driverId, qualifyingTime);
            return _repository.GetDriverById(driverId);
        }
    }
}
