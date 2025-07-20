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

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, EventName, EventDate, RaceType, ClassType FROM RaceSessions ORDER BY Id DESC;";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var summary = new RaceSessionSummary
                        {
                            Id = reader.GetInt32(0),
                            EventName = reader.GetString(1),
                            EventDate = DateTime.Parse(reader.GetString(2)),
                            RaceType = reader.GetString(3),
                            ClassType = reader.GetString(4)
                        };
                        sessions.Add(summary);
                    }

                }
            }

            return sessions;
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
