using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Helpers;
using RCDragManagerProd.Repositories;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.AppServices
{
    /// <summary>
    /// UI-independent match history queries for the Driver Stats screen (issue #291).
    /// Wraps <see cref="RaceSessionRepository"/> so <c>DriverStatsForm</c> never holds
    /// a repository or queries sessions directly. No WinForms references.
    /// </summary>
    public sealed class DriverStatsService
    {
        private readonly RaceSessionRepository _sessionRepo;

        public DriverStatsService(RaceSessionRepository sessionRepo)
        {
            _sessionRepo = sessionRepo ?? throw new ArgumentNullException(nameof(sessionRepo));
        }

        /// <summary>
        /// Returns every recorded match involving <paramref name="driver"/> across all
        /// saved sessions, ordered by session then match insertion order.
        /// </summary>
        public List<MatchHistoryRow> GetMatchHistory(Driver driver)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));

            var rows = new List<MatchHistoryRow>();

            var sessions = _sessionRepo.GetAllSessions() ?? new List<RaceSessionSummary>();
            foreach (var summary in sessions)
            {
                var session = _sessionRepo.LoadSession(summary.Id);
                if (session == null) continue;

                var entry = session.DriverEntries?.FirstOrDefault(d => d.DriverID == driver.Id);
                if (entry == null) continue;

                var savedResults = session.SavedResults ?? new List<Domain.MatchResultSave>();
                foreach (var result in savedResults)
                {
                    bool isWin;
                    int opponentId;

                    if (result.WinnerDriverId == driver.Id)
                    {
                        isWin = true;
                        opponentId = result.LoserDriverId;
                    }
                    else if (result.LoserDriverId == driver.Id)
                    {
                        isWin = false;
                        opponentId = result.WinnerDriverId;
                    }
                    else
                    {
                        continue;
                    }

                    var opponentEntry = session.DriverEntries?.FirstOrDefault(d => d.DriverID == opponentId);
                    string opponentName = opponentEntry != null ? opponentEntry.DriverName : "BYE";

                    var match = MatchLookupHelper.FindMatchInSession(session, result.MatchId);
                    string roundLabel = match?.RoundLabel ?? "";

                    rows.Add(new MatchHistoryRow(
                        session.EventName,
                        session.EventDate.ToString("yyyy-MM-dd"),
                        roundLabel,
                        opponentName,
                        isWin ? "Win" : "Loss"));
                }
            }

            return rows;
        }
    }

    /// <summary>A single row of match history for display in Driver Stats.</summary>
    public sealed class MatchHistoryRow
    {
        public MatchHistoryRow(string eventName, string eventDate, string roundLabel,
                               string opponentName, string result)
        {
            EventName    = eventName;
            EventDate    = eventDate;
            RoundLabel   = roundLabel;
            OpponentName = opponentName;
            Result       = result;
        }

        public string EventName    { get; }
        public string EventDate    { get; }
        public string RoundLabel   { get; }
        public string OpponentName { get; }
        public string Result       { get; }
    }
}
