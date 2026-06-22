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
