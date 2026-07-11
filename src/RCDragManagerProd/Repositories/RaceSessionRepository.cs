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
                            // The embedded JSON id is 0 here; harmless — every load path
                            // overwrites session.Id with the row id (DeserializeSession).
                            using (var cmd = new SQLiteCommand(insertSql, cn, tx))
                            {
                                AddSaveParameters(cmd, eventName, eventDate, classType, raceType, Serialize(session));
                                session.Id = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }
                        else
                        {
                            UpdateExistingSession(cn, tx, session, eventName, eventDate, classType, raceType);
                        }

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
            cmd.Parameters.AddWithValue("@EventDate", DbDate.ToDbString(eventDate));
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
SELECT Id, EventName, EventDate, ClassType, RaceType, SessionData
FROM RaceSessions
ORDER BY datetime(EventDate) DESC";

            using (var cn = Open())
            using (var cmd = new SQLiteCommand(sql, cn))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    int id = rd.GetInt32(0);
                    string eventName = rd.IsDBNull(1) ? "" : rd.GetString(1);
                    string json = rd.IsDBNull(5) ? null : rd.GetString(5);
                    var loadResult = DeserializeSession(id, json);
                    if (!loadResult.Success)
                    {
                        Logger.Log(
                            $"[DB][SessionRepo][WARN] Skipping unloadable session " +
                            $"Id={id}, EventName='{eventName}', Status={loadResult.Status}");
                        continue;
                    }

                    string rawEventDate = rd.IsDBNull(2) ? null : rd.GetString(2);
                    DateTime eventDate = DbDate.ParseOrMinValue(rawEventDate);

                    var s = new RaceSessionSummary
                    {
                        Id = id,
                        EventName = eventName,
                        EventDate = eventDate,
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
            var result = TryLoadSession(id);
            return result.Session;
        }

        public RaceSessionLoadResult TryLoadSession(int id)
        {
            Logger.Log($"[DB][SessionRepo] TryLoadSession(id={id})");

            const string sql = "SELECT SessionData FROM RaceSessions WHERE Id = @Id";

            using (var cn = Open())
            using (var cmd = new SQLiteCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                {
                    Logger.Log($"[DB][SessionRepo][WARN] Session Id={id} was not found");
                    return RaceSessionLoadResult.Fail(RaceSessionLoadStatus.NotFound);
                }

                return DeserializeSession(id, value as string);
            }
        }

        private static RaceSessionLoadResult DeserializeSession(int id, string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Logger.Log($"[DB][SessionRepo][WARN] Session Id={id} has no JSON session data");
                return RaceSessionLoadResult.Fail(RaceSessionLoadStatus.MissingData);
            }

            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var session = JsonSerializer.Deserialize<RaceSession>(json, opts);
                if (session == null)
                {
                    Logger.Log($"[DB][SessionRepo][WARN] Session Id={id} deserialized to null");
                    return RaceSessionLoadResult.Fail(RaceSessionLoadStatus.InvalidData);
                }

                session.Id = id;
                Logger.Log($"[DB][SessionRepo] Session Id={id} deserialized successfully");
                return RaceSessionLoadResult.Ok(session);
            }
            catch (Exception ex)
            {
                Logger.Log($"[DB][SessionRepo][ERROR] Session Id={id} deserialize failed: {ex}");
                return RaceSessionLoadResult.Fail(RaceSessionLoadStatus.InvalidData);
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

    public enum RaceSessionLoadStatus
    {
        Loaded,
        NotFound,
        MissingData,
        InvalidData
    }

    public sealed class RaceSessionLoadResult
    {
        public bool Success => Status == RaceSessionLoadStatus.Loaded;
        public RaceSessionLoadStatus Status { get; }
        public RaceSession Session { get; }

        private RaceSessionLoadResult(RaceSessionLoadStatus status, RaceSession session)
        {
            Status = status;
            Session = session;
        }

        public static RaceSessionLoadResult Ok(RaceSession session) =>
            new RaceSessionLoadResult(
                RaceSessionLoadStatus.Loaded,
                session ?? throw new ArgumentNullException(nameof(session)));

        public static RaceSessionLoadResult Fail(RaceSessionLoadStatus status) =>
            new RaceSessionLoadResult(status, null);
    }
}
