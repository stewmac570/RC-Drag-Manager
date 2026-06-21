// RaceController.Persistence.cs
using System;
using System.Collections.Generic;
using System.Linq;

using RCDragManagerProd.Domain;
using RCDragManagerProd.RaceEngines;
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

                    var roundOrder = _engine.GetRoundOrder() ?? Array.Empty<string>();
                    if (roundOrder.Any(r => RoundLabels.Normalize(r).StartsWith("RR", StringComparison.OrdinalIgnoreCase)))
                        _session.RaceType = RaceTypes.RoundRobin;
                    else if (roundOrder.Any(r => RoundLabels.Normalize(r).StartsWith("LB-", StringComparison.OrdinalIgnoreCase)))
                        _session.RaceType = RaceTypes.LosersBracket;
                    else
                        _session.RaceType = RaceTypes.Finals;

                }

                if (_session.Drivers == null && _drivers != null)
                    _session.Drivers = new List<Driver>(_drivers);

                if (_drivers != null)
                {
                    lock (_dialInLock)
                    {
                        var existingByDriverId = (_session.DriverEntries ?? new List<RaceSessionDriverEntry>())
                            .Where(e => e != null)
                            .GroupBy(e => e.DriverID)
                            .ToDictionary(g => g.Key, g => g.First());

                        var synchronizedEntries = new List<RaceSessionDriverEntry>(_drivers.Count);
                        foreach (var d in _drivers)
                        {
                            if (d == null) continue;

                            if (!existingByDriverId.TryGetValue(d.Id, out var entry))
                                entry = new RaceSessionDriverEntry { DriverID = d.Id };

                            // The controller owns identity/qualifying time. The session
                            // entry owns race metadata such as DialIn, car, class and seed,
                            // so reuse the entry instead of destructively rebuilding it.
                            entry.DriverName = d.Name;
                            entry.QualifyingTime = d.QualTime;
                            synchronizedEntries.Add(entry);
                        }

                        _session.DriverEntries = synchronizedEntries;
                    }
                }

                CaptureCurrentResultSnapshot();
                CaptureResumeSnapshot();

                Logger.Log($"[SAVE] results={list.Count}, rounds={_session.SavedRevealedRounds.Count}, type='{_session.RaceType}'");
            }
            catch (Exception ex)
            {
                Logger.Log($"[SAVE][ERROR] {ex}");
            }
        }

        // Captures the live bracket structure + transient phase state so an interrupted
        // event can be reconstructed on load (see RestoreFromSave). SavedResults already
        // holds the winners/losers; this adds the STRUCTURE the old save path dropped.
        private void CaptureResumeSnapshot()
        {
            if (_engine == null)
            {
                // No bracket generated yet — nothing to resume into. Leave any prior
                // snapshot untouched (a re-save before generation shouldn't wipe it).
                return;
            }

            var snap = new ResumeSnapshot
            {
                OriginalRaceType = string.IsNullOrWhiteSpace(_session.OriginalRaceType)
                    ? _session.RaceType
                    : _session.OriginalRaceType,
                CurrentPhase = InferResumePhase(),
                ActiveRound = _activeRound,
                InLosersPhase = _inLosersPhase,
                FinalsPending = _finalsPending,
                BuybackChampionOverrideId = _buybackChampionOverride?.Id,
                RrTop3Ids = _rrTop3?.Select(d => d.Id).ToList() ?? new List<int>(),
                MainMatches = EngineGetMatches(_engine).Select(ToSavedMatch).ToList(),
                RrSnapshotMatches = _rrMatchesSnapshot?.Select(ToSavedMatch).ToList() ?? new List<SavedMatch>(),
                RrSnapshotRoundOrder = _rrRoundOrderSnapshot?.ToList() ?? new List<string>()
            };

            // Persist the LB structure whenever a losers engine exists — including the
            // in-LB-phase case where _engine IS the losers engine (so restore is unambiguous).
            if (_losersEngine != null)
                snap.LosersMatches = EngineGetMatches(_losersEngine).Select(ToSavedMatch).ToList();

            _session.OriginalRaceType = snap.OriginalRaceType;
            _session.Resume = snap;

            Logger.Log($"[SAVE][RESUME] phase='{snap.CurrentPhase}', original='{snap.OriginalRaceType}', " +
                       $"main={snap.MainMatches.Count}, losers={snap.LosersMatches.Count}, " +
                       $"rrTop3={snap.RrTop3Ids.Count}, activeRound='{snap.ActiveRound ?? "-"}', " +
                       $"inLosers={snap.InLosersPhase}, finalsPending={snap.FinalsPending}");
        }

        private string InferResumePhase()
        {
            var rt = (_session.RaceType ?? string.Empty).Trim();
            if (string.Equals(rt, RaceTypes.Finals, StringComparison.OrdinalIgnoreCase))
                return "Finals";
            if (_inLosersPhase || string.Equals(rt, RaceTypes.LosersBracket, StringComparison.OrdinalIgnoreCase))
                return "LosersBracket";
            return "Main";
        }

        // Instance (not static) so the inline result is read from the retained
        // _matchResult — which still holds a phase's winners even after its matches
        // have left the live engine (e.g. RR results during the LB/Finals phases).
        //
        // Gate on m.HasResult (the OWNING engine's own verdict) before trusting
        // _matchResult: match-id spaces overlap across engines (RR 1..9 collide with
        // the finals Pro-Ladder SF/F ids), so a blind _matchResult.GetWinner(id) lookup
        // would stamp an unplayed finals match with a stale RR winner — which then
        // replays on resume and marks the match resolved when it should still be pending.
        private SavedMatch ToSavedMatch(EngineMatch m)
        {
            var w = m.HasResult ? _matchResult.GetWinner(m.MatchId) : null;
            var l = m.HasResult ? _matchResult.GetLoser(m.MatchId) : null;
            return new SavedMatch
            {
                MatchId = m.MatchId,
                Driver1Id = m.Driver1?.Id,
                Driver2Id = m.Driver2?.Id,
                RoundLabel = m.RoundLabel,
                FromMatch1 = m.FromMatch1,
                FromMatch2 = m.FromMatch2,
                WinnerId = w?.Id,
                LoserId = l?.Id
            };
        }
    }
}
