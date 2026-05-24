using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RCDragManagerProd.Config;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Integration
{
    public sealed class LiveApiClient
    {
        private const string LiveUpdateUrl  = "https://stewmacrc.com/api/update";
        private const string LiveResetUrl   = "https://stewmacrc.com/api/reset";
        private const string DialInPollUrl  = "https://stewmacrc.com/api/dialin";

        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(3)
        };
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Per-key coalescing send channels. Each (eventId, classType) pair gets
        // its own serial worker so a slow push for one class never blocks another,
        // and only the latest pending state for a class is ever sent (drop-old /
        // latest-wins). Resets and updates for the same key are ordered FIFO so a
        // reset issued right before a bracket push cannot overtake it.
        private readonly object _channelsLock = new object();
        private readonly Dictionary<string, SendChannel> _channels =
            new Dictionary<string, SendChannel>();

        private SendChannel GetChannel(string eventId, string classType)
        {
            var key = (eventId ?? string.Empty) + "|" + (classType ?? string.Empty);
            lock (_channelsLock)
            {
                if (!_channels.TryGetValue(key, out var channel))
                {
                    channel = new SendChannel(this);
                    _channels[key] = channel;
                }
                return channel;
            }
        }

        public Task SendAsync(LiveRaceUpdateDto dto)
        {
            if (!AppSettings.LiveBroadcastEnabled)
            {
                Logger.Log("[LIVE][FAIL] Live updates disabled by config.");
                return Task.CompletedTask;
            }

            var payload = dto ?? new LiveRaceUpdateDto();
            GetChannel(payload.EventId, payload.ClassType).EnqueueUpdate(payload);
            return Task.CompletedTask;
        }

        public Task ResetAsync(string eventId, string classType, string eventName)
        {
            if (!AppSettings.LiveBroadcastEnabled) return Task.CompletedTask;
            GetChannel(eventId, classType).EnqueueReset(eventId, eventName);
            return Task.CompletedTask;
        }

        private async Task SendCoreAsync(LiveRaceUpdateDto dto)
        {
            try
            {
                var apiKey = ConfigurationManager.AppSettings["ApiKey"];
                var isNull = apiKey == null;
                var isEmpty = apiKey != null && apiKey.Length == 0;
                var isWhiteSpace = string.IsNullOrWhiteSpace(apiKey);
                var keyLength = apiKey?.Length ?? 0;
                Logger.Log("[LIVE][AUTH] X-API-KEY loaded from ApiKey: null=" + isNull + ", empty=" + isEmpty + ", whitespace=" + isWhiteSpace + ", length=" + keyLength);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Logger.Log("[LIVE][FAIL] Missing config key ApiKey.");
                    return;
                }

                var json = JsonSerializer.Serialize(dto ?? new LiveRaceUpdateDto(), JsonOptions);
                using (var req = new HttpRequestMessage(HttpMethod.Post, LiveUpdateUrl))
                {
                    req.Headers.Remove("X-API-KEY");
                    req.Headers.Add("X-API-KEY", apiKey);
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    if (AppSettings.LiveBroadcastDebugLogging)
                        Logger.Log("[LIVE][SEND][JSON] " + json);
                    Logger.Log($"[LIVE][SEND] eventId={dto?.EventId} class={dto?.ClassType} round={dto?.CurrentRound} matches={dto?.Matches?.Count ?? 0} POST {LiveUpdateUrl}");
                    using (var resp = await Http.SendAsync(req).ConfigureAwait(false))
                    {
                        if (resp.IsSuccessStatusCode)
                        {
                            Logger.Log("[LIVE][OK] Status=" + (int)resp.StatusCode);
                        }
                        else if ((int)resp.StatusCode == 400)
                        {
                            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                            Logger.Log("[LIVE][FAIL] Status=400 Body=" + body);
                        }
                        else
                        {
                            Logger.Log("[LIVE][FAIL] Status=" + (int)resp.StatusCode);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("[LIVE][FAIL] " + ex.Message);
            }
        }

        private async Task ResetCoreAsync(string eventId, string eventName)
        {
            try
            {
                var apiKey = ConfigurationManager.AppSettings["ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey)) return;

                var body = new { eventId, eventName };
                var json = JsonSerializer.Serialize(body, JsonOptions);
                using (var req = new HttpRequestMessage(HttpMethod.Post, LiveResetUrl))
                {
                    req.Headers.Add("X-API-KEY", apiKey);
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    Logger.Log($"[LIVE][RESET] eventId={eventId} eventName={eventName} POST {LiveResetUrl}");
                    using (var resp = await Http.SendAsync(req).ConfigureAwait(false))
                    {
                        Logger.Log("[LIVE][RESET] Status=" + (int)resp.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("[LIVE][RESET][FAIL] " + ex.Message);
            }
        }

        public async Task<Dictionary<int, double?>> GetDialInUpdatesAsync(string eventId)
        {
            try
            {
                var apiKey = ConfigurationManager.AppSettings["ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey)) return null;

                var url = DialInPollUrl + "?eventId=" + Uri.EscapeDataString(eventId ?? string.Empty);
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    req.Headers.Add("X-API-KEY", apiKey);
                    using (var resp = await Http.SendAsync(req).ConfigureAwait(false))
                    {
                        if (!resp.IsSuccessStatusCode)
                        {
                            Logger.Log("[DIALIN][POLL] Status=" + (int)resp.StatusCode);
                            return null;
                        }
                        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return JsonSerializer.Deserialize<Dictionary<int, double?>>(body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("[DIALIN][POLL][ERROR] " + ex.Message);
                return null;
            }
        }

        // A serial, coalescing worker for one (eventId, classType) key. Updates
        // are latest-wins (a queued-but-not-yet-sent update is replaced by a newer
        // one); resets are kept as ordered barriers and never dropped.
        private sealed class SendChannel
        {
            private readonly LiveApiClient _owner;
            private readonly object _lock = new object();
            private readonly LinkedList<LiveOp> _queue = new LinkedList<LiveOp>();
            private bool _draining;

            public SendChannel(LiveApiClient owner)
            {
                _owner = owner;
            }

            public void EnqueueUpdate(LiveRaceUpdateDto dto)
            {
                lock (_lock)
                {
                    // Drop-old: if the tail is still a pending update, overwrite it
                    // rather than queueing another stale send behind it.
                    var tail = _queue.Last;
                    if (tail != null && tail.Value is UpdateOp pending)
                        pending.Dto = dto;
                    else
                        _queue.AddLast(new UpdateOp { Dto = dto });

                    EnsureDraining();
                }
            }

            public void EnqueueReset(string eventId, string eventName)
            {
                lock (_lock)
                {
                    _queue.AddLast(new ResetOp { EventId = eventId, EventName = eventName });
                    EnsureDraining();
                }
            }

            // Caller must hold _lock.
            private void EnsureDraining()
            {
                if (_draining) return;
                _draining = true;
                _ = Task.Run(DrainAsync);
            }

            private async Task DrainAsync()
            {
                while (true)
                {
                    LiveOp op;
                    lock (_lock)
                    {
                        if (_queue.Count == 0)
                        {
                            _draining = false;
                            return;
                        }
                        op = _queue.First.Value;
                        _queue.RemoveFirst();
                    }

                    if (op is UpdateOp u)
                        await _owner.SendCoreAsync(u.Dto).ConfigureAwait(false);
                    else if (op is ResetOp r)
                        await _owner.ResetCoreAsync(r.EventId, r.EventName).ConfigureAwait(false);
                }
            }
        }

        private abstract class LiveOp { }

        private sealed class UpdateOp : LiveOp
        {
            public LiveRaceUpdateDto Dto { get; set; }
        }

        private sealed class ResetOp : LiveOp
        {
            public string EventId { get; set; }
            public string EventName { get; set; }
        }
    }
}
