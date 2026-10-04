using System;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// Rules behind the per-event Settings tab (#415). Pure functions over the
    /// state a class is in, so the tab holds no race logic and the rules can be
    /// asserted headlessly.
    /// </summary>
    public static class EventSettingsService
    {
        /// <summary>
        /// Buybacks are the Round-Robin "Standard" variant; turning them off is the
        /// "QMDRA" variant, where every driver advances after a fixed number of
        /// rounds. Switching after the variant has already shaped the bracket would
        /// leave the class in a state the engine never produces, so it is blocked.
        /// </summary>
        public static BuybackChangeCheck CanChangeBuybacks(
            string raceType,
            bool classComplete,
            bool roundRobinComplete,
            bool buybacksAlreadyUsed,
            bool turningOff,
            int? roundsToRun)
        {
            if (!IsRoundRobin(raceType))
                return BuybackChangeCheck.Blocked(
                    "Buybacks only apply to Round Robin classes.");

            if (classComplete)
                return BuybackChangeCheck.Blocked(
                    "This class is complete — its settings can't be changed.");

            if (buybacksAlreadyUsed)
                return BuybackChangeCheck.Blocked(
                    "A buyback has already been applied in this class. Reset the class first if you need to change this.");

            if (roundRobinComplete)
                return BuybackChangeCheck.Blocked(
                    "Round Robin is already complete, so the buyback decision has been made. Reset the class first if you need to change this.");

            if (turningOff && (!roundsToRun.HasValue || roundsToRun.Value <= 0))
                return BuybackChangeCheck.Blocked(
                    "Turning buybacks off needs a rounds-to-run value for this class, and none is set.");

            return BuybackChangeCheck.Allowed();
        }

        /// <summary>Short status shown against each class in the settings tab.</summary>
        public static string DescribeClassStatus(bool classComplete, bool roundRobinComplete,
                                                 bool bracketStarted)
        {
            if (classComplete) return "Complete";
            if (roundRobinComplete) return "Round Robin complete";
            if (bracketStarted) return "Racing";
            return "Not started";
        }

        /// <summary>
        /// Resetting is refused once a class is finished: its results are already
        /// recorded against driver stats, so clearing the bracket would strand them.
        /// </summary>
        public static BuybackChangeCheck CanResetClass(bool classComplete)
        {
            return classComplete
                ? BuybackChangeCheck.Blocked(
                    "This class is complete and its results are recorded. It can't be reset.")
                : BuybackChangeCheck.Allowed();
        }

        /// <summary>
        /// The race type a class should carry after being reset.
        /// <c>RaceController.Reset</c> blanks <c>RaceSession.RaceType</c>, and that
        /// field also mutates during an event (Round Robin → Losers Bracket →
        /// Finals), so neither the pre-reset value nor an empty one is right on its
        /// own: prefer the captured original, and fall back to the current value for
        /// a class that was configured but never started.
        /// </summary>
        public static string RaceTypeToRestoreOnReset(string originalRaceType, string currentRaceType)
        {
            if (!string.IsNullOrWhiteSpace(originalRaceType)) return originalRaceType.Trim();
            if (!string.IsNullOrWhiteSpace(currentRaceType)) return currentRaceType.Trim();
            return null;
        }

        public static bool IsRoundRobin(string raceType) =>
            string.Equals((raceType ?? "").Trim(), "Round Robin", StringComparison.OrdinalIgnoreCase);

        /// <summary>Session variant string for a buyback on/off choice.</summary>
        public static string VariantFor(bool buybacksEnabled) => buybacksEnabled ? "Standard" : "QMDRA";

        /// <summary>Whether a session variant means buybacks are on.</summary>
        public static bool BuybacksEnabledIn(string variant) =>
            !string.Equals((variant ?? "Standard").Trim(), "QMDRA", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether a settings change is permitted, and why not when it isn't.</summary>
    public sealed class BuybackChangeCheck
    {
        private BuybackChangeCheck(bool ok, string reason)
        {
            IsAllowed = ok;
            Reason = reason;
        }

        public bool IsAllowed { get; }

        /// <summary>Operator-facing explanation; null when allowed.</summary>
        public string Reason { get; }

        public static BuybackChangeCheck Allowed() => new BuybackChangeCheck(true, null);
        public static BuybackChangeCheck Blocked(string reason) => new BuybackChangeCheck(false, reason);
    }
}
