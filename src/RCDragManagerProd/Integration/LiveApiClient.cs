using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RCDragManagerProd.Config;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Integration
{
    public sealed class LiveApiClient
    {
        private const string LiveUpdateUrl = "https://stewmacrc.com/api/update";

        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(3)
        };
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly SemaphoreSlim SendGate = new SemaphoreSlim(1, 1);

        public async Task SendAsync(LiveRaceUpdateDto dto)
        {
            await SendGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!AppSettings.LiveBroadcastEnabled)
                {
                    Logger.Log("[LIVE][FAIL] Live updates disabled by config.");
                    return;
                }

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

                    Logger.Log("[LIVE][SEND][JSON] " + json);
                    Logger.Log("[LIVE][SEND] POST " + LiveUpdateUrl);
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
            finally
            {
                SendGate.Release();
            }
        }
    }
}
