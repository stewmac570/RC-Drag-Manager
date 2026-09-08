using System.Linq;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Controllers
{
    public sealed partial class RaceController
    {
        /// <summary>
        /// Returns true if there are unresolved non-BYE matches in the currently
        /// active round. In RR mode (all rounds pre-revealed) uses _activeRound so
        /// only the pace-gated round is checked, not future rounds.
        /// Used by MultiClassRaceForm to enforce tab switching.
        /// </summary>
        public bool HasPendingMatchesInCurrentRound()
        {
            // Use _activeRound when set (RR pre-reveal mode); fall back to _revealedRounds.
            var currentMatches = EngineGetMatches(_engine)
                .Where(m => _activeRound != null
                                ? string.Equals(m.RoundLabel, _activeRound, System.StringComparison.OrdinalIgnoreCase)
                                : _revealedRounds.Contains(m.RoundLabel))
                .ToList();

            return currentMatches.Any(m =>
                !ByePolicy.IsBye(m.Driver1) &&
                !ByePolicy.IsBye(m.Driver2) &&
                !_matchResult.HasResult(m.MatchId));
        }

        /// <summary>
        /// Returns true if all RR matches are resolved (all rounds complete) OR
        /// if the session has already advanced past the RR phase.
        /// Used by MultiClassRaceForm for LB gate evaluation.
        /// </summary>
        public bool IsRrComplete()
        {
            if (!RaceTypes.IsRoundRobinFormat(_session.RaceType)) return true;

            var allMatches = EngineGetMatches(_engine);
            return allMatches.All(m =>
                _matchResult.HasResult(m.MatchId) ||
                ByePolicy.IsBye(m.Driver1) ||
                ByePolicy.IsBye(m.Driver2));
        }

        private int OwnerDriverId(Driver competitor)
        {
            if (competitor == null) return 0;
            if (!string.Equals(_session?.OriginalRaceType ?? _session?.RaceType,
                               RaceTypes.MultiCarRoundRobin,
                               System.StringComparison.OrdinalIgnoreCase))
                return competitor.Id;

            return _session.DriverEntries
                ?.FirstOrDefault(entry => entry != null && entry.RaceEntryId == competitor.Id)
                ?.DriverID ?? 0;
        }

        private System.Collections.Generic.IReadOnlyList<(int WinnerId, int LoserId)> GetStatResults()
        {
            return _matchResult.GetAllResults()
                .Select(result => (WinnerId: OwnerDriverId(new Driver { Id = result.WinnerId }),
                                   LoserId: OwnerDriverId(new Driver { Id = result.LoserId })))
                .Where(result => result.WinnerId > 0 && result.LoserId > 0 &&
                                 result.WinnerId != result.LoserId)
                .ToList();
        }
    }
}
