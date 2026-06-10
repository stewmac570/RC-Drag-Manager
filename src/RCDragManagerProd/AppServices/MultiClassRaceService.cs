using System;
using RCDragManagerProd.Controllers;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Repositories;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// UI-independent stat persistence for multi-class race events (issue #289).
    /// Wraps <see cref="DriverRepository"/> so <c>MultiClassRaceForm</c> never
    /// holds a connection or writes SQL directly. No WinForms references.
    /// </summary>
    public sealed class MultiClassRaceService
    {
        private readonly DriverRepository _driverRepo;

        public MultiClassRaceService(DriverRepository driverRepo)
        {
            _driverRepo = driverRepo ?? throw new ArgumentNullException(nameof(driverRepo));
        }

        /// <summary>
        /// Persists win/loss and events-won stats for a completed class.
        /// Safe to call with a null summary (no-ops).
        /// </summary>
        public void RecordClassCompletion(RaceController.RaceSummary summary)
        {
            if (summary == null) return;

            if (summary.MatchResults != null)
            {
                foreach (var (wId, lId) in summary.MatchResults)
                {
                    _driverRepo.IncrementWinsAndLosses(wId, lId);
                    Logger.Log($"[MultiClass][STATS] +Wins/Losses → winner={wId}, loser={lId}");
                }
            }

            var winnerId = summary.Winner?.Id ?? 0;
            if (winnerId > 0)
            {
                _driverRepo.IncrementEventsWon(winnerId);
                Logger.Log($"[MultiClass][STATS] +EventsWon → #{winnerId} {summary.Winner?.Name}");
            }
        }
    }
}
