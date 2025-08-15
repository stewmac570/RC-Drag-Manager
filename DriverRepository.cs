using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text.Json;

namespace RCDragManagerProd
{
    public class DriverRepository
    {
        private readonly string connectionString;

        public DriverRepository(string dbPath)
        {
            connectionString = $"Data Source={dbPath};Version=3;";
            DatabaseInitializer.InitializeDatabase(connectionString);
        }

        public List<Driver> GetAllDrivers()
        {
            List<Driver> drivers = new List<Driver>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM Drivers";
                using (var cmd = new SQLiteCommand(sql, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Driver driver = new Driver
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            QualTime = reader["QualTime"] != DBNull.Value ? (double?)Convert.ToDouble(reader["QualTime"]) : null,
                            Notes = reader["Notes"].ToString(),
                            TotalWins = Convert.ToInt32(reader["TotalWins"]),
                            TotalLosses = Convert.ToInt32(reader["TotalLosses"]),
                            EventsEntered = Convert.ToInt32(reader["EventsEntered"]),
                            EventsWon = Convert.ToInt32(reader["EventsWon"]),
                            State = reader["State"] != DBNull.Value ? reader["State"].ToString() : "",
                            Cars = GetCarsByDriverId(Convert.ToInt32(reader["Id"]))
                        };
                        drivers.Add(driver);
                    }
                }
            }
            return drivers;
        }

        public Driver GetDriverById(int id)
        {
            Driver driver = null;

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM Drivers WHERE Id = @Id";
                using (var cmd = new SQLiteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            driver = new Driver
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                QualTime = reader["QualTime"] != DBNull.Value ? (double?)Convert.ToDouble(reader["QualTime"]) : null,
                                Notes = reader["Notes"].ToString(),
                                TotalWins = Convert.ToInt32(reader["TotalWins"]),
                                TotalLosses = Convert.ToInt32(reader["TotalLosses"]),
                                EventsEntered = Convert.ToInt32(reader["EventsEntered"]),
                                EventsWon = Convert.ToInt32(reader["EventsWon"]),
                                State = reader["State"] != DBNull.Value ? reader["State"].ToString() : "",
                                Cars = GetCarsByDriverId(id)
                            };
                        }
                    }
                }
            }

            return driver;
        }

        public void AddDriver(Driver driver)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string sql = @"INSERT INTO Drivers (Name, QualTime, Notes, TotalWins, TotalLosses, EventsEntered, EventsWon, State)
                               VALUES (@Name, @QualTime, @Notes, @TotalWins, @TotalLosses, @EventsEntered, @EventsWon, @State);
                               SELECT last_insert_rowid();";

                using (var cmd = new SQLiteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Name", driver.Name);
                    cmd.Parameters.AddWithValue("@QualTime", (object)driver.QualTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", driver.Notes);
                    cmd.Parameters.AddWithValue("@TotalWins", driver.TotalWins);
                    cmd.Parameters.AddWithValue("@TotalLosses", driver.TotalLosses);
                    cmd.Parameters.AddWithValue("@EventsEntered", driver.EventsEntered);
                    cmd.Parameters.AddWithValue("@EventsWon", driver.EventsWon);
                    cmd.Parameters.AddWithValue("@State", driver.State);

                    driver.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }

                foreach (var car in driver.Cars)
                {
                    AddCar(car, driver.Id, connection);
                }
            }
        }

        public void UpdateDriver(Driver driver)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string sql = @"UPDATE Drivers SET 
                        Name = @Name, 
                        QualTime = @QualTime, 
                        Notes = @Notes, 
                        TotalWins = @TotalWins, 
                        TotalLosses = @TotalLosses, 
                        EventsEntered = @EventsEntered, 
                        EventsWon = @EventsWon,
                        State = @State
                        WHERE Id = @Id";

                using (var cmd = new SQLiteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Name", driver.Name);
                    cmd.Parameters.AddWithValue("@QualTime", (object)driver.QualTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", driver.Notes);
                    cmd.Parameters.AddWithValue("@TotalWins", driver.TotalWins);
                    cmd.Parameters.AddWithValue("@TotalLosses", driver.TotalLosses);
                    cmd.Parameters.AddWithValue("@EventsEntered", driver.EventsEntered);
                    cmd.Parameters.AddWithValue("@EventsWon", driver.EventsWon);
                    cmd.Parameters.AddWithValue("@State", driver.State);
                    cmd.Parameters.AddWithValue("@Id", driver.Id);
                    cmd.ExecuteNonQuery();
                }

                string deleteCars = "DELETE FROM Cars WHERE DriverId = @DriverId";
                using (var delCmd = new SQLiteCommand(deleteCars, connection))
                {
                    delCmd.Parameters.AddWithValue("@DriverId", driver.Id);
                    delCmd.ExecuteNonQuery();
                }

                foreach (var car in driver.Cars)
                {
                    AddCar(car, driver.Id, connection);
                }
            }
        }

        public void DeleteDriver(int id)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string deleteCars = "DELETE FROM Cars WHERE DriverId = @DriverId";
                using (var cmd = new SQLiteCommand(deleteCars, connection))
                {
                    cmd.Parameters.AddWithValue("@DriverId", id);
                    cmd.ExecuteNonQuery();
                }

                string deleteDriver = "DELETE FROM Drivers WHERE Id = @Id";
                using (var cmd = new SQLiteCommand(deleteDriver, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void AddCar(int driverId, Car car)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                AddCar(car, driverId, conn);
            }
        }

        public void AddCar(Car car, int driverId, SQLiteConnection connection)
        {
            string sql = @"INSERT INTO Cars (DriverId, CarName, ClassType, DefaultDialIn)
                           VALUES (@DriverId, @CarName, @ClassType, @DefaultDialIn);";

            using (var cmd = new SQLiteCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@DriverId", driverId);
                cmd.Parameters.AddWithValue("@CarName", car.CarName);
                cmd.Parameters.AddWithValue("@ClassType", car.ClassType);
                cmd.Parameters.AddWithValue("@DefaultDialIn", (object)car.DefaultDialIn ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private List<Car> GetCarsByDriverId(int driverId)
        {
            List<Car> cars = new List<Car>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM Cars WHERE DriverId = @DriverId";
                using (var cmd = new SQLiteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@DriverId", driverId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Car car = new Car
                            {
                                CarID = Convert.ToInt32(reader["CarID"]),
                                CarName = reader["CarName"].ToString(),
                                ClassType = reader["ClassType"].ToString(),
                                DefaultDialIn = reader["DefaultDialIn"] != DBNull.Value ? (double?)Convert.ToDouble(reader["DefaultDialIn"]) : null
                            };
                            cars.Add(car);
                        }
                    }
                }
            }
            return cars;
        }

        public void UpdateQualifyingTime(int driverId, double qualTime)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Drivers SET QualTime = @QualTime WHERE Id = @Id";
                    command.Parameters.AddWithValue("@QualTime", qualTime);
                    command.Parameters.AddWithValue("@Id", driverId);
                    command.ExecuteNonQuery();
                }
            }
        }
        // using System;
        // using System.Collections.Generic;
        // using System.Data.SQLite;
        // using System.Text.Json;

        /// <summary>
        /// Counts how many saved sessions this driver won, using only SavedResults
        /// (winner/loser pairs). Works for all bracket types without round-label mapping.
        /// </summary>
        public int ComputeEventsWonFromSavedSessions(int driverId)
        {
            Logger.Log($"[STATS] ComputeEventsWonFromSavedSessions: driverId={driverId}");

            int wins = 0;

            // Open the same SQLite DB you already use (this._connectionString exists in your repo).


            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                // Pull the JSON blob for every saved session.
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT SessionData FROM RaceSessions";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                string json = reader.GetString(0);
                                if (string.IsNullOrWhiteSpace(json)) continue;

                                using var doc = JsonDocument.Parse(json);
                                var root = doc.RootElement;

                                if (!root.TryGetProperty("SavedResults", out var arr) || arr.ValueKind != JsonValueKind.Array)
                                {
                                    Logger.Log("[STATS] Session skipped: no SavedResults array.");
                                    continue;
                                }

                                // Build sets of winners and losers for this session
                                var winners = new HashSet<int>();
                                var losers = new HashSet<int>();

                                foreach (var r in arr.EnumerateArray())
                                {
                                    int? w = TryReadInt(r, "WinnerDriverId") ?? TryReadInt(r, "WinnerId");
                                    int? l = TryReadInt(r, "LoserDriverId") ?? TryReadInt(r, "LoserId");

                                    if (w.HasValue && w.Value > 0) winners.Add(w.Value);
                                    if (l.HasValue && l.Value > 0) losers.Add(l.Value);
                                }

                                // Champion = winners \ losers (should be exactly 1 in a clean single-elim event)
                                winners.ExceptWith(losers);

                                if (winners.Contains(driverId))
                                {
                                    wins++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log($"[STATS] Session parse failed: {ex.Message}");
                            }
                        }
                    }
                }
            }

            Logger.Log($"[STATS] EventsWon computed for DriverId={driverId}: {wins}");
            return wins;

            // Local helper
            static int? TryReadInt(JsonElement obj, string name)
            {
                if (!obj.TryGetProperty(name, out var el)) return null;
                try
                {
                    // Stored as number?
                    if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n)) return n;
                    // Stored as string?
                    if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var s)) return s;
                }
                catch { /* ignore */ }
                return null;
            }
        }

    }
}
