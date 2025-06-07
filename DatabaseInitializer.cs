using System.Data.SQLite;
using System.Data;

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

                // ✅ Apply schema upgrade for State column automatically
                AddStateColumnIfMissing(connection);
            }
        }

        private static void AddStateColumnIfMissing(SQLiteConnection connection)
        {
            string pragma = "PRAGMA table_info(Drivers);";
            bool stateExists = false;

            using (var cmd = new SQLiteCommand(pragma, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string columnName = reader["name"].ToString();
                    if (columnName == "State")
                    {
                        stateExists = true;
                        break;
                    }
                }
            }

            if (!stateExists)
            {
                using (var cmd = new SQLiteCommand("ALTER TABLE Drivers ADD COLUMN State TEXT;", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
