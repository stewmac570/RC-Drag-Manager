namespace RCDragManagerProd.WPF.ViewModels
{
    /// <summary>One class row in the per-event Settings tab (#415).</summary>
    public sealed class EventSettingsRow
    {
        /// <summary>Index of the class within the event, used to route the row's actions.</summary>
        public int Index { get; set; }

        public string ClassName { get; set; }
        public string RaceType { get; set; }
        public string Status { get; set; }
        public string DriverSummary { get; set; }

        public bool BuybacksEnabled { get; set; }
        public bool CanChangeBuybacks { get; set; }

        /// <summary>Why the buyback toggle is disabled, shown as its tooltip.</summary>
        public string BuybackHint { get; set; }

        public bool CanReset { get; set; }

        /// <summary>Why reset is unavailable, shown as its tooltip.</summary>
        public string ResetHint { get; set; }
    }
}
