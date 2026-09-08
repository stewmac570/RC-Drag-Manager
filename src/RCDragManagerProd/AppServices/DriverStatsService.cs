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

                bool isMultiCarRoundRobin = string.Equals(
                    session.OriginalRaceType ?? session.RaceType,
                    RaceTypes.MultiCarRoundRobin,
                    StringComparison.OrdinalIgnoreCase);
                var entries = session.DriverEntries ?? new List<RaceSessionDriverEntry>();
                if (!entries.Any(d => d.DriverID == driver.Id)) continue;

                var savedResults = session.SavedResults ?? new List<Domain.MatchResultSave>();
                foreach (var result in savedResults)
                {
                    bool isWin;
                    int opponentId;

                    var winnerEntry = entries.FirstOrDefault(d => EntryIdentity(d, isMultiCarRoundRobin) == result.WinnerDriverId);
                    var loserEntry = entries.FirstOrDefault(d => EntryIdentity(d, isMultiCarRoundRobin) == result.LoserDriverId);

                    if (winnerEntry?.DriverID == driver.Id)
                    {
                        isWin = true;
                        opponentId = loserEntry?.DriverID ?? 0;
                    }
                    else if (loserEntry?.DriverID == driver.Id)
                    {
                        isWin = false;
                        opponentId = winnerEntry?.DriverID ?? 0;
                    }
                    else
                    {
                        continue;
                    }

                    var opponentEntry = entries.FirstOrDefault(d => d.DriverID == opponentId);
                    string opponentName = opponentEntry != null
                        ? isMultiCarRoundRobin ? opponentEntry.DriverName + " \u2014 " + opponentEntry.CarName : opponentEntry.DriverName
                        : "BYE";

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

        private static int EntryIdentity(RaceSessionDriverEntry entry, bool isMultiCarRoundRobin)
        {
            if (entry == null) return 0;
            return isMultiCarRoundRobin ? entry.RaceEntryId : entry.DriverID;
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
