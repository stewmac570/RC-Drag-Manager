using RCDragManagerProd.AppServices;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.WPF.ViewModels
{
    /// <summary>
    /// Builds one class row for the event Settings tab (#415). Shared so the
    /// multi-class window and the single-class console describe a class the same
    /// way; the decisions themselves live in <see cref="EventSettingsService"/>.
    /// </summary>
    public static class EventSettingsRowBuilder
    {
        public static EventSettingsRow Build(int index, RaceSession session, string fallbackName,
                                             bool complete, bool roundRobinComplete, bool bracketStarted)
        {
            bool isRoundRobin = EventSettingsService.IsRoundRobin(session?.RaceType);
            bool buybacksUsed = (session?.BuybackDrivers?.Count ?? 0) > 0;
            bool buybacksOn = EventSettingsService.BuybacksEnabledIn(session?.RoundRobinVariant);

            // Ask about the change toggling would make, so a disabled box explains the
            // reason the operator would actually have hit.
            var buybackCheck = EventSettingsService.CanChangeBuybacks(
                session?.RaceType, complete, roundRobinComplete, buybacksUsed,
                turningOff: buybacksOn, roundsToRun: session?.RoundsToRun);
            var resetCheck = EventSettingsService.CanResetClass(complete);

            return new EventSettingsRow
            {
                Index = index,
                ClassName = string.IsNullOrWhiteSpace(session?.ClassType) ? fallbackName : session.ClassType,
                RaceType = session?.RaceType ?? "",
                Status = EventSettingsService.DescribeClassStatus(complete, roundRobinComplete, bracketStarted),
                DriverSummary = DescribeDrivers(session),

                // A non-Round-Robin class has no buyback round at all, so showing the
                // box ticked would imply one is coming.
                BuybacksEnabled = isRoundRobin && buybacksOn,
                CanChangeBuybacks = buybackCheck.IsAllowed,
                BuybackHint = buybackCheck.Reason ?? "Turn the buyback round on or off for this class.",
                CanReset = resetCheck.IsAllowed,
                ResetHint = resetCheck.Reason ?? "Clear this class's bracket and start it again."
            };
        }

        /// <summary>
        /// Setup fills DriverEntries; the live roster list is only populated once the
        /// console syncs it, so entries are the reliable count before racing starts.
        /// </summary>
        private static string DescribeDrivers(RaceSession session)
        {
            int count = session?.DriverEntries?.Count ?? 0;
            if (count == 0) count = session?.Drivers?.Count ?? 0;
            return $"{count} {(count == 1 ? "driver" : "drivers")}";
        }
    }
}
