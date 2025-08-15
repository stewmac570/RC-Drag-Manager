using System;
using System.Data.SQLite;

namespace RCDragManagerProd
{
    public static class DatabaseInitializer
    {
        public static void InitializeDatabase(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            using (var cn = new SQLiteConnection(connectionString))
            {
                cn.Open();

                using (var cmd = cn.CreateCommand())
                {
                    // Pragmas
                    cmd.CommandText = "PRAGMA foreign_keys = ON;";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "PRAGMA journal_mode = WAL;";
                    cmd.ExecuteNonQuery();

                    // ───────── DRIVERS ─────────
                    cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Drivers
(
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Name          TEXT    NOT NULL,
    QualTime      REAL    NULL,
    Notes         TEXT    NULL,
    TotalWins     INTEGER NOT NULL DEFAULT 0,
    TotalLosses   INTEGER NOT NULL DEFAULT 0,
    EventsEntered INTEGER NOT NULL DEFAULT 0,
    EventsWon     INTEGER NOT NULL DEFAULT 0,
    State         TEXT    NULL
);";
                    cmd.ExecuteNonQuery();

                    // ───────── CARS ─────────
                    cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Cars
(
    CarID         INTEGER PRIMARY KEY AUTOINCREMENT,
    DriverId      INTEGER NOT NULL,
    CarName       TEXT    NOT NULL,
    ClassType     TEXT    NULL,
    DefaultDialIn REAL    NULL,
    FOREIGN KEY (DriverId) REFERENCES Drivers(Id) ON DELETE CASCADE
);";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"CREATE INDEX IF NOT EXISTS IX_Cars_DriverId ON Cars(DriverId);";
                    cmd.ExecuteNonQuery();

                    // ───────── RACE SESSIONS ─────────
                    cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS RaceSessions
(
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    EventName   TEXT    NOT NULL,
    EventDate   TEXT    NOT NULL,     -- stored as 'yyyy-MM-dd HH:mm:ss'
    ClassType   TEXT    NULL,
    RaceType    TEXT    NULL,
    SessionData TEXT    NOT NULL      -- JSON blob of full session
);";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"CREATE INDEX IF NOT EXISTS IX_RaceSessions_EventDate ON RaceSessions(EventDate);";
                    cmd.ExecuteNonQuery();
                }
            }

            Logger.Log("[DB][Init] Schema ensured (Drivers, Cars, RaceSessions).");
        }
    }
}
