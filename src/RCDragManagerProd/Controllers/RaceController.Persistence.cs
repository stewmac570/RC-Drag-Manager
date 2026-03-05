// RaceController.Persistence.cs
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
        public void SaveSession()
        {
            try
            {
                if (_session == null)
                {
                    Logger.Log("[SAVE] No active session — skipping save.");
                    return;
                }

                var mainMatches = _engine != null ? EngineGetMatches(_engine) : Enumerable.Empty<EngineMatch>();
                var lbMatches = _losersEngine != null ? EngineGetMatches(_losersEngine) : Enumerable.Empty<EngineMatch>();
                var allMatches = mainMatches.Concat(lbMatches);
                var seenMatchIds = new HashSet<int>();
                int beforeCount = 0;
                bool overlapDetected = ReferenceEquals(_engine, _losersEngine);

                var list = new List<RCDragManagerProd.Domain.MatchResultSave>();
                foreach (var m in allMatches)
                {
                    beforeCount++;
                    if (!seenMatchIds.Add(m.MatchId))
                    {
                        overlapDetected = true;
                        continue;
                    }

                    var w = _matchResult.GetWinner(m.MatchId);
                    var l = _matchResult.GetLoser(m.MatchId);

                    if (m.HasResult || w != null || l != null)
                    {
                        list.Add(new RCDragManagerProd.Domain.MatchResultSave
                        {
                            MatchId = m.MatchId,
                            WinnerDriverId = w?.Id ?? -1,
                            LoserDriverId = l?.Id ?? -1
                        });
                    }
                }
                Logger.Log($"[SAVE] Dedup matches: beforeCount={beforeCount}, afterCount={seenMatchIds.Count}, overlapDetected={overlapDetected}");

                _session.SavedResults = list;
                _session.SavedRevealedRounds = _revealedRounds?.ToList() ?? new List<string>();

                if (string.IsNullOrWhiteSpace(_session.RaceType) && _engine != null)
                {
                    _session.RaceType = _engine.GetType().Name;
                }

                if (_session.Drivers == null && _drivers != null)
                    _session.Drivers = new List<Driver>(_drivers);

                Logger.Log($"[SAVE] results={list.Count}, rounds={_session.SavedRevealedRounds.Count}, type='{_session.RaceType}'");
            }
            catch (Exception ex)
            {
                Logger.Log($"[SAVE][ERROR] {ex}");
            }
        }
    }
}
