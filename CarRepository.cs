// =====================================================================================
// FILE         : CarRepository.cs
// DESCRIPTION  : SQLite repository for Car storage
// VERSION      : 1.00
// AUTHOR       : Stewart McMillan + ChatGPT
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace RCDragManagerProd
{
    public class CarRepository
    {
        private readonly string _dbFile = "C:\\Temp\\drivers.db";
        private readonly string _connectionString;

        public CarRepository()
        {
            _connectionString = $"Data Source={_dbFile};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string sql = @"CREATE TABLE IF NOT EXISTS Cars (
                                   CarID INTEGER PRIMARY KEY AUTOINCREMENT,
                                   DriverId INTEGER NOT NULL,
                                   CarName TEXT NOT NULL,
                                   ClassType TEXT,
                                   DefaultDialIn REAL
                               )";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void AddCar(Car car)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string sql = @"INSERT INTO Cars (DriverId, CarName, ClassType, DefaultDialIn)
                               VALUES (@DriverId, @CarName, @ClassType, @DefaultDialIn)";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    //cmd.Parameters.AddWithValue("@DriverId", car.DriverId);
                    cmd.Parameters.AddWithValue("@CarName", car.CarName);
                    cmd.Parameters.AddWithValue("@ClassType", car.ClassType);
                    cmd.Parameters.AddWithValue("@DefaultDialIn", (object)car.DefaultDialIn ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Car> GetCarsByDriver(int driverId)
        {
            var cars = new List<Car>();

            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string sql = @"SELECT CarID, DriverId, CarName, ClassType, DefaultDialIn
                               FROM Cars WHERE DriverId = @DriverId";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@DriverId", driverId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cars.Add(new Car
                            {
                                CarID = reader.GetInt32(0),
                                //DriverId = reader.GetInt32(1),
                                CarName = reader.GetString(2),
                                ClassType = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                DefaultDialIn = reader.IsDBNull(4) ? null : (double?)reader.GetDouble(4)
                            });
                        }
                    }
                }
            }

            return cars;
        }
    }
}
