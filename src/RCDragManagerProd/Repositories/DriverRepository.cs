using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Repositories
{
    public class DriverRepository
    {
        private readonly string _connStr;

        public DriverRepository(string connectionOrPath)
        {
            if (string.IsNullOrWhiteSpace(connectionOrPath))
                throw new ArgumentNullException(nameof(connectionOrPath));

            _connStr = NormalizeConnString(connectionOrPath);
            Logger.Log($"[DB][DriverRepo] ctor | conn='{_connStr}'");
        }

        private static string NormalizeConnString(string input)
        {
            if (input.IndexOf('=') >= 0 &&
                input.IndexOf("Data Source", StringComparison.OrdinalIgnoreCase) >= 0)
                return input;

            string path = input;
            if (!Path.IsPathRooted(path))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder = Path.Combine(appData, "RC_Drag_Manager");
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

        public List<Driver> GetAllDrivers()
        {
            Logger.Log("[DB][DriverRepo] GetAllDrivers()");
            var drivers = new List<Driver>();

            using (var connection = Open())
            using (var cmd = new SQLiteCommand("SELECT * FROM Drivers", connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = Convert.ToInt32(reader["Id"]);
                    var d = new Driver
                    {
                        Id = id,
                        Name = reader["Name"].ToString(),
                        QualTime = reader["QualTime"] != DBNull.Value ? (double?)Convert.ToDouble(reader["QualTime"]) : null,
                        Notes = reader["Notes"].ToString(),
                        TotalWins = Convert.ToInt32(reader["TotalWins"]),
                        TotalLosses = Convert.ToInt32(reader["TotalLosses"]),
                        EventsEntered = Convert.ToInt32(reader["EventsEntered"]),
                        EventsWon = Convert.ToInt32(reader["EventsWon"]),
                        State = reader["State"] != DBNull.Value ? reader["State"].ToString() : string.Empty,
                        Cars = new List<Car>()
                    };
                    drivers.Add(d);
                }

                Logger.Log($"[DB][DriverRepo] GetAllDrivers drivers loaded: {drivers.Count}");
                if (drivers.Count > 0)
                {
                    var carsByDriver = GetCarsByDriverIds(connection, drivers.ConvertAll(d => d.Id));
                    foreach (var d in drivers)
                        d.Cars = carsByDriver.TryGetValue(d.Id, out var cars) ? cars : new List<Car>();
                    Logger.Log($"[DB][DriverRepo] GetAllDrivers batch cars loaded: {carsByDriver.Count} driver groups");
                }
            }

            Logger.Log($"[DB][DriverRepo] GetAllDrivers → {drivers.Count} rows");
            return drivers;
        }

        public Driver GetDriverById(int id)
        {
            Logger.Log($"[DB][DriverRepo] GetDriverById(id={id})");
            Driver driver = null;

            using (var connection = Open())
            using (var cmd = new SQLiteCommand("SELECT * FROM Drivers WHERE Id = @Id", connection))
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
                            State = reader["State"] != DBNull.Value ? reader["State"].ToString() : string.Empty,
                            Cars = GetCarsByDriverId(id)
                        };
                    }
                }
            }

            Logger.Log($"[DB][DriverRepo] GetDriverById({id}) → {(driver == null ? "null" : "found")}");
            return driver;
        }

        public void AddDriver(Driver driver)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            Logger.Log($"[DB][DriverRepo] AddDriver(Name='{driver.Name}')");

            using (var connection = Open())
            {
                using (var tx = connection.BeginTransaction())
                {
                    Logger.Log("[DB][DriverRepo][TX] AddDriver begin");
                    try
                    {
                        const string sql = @"
INSERT INTO Drivers (Name, QualTime, Notes, TotalWins, TotalLosses, EventsEntered, EventsWon, State)
VALUES (@Name, @QualTime, @Notes, @TotalWins, @TotalLosses, @EventsEntered, @EventsWon, @State);
SELECT last_insert_rowid();";

                        using (var cmd = new SQLiteCommand(sql, connection, tx))
                        {
                            cmd.Parameters.AddWithValue("@Name", driver.Name);
                            cmd.Parameters.AddWithValue("@QualTime", (object)driver.QualTime ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Notes", driver.Notes ?? string.Empty);
                            cmd.Parameters.AddWithValue("@TotalWins", driver.TotalWins);
                            cmd.Parameters.AddWithValue("@TotalLosses", driver.TotalLosses);
                            cmd.Parameters.AddWithValue("@EventsEntered", driver.EventsEntered);
                            cmd.Parameters.AddWithValue("@EventsWon", driver.EventsWon);
                            cmd.Parameters.AddWithValue("@State", driver.State ?? string.Empty);

                            driver.Id = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (driver.Cars != null)
                        {
                            foreach (var car in driver.Cars)
                                AddCar(car, driver.Id, connection, tx);
                        }

                        tx.Commit();
                        Logger.Log("[DB][DriverRepo][TX] AddDriver commit");
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        Logger.Log($"[DB][DriverRepo][TX][ERROR] AddDriver rollback: {ex}");
                        throw;
                    }
                }
            }

            Logger.Log($"[DB][DriverRepo] AddDriver → new Id={driver.Id}");
        }

        public void UpdateDriver(Driver driver)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            Logger.Log($"[DB][DriverRepo] UpdateDriver(Id={driver.Id}, Name='{driver.Name}')");

            using (var connection = Open())
            {
                using (var tx = connection.BeginTransaction())
                {
                    Logger.Log("[DB][DriverRepo][TX] UpdateDriver begin");
                    try
                    {
                        const string sql = @"
UPDATE Drivers SET 
    Name = @Name, 
    QualTime = @QualTime, 
    Notes = @Notes, 
    TotalWins = @TotalWins, 
    TotalLosses = @TotalLosses, 
    EventsEntered = @EventsEntered, 
    EventsWon = @EventsWon,
    State = @State
WHERE Id = @Id";

                        using (var cmd = new SQLiteCommand(sql, connection, tx))
                        {
                            cmd.Parameters.AddWithValue("@Name", driver.Name);
                            cmd.Parameters.AddWithValue("@QualTime", (object)driver.QualTime ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Notes", driver.Notes ?? string.Empty);
                            cmd.Parameters.AddWithValue("@TotalWins", driver.TotalWins);
                            cmd.Parameters.AddWithValue("@TotalLosses", driver.TotalLosses);
                            cmd.Parameters.AddWithValue("@EventsEntered", driver.EventsEntered);
                            cmd.Parameters.AddWithValue("@EventsWon", driver.EventsWon);
                            cmd.Parameters.AddWithValue("@State", driver.State ?? string.Empty);
                            cmd.Parameters.AddWithValue("@Id", driver.Id);
                            cmd.ExecuteNonQuery();
                        }

                        var existingCarIds = new HashSet<int>();
                        using (var getExisting = new SQLiteCommand("SELECT CarID FROM Cars WHERE DriverId = @DriverId", connection, tx))
                        {
                            getExisting.Parameters.AddWithValue("@DriverId", driver.Id);
                            using (var reader = getExisting.ExecuteReader())
                            {
                                while (reader.Read())
                                    existingCarIds.Add(Convert.ToInt32(reader["CarID"]));
                            }
                        }

                        var keptCarIds = new HashSet<int>();
                        if (driver.Cars != null)
                        {
                            foreach (var car in driver.Cars)
                            {
                                if (car.CarID > 0 && existingCarIds.Contains(car.CarID))
                                {
                                    using (var updateCarCmd = new SQLiteCommand(@"
UPDATE Cars SET
    CarName = @CarName,
    ClassType = @ClassType,
    DefaultDialIn = @DefaultDialIn
WHERE CarID = @CarID AND DriverId = @DriverId", connection, tx))
                                    {
                                        updateCarCmd.Parameters.AddWithValue("@CarID", car.CarID);
                                        updateCarCmd.Parameters.AddWithValue("@DriverId", driver.Id);
                                        updateCarCmd.Parameters.AddWithValue("@CarName", car.CarName ?? string.Empty);
                                        updateCarCmd.Parameters.AddWithValue("@ClassType", car.ClassType ?? string.Empty);
                                        updateCarCmd.Parameters.AddWithValue("@DefaultDialIn", (object)car.DefaultDialIn ?? DBNull.Value);
                                        updateCarCmd.ExecuteNonQuery();
                                    }

                                    keptCarIds.Add(car.CarID);
                                }
                                else
                                {
                                    const string insertSql = @"
INSERT INTO Cars (DriverId, CarName, ClassType, DefaultDialIn)
VALUES (@DriverId, @CarName, @ClassType, @DefaultDialIn);
SELECT last_insert_rowid();";

                                    using (var insertCarCmd = new SQLiteCommand(insertSql, connection, tx))
                                    {
                                        insertCarCmd.Parameters.AddWithValue("@DriverId", driver.Id);
                                        insertCarCmd.Parameters.AddWithValue("@CarName", car.CarName ?? string.Empty);
                                        insertCarCmd.Parameters.AddWithValue("@ClassType", car.ClassType ?? string.Empty);
                                        insertCarCmd.Parameters.AddWithValue("@DefaultDialIn", (object)car.DefaultDialIn ?? DBNull.Value);
                                        car.CarID = Convert.ToInt32(insertCarCmd.ExecuteScalar());
                                    }

                                    keptCarIds.Add(car.CarID);
                                }
                            }
                        }

                        foreach (var existingCarId in existingCarIds)
                        {
                            if (keptCarIds.Contains(existingCarId)) continue;

                            using (var deleteCarCmd = new SQLiteCommand(
                                "DELETE FROM Cars WHERE DriverId = @DriverId AND CarID = @CarID",
                                connection,
                                tx))
                            {
                                deleteCarCmd.Parameters.AddWithValue("@DriverId", driver.Id);
                                deleteCarCmd.Parameters.AddWithValue("@CarID", existingCarId);
                                deleteCarCmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        Logger.Log("[DB][DriverRepo][TX] UpdateDriver commit");
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        Logger.Log($"[DB][DriverRepo][TX][ERROR] UpdateDriver rollback: {ex}");
                        throw;
                    }
                }
            }

            Logger.Log("[DB][DriverRepo] UpdateDriver → OK");
        }

        public void DeleteDriver(int id)
        {
            Logger.Log($"[DB][DriverRepo] DeleteDriver(Id={id})");

            using (var connection = Open())
            {
                using (var tx = connection.BeginTransaction())
                {
                    Logger.Log("[DB][DriverRepo][TX] DeleteDriver begin");
                    try
                    {
                        using (var cmd = new SQLiteCommand("DELETE FROM Cars WHERE DriverId = @DriverId", connection, tx))
                        {
                            cmd.Parameters.AddWithValue("@DriverId", id);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new SQLiteCommand("DELETE FROM Drivers WHERE Id = @Id", connection, tx))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        Logger.Log("[DB][DriverRepo][TX] DeleteDriver commit");
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        Logger.Log($"[DB][DriverRepo][TX][ERROR] DeleteDriver rollback: {ex}");
                        throw;
                    }
                }
            }

            Logger.Log("[DB][DriverRepo] DeleteDriver → OK");
        }

        public void AddCar(int driverId, Car car)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            Logger.Log($"[DB][DriverRepo] AddCar(driverId={driverId}, Car='{car.CarName}')");

            using (var conn = Open())
            {
                AddCar(car, driverId, conn);
            }
        }

        public void AddCar(Car car, int driverId, SQLiteConnection connection)
        {
            const string sql = @"
INSERT INTO Cars (DriverId, CarName, ClassType, DefaultDialIn)
VALUES (@DriverId, @CarName, @ClassType, @DefaultDialIn);";

            using (var cmd = new SQLiteCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@DriverId", driverId);
                cmd.Parameters.AddWithValue("@CarName", car.CarName ?? string.Empty);
                cmd.Parameters.AddWithValue("@ClassType", car.ClassType ?? string.Empty);
                cmd.Parameters.AddWithValue("@DefaultDialIn", (object)car.DefaultDialIn ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void AddCar(Car car, int driverId, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            const string sql = @"
INSERT INTO Cars (DriverId, CarName, ClassType, DefaultDialIn)
VALUES (@DriverId, @CarName, @ClassType, @DefaultDialIn);";

            using (var cmd = new SQLiteCommand(sql, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@DriverId", driverId);
                cmd.Parameters.AddWithValue("@CarName", car.CarName ?? string.Empty);
                cmd.Parameters.AddWithValue("@ClassType", car.ClassType ?? string.Empty);
                cmd.Parameters.AddWithValue("@DefaultDialIn", (object)car.DefaultDialIn ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private List<Car> GetCarsByDriverId(int driverId)
        {
            var cars = new List<Car>();
            Logger.Log($"[DB][DriverRepo] GetCarsByDriverId(driverId={driverId})");

            using (var connection = Open())
            using (var cmd = new SQLiteCommand("SELECT * FROM Cars WHERE DriverId = @DriverId", connection))
            {
                cmd.Parameters.AddWithValue("@DriverId", driverId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var car = new Car
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

            Logger.Log($"[DB][DriverRepo] GetCarsByDriverId → {cars.Count} rows");
            return cars;
        }

        public void UpdateQualifyingTime(int driverId, double qualTime)
        {
            Logger.Log($"[DB][DriverRepo] UpdateQualifyingTime(id={driverId}, time={qualTime})");

            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Drivers SET QualTime = @QualTime WHERE Id = @Id";
                command.Parameters.AddWithValue("@QualTime", qualTime);
                command.Parameters.AddWithValue("@Id", driverId);
                command.ExecuteNonQuery();
            }

            Logger.Log("[DB][DriverRepo] UpdateQualifyingTime → OK");
        }

        public void IncrementEventsEntered(int driverId, int delta = 1)
        {
            Logger.Log($"[DB][DriverRepo][STATS] IncrementEventsEntered(driverId={driverId}, delta={delta})");
            ExecuteStatIncrement(driverId, "EventsEntered", delta);
        }

        public void IncrementEventsWon(int driverId, int delta = 1)
        {
            Logger.Log($"[DB][DriverRepo][STATS] IncrementEventsWon(driverId={driverId}, delta={delta})");
            ExecuteStatIncrement(driverId, "EventsWon", delta);
        }

        public void IncrementWins(int driverId, int delta = 1)
        {
            Logger.Log($"[DB][DriverRepo][STATS] IncrementWins(driverId={driverId}, delta={delta})");
            ExecuteStatIncrement(driverId, "TotalWins", delta);
        }

        public void IncrementLosses(int driverId, int delta = 1)
        {
            Logger.Log($"[DB][DriverRepo][STATS] IncrementLosses(driverId={driverId}, delta={delta})");
            ExecuteStatIncrement(driverId, "TotalLosses", delta);
        }

        public void IncrementWinsAndLosses(int winnerDriverId, int loserDriverId, int winDelta = 1, int lossDelta = 1)
        {
            Logger.Log($"[DB][DriverRepo][STATS] IncrementWinsAndLosses(winnerId={winnerDriverId}, loserId={loserDriverId}, winDelta={winDelta}, lossDelta={lossDelta})");
            using (var connection = Open())
            using (var tx = connection.BeginTransaction())
            {
                Logger.Log("[DB][DriverRepo][TX] IncrementWinsAndLosses begin");
                try
                {
                    using (var winCmd = new SQLiteCommand("UPDATE Drivers SET TotalWins = TotalWins + @Delta WHERE Id = @Id", connection, tx))
                    {
                        winCmd.Parameters.AddWithValue("@Delta", winDelta);
                        winCmd.Parameters.AddWithValue("@Id", winnerDriverId);
                        winCmd.ExecuteNonQuery();
                    }

                    using (var lossCmd = new SQLiteCommand("UPDATE Drivers SET TotalLosses = TotalLosses + @Delta WHERE Id = @Id", connection, tx))
                    {
                        lossCmd.Parameters.AddWithValue("@Delta", lossDelta);
                        lossCmd.Parameters.AddWithValue("@Id", loserDriverId);
                        lossCmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                    Logger.Log("[DB][DriverRepo][TX] IncrementWinsAndLosses commit");
                }
                catch (Exception ex)
                {
                    try { tx.Rollback(); } catch { }
                    Logger.Log($"[DB][DriverRepo][TX][ERROR] IncrementWinsAndLosses rollback: {ex}");
                    throw;
                }
            }
        }

        // ---- Stats helper unchanged ----
        public int ComputeEventsWonFromSavedSessions(int driverId)
        {
            Logger.Log($"[STATS] ComputeEventsWonFromSavedSessions: driverId={driverId}");
            int wins = 0;

            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT SessionData FROM RaceSessions";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            string json = reader.IsDBNull(0) ? null : reader.GetString(0);
                            if (string.IsNullOrWhiteSpace(json)) continue;

                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;

                            if (!root.TryGetProperty("SavedResults", out var arr) || arr.ValueKind != JsonValueKind.Array)
                            {
                                Logger.Log("[STATS] Session skipped: no SavedResults array.");
                                continue;
                            }

                            var winners = new HashSet<int>();
                            var losers = new HashSet<int>();

                            foreach (var r in arr.EnumerateArray())
                            {
                                int? w = TryReadInt(r, "WinnerDriverId") ?? TryReadInt(r, "WinnerId");
                                int? l = TryReadInt(r, "LoserDriverId") ?? TryReadInt(r, "LoserId");
                                if (w.HasValue && w.Value > 0) winners.Add(w.Value);
                                if (l.HasValue && l.Value > 0) losers.Add(l.Value);
                            }

                            winners.ExceptWith(losers);
                            if (winners.Contains(driverId)) wins++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"[STATS] Session parse failed: {ex.Message}");
                        }
                    }
                }
            }

            Logger.Log($"[STATS] EventsWon computed for DriverId={driverId}: {wins}");
            return wins;

            static int? TryReadInt(JsonElement obj, string name)
            {
                if (!obj.TryGetProperty(name, out var el)) return null;
                try
                {
                    if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n)) return n;
                    if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var s)) return s;
                }
                catch { }
                return null;
            }
        }

        private Dictionary<int, List<Car>> GetCarsByDriverIds(SQLiteConnection connection, List<int> driverIds)
        {
            var map = new Dictionary<int, List<Car>>();
            if (driverIds == null || driverIds.Count == 0)
                return map;

            var placeholders = new List<string>();
            for (int i = 0; i < driverIds.Count; i++)
                placeholders.Add("@d" + i);

            var sql = "SELECT CarID, DriverId, CarName, ClassType, DefaultDialIn FROM Cars WHERE DriverId IN (" + string.Join(",", placeholders) + ")";
            using (var cmd = new SQLiteCommand(sql, connection))
            {
                for (int i = 0; i < driverIds.Count; i++)
                    cmd.Parameters.AddWithValue(placeholders[i], driverIds[i]);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var driverId = Convert.ToInt32(reader["DriverId"]);
                        if (!map.TryGetValue(driverId, out var list))
                        {
                            list = new List<Car>();
                            map[driverId] = list;
                        }

                        list.Add(new Car
                        {
                            CarID = Convert.ToInt32(reader["CarID"]),
                            CarName = reader["CarName"].ToString(),
                            ClassType = reader["ClassType"].ToString(),
                            DefaultDialIn = reader["DefaultDialIn"] != DBNull.Value ? (double?)Convert.ToDouble(reader["DefaultDialIn"]) : null
                        });
                    }
                }
            }

            return map;
        }

        private void ExecuteStatIncrement(int driverId, string columnName, int delta)
        {
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"UPDATE Drivers SET {columnName} = {columnName} + @Delta WHERE Id = @Id";
                command.Parameters.AddWithValue("@Delta", delta);
                command.Parameters.AddWithValue("@Id", driverId);
                command.ExecuteNonQuery();
            }
        }
    }
}
