using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Repositories
{
    public sealed class CarRepository
    {
        private readonly string _connStr;

        public CarRepository(string connectionOrPath)
        {
            if (string.IsNullOrWhiteSpace(connectionOrPath))
                throw new ArgumentNullException(nameof(connectionOrPath));

            _connStr = NormalizeConnString(connectionOrPath);
            Logger.Log($"[DB][CarRepo] ctor | conn='{_connStr}'");
        }

        // Accepts either a full connection string or a db file path
        private static string NormalizeConnString(string input)
        {
            if (input.IndexOf('=') >= 0 &&
                input.IndexOf("Data Source", StringComparison.OrdinalIgnoreCase) >= 0)
                return input;

            var path = input;
            if (!Path.IsPathRooted(path))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = Path.Combine(appData, "RC_Drag_Manager");
                Directory.CreateDirectory(folder);
                path = Path.Combine(folder, path);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            return $"Data Source={path};Version=3;";
        }

        private SQLiteConnection Open()
        {
            var cn = new SQLiteConnection(_connStr);
            cn.Open();
            return cn;
        }

        // ───────── CRUD ─────────

        public void AddCar(int driverId, Car car)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            Logger.Log($"[DB][CarRepo] AddCar(driverId={driverId}, Car='{car.CarName}')");

            using (var cn = Open())
            using (var cmd = new SQLiteCommand(@"
INSERT INTO Cars (DriverId, CarName, ClassType, DefaultDialIn)
VALUES (@DriverId, @CarName, @ClassType, @DefaultDialIn);
SELECT last_insert_rowid();", cn))
            {
                cmd.Parameters.AddWithValue("@DriverId", driverId);
                cmd.Parameters.AddWithValue("@CarName", car.CarName ?? string.Empty);
                cmd.Parameters.AddWithValue("@ClassType", car.ClassType ?? string.Empty);
                cmd.Parameters.AddWithValue("@DefaultDialIn", (object)car.DefaultDialIn ?? DBNull.Value);

                var newId = Convert.ToInt32(cmd.ExecuteScalar());
                car.CarID = newId;
            }
        }

        public List<Car> GetCarsByDriver(int driverId)
        {
            var cars = new List<Car>();
            Logger.Log($"[DB][CarRepo] GetCarsByDriver(driverId={driverId})");

            using (var cn = Open())
            using (var cmd = new SQLiteCommand(
                "SELECT CarID, CarName, ClassType, DefaultDialIn FROM Cars WHERE DriverId = @DriverId", cn))
            {
                cmd.Parameters.AddWithValue("@DriverId", driverId);
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        cars.Add(new Car
                        {
                            CarID = rd.GetInt32(0),
                            CarName = rd.IsDBNull(1) ? "" : rd.GetString(1),
                            ClassType = rd.IsDBNull(2) ? "" : rd.GetString(2),
                            DefaultDialIn = rd.IsDBNull(3) ? (double?)null : rd.GetDouble(3)
                        });
                    }
                }
            }

            Logger.Log($"[DB][CarRepo] GetCarsByDriver → {cars.Count} row(s)");
            return cars;
        }

        public void UpdateCar(Car car)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            Logger.Log($"[DB][CarRepo] UpdateCar(CarID={car.CarID}, Name='{car.CarName}')");

            using (var cn = Open())
            using (var cmd = new SQLiteCommand(@"
UPDATE Cars SET
    CarName = @CarName,
    ClassType = @ClassType,
    DefaultDialIn = @DefaultDialIn
WHERE CarID = @CarID", cn))
            {
                cmd.Parameters.AddWithValue("@CarID", car.CarID);
                cmd.Parameters.AddWithValue("@CarName", car.CarName ?? string.Empty);
                cmd.Parameters.AddWithValue("@ClassType", car.ClassType ?? string.Empty);
                cmd.Parameters.AddWithValue("@DefaultDialIn", (object)car.DefaultDialIn ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteCar(int carId)
        {
            Logger.Log($"[DB][CarRepo] DeleteCar(CarID={carId})");
            using (var cn = Open())
            using (var cmd = new SQLiteCommand("DELETE FROM Cars WHERE CarID = @CarID", cn))
            {
                cmd.Parameters.AddWithValue("@CarID", carId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
