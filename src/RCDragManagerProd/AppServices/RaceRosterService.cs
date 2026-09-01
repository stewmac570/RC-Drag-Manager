using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// Roster editing for one race before its first result: pull a driver in from the
    /// driver database, create a driver who is not in it yet, or drop a driver from the
    /// race. Wraps <see cref="DriverRepository"/> so the roster dialog holds no SQL and
    /// no validation of its own.
    ///
    /// Every driver on a race roster carries their real database id. A driver created
    /// here is therefore written to the database first and joins the roster with the id
    /// the database gave them — an id invented locally would collide with a real one and
    /// credit the end-of-event stats to whichever driver happened to own it.
    /// </summary>
    public sealed class RaceRosterService
    {
        private readonly DriverRepository _driverRepo;
        private readonly SessionRosterService _rosterService = new SessionRosterService();

        public RaceRosterService(DriverRepository driverRepo)
        {
            _driverRepo = driverRepo ?? throw new ArgumentNullException(nameof(driverRepo));
        }

        /// <summary>Every driver in the database, for the "add an existing driver" pane.</summary>
        public List<Driver> GetDatabaseDrivers() => _driverRepo.GetAllDrivers() ?? new List<Driver>();

        /// <summary>
        /// Adds a driver who is already in the database to <paramref name="roster"/>,
        /// keeping their database id and car. Rejects a driver who is already racing.
        /// The race's qualifying time is deliberately not seeded from the database row —
        /// it is a per-race value the operator sets on the console.
        /// </summary>
        public RosterAddResult AddFromDatabase(Driver dbDriver, IList<Driver> roster)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (dbDriver == null) return RosterAddResult.Failed("Select a driver to add.");

            var existing = FindInRoster(dbDriver.Id, dbDriver.Name, roster);
            if (existing != null)
                return RosterAddResult.Failed($"{existing.Name} is already in this race.");

            var driver = ToRosterDriver(dbDriver);
            roster.Add(driver);
            Logger.Log($"[SVC][RaceRoster] Added #{driver.Id} {driver.Name} from the driver database.");
            return RosterAddResult.Added(driver);
        }

        /// <summary>
        /// Adds a driver by name, creating them in the database when they are new.
        /// A name already on the roster only has its qualifying time updated; a name
        /// already in the database is pulled in rather than duplicated, so a late entry
        /// typed at the console never creates a second row for an existing driver.
        /// </summary>
        public RosterAddResult CreateAndAdd(string name, string qualTimeText, IList<Driver> roster)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));

            name = (name ?? "").Trim();
            qualTimeText = (qualTimeText ?? "").Trim();

            var error = _rosterService.Validate(name, qualTimeText);
            if (error != null) return RosterAddResult.Failed(error);

            var qual = _rosterService.ParseQualTime(qualTimeText);

            var onRoster = FindInRoster(0, name, roster);
            if (onRoster != null)
            {
                if (qual.HasValue) onRoster.QualTime = qual.Value;
                return RosterAddResult.Updated(onRoster);
            }

            var inDatabase = GetDatabaseDrivers().FirstOrDefault(
                d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));

            Driver driver;
            if (inDatabase != null)
            {
                driver = ToRosterDriver(inDatabase);
                Logger.Log($"[SVC][RaceRoster] '{name}' already in the driver database — reused #{driver.Id}.");
            }
            else
            {
                var created = new Driver { Name = name, Notes = "", State = "", Cars = new List<Car>() };
                _driverRepo.AddDriver(created);
                driver = ToRosterDriver(created);
                Logger.Log($"[SVC][RaceRoster] Created driver #{driver.Id} '{name}' in the driver database.");
            }

            driver.QualTime = qual;
            roster.Add(driver);
            return RosterAddResult.Added(driver);
        }

        /// <summary>
        /// Renames a driver on the roster and writes the new name and state back to the
        /// driver database, so the race and the database never disagree. Returns an
        /// operator-facing error, or null when the rename was applied.
        /// </summary>
        public string Rename(Driver rosterDriver, string newName, string newState, IList<Driver> roster)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (rosterDriver == null) return "Select the driver you want to rename.";

            newName = (newName ?? "").Trim();
            newState = (newState ?? "").Trim();
            if (newName.Length == 0) return "Enter a driver name.";

            var clash = roster.FirstOrDefault(
                d => d != null && d.Id != rosterDriver.Id &&
                     string.Equals(d.Name, newName, StringComparison.OrdinalIgnoreCase));
            if (clash != null) return $"{clash.Name} is already in this race.";

            var db = _driverRepo.GetDriverById(rosterDriver.Id);
            if (db != null)
            {
                db.Name = newName;
                db.State = newState;
                _driverRepo.UpdateDriver(db);
            }

            rosterDriver.Name = newName;
            rosterDriver.State = newState;
            Logger.Log($"[SVC][RaceRoster] Renamed driver #{rosterDriver.Id} to '{newName}'.");
            return null;
        }

        /// <summary>Drops a driver from the race. The driver database is untouched.</summary>
        public bool Remove(Driver rosterDriver, IList<Driver> roster)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (rosterDriver == null) return false;

            var removed = roster.Remove(rosterDriver);
            if (removed)
                Logger.Log($"[SVC][RaceRoster] Removed #{rosterDriver.Id} {rosterDriver.Name} from the race.");
            return removed;
        }

        /// <summary>
        /// Returns an operator-facing reason the roster cannot be saved, or null when it
        /// can. A race needs two drivers before anything can be paired.
        /// </summary>
        public string ValidateRoster(IReadOnlyCollection<Driver> roster) =>
            (roster?.Count ?? 0) < 2 ? "A race needs at least two drivers." : null;

        /// <summary>
        /// The roster entry matching a database id or, failing that, a name. Both tests
        /// matter: the same person must not be added twice under a second database row.
        /// </summary>
        private static Driver FindInRoster(int id, string name, IEnumerable<Driver> roster) =>
            roster.FirstOrDefault(d => d != null &&
                ((id > 0 && d.Id == id) ||
                 string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)));

        /// <summary>
        /// Copies the fields a race roster needs. Cars come along so the console can seed
        /// a new driver's dial-in from their car default; stat tallies stay in the
        /// database, which is the only place they are ever written.
        /// </summary>
        private static Driver ToRosterDriver(Driver source) => new Driver
        {
            Id = source.Id,
            Name = source.Name,
            QualTime = null,
            State = source.State,
            Cars = source.Cars == null ? new List<Car>() : new List<Car>(source.Cars)
        };
    }
}
