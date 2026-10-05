using System;
using System.Collections.Generic;

namespace RCDragManagerProd.Domain
{
    /// <summary>
    /// The event's money sheet: who has paid what. One row per driver and car at the
    /// event. Saved inside the event, so it travels with the event and needs no table
    /// of its own. A first version from race-day feedback (Oct 2026); the design is
    /// expected to change once it has been used.
    /// </summary>
    public sealed class EventMoneySheet
    {
        /// <summary>Track fee per driver per day, in dollars.</summary>
        public decimal TrackFee { get; set; } = 10m;

        /// <summary>Entry fee per class entered, in dollars.</summary>
        public decimal EntryFee { get; set; } = 100m;

        /// <summary>Buyback fee per buyback taken, in dollars. No standard amount was
        /// given, so it starts at zero for the operator to set.</summary>
        public decimal BuybackFee { get; set; }

        public List<MoneySheetEntry> Entries { get; set; } = new List<MoneySheetEntry>();
    }

    public sealed class MoneySheetEntry
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public string CarName { get; set; } = "";

        /// <summary>How many days of track fee this driver has paid.</summary>
        public int TrackDaysPaid { get; set; }

        /// <summary>Classes (by class name) whose entry fee is paid.</summary>
        public List<string> EntriesPaid { get; set; } = new List<string>();

        /// <summary>Classes (by class name) whose buyback fee is paid.</summary>
        public List<string> BuybacksPaid { get; set; } = new List<string>();

        public string Notes { get; set; } = "";
    }
}
