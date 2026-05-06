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

        private LiveRaceUpdateDto BuildLiveRaceUpdateDto()
        {
            if (_session == null || _engine == null) return null;
            if (_session.EventDate == default) return null;

            var eventName = string.IsNullOrWhiteSpace(_session.EventName) ? "Quick Session" : _session.EventName;
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

            var nextMatch = EngineGetMatches(_engine)
                .Where(m => _revealedRounds.Contains(m.RoundLabel) && !m.HasResult)
                .OrderBy(m => m.MatchId)
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
                Matches = matches,
                Winners = winners,
                RRStandings = rrStandings,
                DialInLocked = _dialInLocked
            };
        }

        private void QueueLiveUpdate(string reason)
        {
            try
            {
                if (!AppSettings.LiveBroadcastEnabled)
                {
                    Logger.Log("[LIVE][SKIP] reason=" + reason + " disabled=true");
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
