using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace RCDragManager
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
                string sql = @"INSERT INTO Drivers (Name, QualTime, Notes, TotalWins, TotalLosses, EventsEntered, EventsWon)
                               VALUES (@Name, @QualTime, @Notes, @TotalWins, @TotalLosses, @EventsEntered, @EventsWon);
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
                                EventsWon = @EventsWon 
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
                    cmd.Parameters.AddWithValue("@Id", driver.Id);
                    cmd.ExecuteNonQuery();
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

        private void AddCar(Car car, int driverId, SQLiteConnection connection)
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
    }
}
