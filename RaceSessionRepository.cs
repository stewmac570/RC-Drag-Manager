using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;

namespace RCDragManagerProd
{
    public class RaceSessionRepository
    {
        private readonly string _connectionString;

        public RaceSessionRepository(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";

            // ADD THIS LINE INSIDE YOUR EXISTING CONSTRUCTOR:
            Console.WriteLine("DB File Path: " + Path.GetFullPath(dbPath));

            EnsureTableExists();
        }


        private void EnsureTableExists()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS RaceSessions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        EventName TEXT,
                        EventDate TEXT,
                        RaceType TEXT,
                        ClassType TEXT,
                        FixedDialIn REAL,
                        SessionData TEXT
                    );
                ";
                cmd.ExecuteNonQuery();
            }
        }

        public void SaveSession(RaceSession session)
        {
            string jsonData = JsonSerializer.Serialize(session);

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                if (session.Id == 0)
                {
                    // INSERT
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO RaceSessions 
                        (EventName, EventDate, RaceType, ClassType, FixedDialIn, SessionData)
                        VALUES (@EventName, @EventDate, @RaceType, @ClassType, @FixedDialIn, @SessionData);
                        SELECT last_insert_rowid();
                    ";

                    cmd.Parameters.AddWithValue("@EventName", session.EventName);
                    cmd.Parameters.AddWithValue("@EventDate", session.EventDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@RaceType", session.RaceType);
                    cmd.Parameters.AddWithValue("@ClassType", session.ClassType);
                    cmd.Parameters.AddWithValue("@FixedDialIn", session.FixedDialIn ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SessionData", jsonData);

                    long insertedId = (long)cmd.ExecuteScalar();
                    session.Id = (int)insertedId;
                }
                else
                {
                    // UPDATE
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        UPDATE RaceSessions
                        SET EventName = @EventName,
                            EventDate = @EventDate,
                            RaceType = @RaceType,
                            ClassType = @ClassType,
                            FixedDialIn = @FixedDialIn,
                            SessionData = @SessionData
                        WHERE Id = @Id;
                    ";

                    cmd.Parameters.AddWithValue("@EventName", session.EventName);
                    cmd.Parameters.AddWithValue("@EventDate", session.EventDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@RaceType", session.RaceType);
                    cmd.Parameters.AddWithValue("@ClassType", session.ClassType);
                    cmd.Parameters.AddWithValue("@FixedDialIn", session.FixedDialIn ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SessionData", jsonData);
                    cmd.Parameters.AddWithValue("@Id", session.Id);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<RaceSessionSummary> GetAllSessions()
        {
            var sessions = new List<RaceSessionSummary>();

            try
            {
                Logger.Log("[LOAD] Fetching session summaries…");

                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Id, EventName, EventDate, RaceType, ClassType FROM RaceSessions ORDER BY Id DESC";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0));
                                string eventName = reader.IsDBNull(1) ? string.Empty : reader.GetValue(1).ToString();

                                DateTime eventDate;
                                if (reader.IsDBNull(2))
                                {
                                    eventDate = DateTime.MinValue;
                                }
                                else if (reader.GetFieldType(2) == typeof(DateTime))
                                {
                                    eventDate = reader.GetDateTime(2);
                                }
                                else
                                {
                                    string raw = reader.GetValue(2).ToString();
                                    if (!DateTime.TryParse(raw, out eventDate))
                                        eventDate = DateTime.MinValue;
                                }

                                string raceType = reader.IsDBNull(3) ? string.Empty : reader.GetValue(3).ToString();
                                string classType = reader.IsDBNull(4) ? string.Empty : reader.GetValue(4).ToString();

                                var summary = new RaceSessionSummary
                                {
                                    Id = id,
                                    EventName = eventName,
                                    EventDate = eventDate,
                                    RaceType = raceType,
                                    ClassType = classType
                                };

                                sessions.Add(summary);
                            }
                        }
                    }
                }

                Logger.Log($"[LOAD] Session summaries loaded: {sessions.Count}");
                return sessions;
            }
            catch (Exception ex)
            {
                Logger.Log($"[LOAD][ERROR] GetAllSessions failed: {ex}");
                throw;
            }
        }


        public RaceSession LoadSession(int id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, SessionData FROM RaceSessions WHERE Id = @Id;";
                cmd.Parameters.AddWithValue("@Id", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int dbId = reader.GetInt32(0);
                        string jsonData = reader.GetString(1);
                        var session = JsonSerializer.Deserialize<RaceSession>(jsonData);
                        session.Id = dbId; // Restore DB Id to object
                        return session;
                    }
                }
            }
            return null;
        }

        public void DeleteSession(int id)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM RaceSessions WHERE Id = @Id;";
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public class RaceSessionSummary
    {
        public int Id { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string RaceType { get; set; }
        public string ClassType { get; set; }
    }
}
