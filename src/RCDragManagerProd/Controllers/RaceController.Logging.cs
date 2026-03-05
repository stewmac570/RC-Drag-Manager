// RaceController.Logging.cs
using System.Linq;

using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.RoundRobinMode;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Controllers
{
    public partial class RaceController
    {
        // DEBUG helper – prints one-line snapshot of key state
        private void LogEngineSnapshot(string context)
        {
            Logger.Log($"[SNAP] {context}  |  _engine={_engine?.GetType().Name ?? "null"}  |  _losersEngine={_losersEngine?.GetType().Name ?? "null"}  |  revealedRounds={string.Join(",", _revealedRounds)}");
        }

        // Log per-completed-round standings while in RR
        private void TryLogCompletedRound(RoundRobinEngineAdapter rr)
        {
            var rounds = EngineGetRoundOrder(_engine).ToList();
            var matches = EngineGetMatches(_engine).ToList();

            foreach (var r in rounds)
            {
                if (_rrLoggedRounds.Contains(r)) continue;

                bool allResolved = matches
                    .Where(m => m.RoundLabel == r)
                    .All(m => _matchResult.HasResult(m.MatchId));

                if (!allResolved) break;

                Logger.Log($"[RR-SCORE] === {r} complete — standings so far ===");
                RoundRobinScorecardLogger.Log(rr, _matchResult);
                _rrLoggedRounds.Add(r);
            }
        }
    }
}
