# RC Drag Manager — SQLite Schema Reference  
**File:** 06_SQLite_Schema.md  
**Version:** 1.00  
**Status:** ✅ Stable (ChatGPT-Pack Ready)  
**Last Updated:** 2025-10-12  
**Owner:** Stewart McMillan  
**Source of Truth:** Derived from repository architecture, controller contracts, and verified engine specs.

---

## 🤖 How ChatGPT Should Use This Doc

This file defines the **persistent data model** for the RC Drag Manager application.  
Use it to understand:
- How objects in memory (`RaceSession`, `Match`, `Driver`, etc.) map to SQLite tables.  
- Which fields are serialized versus relational.  
- How session and lane data are persisted and reloaded deterministically.  

See also:  
- `02_System_Overview.md` — lifecycle and architecture.  
- `03_Controller_Engine_Contracts.md` — logic orchestration.  
- `05_Mode_RoundRobin_Spec.md` — lane shuffle context.

---

## 🎯 Purpose

Provide a clear, consistent, database-level schema reference that matches the application’s current production data model.  
Defines all tables, fields, and relationships used by RC Drag Manager, including newly added **lane persistence** in the `Matches` table.

---

## 🧱 Schema Overview

| Table | Purpose |
|--------|----------|
| `Drivers` | Persistent driver registry with stats. |
| `Cars` | Vehicles linked to drivers. |
| `RaceSessions` | Event/session metadata and serialized match data. |
| `Matches` | Individual race match entries, including lanes and results. |
| `Settings` | Global configuration and flags (JSON). |
| `Logs` | Optional persistent log table (supplementary). |

All tables created automatically on startup by `DatabaseInitializer.cs`.

---

## 🗂️ 1. Drivers Table

| Column | Type | Description |
|---------|------|-------------|
| `DriverId` | TEXT (GUID, PK) | Unique driver identifier. |
| `Name` | TEXT | Full driver name. |
| `Alias` | TEXT | Optional display nickname. |
| `Team` | TEXT | Team name or sponsor. |
| `CarId` | TEXT (FK) | Associated car record. |
| `Stats_Wins` | INTEGER | Lifetime wins. |
| `Stats_Losses` | INTEGER | Lifetime losses. |
| `Stats_EventsWon` | INTEGER | Event victories. |
| `LastUpdated` | TEXT (ISO 8601) | Last modification timestamp. |

---

## 🗂️ 2. Cars Table

| Column | Type | Description |
|---------|------|-------------|
| `CarId` | TEXT (GUID, PK) | Unique vehicle identifier. |
| `DriverId` | TEXT (FK) | Linked driver. |
| `Model` | TEXT | Car model name. |
| `Class` | TEXT | Race class (Outlaw, Dry Tire, etc.). |
| `Chassis` | TEXT | Chassis manufacturer (e.g., Rlaarlo, Limitless). |
| `Motor` | TEXT | Motor type or KV. |
| `ESC` | TEXT | ESC model. |
| `GearRatio` | TEXT | Optional setup notes. |

---

## 🗂️ 3. RaceSessions Table

| Column | Type | Description |
|---------|------|-------------|
| `SessionId` | TEXT (GUID, PK) | Unique identifier for each session. |
| `Name` | TEXT | Event name or label. |
| `DateCreated` | TEXT (ISO 8601) | Session creation date/time. |
| `RaceType` | TEXT | “ProLadder”, “Random”, or “RoundRobin”. |
| `DriverList` | TEXT (JSON) | Serialized list of participating drivers. |
| `CurrentPhase` | TEXT | Current race phase (`RoundRobin`, `LosersBracket`, `ProLadder`, `Complete`). |
| `StandingsData` | TEXT (JSON) | Current ranking or point table. |
| `PairingHistory` | TEXT (JSON) | Record of past matchups for repeat prevention. |
| `SerializedMatches` | TEXT (JSON) | Serialized matches (see Matches table format). |
| `Notes` | TEXT | Optional comments. |

> 💾 Sessions serialize full objects (driver GUIDs, match lists, standings) into JSON for deterministic reloads.

---

## 🗂️ 4. Matches Table

**Purpose:** Track every race pairing and result — persistent between saves.  
Updated for lane shuffle support.

| Column | Type | Description |
|---------|------|-------------|
| `MatchId` | TEXT (GUID, PK) | Unique match record identifier. |
| `SessionId` | TEXT (FK) | Parent `RaceSession`. |
| `RoundNumber` | INTEGER | Round in which this match occurred. |
| `LeftLaneDriver` | TEXT (FK → Drivers.DriverId) | Driver assigned to the left lane. |
| `RightLaneDriver` | TEXT (FK → Drivers.DriverId) | Driver assigned to the right lane. |
| `WinnerId` | TEXT (FK → Drivers.DriverId, NULL) | Recorded winner. |
| `IsComplete` | INTEGER (0/1) | Completion flag. |
| `Timestamp` | TEXT (ISO 8601) | Last update time. |
| `LaneSeed` | INTEGER | Random seed used for lane assignment (for replay determinism). |

> The `LaneSeed` is logged with each match to ensure replay consistency if re-randomization logic changes.

---

## 🗂️ 5. Settings Table

| Column | Type | Description |
|---------|------|-------------|
| `Key` | TEXT (PK) | Setting identifier. |
| `Value` | TEXT | JSON-encoded setting value. |

Example:
```json
{
  "EnableLogging": true,
  "AppDataPath": "%APPDATA%\\RC_Drag_Manager",
  "LaneShuffleBias": "Random"   // or "Balanced"
}
```

---

## 🗂️ 6. Logs Table (optional)

| Column | Type | Description |
|---------|------|-------------|
| `LogId` | INTEGER (PK, AUTOINCREMENT) | Log entry ID. |
| `Timestamp` | TEXT (ISO 8601) | Log timestamp. |
| `Level` | TEXT | Log severity (INFO, WARN, ERROR). |
| `Message` | TEXT | Log text. |
| `Source` | TEXT | Component name (Engine, Controller, UI). |

Typically not persisted in production; logs default to file-based at:  
`%APPDATA%\RC_Drag_Manager\app.log`

---

## 🔄 Relationships Overview

```
Drivers (1)───(1..*) Cars
Drivers (1)───(0..*) Matches
RaceSessions (1)───(0..*) Matches
RaceSessions (1)───(1) StandingsData (JSON)
```

All key relationships use GUIDs for deterministic serialization.

---

## 🧩 JSON Structures (Embedded in RaceSessions)

### 🔹 Driver Object (Serialized)
```json
{
  "DriverId": "guid",
  "Name": "John Doe",
  "CarId": "guid",
  "Stats": { "Wins": 3, "Losses": 1 }
}
```

### 🔹 Match Object (Serialized)
```json
{
  "MatchId": "guid",
  "RoundNumber": 2,
  "LeftLaneDriver": "guid",
  "RightLaneDriver": "guid",
  "WinnerId": "guid",
  "IsComplete": true,
  "LaneSeed": 12345
}
```

### 🔹 Standings Object
```json
{
  "DriverId": "guid",
  "Points": 5,
  "Wins": 2,
  "Losses": 1,
  "Rank": 3
}
```

---

## 🔐 Data Integrity Rules

| Rule | Description |
|------|-------------|
| **All GUIDs persistent** | No re-generation on reload. |
| **Match results atomic** | Each match has exactly one winner or BYE. |
| **Lane assignments saved** | `LeftLaneDriver` and `RightLaneDriver` always populated. |
| **No direct schema edits** | Only modified via `DatabaseInitializer` or repository logic. |
| **Version-safe JSON** | Backward compatible; deserializer tolerates missing keys. |

---

## 🧱 Adjacent Docs

| File | Purpose |
|------|----------|
| `05_Mode_RoundRobin_Spec.md` | Defines lane shuffle and RR flow. |
| `03_Controller_Engine_Contracts.md` | Outlines engine interaction. |
| `07_Repository_Contracts.md` | Describes how repositories write to these tables. |
| `09_Error_Handling_Logging.md` | Logging and fault policy. |
| `13_Project_Status_Summary.md` | Phase-tracking overview. |

---

## ✅ Summary

The RC Drag Manager SQLite schema provides a lightweight, deterministic data model supporting:
- Driver and car registries.  
- Serialized sessions with complete lane and pairing history.  
- Full replay integrity.  

New **lane shuffle** fields (`LeftLaneDriver`, `RightLaneDriver`, and `LaneSeed`) guarantee that lane assignments remain identical when sessions are saved and reloaded — maintaining fairness and consistency across all race modes.

---
