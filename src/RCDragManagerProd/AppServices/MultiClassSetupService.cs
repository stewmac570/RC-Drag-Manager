using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// UI-independent race setup and class configuration operations for the Multi-Class
    /// Setup and Class Config screens (issue #287). Wraps <see cref="DriverRepository"/>
    /// so neither form holds a repository or performs persistence, validation, or domain
    /// assembly itself. No WinForms references — the same operations can back a future
    /// UI and are covered by headless tests against an in-memory database.
    /// </summary>
    public sealed class MultiClassSetupService
    {
        private readonly DriverRepository _driverRepo;

        public MultiClassSetupService(DriverRepository driverRepo)
        {
            _driverRepo = driverRepo ?? throw new ArgumentNullException(nameof(driverRepo));
        }

        // ── Driver access ─────────────────────────────────────────────────────

        public List<Driver> GetAllDrivers() => _driverRepo.GetAllDrivers() ?? new List<Driver>();

        /// <summary>Adds a new driver with their first car and persists immediately.</summary>
        public Driver QuickAddDriver(string driverName, Car car)
        {
            if (string.IsNullOrWhiteSpace(driverName))
                throw new ArgumentException("Driver name must not be empty.", nameof(driverName));
            if (car == null) throw new ArgumentNullException(nameof(car));

            var driver = new Driver
            {
                Name  = driverName,
                Notes = "",
                State = "",
                Cars  = new List<Car> { car }
            };
            _driverRepo.AddDriver(driver);
            Logger.Log($"[SVC][MultiClassSetup] QuickAddDriver '{driverName}'");
            return driver;
        }

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns an error message if <paramref name="name"/> is blank or duplicates a name in
        /// <paramref name="existingNames"/> (case-insensitive); otherwise returns null.
        /// </summary>
        public string ValidateClassName(string name, IEnumerable<string> existingNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Class name must not be empty.";

            foreach (var existing in existingNames ?? Enumerable.Empty<string>())
            {
                if (string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
                    return $"A class named '{name}' already exists. Please use a different name.";
            }

            return null;
        }

        /// <summary>
        /// Returns an error message if any class has zero drivers; otherwise returns null.
        /// </summary>
        public string ValidateCanStart(IEnumerable<ClassConfigDto> classes)
        {
            foreach (var cc in classes ?? Enumerable.Empty<ClassConfigDto>())
            {
                if (cc.DriverEntries == null || cc.DriverEntries.Count == 0)
                    return $"Class '{cc.ClassName}' has no drivers. Add at least one driver or remove the class.";
            }
            return null;
        }

        // ── Driver entry construction ─────────────────────────────────────────

        /// <summary>
        /// Builds the <see cref="RaceSessionDriverEntry"/> list from the driver selection state
        /// collected by the UI. Class type determines how dial-in is assigned:
        /// <list type="bullet">
        /// <item>Bracket Class → <paramref name="fixedDialIn"/> applied to every driver.</item>
        /// <item>Heads Up → null (no dial-in).</item>
        /// <item>Dial-In → per-driver override from <paramref name="dialInOverrides"/> if present,
        ///   otherwise the driver's car default dial-in.</item>
        /// </list>
        /// </summary>
        public List<RaceSessionDriverEntry> BuildDriverEntries(
            IEnumerable<int> checkedDriverIds,
            IReadOnlyList<Driver> allDrivers,
            string classType,
            double? fixedDialIn,
            IDictionary<int, double?> dialInOverrides,
            string className)
        {
            bool isHeadsUp = string.Equals(classType, "Heads Up", StringComparison.OrdinalIgnoreCase);
            bool isBracket = string.Equals(classType, "Bracket Class", StringComparison.OrdinalIgnoreCase);

            var entries = new List<RaceSessionDriverEntry>();

            foreach (var driverId in checkedDriverIds ?? Enumerable.Empty<int>())
            {
                var driver = allDrivers?.FirstOrDefault(d => d.Id == driverId);
                if (driver == null) continue;

                var car = driver.Cars?.FirstOrDefault();

                double? dialIn;
                if (isBracket)
                    dialIn = fixedDialIn;
                else if (isHeadsUp)
                    dialIn = null;
                else
                    dialIn = dialInOverrides != null && dialInOverrides.TryGetValue(driverId, out var ov)
                        ? ov
                        : car?.DefaultDialIn;

                entries.Add(new RaceSessionDriverEntry
                {
                    DriverID   = driver.Id,
                    DriverName = driver.Name,
                    CarID      = car?.CarID ?? 0,
                    CarName    = car?.CarName ?? "",
                    ClassType  = className,
                    DialIn     = dialIn
                });
            }

            return entries;
        }

        // ── Event construction ────────────────────────────────────────────────

        /// <summary>
        /// Increments each driver's events-entered counter, then assembles and returns the
        /// <see cref="MultiClassEvent"/> (with one <see cref="RaceSession"/> per class).
        /// </summary>
        public MultiClassEvent StartEvent(string eventName, DateTime date, IEnumerable<ClassConfigDto> classes)
        {
            var classesList = (classes ?? throw new ArgumentNullException(nameof(classes))).ToList();

            foreach (var cc in classesList)
                foreach (var entry in cc.DriverEntries ?? Enumerable.Empty<RaceSessionDriverEntry>())
                    _driverRepo.IncrementEventsEntered(entry.DriverID);

            var multiEvent = new MultiClassEvent
            {
                EventName    = eventName?.Trim() ?? "",
                EventDate    = date.Date
            };

            foreach (var cc in classesList)
            {
                bool isRR = string.Equals(cc.RaceType, RaceTypes.RoundRobin, StringComparison.OrdinalIgnoreCase);

                var session = new RaceSession
                {
                    EventName         = multiEvent.EventName,
                    EventDate         = multiEvent.EventDate,
                    RaceType          = cc.RaceType ?? RaceTypes.RoundRobin,
                    ClassType         = cc.ClassName,
                    FixedDialIn       = cc.FixedDialIn,
                    RoundRobinVariant = isRR ? cc.Variant : null,
                    RoundsToRun       = isRR ? cc.RoundsToRun : null,
                    DriverEntries     = cc.DriverEntries
                };
                multiEvent.ClassSessions.Add(session);

                if (isRR)
                    Logger.Log($"[SVC][MultiClassSetup] RR class '{cc.ClassName}' → Variant='{session.RoundRobinVariant}', Rounds={session.RoundsToRun}");
            }

            return multiEvent;
        }
    }

    /// <summary>
    /// Carries the configuration for a single class between the Config Dialog, Setup Form,
    /// and <see cref="MultiClassSetupService"/>. Replaces the private <c>ClassConfig</c>
    /// inner class that used to live in <c>MultiClassSetupForm</c>.
    /// </summary>
    public sealed class ClassConfigDto
    {
        public string ClassName { get; set; }
        public string RaceType { get; set; }
        public string ClassType { get; set; }
        public double? FixedDialIn { get; set; }
        public string Variant { get; set; }
        public int? RoundsToRun { get; set; }
        public List<RaceSessionDriverEntry> DriverEntries { get; set; }
    }
}
