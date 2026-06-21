using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// UI-independent validation and construction helpers for the in-memory session
    /// roster (issue #290). Used by <c>Form1</c>'s add-driver flow so that name and
    /// qual-time validation live outside the form. No database access — the roster is
    /// ephemeral session state owned by the console form.
    /// </summary>
    public sealed class SessionRosterService
    {
        /// <summary>
        /// Returns an error message when <paramref name="name"/> is blank or
        /// <paramref name="qualTimeText"/> is non-empty but not a valid number;
        /// otherwise returns null.
        /// </summary>
        public string Validate(string name, string qualTimeText)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Enter a driver name.";

            if (!string.IsNullOrWhiteSpace(qualTimeText) &&
                !double.TryParse(qualTimeText.Trim(), NumberStyles.Any,
                                  CultureInfo.InvariantCulture, out _))
                return "Qualifying time is invalid. Leave it blank or enter a number.";

            return null;
        }

        /// <summary>
        /// Parses a qual-time string: returns null for blank/whitespace, or the
        /// parsed value. Assumes <see cref="Validate"/> was called first.
        /// </summary>
        public double? ParseQualTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            double.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v);
            return v;
        }

        /// <summary>
        /// Builds a new <see cref="Driver"/> for the roster, assigning an ID one
        /// greater than the current roster maximum (or 1 for an empty roster).
        /// </summary>
        public Driver BuildNewDriver(string name, double? qualTime, IReadOnlyCollection<Driver> roster)
        {
            int newId = (roster == null || roster.Count == 0) ? 1 : roster.Max(d => d.Id) + 1;
            return new Driver { Id = newId, Name = name, QualTime = qualTime };
        }

        /// <summary>
        /// Synchronizes the editable console roster into the session while preserving
        /// race-entry metadata such as dial-in, car, class and seed.
        /// </summary>
        public void SyncSession(RaceSession session, IReadOnlyCollection<Driver> roster)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var drivers = (roster ?? Array.Empty<Driver>())
                .Where(d => d != null)
                .ToList();
            var existing = (session.DriverEntries ?? new List<RaceSessionDriverEntry>())
                .Where(e => e != null)
                .GroupBy(e => e.DriverID)
                .ToDictionary(g => g.Key, g => g.First());

            session.DriverEntries = drivers.Select(d =>
            {
                if (!existing.TryGetValue(d.Id, out var entry))
                    entry = new RaceSessionDriverEntry { DriverID = d.Id };
                entry.DriverName = d.Name;
                entry.QualifyingTime = d.QualTime;
                return entry;
            }).ToList();

            session.Drivers = new List<Driver>(drivers);
        }
    }
}
