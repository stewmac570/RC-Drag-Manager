using System.Data.SQLite;

namespace RCDragManager
{
    public static class DatabaseInitializer
    {
        public static void InitializeDatabase(string connectionString)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string driverTable = @"
                    CREATE TABLE IF NOT EXISTS Drivers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        QualTime REAL,
                        Notes TEXT,
                        TotalWins INTEGER DEFAULT 0,
                        TotalLosses INTEGER DEFAULT 0,
                        EventsEntered INTEGER DEFAULT 0,
                        EventsWon INTEGER DEFAULT 0
                    );";

                string carTable = @"
                    CREATE TABLE IF NOT EXISTS Cars (
                        CarID INTEGER PRIMARY KEY AUTOINCREMENT,
                        DriverId INTEGER NOT NULL,
                        CarName TEXT NOT NULL,
                        ClassType TEXT,
                        DefaultDialIn REAL,
                        FOREIGN KEY (DriverId) REFERENCES Drivers(Id)
                    );";

                using (var cmd = new SQLiteCommand(driverTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SQLiteCommand(carTable, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
