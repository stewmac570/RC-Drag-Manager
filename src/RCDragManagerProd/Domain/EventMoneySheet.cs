using System;
using System.Collections.Generic;

namespace RCDragManagerProd.Domain
{
    /// <summary>
    /// The event's money sheet: prices, and one row per driver and car with what they
    /// have paid. Saved inside the event, so it needs no table of its own.
    /// </summary>
    public sealed class EventMoneySheet
    {
        /// <summary>Track fee per driver, in dollars. Kept separate from the pots.</summary>
        public decimal TrackFee { get; set; } = 10m;

        /// <summary>Entry price for a class with no price of its own, in dollars.</summary>
        public decimal EntryFee { get; set; } = 100m;

        /// <summary>Buyback price for a class with no price of its own. No standard
        /// amount was given, so it starts at zero for the operator to set.</summary>
        public decimal BuybackFee { get; set; }

        /// <summary>Each class's own entry and buyback price.</summary>
        public List<MoneySheetClassPrice> ClassPrices { get; set; } = new List<MoneySheetClassPrice>();

        public List<MoneySheetEntry> Entries { get; set; } = new List<MoneySheetEntry>();
    }

    public sealed class MoneySheetClassPrice
    {
        public string ClassName { get; set; } = "";
        public decimal Entry { get; set; }
        public decimal Buyback { get; set; }
    }

    public sealed class MoneySheetEntry
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public string CarName { get; set; } = "";

        /// <summary>Track fee paid: 1 when ticked, 0 when not. (A count, so a sheet
        /// saved by the first version, which counted days, still loads.)</summary>
        public int TrackDaysPaid { get; set; }

        /// <summary>Classes (by class name) whose entry is paid.</summary>
        public List<string> EntriesPaid { get; set; } = new List<string>();

        /// <summary>Classes (by class name) whose buyback is paid.</summary>
        public List<string> BuybacksPaid { get; set; } = new List<string>();

        public string Notes { get; set; } = "";
    }
}
