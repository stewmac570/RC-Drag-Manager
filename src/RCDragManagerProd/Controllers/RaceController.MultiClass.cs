using System.Linq;
using RCDragManagerProd.Domain;

namespace RCDragManagerProd.Controllers
{
    public sealed partial class RaceController
    {
        /// <summary>
        /// Returns true if there are unresolved non-BYE matches in the currently
        /// revealed round. Used by MultiClassRaceForm to enforce tab switching.
        /// </summary>
        public bool HasPendingMatchesInCurrentRound()
        {
            var visibleMatches = EngineGetMatches(_engine)
                .Where(m => _revealedRounds.Contains(m.RoundLabel))
                .ToList();

            return visibleMatches.Any(m =>
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
            if (_session.RaceType != "Round Robin") return true;

            var allMatches = EngineGetMatches(_engine);
            return allMatches.All(m =>
                _matchResult.HasResult(m.MatchId) ||
                ByePolicy.IsBye(m.Driver1) ||
                ByePolicy.IsBye(m.Driver2));
        }
    }
}
