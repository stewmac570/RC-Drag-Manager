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
        /// <summary>Rounds a Round Robin class runs when setup supplies none; matches the
        /// class dialog's default.</summary>
        public const int DefaultRoundRobinRounds = 3;

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
            var configuredClasses = (classes ?? Enumerable.Empty<ClassConfigDto>()).ToList();
            if (configuredClasses.Count == 0)
                return "Add at least one class with drivers to start.";

            foreach (var cc in configuredClasses)
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

        /// <summary>
        /// Builds the car-level roster for Multi-Car Round Robin. A registered
        /// driver may race any number of distinct cars in the class; the persisted entry id
        /// becomes the unique in-race identity used by the fixture and standings.
        /// </summary>
        public List<RaceSessionDriverEntry> BuildMultiCarRoundRobinEntries(
            IEnumerable<int> selectedCarIds,
            IReadOnlyList<Driver> allDrivers,
            string classType,
            double? fixedDialIn,
            IDictionary<int, double?> dialInOverrides,
            string className)
        {
            var selected = new HashSet<int>(selectedCarIds ?? Enumerable.Empty<int>());
            var candidates = (allDrivers ?? Array.Empty<Driver>())
                .Where(d => d != null)
                .SelectMany(d => (d.Cars ?? new List<Car>()).Select(c => new { Driver = d, Car = c }))
                .Where(x => x.Car != null && selected.Contains(x.Car.CarID))
                .OrderBy(x => x.Driver.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Car.CarID)
                .ToList();

            bool isHeadsUp = string.Equals(classType, "Heads Up", StringComparison.OrdinalIgnoreCase);
            bool isBracket = string.Equals(classType, "Bracket Class", StringComparison.OrdinalIgnoreCase);
            int entryId = 1;

            return candidates.Select(x => new RaceSessionDriverEntry
            {
                RaceEntryId = entryId++,
                DriverID = x.Driver.Id,
                DriverName = x.Driver.Name,
                CarID = x.Car.CarID,
                CarName = x.Car.CarName,
                ClassType = className,
                QualifyingTime = x.Driver.QualTime,
                DialIn = isHeadsUp ? null : isBracket ? fixedDialIn :
                    (dialInOverrides != null && dialInOverrides.TryGetValue(x.Car.CarID, out var value)
                        ? value
                        : x.Car.DefaultDialIn)
            }).ToList();
        }

        /// <summary>
        /// Puts one driver's car into a class, or takes it out. Used when entry money is
        /// ticked on the money sheet, which is where drivers are entered into classes.
        /// A Multi-Car class takes any number of a driver's cars; any other class takes
        /// a driver once. Returns an operator-facing error, or null.
        /// </summary>
        public string SetEntered(ClassConfigDto cc, Driver driver, Car car, bool entered, IReadOnlyList<Driver> allDrivers)
        {
            if (cc == null) return "That class no longer exists.";
            if (driver == null) return "That driver is not in the driver database.";
            var existing = cc.DriverEntries ?? new List<RaceSessionDriverEntry>();
            bool multiCar = string.Equals(cc.RaceType, RaceTypes.MultiCarRoundRobin, StringComparison.OrdinalIgnoreCase);

            if (multiCar)
            {
                if (car == null || car.CarID <= 0)
                    return $"{driver.Name} has no car. Add a car in Driver Manager to enter a multi-car class.";

                var carIds = existing.Select(e => e.CarID).Where(id => id > 0).ToList();
                if (entered && carIds.Contains(car.CarID)) return null;
                if (!entered && !carIds.Contains(car.CarID)) return null;
                if (entered) carIds.Add(car.CarID); else carIds.Remove(car.CarID);

                // Keep any dial-in already set on the cars that stay in the class.
                var keepDialIns = existing.Where(e => e.CarID > 0)
                    .GroupBy(e => e.CarID).ToDictionary(g => g.Key, g => g.First().DialIn);
                cc.DriverEntries = BuildMultiCarRoundRobinEntries(carIds, allDrivers, cc.ClassType, cc.FixedDialIn,
                    cc.ClassType == "Dial-In" ? keepDialIns : null, cc.ClassName);
                return null;
            }

            var mine = existing.FirstOrDefault(e => e.DriverID == driver.Id);
            if (!entered)
            {
                if (mine != null && (car == null || mine.CarID == car.CarID || mine.CarID == 0))
                    cc.DriverEntries = existing.Where(e => e.DriverID != driver.Id).ToList();
                return null;
            }

            if (mine != null)
                return mine.CarID == (car?.CarID ?? 0)
                    ? null
                    : $"{driver.Name} is already in {cc.ClassName} with {(string.IsNullOrEmpty(mine.CarName) ? "another car" : mine.CarName)}.";

            var built = BuildDriverEntries(new[] { driver.Id }, allDrivers, cc.ClassType, cc.FixedDialIn, null, cc.ClassName)
                .FirstOrDefault();
            if (built == null) return $"{driver.Name} could not be entered.";
            if (car != null && car.CarID > 0)
            {
                // Enter the car that was ticked, not just the driver's first car.
                built.CarID = car.CarID;
                built.CarName = car.CarName ?? "";
                if (string.Equals(cc.ClassType, "Dial-In", StringComparison.OrdinalIgnoreCase))
                    built.DialIn = car.DefaultDialIn;
            }
            cc.DriverEntries = existing.Concat(new[] { built }).ToList();
            return null;
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
                foreach (var driverId in (cc.DriverEntries ?? Enumerable.Empty<RaceSessionDriverEntry>())
                    .Where(entry => entry != null && entry.DriverID > 0)
                    .Select(entry => entry.DriverID)
                    .Distinct())
                    _driverRepo.IncrementEventsEntered(driverId);

            var multiEvent = new MultiClassEvent
            {
                EventName    = eventName?.Trim() ?? "",
                EventDate    = date.Date
            };

            // All classes in a multi-class event share one EventId. The live server
            // buckets classes by EventName but treats a push whose EventId differs
            // from the last one it saw as a *new session* and clears the whole bucket
            // (all sibling classes). Giving every class the same EventId keeps them
            // together on the live site, while a genuinely new event still gets a
            // fresh shared id that correctly clears stale state.
            var sharedEventId = Guid.NewGuid();

            foreach (var cc in classesList)
            {
                bool isRR = RaceTypes.IsRoundRobinFormat(cc.RaceType);

                // A Round Robin class must always leave setup with a variant and a round
                // count. A blank variant used to fall through as buybacks-on, which is how
                // the race-day Multi-Car class ran a losers bracket with the box unticked.
                // Buybacks are off by default, so a missing value means off.
                if (isRR && string.IsNullOrWhiteSpace(cc.Variant))
                {
                    Logger.Log($"[SVC][MultiClassSetup][WARN] RR class '{cc.ClassName}' arrived with no variant; defaulting to buybacks off (QMDRA).");
                    cc.Variant = "QMDRA";
                }
                if (isRR && !(cc.RoundsToRun > 0))
                {
                    Logger.Log($"[SVC][MultiClassSetup][WARN] RR class '{cc.ClassName}' arrived with no round count; defaulting to {DefaultRoundRobinRounds}.");
                    cc.RoundsToRun = DefaultRoundRobinRounds;
                }

                var session = new RaceSession
                {
                    EventId           = sharedEventId,
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
