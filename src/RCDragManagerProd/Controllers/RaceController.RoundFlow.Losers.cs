// RaceController.RoundFlow.Losers.cs
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
        // New, minimal wrapper: persist selection to session and funnel into StartLosersBracket
        public void GenerateLosersBracket(List<Driver> selectedDrivers)
        {
            Logger.Log("📦 GenerateLosersBracket wrapper called…");

            if (selectedDrivers == null || selectedDrivers.Count < 2)
            {
                Logger.Log("⚠️  Cannot generate LB — <2 drivers selected");
                return;
            }

            _session.BuybackDrivers = new List<Driver>(selectedDrivers);
            var names = string.Join(", ", _session.BuybackDrivers.Select(d => d.Name));
            Logger.Log($"🔒 Stored {_session.BuybackDrivers.Count} selected LB drivers → [{names}]");

            StartLosersBracket();
        }

        public void StartLosersBracket()
        {
            if (_session.BuybackDrivers == null || _session.BuybackDrivers.Count < 2)
            {
                Logger.Log("⚠️ Cannot start Losers Bracket — no drivers stored.");
                return;
            }

            var buyNames = string.Join(", ", _session.BuybackDrivers.Select(d => d.Name));
            Logger.Log($"📦 Starting Losers Bracket… drivers={_session.BuybackDrivers.Count} [{buyNames}]");
            Logger.Log($"[STATE] Before LB start → engine={_engine?.GetType().Name ?? "null"}, losersEngine={_losersEngine?.GetType().Name ?? "null"}, rrSnapshotMatches={_rrMatchesSnapshot?.Count.ToString() ?? "null"}, rrSnapshotRounds={_rrRoundOrderSnapshot?.Count.ToString() ?? "null"}");

            _session.PairingHistory ??= new HashSet<(int, int)>();

            if ((_rrMatchesSnapshot == null || _rrRoundOrderSnapshot == null) && _engine is RoundRobinEngineAdapter rrSnap)
            {
                _rrMatchesSnapshot = rrSnap.GetMatches().ToList();
                _rrRoundOrderSnapshot = rrSnap.GetRoundOrder().ToList();
                Logger.Log($"[RR] Fallback snapshot saved in StartLosersBracket: matches={_rrMatchesSnapshot.Count}, rounds={_rrRoundOrderSnapshot.Count}");
            }

            var lbMatches = LosersBracketBuilder.Build(
                _session.BuybackDrivers,
                _session.PairingHistory,
                1000);

            Logger.Log($"📊 LB matches generated: {lbMatches.Count}");

            if (lbMatches == null || lbMatches.Count == 0)
            {
                Logger.Log("⚠️ No LB matches generated — aborting Losers Bracket start.");
                return;
            }

            var lbEngine = new RandomEngineAdapter();
            lbEngine.LoadDrivers(_session.BuybackDrivers);
            lbEngine.InjectMatches(lbMatches);
            Logger.Log($"🛠️ Injected LB matches into RandomEngineAdapter (drivers={_session.BuybackDrivers.Count})");

            _losersEngine = lbEngine;
            _engine = lbEngine;
            _inLosersPhase = true;                 // key flag for finals injection
            _session.RaceType = "Losers Bracket";
            Logger.Log("[LB] Engine swapped to LB adapter; LB phase entered; session type='Losers Bracket'.");

            _revealedRounds.Clear();
            _revealedRounds.Add("Losers Bracket R1");
            Logger.Log("🎬 Revealed: Losers Bracket R1");

            var rows = BuildCurrentBracketRows();
            BracketRedrawn?.Invoke(rows);
            Logger.Log($"🖼️ BracketRedrawn (LB R1) with {rows.Count} rows.");

            PushNextMatch();
            Logger.Log("🔔 First LB match pushed to UI");

            PushAdvanceState();
            Logger.Log("[LB] Advance state evaluated after LB R1 reveal.");
        }
    }
}
