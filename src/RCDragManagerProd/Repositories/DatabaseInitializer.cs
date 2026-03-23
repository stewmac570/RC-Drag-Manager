using System;
using System.Data.SQLite;

namespace RCDragManagerProd.Repositories
{
    public static class DatabaseInitializer
    {
        public static void InitializeDatabase(string connectionString)
        {
            using var cn = new SQLiteConnection(connectionString);
            cn.Open();

            // Be safe with FK behaviour
            using (var pragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", cn))
                pragma.ExecuteNonQuery();

            // --- Drivers -------------------------------------------------------
            const string createDrivers = @"
CREATE TABLE IF NOT EXISTS Drivers
(
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Name          TEXT    NOT NULL,
    QualTime      REAL,
    Notes         TEXT,
    TotalWins     INTEGER NOT NULL DEFAULT 0,
    TotalLosses   INTEGER NOT NULL DEFAULT 0,
    EventsEntered INTEGER NOT NULL DEFAULT 0,
    EventsWon     INTEGER NOT NULL DEFAULT 0,
    State         TEXT
);";
            Exec(cn, createDrivers);

            // --- Cars ----------------------------------------------------------
            const string createCars = @"
CREATE TABLE IF NOT EXISTS Cars
(
    CarID         INTEGER PRIMARY KEY AUTOINCREMENT,
    DriverId      INTEGER NOT NULL,
    CarName       TEXT    NOT NULL,
    ClassType     TEXT    NOT NULL,
    DefaultDialIn REAL,
    FOREIGN KEY (DriverId) REFERENCES Drivers(Id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS IX_Cars_DriverId ON Cars(DriverId);";
            Exec(cn, createCars);

            // --- RaceSessions --------------------------------------------------
            // NOTE: the repository uses the plural name 'RaceSessions'
            const string createRaceSessions = @"
CREATE TABLE IF NOT EXISTS RaceSessions
(
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    EventName   TEXT,
    EventDate   TEXT,        -- stored as 'yyyy-MM-dd HH:mm:ss'
    ClassType   TEXT,
    RaceType    TEXT,
    SessionData TEXT         -- JSON blob
);
CREATE INDEX IF NOT EXISTS IX_RaceSessions_EventDate ON RaceSessions(EventDate);";
            Exec(cn, createRaceSessions);

            // --- MultiClassEvents ----------------------------------------------
            const string createMultiClassEvents = @"
CREATE TABLE IF NOT EXISTS MultiClassEvents (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    EventName   TEXT,
    EventDate   TEXT,
    ClassCount  INTEGER,
    EventData   TEXT
);";
            Exec(cn, createMultiClassEvents);
        }

        private static void Exec(SQLiteConnection cn, string sql)
        {
            using var cmd = new SQLiteCommand(sql, cn);
            cmd.ExecuteNonQuery();
        }
    }
}
