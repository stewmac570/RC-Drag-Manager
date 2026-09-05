using System;
using System.Collections.Generic;
using System.Linq;
using RCDragManagerProd.Config;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Integration;
using RCDragManagerProd.Logging;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.RoundRobinMode;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.Controllers
{
    public partial class RaceController
    {
        private readonly LiveApiClient _liveApiClient = new LiveApiClient();

        /// <summary>Entry list for the loaded class. This is the only source of
        /// driver names before a round is generated -- the match list is empty
        /// until then, so without this the live site has nobody to offer.</summary>
        private List<LiveDriverEntryDto> BuildDriverEntryDtos()
        {
            if (_session == null || _session.DriverEntries == null)
                return new List<LiveDriverEntryDto>();

            var classType = _session.ClassType;

            return _session.DriverEntries
                .Where(e => e != null && e.DriverID > 0 && !string.IsNullOrWhiteSpace(e.DriverName))
                .Where(e => string.IsNullOrWhiteSpace(classType) ||
                            string.IsNullOrWhiteSpace(e.ClassType) ||
                            string.Equals(e.ClassType, classType, StringComparison.OrdinalIgnoreCase))
                .GroupBy(e => e.DriverID)
                .Select(g => g.First())
                .Select(e => new LiveDriverEntryDto
                {
                    DriverId = e.DriverID,
                    DriverName = e.DriverName,
                    DialIn = e.DialIn
                })
                .ToList();
        }

        private LiveRaceUpdateDto BuildLiveRaceUpdateDto()
        {
            if (_session == null || _engine == null) return null;
            if (_session.EventDate == default) return null;

            var eventName = string.IsNullOrWhiteSpace(_session.EventName) ? "Unsaved class" : _session.EventName;
            var eventDate = _session.EventDate.ToString("yyyy-MM-dd");

            string currentRound;
            try
            {
                currentRound = GetActiveRoundLabel();
            }
            catch
            {
                currentRound = null;
            }

            if (string.IsNullOrWhiteSpace(currentRound))
                currentRound = _revealedRounds.LastOrDefault();

            var nextMatch = ApplyRaceOrder(
                    EngineGetMatches(_engine).Where(m => _revealedRounds.Contains(m.RoundLabel) && !m.HasResult))
                .FirstOrDefault();

            string nextUp = string.Empty;
            if (nextMatch != null)
            {
                GetLaneAdjustedNames(nextMatch, out var leftName, out var rightName);
                nextUp = leftName + " vs " + rightName;
            }

            var allMatches = CollectAllRevealedMatchesAcrossPhases();

            var matches = allMatches
                .Select(m =>
                {
                    GetLaneAdjustedNames(m, out var leftName, out var rightName);
                    GetLaneAdjustedDrivers(m, out var leftDriver, out var rightDriver);
                    return new LiveMatchDto
                    {
                        RoundLabel = m.RoundLabel,
                        Driver1 = m.Driver1?.Name ?? "BYE",
                        Driver2 = m.Driver2?.Name ?? "BYE",
                        LeftDriver = leftName,
                        RightDriver = rightName,
                        LeftDriverId = leftDriver?.Id ?? 0,
                        RightDriverId = rightDriver?.Id ?? 0,
                        WinnerName = _matchResult.HasResult(m.MatchId) ? _matchResult.GetWinner(m.MatchId)?.Name : null,
                        LeftDriverDialIn  = GetDriverDialIn(leftDriver?.Id ?? -1),
                        RightDriverDialIn = GetDriverDialIn(rightDriver?.Id ?? -1)
                    };
                })
                .ToList();

            var winners = allMatches
                .Where(m => _matchResult.HasResult(m.MatchId))
                .Select(m => new LiveWinnerDto
                {
                    RoundLabel = m.RoundLabel,
                    WinnerName = _matchResult.GetWinner(m.MatchId)?.Name,
                    LoserName = _matchResult.GetLoser(m.MatchId)?.Name
                })
                .Where(w => w.WinnerName != null && w.LoserName != null)
                .ToList();

            string rrStandings = null;
            if (string.Equals(_session.RaceType, RaceTypes.RoundRobin, StringComparison.OrdinalIgnoreCase))
            {
                var rr = _engine as RoundRobinEngineAdapter;
                if (rr != null)
                {
                    if (_rrStandingsCardCache == null)
                        _rrStandingsCardCache = RoundRobinScorecardLogger.BuildScorecard(rr, _matchResult);
                    rrStandings = _rrStandingsCardCache;
                }
            }

            return new LiveRaceUpdateDto
            {
                EventId = _session.EventId.ToString("N"),
                EventName = eventName,
                EventDate = eventDate,
                ClassType = _session.ClassType,
                RaceType = _session.RaceType,
                CurrentRound = currentRound,
                NextUp = nextUp,
                Drivers = BuildDriverEntryDtos(),
                Matches = matches,
                Winners = winners,
                RRStandings = rrStandings,
                DialInLocked = _dialInLocked
            };
        }

        /// <summary>
        /// Pushes the current live state if a bracket exists, otherwise a roster-only
        /// "initial" state so the class still appears on the live site before any bracket
        /// is generated. Called when the race window opens so every class of an event is
        /// visible the moment the event starts, regardless of which classes have brackets.
        /// </summary>
        public void BroadcastLiveSnapshot(string reason)
        {
            if (_engine != null)
                QueueLiveUpdate(reason);
            else
                BroadcastInitialState(reason);
        }

        /// <summary>
        /// Sends a minimal state for this class (event/class identity, no matches) so the
        /// class shows up on the live site before its bracket is generated. Built from the
        /// session alone because the engine does not exist until the first bracket.
        /// </summary>
        private void BroadcastInitialState(string reason)
        {
            try
            {
                if (!AppSettings.LiveBroadcastEnabled)
                {
                    Logger.Debug("[LIVE][SKIP] reason=" + reason + " disabled=true");
                    return;
                }
                if (_session == null || _session.EventDate == default)
                {
                    Logger.Log("[LIVE][SKIP] reason=" + reason + " initialState session/date invalid");
                    return;
                }

                var eventName = string.IsNullOrWhiteSpace(_session.EventName) ? "Unsaved class" : _session.EventName;
                var dto = new LiveRaceUpdateDto
                {
                    EventId      = _session.EventId.ToString("N"),
                    EventName    = eventName,
                    EventDate    = _session.EventDate.ToString("yyyy-MM-dd"),
                    ClassType    = _session.ClassType,
                    RaceType     = _session.RaceType,
                    CurrentRound = string.Empty,
                    NextUp       = string.Empty,
                    Drivers      = BuildDriverEntryDtos(),
                    Matches      = new List<LiveMatchDto>(),
                    Winners      = new List<LiveWinnerDto>(),
                    RRStandings  = null,
                    DialInLocked = false
                };

                Logger.Log("[LIVE][BUILD] reason=" + reason + " initialState class=" +
                           (string.IsNullOrWhiteSpace(dto.ClassType) ? "(none)" : dto.ClassType));
                _ = _liveApiClient.SendAsync(dto);
            }
            catch (Exception ex)
            {
                Logger.Log("[LIVE][SKIP] reason=" + reason + " initialState exception=" + ex.Message);
            }
        }

        private void QueueLiveUpdate(string reason)
        {
            try
            {
                if (!AppSettings.LiveBroadcastEnabled)
                {
                    Logger.Debug("[LIVE][SKIP] reason=" + reason + " disabled=true");
                    return;
                }

                var dto = BuildLiveRaceUpdateDto();
                if (dto == null)
                {
                    Logger.Log("[LIVE][SKIP] reason=" + reason + " dto=null");
                    return;
                }

                if (string.IsNullOrWhiteSpace(dto.EventDate) ||
                    string.IsNullOrWhiteSpace(dto.CurrentRound) ||
                    dto.Matches == null ||
                    dto.Matches.Count == 0)
                {
                    Logger.Log(
                        "[LIVE][SKIP] reason=" + reason +
                        " invalidState eventDate=" + (!string.IsNullOrWhiteSpace(dto.EventDate)) +
                        " currentRound=" + (!string.IsNullOrWhiteSpace(dto.CurrentRound)) +
                        " matches=" + (dto.Matches?.Count ?? 0));
                    return;
                }

                Logger.Log(
                    "[LIVE][BUILD] reason=" + reason +
                    " currentRound=" + dto.CurrentRound +
                    " nextUp=" + (string.IsNullOrWhiteSpace(dto.NextUp) ? "(none)" : dto.NextUp) +
                    " matches=" + dto.Matches.Count);

                _ = _liveApiClient.SendAsync(dto);
            }
            catch (Exception ex)
            {
                Logger.Log("[LIVE][SKIP] reason=" + reason + " exception=" + ex.Message);
            }
        }
    }
}
