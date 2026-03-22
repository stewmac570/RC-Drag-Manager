using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using RCDragManagerProd.Integration;
using RCDragManagerProd.Logging;
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

            var rows = BuildCurrentBracketRows() ?? Array.Empty<PairingRow>();
            var matches = rows
                .Where(r => !r.IsHeader)
                .Select(r => new LiveMatchDto
                {
                    Driver1 = string.IsNullOrWhiteSpace(r.Driver1) ? "BYE" : r.Driver1,
                    Driver2 = string.IsNullOrWhiteSpace(r.Driver2) ? "BYE" : r.Driver2
                })
                .ToList();

            return new LiveRaceUpdateDto
            {
                EventName = eventName,
                EventDate = eventDate,
                CurrentRound = currentRound,
                NextUp = nextUp,
                Matches = matches
            };
        }

        private void QueueLiveUpdate(string reason)
        {
            try
            {
                var enabledText = ConfigurationManager.AppSettings["LiveUpdateEnabled"];
                if (bool.TryParse(enabledText, out var enabled) && !enabled)
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
