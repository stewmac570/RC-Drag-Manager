# RC Drag Manager — Data Layer

## Database Technology

- **Engine:** SQLite (via `System.Data.SQLite` NuGet)
- **File:** `%APPDATA%\RC_Drag_Manager\race_data.db`
- **Versioning:** SQLite Version 3 (`Version=3` in connection string)
- **Connection string format:** `Data Source=<absolute path>;Version=3;`

Both `DriverRepository` and `RaceSessionRepository` accept either a full connection string or a bare file path — they normalize internally via `NormalizeConnString()`.

Schema is created and maintained by `DatabaseInitializer.InitializeDatabase(connStr)`, called once at app startup (`Program.cs`). It uses `CREATE TABLE IF NOT EXISTS` so it is safe to call on every launch.

---

## Schema

### `Drivers`

```sql
CREATE TABLE IF NOT EXISTS Drivers (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Name            TEXT    NOT NULL,
    QualTime        REAL,
    Notes           TEXT,
    TotalWins       INTEGER NOT NULL DEFAULT 0,
    TotalLosses     INTEGER NOT NULL DEFAULT 0,
    EventsEntered   INTEGER NOT NULL DEFAULT 0,
    EventsWon       INTEGER NOT NULL DEFAULT 0,
    State           TEXT
);
```

`State` was added post-initial-release. `DatabaseInitializer` adds the column via `ALTER TABLE IF NOT EXISTS` for backward compatibility with existing databases.

---

### `Cars`

```sql
CREATE TABLE IF NOT EXISTS Cars (
    CarID           INTEGER PRIMARY KEY AUTOINCREMENT,
    DriverId        INTEGER NOT NULL REFERENCES Drivers(Id),
    CarName         TEXT,
    ClassType       TEXT,
    DefaultDialIn   REAL
);
```

Cars are child records. When a driver is deleted, their cars are deleted first (application-level cascade in `DriverRepository.DeleteDriver`).

---

### `RaceSessions`

```sql
CREATE TABLE IF NOT EXISTS RaceSessions (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    EventName   TEXT,
    EventDate   TEXT,
    ClassType   TEXT,
    RaceType    TEXT,
    SessionData TEXT    -- full JSON-serialized RaceSession object
);
```

The entire `RaceSession` object is serialized to JSON and stored in `SessionData`. The other columns (`EventName`, `EventDate`, etc.) are scalar copies for the session list view — they do not need to be kept in sync with the JSON blob; the blob is the source of truth on load.

---

## Repositories

### `DatabaseInitializer` (`Repositories/DatabaseInitializer.cs`)

Sole responsibility: ensure the schema exists.

- Called once from `Program.cs` at startup.
- Uses `IF NOT EXISTS` and `ALTER TABLE … ADD COLUMN IF NOT EXISTS` — safe to run every launch.
- No instance state; all methods are static.

---

### `DriverRepository` (`Repositories/DriverRepository.cs`)

Owns all read/write for the `Drivers` and `Cars` tables.

| Method | Notes |
|--------|-------|
| `GetAllDrivers()` | Loads all drivers + their cars in two queries (batch car load by driver IDs) |
| `GetDriverById(id)` | Single driver with cars |
| `AddDriver(driver)` | INSERT driver + its cars in a transaction; sets `driver.Id` |
| `UpdateDriver(driver)` | UPDATE driver fields; diff-syncs car list (insert new, update existing, delete removed) |
| `DeleteDriver(id)` | DELETE cars first, then driver |
| `AddCar(driverId, car)` | Insert a new car for a driver |
| `UpdateQualifyingTime(driverId, qualTime)` | Targeted qual time update |
| `IncrementWins(driverId, delta)` | `UPDATE Drivers SET TotalWins = TotalWins + @Delta` |
| `IncrementLosses(driverId, delta)` | Same pattern |
| `IncrementEventsEntered(driverId, delta)` | Same pattern |
| `IncrementEventsWon(driverId, delta)` | Same pattern |
| `IncrementWinsAndLosses(winnerId, loserId, ...)` | Atomically increments both in one transaction |
| `ComputeEventsWonFromSavedSessions(driverId)` | Scans all `RaceSessions.SessionData` JSON to count events won |

**Security note:** `ExecuteStatIncrement` uses a whitelist (`_allowedStatColumns`) to prevent SQL injection from column name interpolation — fixed in issue #101.

---

### `CarRepository` (`Repositories/CarRepository.cs`)

Lightweight alternative car access. Used in some forms. `DriverRepository` also handles cars, so these two can overlap. `CarRepository` is not a full replacement.

---

### `RaceSessionRepository` (`Repositories/RaceSessionRepository.cs`)

Owns the `RaceSessions` table.

| Method | Notes |
|--------|-------|
| `SaveSession(session)` | INSERT new row; sets `session.Id`; full `RaceSession` serialized to JSON |
| `GetAllSessions()` | Returns `List<RaceSessionSummary>` (no JSON deserialization — scalar columns only) |
| `LoadSession(id)` | SELECT `SessionData`, deserialize JSON → `RaceSession` |
| `DeleteSession(id)` | DELETE by Id |

There is no `UpdateSession`. Every save is a new INSERT. Sessions are append-only; old records are not overwritten. (This means saving an in-progress event creates a new row each time.)

---

## Serialization Approach

The entire `RaceSession` object is serialized using `System.Text.Json`:

```csharp
string json = JsonSerializer.Serialize(session, new JsonSerializerOptions {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
});
```

On load, deserialized with:

```csharp
var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var session = JsonSerializer.Deserialize<RaceSession>(json, opts);
```

### What Gets Serialized

Everything in `RaceSession` that is not `[JsonIgnore]`:
- `DriverEntries`, `Drivers`, `BuybackDrivers`, `TopDriversSnapshot`
- `Matches` (RandomMatch list), `RoundRobinMatches`
- `SavedResults` (MatchResultSave list)
- `SavedRevealedRounds`
- `PairingHistoryRaw` (the `int[]` list — see below)
- All scalar fields: `EventName`, `EventDate`, `RaceType`, `ClassType`, etc.

### What Does NOT Get Serialized

- `PairingHistory` — marked `[JsonIgnore]` because `HashSet<(int,int)>` contains `ValueTuple`, which `System.Text.Json` cannot serialize. Its backing store `PairingHistoryRaw` is serialized instead.
- `MatchResult` — in-memory only; reconstructed from `SavedResults` on load.
- Engine state (bracket structure) — **not persisted at all**. The bracket is regenerated from the session's driver list and race type when a session is resumed. **This means a loaded session does not automatically resume mid-bracket** — it restarts from the beginning.

---

## Known Quirks and Historical Issues

### PairingHistory Serialization (Issue #96)

**Problem:** `RaceSession.PairingHistory` was `HashSet<(int,int)>`. `System.Text.Json` cannot handle `ValueTuple`, so this field was silently dropped on serialize/deserialize. After a session reload, the pairing history was empty, meaning rematch avoidance didn't work across sessions.

**Fix:** Added `PairingHistoryRaw = List<int[]>` as the serialization backing store. `PairingHistory` is now `[JsonIgnore]` and computed on demand from `PairingHistoryRaw`. The setter converts the `HashSet` back to `List<int[]>`.

```csharp
[JsonIgnore]
public HashSet<(int, int)> PairingHistory
{
    get => new HashSet<(int, int)>(PairingHistoryRaw.Select(a => (a[0], a[1])));
    set => PairingHistoryRaw = value.Select(t => new[] { t.Item1, t.Item2 }).ToList();
}
```

### Sessions Are Append-Only

`SaveSession` always INSERTs. There is no UPDATE. If a user saves mid-event and then saves again, two rows exist. `LoadSessionForm` shows all rows; the user picks the latest.

### Stats Increment vs Recompute

`TotalWins` / `TotalLosses` are incremented live via `IncrementWinsAndLosses` when a winner is submitted. `EventsWon` can also be computed from scratch by `ComputeEventsWonFromSavedSessions` — this re-scans all JSON blobs and counts events won. This is used by `DriverStatsForm` for accuracy.

### SQL Column Interpolation Risk (Issue #101 — Fixed)

`ExecuteStatIncrement` previously interpolated the column name directly into the SQL string. This is now guarded by a `HashSet<string>` whitelist of allowed column names, throwing `ArgumentException` if an unexpected name is passed.
