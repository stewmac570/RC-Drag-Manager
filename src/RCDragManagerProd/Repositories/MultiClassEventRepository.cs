using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;
using RCDragManagerProd.ViewModels;

namespace RCDragManagerProd.Repositories
{
    public sealed class MultiClassEventRepository
    {
        private readonly string _connStr;

        public MultiClassEventRepository(string connectionOrPath)
        {
            if (string.IsNullOrWhiteSpace(connectionOrPath))
                throw new ArgumentNullException(nameof(connectionOrPath));

            _connStr = NormalizeConnString(connectionOrPath);
            Logger.Log($"[DB][MultiClassEventRepo] ctor | conn='{_connStr}'");
        }

        private static string NormalizeConnString(string input)
        {
            if (input.IndexOf('=') >= 0 &&
                input.IndexOf("Data Source", StringComparison.OrdinalIgnoreCase) >= 0)
                return input;

            string path = input;
            if (!Path.IsPathRooted(path))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder = Path.Combine(appData, "RC_Drag_Manager");
                Directory.CreateDirectory(folder);
                path = Path.Combine(folder, path);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            return $"Data Source={path};Version=3;";
        }

        private SQLiteConnection Open()
        {
            var cn = new SQLiteConnection(_connStr);
            cn.Open();
            return cn;
        }

        // ---------- SAVE ----------
        public void SaveEvent(MultiClassEvent evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));
            Logger.Log($"[DB][MultiClassEventRepo] SaveEvent(id={evt.Id})");

            string eventName = evt.EventName ?? "(event)";
            DateTime eventDate = evt.EventDate != default ? evt.EventDate : DateTime.Now;
            int classCount = evt.ClassSessions?.Count ?? 0;

            const string insertSql = @"
INSERT INTO MultiClassEvents (EventName, EventDate, ClassCount, EventData)
VALUES (@EventName, @EventDate, @ClassCount, @EventData);
SELECT last_insert_rowid();";

            using (var cn = Open())
            using (var tx = cn.BeginTransaction())
            {
                Logger.Log("[TX] BEGIN SaveEvent");
                try
                {
                    if (evt.Id <= 0)
                    {
                        // The embedded JSON id is 0 here; harmless — LoadEvent overwrites
                        // evt.Id with the row id.
                        using (var cmd = new SQLiteCommand(insertSql, cn, tx))
                        {
                            AddSaveParameters(cmd, eventName, eventDate, classCount, Serialize(evt));
                            evt.Id = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                    else
                    {
                        UpdateExistingEvent(cn, tx, evt, eventName, eventDate, classCount);
                    }

                    tx.Commit();
                    Logger.Log("[TX] COMMIT SaveEvent");
                }
                catch (Exception ex)
                {
                    try { tx.Rollback(); } catch { }
                    Logger.Log($"[TX] ROLLBACK SaveEvent: {ex}");
                    throw;
                }
            }

            Logger.Log($"[DB][MultiClassEventRepo] SaveEvent → Id={evt.Id}");
        }

        private static void UpdateExistingEvent(
            SQLiteConnection cn,
            SQLiteTransaction tx,
            MultiClassEvent evt,
            string eventName,
            DateTime eventDate,
            int classCount)
        {
            const string sql = @"
UPDATE MultiClassEvents
SET EventName = @EventName,
    EventDate = @EventDate,
    ClassCount = @ClassCount,
    EventData = @EventData
WHERE Id = @Id;";

            using (var cmd = new SQLiteCommand(sql, cn, tx))
            {
                AddSaveParameters(cmd, eventName, eventDate, classCount, Serialize(evt));
                cmd.Parameters.AddWithValue("@Id", evt.Id);
                int affected = cmd.ExecuteNonQuery();
                if (affected != 1)
                    throw new InvalidOperationException(
                        $"Expected to update MultiClassEvent Id={evt.Id}, but affected {affected} rows.");
            }
        }

        private static void AddSaveParameters(
            SQLiteCommand cmd,
            string eventName,
            DateTime eventDate,
            int classCount,
            string json)
        {
            cmd.Parameters.AddWithValue("@EventName", eventName);
            cmd.Parameters.AddWithValue("@EventDate", DbDate.ToDbString(eventDate));
            cmd.Parameters.AddWithValue("@ClassCount", classCount);
            cmd.Parameters.AddWithValue("@EventData", json ?? "{}");
        }

        private static string Serialize(MultiClassEvent evt) =>
            JsonSerializer.Serialize(evt, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

        // ---------- LIST ----------
        public List<MultiClassEventSummary> GetAllEvents()
        {
            Logger.Log("[DB][MultiClassEventRepo] GetAllEvents()");
            var list = new List<MultiClassEventSummary>();

            const string sql = @"
SELECT Id, EventName, EventDate, ClassCount
FROM MultiClassEvents
ORDER BY datetime(EventDate) DESC";

            using (var cn = Open())
            using (var cmd = new SQLiteCommand(sql, cn))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    list.Add(new MultiClassEventSummary
                    {
                        Id = rd.GetInt32(0),
                        EventName = rd.IsDBNull(1) ? "" : rd.GetString(1),
                        EventDate = rd.IsDBNull(2) ? DateTime.MinValue : DbDate.ParseOrMinValue(rd.GetString(2)),
                        ClassCount = rd.IsDBNull(3) ? 0 : rd.GetInt32(3)
                    });
                }
            }

            Logger.Log($"[DB][MultiClassEventRepo] GetAllEvents → {list.Count} rows");
            return list;
        }

        // ---------- LOAD ----------
        public MultiClassEvent LoadEvent(int id)
        {
            Logger.Log($"[DB][MultiClassEventRepo] LoadEvent(id={id})");

            const string sql = "SELECT EventData FROM MultiClassEvents WHERE Id = @Id";

            using (var cn = Open())
            using (var cmd = new SQLiteCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                var json = cmd.ExecuteScalar() as string;
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Log("[DB][MultiClassEventRepo][WARN] No JSON event data found");
                    return null;
                }

                try
                {
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var multiEvent = JsonSerializer.Deserialize<MultiClassEvent>(json, opts);
                    if (multiEvent != null) multiEvent.Id = id;
                    Logger.Log("[DB][MultiClassEventRepo] LoadEvent → OK");
                    return multiEvent;
                }
                catch (Exception ex)
                {
                    Logger.Log($"[DB][MultiClassEventRepo][ERROR] Deserialize failed: {ex}");
                    return null;
                }
            }
        }

        // ---------- DELETE ----------
        public void DeleteEvent(int id)
        {
            Logger.Log($"[DB][MultiClassEventRepo] DeleteEvent(id={id})");
            using (var cn = Open())
            using (var tx = cn.BeginTransaction())
            {
                Logger.Log("[DB][MultiClassEventRepo][TX] DeleteEvent begin");
                try
                {
                    using (var cmd = new SQLiteCommand("DELETE FROM MultiClassEvents WHERE Id = @Id", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                    Logger.Log("[DB][MultiClassEventRepo][TX] DeleteEvent commit");
                }
                catch (Exception ex)
                {
                    try { tx.Rollback(); } catch { }
                    Logger.Log($"[DB][MultiClassEventRepo][TX][ERROR] DeleteEvent rollback: {ex}");
                    throw;
                }
            }

            Logger.Log("[DB][MultiClassEventRepo] DeleteEvent → OK");
        }
    }
}
