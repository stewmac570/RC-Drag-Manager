using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RCDragManagerProd.Config;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Controllers
{
    public partial class RaceController
    {
        private bool _dialInLocked = false;
        private Timer _dialInPollTimer;

        public bool DialInLocked => _dialInLocked;

        public double? GetDriverDialIn(int driverId)
        {
            if (_session == null) return null;
            return _session.DriverEntries
                ?.FirstOrDefault(e => e.DriverID == driverId)
                ?.DialIn;
        }

        public void UpdateDriverDialIn(int driverId, double? dialIn)
        {
            if (_session == null) return;
            var entry = _session.DriverEntries?.FirstOrDefault(e => e.DriverID == driverId);
            if (entry == null) return;

            entry.DialIn = dialIn;
            Logger.Log($"[DIALIN] Updated driverId={driverId} dialIn={dialIn?.ToString("F3") ?? "null"}");
            QueueLiveUpdate("dialin-edit");
        }

        public void LockDialIn()
        {
            _dialInLocked = true;
            Logger.Log("[DIALIN] Locked");
        }

        public void UnlockDialIn()
        {
            _dialInLocked = false;
            Logger.Log("[DIALIN] Unlocked");
        }

        public void StartDialInPolling()
        {
            if (!AppSettings.LiveBroadcastEnabled) return;
            StopDialInPolling();
            _dialInPollTimer = new Timer(_ => PollDialInUpdatesAsync(), null, 10000, 15000);
            Logger.Log("[DIALIN] Poll timer started");
        }

        public void StopDialInPolling()
        {
            _dialInPollTimer?.Dispose();
            _dialInPollTimer = null;
        }

        private void PollDialInUpdatesAsync()
        {
            if (_dialInLocked) return;
            if (!AppSettings.LiveBroadcastEnabled) return;

            _ = PollAndApplyAsync();
        }

        private async System.Threading.Tasks.Task PollAndApplyAsync()
        {
            try
            {
                var updates = await _liveApiClient.GetDialInUpdatesAsync(_session?.EventId.ToString() ?? string.Empty).ConfigureAwait(false);
                if (updates == null || updates.Count == 0) return;

                bool changed = false;
                foreach (var kv in updates)
                {
                    if (_session?.DriverEntries == null) break;
                    var entry = _session.DriverEntries.FirstOrDefault(e => e.DriverID == kv.Key);
                    if (entry == null) continue;

                    if (entry.DialIn != kv.Value)
                    {
                        entry.DialIn = kv.Value;
                        Logger.Log($"[DIALIN][POLL] Applied driverId={kv.Key} dialIn={kv.Value?.ToString("F3") ?? "null"}");
                        changed = true;
                    }
                }

                if (changed)
                    QueueLiveUpdate("dialin-poll");
            }
            catch (Exception ex)
            {
                Logger.Log("[DIALIN][POLL][ERROR] " + ex.Message);
            }
        }
    }
}
