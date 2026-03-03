// RaceController.RoundFlow.Finals.cs
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
        public void InjectFinal4Bracket()
        {
            Logger.Log("🏁 Injecting Final-4 Pro Ladder bracket…");

            if (_rrTop3 == null || _rrTop3.Count != 3)
            {
                Logger.Log("❌ Cannot inject finals — Top-3 snapshot missing or incomplete");
                return;
            }

            Driver lbChampion = null;

            if (_buybackChampionOverride != null)
            {
                lbChampion = _buybackChampionOverride;
                Logger.Log($"[FINALS] Using auto wildcard (no LB): {lbChampion.Name}");
                _buybackChampionOverride = null;
            }
            else if (_losersEngine is RandomEngineAdapter adapter)
            {
                lbChampion = adapter.GetWinner();
                if (lbChampion == null)
                {
                    Logger.Log("❌ Cannot inject finals — Losers bracket champion not found");
                    return;
                }
            }
            else
            {
                Logger.Log("❌ Cannot inject finals — no losers bracket and no wildcard override set");
                return;
            }

            _session.RaceType = "Finals";
            _inLosersPhase = false;
            Logger.Log("[FINALS] Session race type set to 'Finals' (LB phase cleared).");

            var finalists = new List<Driver>(4);
            finalists.AddRange(_rrTop3);
            finalists.Add(lbChampion);
            Logger.Log($"[PRO] Final-4 = {string.Join(", ", finalists.Select(d => d.Name))}");

            var proAdapter = new ProLadderEngineAdapter();
            proAdapter.LoadDrivers(finalists);
            proAdapter.GenerateBracket();
            _engine = proAdapter;

            var finalMatches = proAdapter.GetMatches();
            Logger.Log($"[PRO] Matches generated: {finalMatches.Count}");
            foreach (var match in finalMatches)
            {
                Logger.Log($"[PRO] Match {match.MatchId}: Round={match.RoundLabel}, Driver1={(match.Driver1?.Name ?? "BYE")}, Driver2={(match.Driver2?.Name ?? "BYE")}");
            }

            var preserveLb = _revealedRounds
                .Where(r => r.StartsWith("Losers Bracket", StringComparison.OrdinalIgnoreCase))
                .ToList();

            _revealedRounds.Clear();
            foreach (var r in preserveLb) _revealedRounds.Add(r);
            _revealedRounds.Add("SF");

            Logger.Log($"🎯 Final-4 revealedRounds set to: {string.Join(",", _revealedRounds)} (Final will be revealed on Next Round)");

            var rows = BuildCurrentBracketRows();
            BracketRedrawn?.Invoke(rows);
            Logger.Log($"🖼️  Final-4 bracket redrawn with {rows.Count} rows");

            PushNextMatch();
            Logger.Log("🔔 Final-4 first match pushed to UI");

            PushAdvanceState();
            Logger.Log("[FINALS] Advance state evaluated after SF reveal (F gated).");
        }

        public void StartFinals()
        {
            if (!_finalsPending)
            {
                Logger.Log("[CTRL] StartFinals called but finals are not pending.");
                return;
            }

            if (_rrTop3 == null || _rrTop3.Count < 3)
            {
                Logger.Log("❌ Finals cannot start — RR Top-3 snapshot missing. Keeping Finals pending.");
                CanStartFinalsChanged?.Invoke(true);
                return;
            }

            Logger.Log($"[FINALS] Start request accepted. rrTop3=[{string.Join(", ", _rrTop3.Select(d => d.Name))}] losersEngine={_losersEngine?.GetType().Name ?? "null"} engine={_engine?.GetType().Name ?? "null"} revealed=[{string.Join(",", _revealedRounds)}]");

            Logger.Log("🏁 Starting Finals — injecting Final-4 Pro Ladder bracket…");
            InjectFinal4Bracket();

            _finalsPending = false;
            CanStartFinalsChanged?.Invoke(false);
            Logger.Log("[FINALS] Finals gate lowered (button disabled).");
        }

        public void StartFinalsTop3NoBuyback()
        {
            Logger.Log("[FINALS][NOBUYBACK] Starting Finals with Top-3 only (no buyback entries).");

            if (_rrTop3 == null || _rrTop3.Count != 3)
            {
                Logger.Log("❌ [FINALS][NOBUYBACK] Top-3 snapshot missing or incomplete — aborting.");
                return;
            }

            _inLosersPhase = false;
            _session.RaceType = "Finals";
            _finalsPending = false;
            CanStartFinalsChanged?.Invoke(false);
            Logger.Log("[FINALS][NOBUYBACK] Session race type set to 'Finals'. LB phase cleared. Finals gate lowered.");

            var finalists = new List<Driver>(_rrTop3);
            Logger.Log($"[PRO] Final-3 = {string.Join(", ", finalists.Select(d => d.Name))}");

            var proAdapter = new ProLadderEngineAdapter();
            proAdapter.LoadDrivers(finalists);
            proAdapter.GenerateBracket();
            _engine = proAdapter;

            var finalMatches = proAdapter.GetMatches();
            Logger.Log($"[PRO] Matches generated: {finalMatches.Count}");
            foreach (var match in finalMatches)
            {
                Logger.Log($"[PRO] Match {match.MatchId}: Round={match.RoundLabel}, Driver1={(match.Driver1?.Name ?? "BYE")}, Driver2={(match.Driver2?.Name ?? "BYE")}");
            }

            _revealedRounds.Clear();
            _revealedRounds.Add("SF");
            Logger.Log($"🎯 [FINALS][NOBUYBACK] Revealed rounds set to: {string.Join(",", _revealedRounds)}");

            var rows = BuildCurrentBracketRows();
            BracketRedrawn?.Invoke(rows);
            Logger.Log($"🖼️  [FINALS][NOBUYBACK] Bracket redrawn with {rows.Count} rows");

            PushNextMatch();
            Logger.Log("🔔 [FINALS][NOBUYBACK] First Finals match pushed to UI");

            PushAdvanceState();
            Logger.Log("[FINALS][NOBUYBACK] Advance state evaluated.");
        }
        // ─────────────────────────────────────────────────────────────
        // QMDRA Finals Injection — ALL drivers advance (no buyback)
        // Seed order MUST follow Round Robin ranking order
        // ─────────────────────────────────────────────────────────────
        private void InjectFinalsAllAdvance(List<Driver> rankedDrivers)
        {
            if (rankedDrivers == null || rankedDrivers.Count < 2)
            {
                Logger.Log("⛔ InjectFinalsAllAdvance aborted — rankedDrivers invalid or < 2.");
                return;
            }

            Logger.Log($"[FINALS][QMDRA] Injecting finals — Drivers={rankedDrivers.Count}");
            Logger.Log("[FINALS][QMDRA] Seed order: " +
                string.Join(", ", rankedDrivers.Select(d => d.Name)));

            // Switch session to Finals
            _session.RaceType = "Finals";
            _inLosersPhase = false;
            _finalsPending = false;

            // Reset state
            _revealedRounds.Clear();
            _winners.Clear();

            // Create Pro Ladder engine
            var pro = new ProLadderEngineAdapter();
            _engine = pro;

            _engine.LoadDrivers(rankedDrivers);
            Logger.Log("[FINALS][QMDRA] Drivers loaded into ProLadder engine.");

            _engine.GenerateBracket();
            Logger.Log("[FINALS][QMDRA] ProLadder bracket generated.");

            // Reveal FIRST round only (do NOT hardcode round name)
            var firstRound = _engine.GetRoundOrder().FirstOrDefault();
            if (string.IsNullOrEmpty(firstRound))
            {
                Logger.Log("⛔ InjectFinalsAllAdvance failed — no rounds returned from ProLadder.");
                return;
            }

            _revealedRounds.Add(firstRound);
            Logger.Log($"[FINALS][QMDRA] First round revealed: {firstRound}");

            // Push UI + flow
            PushFullRefresh();
            PushNextMatch();
            PushAdvanceState();
        }

    }
}
