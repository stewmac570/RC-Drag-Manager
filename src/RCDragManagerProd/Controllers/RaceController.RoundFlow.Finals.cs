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
            else
            {
                if (_losersEngine == null)
                {
                    Logger.Log("❌ Cannot inject finals — no losers bracket and no wildcard override set");
                    return;
                }

                lbChampion = TryResolveEngineChampion(_losersEngine, "LB");
                if (lbChampion == null)
                {
                    Logger.Log("❌ Cannot inject finals — Losers bracket champion not found");
                    return;
                }
            }

            _session.RaceType = "Finals";
            _inLosersPhase = false;
            _activeRound = null;   // RR active-round gate no longer applies in Finals
            Logger.Log("[FINALS] Session race type set to 'Finals' (LB phase cleared).");

            var finalists = new List<Driver>(4);
            finalists.AddRange(_rrTop3);
            finalists.Add(lbChampion);
            Logger.Log($"[PRO] Final-4 = {string.Join(", ", finalists.Select(d => d.Name))}");

            IRaceEngine proAdapter = new ProLadderEngineAdapter();
            EngineLoadDrivers(proAdapter, finalists);
            EngineGenerateBracket(proAdapter);
            _engine = proAdapter;

            var finalMatches = EngineGetMatches(_engine);
            Logger.Log($"[PRO] Matches generated: {finalMatches.Count}");
            foreach (var match in finalMatches)
            {
                Logger.Log($"[PRO] Match {match.MatchId}: Round={match.RoundLabel}, Driver1={(match.Driver1?.Name ?? "BYE")}, Driver2={(match.Driver2?.Name ?? "BYE")}");
            }

            var preserveLb = _revealedRounds
                .Where(r => string.Equals(RoundLabels.Normalize(r), "LB-F", StringComparison.OrdinalIgnoreCase) ||
                            RoundLabels.Normalize(r).StartsWith("LB-R", StringComparison.OrdinalIgnoreCase))
                .ToList();

            _revealedRounds.Clear();
            foreach (var r in preserveLb) _revealedRounds.Add(r);
            var firstRound = EngineGetRoundOrder(_engine).FirstOrDefault();
            if (!string.IsNullOrEmpty(firstRound))
                _revealedRounds.Add(firstRound);

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
            _activeRound = null;   // RR active-round gate cleared on Finals injection
            CanStartFinalsChanged?.Invoke(false);
            Logger.Log("[FINALS][NOBUYBACK] Session race type set to 'Finals'. LB phase cleared. Finals gate lowered.");

            var finalists = new List<Driver>(_rrTop3);
            Logger.Log($"[PRO] Final-3 = {string.Join(", ", finalists.Select(d => d.Name))}");

            IRaceEngine proAdapter = new ProLadderEngineAdapter();
            EngineLoadDrivers(proAdapter, finalists);
            EngineGenerateBracket(proAdapter);
            _engine = proAdapter;

            var finalMatches = EngineGetMatches(_engine);
            Logger.Log($"[PRO] Matches generated: {finalMatches.Count}");
            foreach (var match in finalMatches)
            {
                Logger.Log($"[PRO] Match {match.MatchId}: Round={match.RoundLabel}, Driver1={(match.Driver1?.Name ?? "BYE")}, Driver2={(match.Driver2?.Name ?? "BYE")}");
            }

            _revealedRounds.Clear();
            var firstRoundNoBuyback = EngineGetRoundOrder(_engine).FirstOrDefault();
            if (!string.IsNullOrEmpty(firstRoundNoBuyback))
                _revealedRounds.Add(firstRoundNoBuyback);
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
            _activeRound = null;   // RR active-round gate cleared on Finals injection

            // Reset state
            _revealedRounds.Clear();
            _winners.Clear();

            // Create Pro Ladder engine
            var pro = new ProLadderEngineAdapter();
            _engine = pro;

            EngineLoadDrivers(_engine, rankedDrivers);
            Logger.Log("[FINALS][QMDRA] Drivers loaded into ProLadder engine.");

            EngineGenerateBracket(_engine);
            Logger.Log("[FINALS][QMDRA] ProLadder bracket generated.");

            // Reveal FIRST round only (do NOT hardcode round name)
            var firstRound = EngineGetRoundOrder(_engine).FirstOrDefault();
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

        private Driver TryResolveEngineChampion(IRaceEngine engine, string roundHint)
        {
            if (engine == null) return null;

            var matches = EngineGetMatches(engine, round: roundHint).ToList();
            if (matches.Count == 0) return null;

            var final = matches.FirstOrDefault(m =>
                            string.Equals(RoundLabels.Normalize(m.RoundLabel), "F", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(RoundLabels.Normalize(m.RoundLabel), "LB-F", StringComparison.OrdinalIgnoreCase))
                        ?? matches.OrderBy(m => RoundLabels.CompareKey(m.RoundLabel ?? string.Empty))
                                 .ThenBy(m => m.MatchId)
                                 .LastOrDefault();

            if (final == null) return null;

            if (!final.HasResult && !EngineHasWinner(engine, final.MatchId, final.RoundLabel))
                return null;

            return _matchResult.GetWinner(final.MatchId);
        }

    }
}
