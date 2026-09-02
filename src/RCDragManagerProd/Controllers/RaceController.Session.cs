// RaceController.Session.cs
using System;
using System.Collections.Generic;
using System.Linq;

using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Controllers
{
    public partial class RaceController
    {
        private readonly LaneFairnessManager laneFairness = new LaneFairnessManager();

        public void Reset()
        {
            _engine = null;
            _losersEngine = null;

            _inLosersPhase = false;
            _finalsPending = false;
            FinalsPendingReason = null;
            FinalsPendingWildcardName = null;
            _pendingFinalsRanking = null;
            _tournamentClosed = false;

            _revealedRounds.Clear();
            _activeRound = null;
            _winners.Clear();
            ClearDeferrals();

            _matchResult.Clear();
            _rrMatchesSnapshot = null;
            _rrRoundOrderSnapshot = null;
            _rrTop3 = null;
            _rrCompletionAnnounced = false;
            _buybackChampionOverride = null;
            _rrStandingsCardCache = null;
            _rrLoggedRounds.Clear();

            // Reset returns the console to pre-bracket setup. A lock from the
            // previous round must not block dial-in entry for the new race.
            UnlockDialIn();
            laneFairness.Reset();

            if (_session != null)
            {
                _session.RaceType = string.Empty;
                _session.Resume = null;
                _session.SavedResults?.Clear();
                _session.SavedRevealedRounds?.Clear();
                _session.ResultsArchive = new RaceResultsArchive();
            }

            BracketRedrawn?.Invoke(Array.Empty<PairingRow>());
            WinnersUpdated?.Invoke(Array.Empty<WinnerRow>());
            NextMatchReady?.Invoke(null);
            CanAdvanceChanged?.Invoke(false);
            CanPickWinnerChanged?.Invoke(false);
            CanDeferChanged?.Invoke(false);

            Logger.Log("[RESET] Controller cleared — ready for new class.");
        }

        /// <summary>Discards a generated bracket only while it has no results.</summary>
        public bool TryDiscardUnrunBracket()
        {
            if (_engine == null) return true;
            if (IsCompleted || HasRaceRun)
            {
                Logger.Log("[RESET][REJECT] Roster edit requested after a race result was recorded.");
                return false;
            }

            var configuredRaceType = !string.IsNullOrWhiteSpace(_session?.OriginalRaceType)
                ? _session.OriginalRaceType
                : _session?.RaceType;

            Reset();

            if (_session != null)
                _session.RaceType = configuredRaceType ?? string.Empty;

            Logger.Log("[RESET] Unrun bracket discarded — roster editing re-enabled.");
            return true;
        }


        public void SetBuybackDrivers(List<Driver> drivers)
        {
            if (drivers == null || drivers.Count < 2)
            {
                Logger.Log($"[CTRL] SetBuybackDrivers: invalid list — count = {drivers?.Count ?? 0}");
                return;
            }

            _session.BuybackDrivers = new List<Driver>(drivers);
            _inLosersPhase = true;

            Logger.Log($"[CTRL] Buy-back drivers stored: {_session.BuybackDrivers.Count} → {string.Join(", ", _session.BuybackDrivers.Select(d => d.Name))}");
        }
    }
}
