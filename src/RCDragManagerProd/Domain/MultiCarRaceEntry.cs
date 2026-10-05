using System;

namespace RCDragManagerProd.Domain
{
    /// <summary>
    /// One car competing in a Multi-Car Round Robin class. The entry identity is
    /// deliberately separate from the registered driver identity: standings are
    /// car-by-car, while persistence and stats can still credit the real driver.
    /// </summary>
    public sealed class MultiCarRaceEntry
    {
        public int RaceEntryId { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public int CarId { get; set; }
        public string CarName { get; set; }
        public double? QualifyingTime { get; set; }
        public double? DialIn { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(CarName)
            ? (DriverName ?? "")
            : (DriverName ?? "") + " — " + CarName;

        public Driver ToCompetitor() => new Driver
        {
            // Engine identity is the entry, not the person. The person identity
            // remains on this object and in RaceSessionDriverEntry.
            Id = RaceEntryId,
            Name = DisplayName,
            QualTime = QualifyingTime
        };
    }

    /// <summary>
    /// Naming rules for Multi-Car Round Robin entries. A competitor is shown as
    /// "Driver — Car", but the entry's DriverName must stay the person's name: saves
    /// used to write the "Driver — Car" label back into DriverName, so every reload
    /// added another " — Car".
    /// </summary>
    public static class MultiCarNaming
    {
        public const string Separator = " — ";

        public static bool IsMultiCar(RaceSession session) =>
            session != null &&
            string.Equals(session.OriginalRaceType ?? session.RaceType, RaceTypes.MultiCarRoundRobin,
                StringComparison.OrdinalIgnoreCase);

        public static string Display(string ownerName, string carName) =>
            string.IsNullOrWhiteSpace(carName) ? (ownerName ?? "") : (ownerName ?? "") + Separator + carName;

        /// <summary>The person's name, with any " — Car" suffixes a bad save left behind removed.</summary>
        public static string OwnerName(string driverName, string carName)
        {
            var name = driverName ?? "";
            if (string.IsNullOrWhiteSpace(carName)) return name;
            var suffix = Separator + carName;
            while (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
                name = name.Substring(0, name.Length - suffix.Length);
            return name;
        }
    }
}
