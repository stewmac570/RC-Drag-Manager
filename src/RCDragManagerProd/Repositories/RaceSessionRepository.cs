using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using RCDragManagerProd.Logging;
using RCDragManagerProd.Domain;
using RCDragManagerProd.ViewModels;


namespace RCDragManagerProd.Repositories
{
    public sealed class RaceSessionRepository
    {
        private readonly string _connStr;

        public RaceSessionRepository(string connectionOrPath)
        {
            if (string.IsNullOrWhiteSpace(connectionOrPath))
                throw new ArgumentNullException(nameof(connectionOrPath));

            _connStr = NormalizeConnString(connectionOrPath);
            Logger.Log($"[DB][SessionRepo] ctor | conn='{_connStr}'");
        }

        // Accepts EITHER a full connection string or a db file path (relative or absolute)
        private static string NormalizeConnString(string input)
        {
            // Already a connection string?
            if (input.IndexOf('=') >= 0 &&
                input.IndexOf("Data Source", StringComparison.OrdinalIgnoreCase) >= 0)
                return input;

            // Treat as path. If relative, place under %APPDATA%\RC_Drag_Manager
            string path = input;
            if (!Path.IsPathRooted(path))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder = Path.Combine(appData, "RC_Drag_Manager");
                Directory.CreateDirectory(folder);
                path = Path.Combine(folder, path);
            }
            // Ensure parent exists
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
        public int SaveSession(RaceSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            Logger.Log($"[DB][SessionRepo] SaveSession(id={session.Id})");

            string eventName = session.EventName ?? "(event)";
            string classType = session.ClassType ?? "";
            string raceType = session.RaceType ?? "";
            DateTime eventDate = session.EventDate != default ? session.EventDate : DateTime.Now;

            const string insertSql = @"
INSERT INTO RaceSessions (EventName, EventDate, ClassType, RaceType, SessionData)
VALUES (@EventName, @EventDate, @ClassType, @RaceType, @SessionData);
SELECT last_insert_rowid();";

            using (var cn = Open())
            {
                using (var tx = cn.BeginTransaction())
                {
                    Logger.Log("[TX] BEGIN SaveSession");
                    try
                    {
                        if (session.Id <= 0)
                        {
                            using (var cmd = new SQLiteCommand(insertSql, cn, tx))
                            {
                                AddSaveParameters(cmd, eventName, eventDate, classType, raceType, Serialize(session));
                                session.Id = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }

                        UpdateExistingSession(cn, tx, session, eventName, eventDate, classType, raceType);
                        tx.Commit();
                        Logger.Log("[TX] COMMIT SaveSession");
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        Logger.Log($"[TX] ROLLBACK SaveSession: {ex}");
                        throw;
                    }
                }
            }

            Logger.Log($"[DB][SessionRepo] SaveSession → Id={session.Id}");
            return session.Id;
        }

        private static void UpdateExistingSession(
            SQLiteConnection cn,
            SQLiteTransaction tx,
            RaceSession session,
            string eventName,
            DateTime eventDate,
            string classType,
            string raceType)
        {
            const string sql = @"
UPDATE RaceSessions
SET EventName = @EventName,
    EventDate = @EventDate,
    ClassType = @ClassType,
    RaceType = @RaceType,
    SessionData = @SessionData
WHERE Id = @Id;";

            using (var cmd = new SQLiteCommand(sql, cn, tx))
            {
                AddSaveParameters(cmd, eventName, eventDate, classType, raceType, Serialize(session));
                cmd.Parameters.AddWithValue("@Id", session.Id);
                int affected = cmd.ExecuteNonQuery();
                if (affected != 1)
                    throw new InvalidOperationException(
                        $"Expected to update RaceSession Id={session.Id}, but affected {affected} rows.");
            }
        }

        private static void AddSaveParameters(
            SQLiteCommand cmd,
            string eventName,
            DateTime eventDate,
            string classType,
            string raceType,
            string json)
        {
            cmd.Parameters.AddWithValue("@EventName", eventName);
            cmd.Parameters.AddWithValue("@EventDate", eventDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@ClassType", classType);
            cmd.Parameters.AddWithValue("@RaceType", raceType);
            cmd.Parameters.AddWithValue("@SessionData", json ?? "{}");
        }

        private static string Serialize(RaceSession session) =>
            JsonSerializer.Serialize(session, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

        // ---------- LIST ----------
        public List<RaceSessionSummary> GetAllSessions()
        {
            Logger.Log("[DB][SessionRepo] GetAllSessions()");
            var list = new List<RaceSessionSummary>();

            const string sql = @"
SELECT Id, EventName, EventDate, ClassType, RaceType
FROM RaceSessions
ORDER BY datetime(EventDate) DESC";

            using (var cn = Open())
            using (var cmd = new SQLiteCommand(sql, cn))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    var s = new RaceSessionSummary
                    {
                        Id = rd.GetInt32(0),
                        EventName = rd.IsDBNull(1) ? "" : rd.GetString(1),
                        EventDate = rd.IsDBNull(2) ? DateTime.MinValue : DateTime.Parse(rd.GetString(2)),
                        ClassType = rd.IsDBNull(3) ? "" : rd.GetString(3),
                        RaceType = rd.IsDBNull(4) ? "" : rd.GetString(4)
                    };
                    list.Add(s);
                }
            }

            Logger.Log($"[DB][SessionRepo] GetAllSessions → {list.Count} rows");
            return list;
        }

        // ---------- LOAD ----------
        public RaceSession LoadSession(int id)
        {
            Logger.Log($"[DB][SessionRepo] LoadSession(id={id})");

            const string sql = "SELECT SessionData FROM RaceSessions WHERE Id = @Id";

            using (var cn = Open())
            using (var cmd = new SQLiteCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                var json = cmd.ExecuteScalar() as string;
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Log("[DB][SessionRepo][WARN] No JSON session data found");
                    return null;
                }

                try
                {
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var session = JsonSerializer.Deserialize<RaceSession>(json, opts);
                    if (session != null) session.Id = id;
                    Logger.Log("[DB][SessionRepo] LoadSession → OK");
                    return session;
                }
                catch (Exception ex)
                {
                    Logger.Log($"[DB][SessionRepo][ERROR] Deserialize failed: {ex}");
                    return null;
                }
            }
        }

        // ---------- DELETE ----------
        public void DeleteSession(int id)
        {
            Logger.Log($"[DB][SessionRepo] DeleteSession(id={id})");
            using (var cn = Open())
            {
                using (var tx = cn.BeginTransaction())
                {
                    Logger.Log("[DB][SessionRepo][TX] DeleteSession begin");
                    try
                    {
                        using (var cmd = new SQLiteCommand("DELETE FROM RaceSessions WHERE Id = @Id", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        Logger.Log("[DB][SessionRepo][TX] DeleteSession commit");
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        Logger.Log($"[DB][SessionRepo][TX][ERROR] DeleteSession rollback: {ex}");
                        throw;
                    }
                }
            }
            Logger.Log("[DB][SessionRepo] DeleteSession → OK");
        }

    }
}
