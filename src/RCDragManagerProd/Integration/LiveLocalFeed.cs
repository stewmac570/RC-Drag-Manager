using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Integration
{
    /// <summary>
    /// Writes the same per-class live state that goes to the live site into a
    /// file on this machine, so the stream overlay on the race-day laptop gets
    /// its driver names with no internet. The file holds a JSON array with one
    /// entry per (eventId, classType), the same shape the live site serves.
    /// </summary>
    public sealed class LiveLocalFeed
    {
        public static readonly LiveLocalFeed Default = new LiveLocalFeed(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "RC_Drag_Manager", "live.json"));

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly object _lock = new object();
        private readonly Dictionary<string, LiveRaceUpdateDto> _classes =
            new Dictionary<string, LiveRaceUpdateDto>();

        public LiveLocalFeed(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }

        public void Update(LiveRaceUpdateDto dto)
        {
            if (dto == null) return;
            lock (_lock)
            {
                _classes[(dto.EventId ?? string.Empty) + "|" + (dto.ClassType ?? string.Empty)] = dto;
                Write();
            }
        }

        /// <summary>Empties the file, so the overlay cannot pick up names from an
        /// earlier event (it trusts the file for 12 hours).</summary>
        public void Clear()
        {
            lock (_lock)
            {
                _classes.Clear();
                Write();
            }
        }

        /// <summary>Mirrors the live site's reset: by event id when one is given,
        /// otherwise by event name (a finished multi-class event).</summary>
        public void Reset(string eventId, string eventName)
        {
            lock (_lock)
            {
                var gone = _classes
                    .Where(kv => !string.IsNullOrEmpty(eventId)
                        ? kv.Value.EventId == eventId
                        : kv.Value.EventName == eventName)
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var key in gone) _classes.Remove(key);
                Write();
            }
        }

        // Caller must hold _lock. Written to a temp file and swapped in, so a
        // reader never sees half a file.
        private void Write()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var json = JsonSerializer.Serialize(_classes.Values.ToList(), JsonOptions);
                var temp = FilePath + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(FilePath))
                    File.Replace(temp, FilePath, null);
                else
                    File.Move(temp, FilePath);
            }
            catch (Exception ex)
            {
                // The next race action writes again. Never let the file stop racing.
                Logger.Log("[LIVE][LOCAL][FAIL] " + ex.Message);
            }
        }
    }
}
