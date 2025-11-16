// RaceController.Session.cs
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
        public void Reset()
        {
            _engine = null;
            _losersEngine = null;

            _inLosersPhase = false;
            _finalsPending = false;
            _tournamentClosed = false;

            _revealedRounds.Clear();
            _winners.Clear();

            if (_session != null) _session.RaceType = string.Empty;

            BracketRedrawn?.Invoke(Array.Empty<PairingRow>());
            WinnersUpdated?.Invoke(Array.Empty<WinnerRow>());
            NextMatchReady?.Invoke(null);
            CanAdvanceChanged?.Invoke(false);
            CanPickWinnerChanged?.Invoke(false);

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
