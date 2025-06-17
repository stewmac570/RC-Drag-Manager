using System.Collections.Generic;
using System.Data.SqlClient;

namespace RCDragManagerProd
{
    public class CarRepository
    {
        private readonly string _connectionString;

        public CarRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void AddCar(Car car)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand("INSERT INTO Cars (DriverId, CarName, ClassType, DefaultDialIn) VALUES (@DriverId, @CarName, @ClassType, @DefaultDialIn)", connection);
                cmd.Parameters.AddWithValue("@DriverId", car.DriverId); // ✅ uncommented
                cmd.Parameters.AddWithValue("@CarName", car.CarName);
                cmd.Parameters.AddWithValue("@ClassType", car.ClassType);
                cmd.Parameters.AddWithValue("@DefaultDialIn", (object)car.DefaultDialIn ?? System.DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public List<Car> GetCarsByDriver(int driverId)
        {
            var cars = new List<Car>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand("SELECT Id, DriverId, CarName, ClassType, DefaultDialIn FROM Cars WHERE DriverId = @DriverId", connection);
                cmd.Parameters.AddWithValue("@DriverId", driverId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var car = new Car
                        {
                            Id = reader.GetInt32(0),
                            DriverId = reader.GetInt32(1), // ✅ uncommented
                            CarName = reader.GetString(2),
                            ClassType = reader.GetString(3),
                            DefaultDialIn = reader.IsDBNull(4) ? (double?)null : reader.GetDouble(4)
                        };
                        cars.Add(car);
                    }
                }
            }
            return cars;
        }
    }
}
