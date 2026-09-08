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
}
