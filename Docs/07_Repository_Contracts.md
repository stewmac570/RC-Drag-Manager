# RC Drag Manager — Repository Contracts  
**File:** 07_Repository_Contracts.md  
**Version:** 1.00  
**Status:** ✅ Stable (ChatGPT-Pack Ready)  
**Last Updated:** 2025-10-12  
**Owner:** Stewart McMillan  
**Source of Truth:** Derived from `06_SQLite_Schema.md`, `MatchEngine_Refactor_Spec.md`, and production repository classes.

---

## 🤖 How ChatGPT Should Use This Doc

This document defines the **data repository layer** used by RC Drag Manager.  
It describes the contract interfaces, expected behaviors, and error-handling guarantees for all persistence classes that communicate with SQLite.  

Use this to understand:
- Which repositories manage which tables.  
- How objects are serialized/deserialized.  
- How transactions ensure consistency across race sessions.  
- Where logs, backups, and recovery hooks apply.

See also:  
- `06_SQLite_Schema.md` — table definitions.  
- `03_Controller_Engine_Contracts.md` — where repositories integrate with controllers.  
- `09_Error_Handling_Logging.md` — for logging behaviors.

---

## 🎯 Purpose

Provide a consistent, testable abstraction over SQLite persistence so that business logic (race engines, controllers, UI) never directly touches SQL statements.  
Repositories handle all data CRUD operations and serialization logic.

---

## 🧱 Repository Overview

| Repository | Primary Responsibility |
|-------------|------------------------|
| `DriverRepository` | Manage driver data (CRUD, stats tracking). |
| `CarRepository` | Store and retrieve car configurations. |
| `MatchRepository` | Manage per-match records, lane assignments, and results. |
| `RaceSessionRepository` | Save/load full race sessions and standings. |
| `SettingsRepository` | Handle app-level configuration and flags. |
| `LogRepository` *(optional)* | Persist logs if file output unavailable. |

---

## 🔗 1. Common Contract Pattern

All repositories implement a consistent interface pattern:

```vbnet
Public Interface IRepository(Of T)
    Function GetAll() As List(Of T)
    Function GetById(id As Guid) As T
    Sub Insert(entity As T)
    Sub Update(entity As T)
    Sub Delete(id As Guid)
End Interface
```

Each repository extends this pattern with mode-specific helpers and transactional guarantees.

---

## 🗂️ 2. DriverRepository

**Purpose:** Manage driver roster, stats, and basic metadata.

**Responsibilities:**
- Create and update driver profiles.  
- Track wins, losses, and event history.  
- Link drivers to car records.  

**Methods:**
```vbnet
Sub SaveDriver(driver As Driver)
Function GetDriverById(driverId As Guid) As Driver
Sub UpdateStats(driverId As Guid, wins As Integer, losses As Integer)
Function GetAllDrivers() As List(Of Driver)
```

**Behavior:**
- Updates stats atomically after each race result.  
- Logs all writes using `Logger.Log("DriverRepository.Save", driver.Name)`.

---

## 🗂️ 3. CarRepository

**Purpose:** Persist and retrieve cars linked to drivers.  
Supports future tuning and setup management.

**Methods:**
```vbnet
Sub SaveCar(car As Car)
Function GetCarByDriver(driverId As Guid) As Car
Function GetAllCars() As List(Of Car)
```

**Storage Notes:**
- Linked to `Drivers.CarId`.  
- No orphaned cars allowed — integrity enforced by foreign key.

---

## 🗂️ 4. MatchRepository

**Purpose:** Manage all match records, including lane assignments and results.  

**Methods:**
```vbnet
Sub SaveMatch(match As Match)
Function GetMatchesBySession(sessionId As Guid) As List(Of Match)
Sub UpdateWinner(matchId As Guid, winnerId As Guid)
Function GetUnfinishedMatches(sessionId As Guid) As List(Of Match)
```

**Behavior:**
- Ensures each match has unique `(SessionId, RoundNumber, LeftLaneDriver, RightLaneDriver)`.  
- Writes serialized JSON to `Matches` table.  
- Automatically logs `LaneSeed` for deterministic replay.  

**Lane Shuffle Integration:**
- The repository **never randomizes lanes**.  
- It simply stores values passed from the engine.  
- Ensures that `LeftLaneDriver` and `RightLaneDriver` persist identically across reloads.

---

## 🗂️ 5. RaceSessionRepository

**Purpose:** Core persistence layer — handles saving and restoring full sessions.

**Methods:**
```vbnet
Sub SaveSession(session As RaceSession)
Function LoadSession(sessionId As Guid) As RaceSession
Function GetRecentSessions(limit As Integer) As List(Of RaceSession)
Sub DeleteSession(sessionId As Guid)
```

**Behavior:**
- Serializes all in-memory structures (drivers, matches, standings) into JSON fields in `RaceSessions`.  
- Maintains **PairingHistory**, **StandingsData**, and **SerializedMatches** as embedded JSON.  
- Automatically version-tags sessions for backward compatibility.

**Atomic Commit Example:**
```vbnet
BeginTransaction()
    SaveSessionMetadata()
    SaveAllMatches()
Commit()
```

**Logging:**  
Each commit entry is logged:  
`[Session Save] {SessionId} | Phase: {CurrentPhase} | {MatchCount} matches saved.`

---

## 🗂️ 6. SettingsRepository

**Purpose:** Store global configuration and user preferences.

**Methods:**
```vbnet
Function GetSetting(key As String) As String
Sub SetSetting(key As String, value As String)
Function GetAllSettings() As Dictionary(Of String, String)
```

**Typical Keys:**
| Key | Example Value |
|-----|----------------|
| `EnableLogging` | `"true"` |
| `LaneShuffleBias` | `"Balanced"` |
| `AppDataPath` | `"C:\\Users\\%USERNAME%\\AppData\\RCDragManager"` |

**Behavior:**
- Values stored as JSON.  
- Automatically created on first use if missing.  
- Supports export/import to file for migration.

---

## 🗂️ 7. LogRepository *(optional)*

**Purpose:** Persist logs into SQLite instead of file system (fallback mode).  
Used when file write access is unavailable or disabled.

**Methods:**
```vbnet
Sub WriteLog(level As String, message As String, source As String)
Function GetLogsByLevel(level As String) As List(Of LogEntry)
```

**Storage:**  
Writes to `Logs` table, matching structure from `06_SQLite_Schema.md`.

---

## ⚙️ 8. Transaction Management

All repositories share a simple transaction wrapper via `DatabaseConnectionManager`:

```vbnet
Using conn = New SQLiteConnection(DB_PATH)
    Using tran = conn.BeginTransaction()
        ' repository writes
        tran.Commit()
    End Using
End Using
```

- Rollback on any exception.  
- Exceptions logged by `ErrorLogger`.  
- Ensures no partial session saves.

---

## 🧩 Serialization Rules

| Object | Method | Format |
|--------|---------|--------|
| `Driver` | JSON | Stored in `RaceSessions.DriverList` |
| `Match` | JSON | Stored in `SerializedMatches` |
| `Standings` | JSON | Stored in `StandingsData` |

### Example Serialization Snapshot
```json
{
  "SessionId": "guid",
  "RoundNumber": 3,
  "LeftLaneDriver": "guid",
  "RightLaneDriver": "guid",
  "WinnerId": "guid",
  "LaneSeed": 4782
}
```

---

## 🧱 Repository Error Policy

| Condition | Action | Logged Message |
|------------|---------|----------------|
| Insert fails (constraint) | Retry once | `[Repository] Constraint violation, retrying insert.` |
| JSON serialization error | Skip entity, flag warning | `[Serializer] Failed to encode Match {Id}.` |
| Missing FK (Driver/Car) | Skip record, log error | `[Integrity] Match missing driver reference.` |
| File write fail | Redirect to LogRepository | `[IOFallback] Writing to DB instead of file.` |

---

## 🧱 Adjacent Docs

| File | Purpose |
|------|----------|
| `06_SQLite_Schema.md` | Defines physical DB schema. |
| `03_Controller_Engine_Contracts.md` | Connects repository calls to logic flow. |
| `09_Error_Handling_Logging.md` | Describes logger & exception handling. |
| `13_Project_Status_Summary.md` | Development timeline overview. |

---

## ✅ Summary

The repository layer in RC Drag Manager provides a **clean separation between logic and data persistence**, ensuring:
- Deterministic JSON-based session saves.  
- Fair lane replay integrity.  
- Atomic commits and automatic error logging.  

Each repository enforces schema alignment, transaction safety, and full traceability of driver and match data — a foundation for all race mode engines (Randomized, Round Robin, and Pro Ladder).

---
