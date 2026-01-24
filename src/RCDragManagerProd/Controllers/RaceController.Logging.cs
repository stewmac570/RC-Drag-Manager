// RaceController.Logging.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;            // still used for a couple of info popups

using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;
using RCDragManagerProd.RaceEngines;
using RCDragManagerProd.RandomMode;
using RCDragManagerProd.RoundRobinMode;
using RCDragManagerProd.UI.Forms;
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
            var rounds = rr.GetRoundOrder().ToList();
            var matches = rr.GetMatches().ToList();

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
