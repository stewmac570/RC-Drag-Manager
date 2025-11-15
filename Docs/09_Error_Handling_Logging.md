# RC Drag Manager — Error Handling & Logging Policy  
**File:** 09_Error_Handling_Logging.md  
**Version:** 1.00  
**Status:** ✅ Stable (ChatGPT-Pack Ready)  
**Last Updated:** 2025-10-12  
**Owner:** Stewart McMillan  
**Source of Truth:** Derived from repository logs, `MatchEngine_Refactor_Spec.md`, and `Form1` UI behavior.

---

## 🤖 How ChatGPT Should Use This Doc

Use this file to understand how **RC Drag Manager** detects, handles, and logs errors across its application stack.  
It defines:
- Centralized logger conventions.  
- Error severity levels.  
- User-facing message patterns (non-blocking).  
- Retry, rollback, and recovery policies.  

See also:  
- `07_Repository_Contracts.md` — DB exception handling and retry logic.  
- `08_UI_UX_Surface_Map.md` — message display behavior.  
- `06_SQLite_Schema.md` — schema integrity constraints.  
- `13_Project_Status_Summary.md` — tracked stability notes.

---

## 🎯 Purpose

To guarantee **traceable, recoverable, and user-safe error handling**.  
No operation in RC Drag Manager should terminate silently or block the UI with modal errors.  
All faults must:
1. Be logged to disk or SQLite fallback.  
2. Display a clear message in the UI (non-blocking).  
3. Recover gracefully where possible.

---

## 🧱 Logger Overview

| Component | Target | Output |
|------------|---------|--------|
| **AppLogger** | File-based logger (`app.log`) | Main event trace. |
| **ErrorLogger** | File + console (UI-safe) | Error and exception capture. |
| **RepositoryLogger** | Same file, `[Repository]` prefix | DB and transaction tracing. |
| **EngineLogger** | `[Engine]` prefix | Race logic, pairing, lane shuffle details. |

Default file path:  
```
%APPDATA%\RC_Drag_Manager\logs\app.log
```

---

## 🧩 Log Levels

| Level | Use Case | Example |
|--------|-----------|---------|
| **INFO** | Normal state changes | “Round 2 complete (3 matches)” |
| **WARN** | Recoverable issues | “Missing driver alias — default applied.” |
| **ERROR** | Non-critical fault handled automatically | “Could not serialize Match {GUID}.” |
| **FATAL** | Critical exception (requires shutdown) | “DB connection lost — app terminating.” |
| **DEBUG** | Development or replay diagnostics | “Lane seed 3271 applied (L=Driver5 / R=Driver3).” |

---

## 🔄 Logging Flow

```
UI Action
  ↓
Controller Event
  ↓
Engine Operation
  ↓
Repository Save
  ↓
AppLogger → app.log
```

Each layer adds a prefix (e.g., `[UI]`, `[Controller]`, `[Engine]`, `[Repository]`) to identify source context.

---

## 🧩 Typical Log Entry

```
[INFO] 2025-10-12 14:45:22 [RoundRobinEngine] Round 2 generated (6 matches)
[DEBUG] 2025-10-12 14:45:22 [Engine] LaneSeed=3941 | Left=DriverA | Right=DriverC
[INFO] 2025-10-12 14:45:30 [Controller] Winner set: Match 2 | Winner=DriverC
[INFO] 2025-10-12 14:46:00 [Repository] Session saved | Matches=18 | Phase=RoundRobin
```

---

## 🛑 Error Handling Policy by Layer

### 🔹 UI Layer (`Form1`)
| Fault | Handling | User Message |
|--------|-----------|---------------|
| Button click throws | Catch in event handler | “Action failed. Check logs for details.” |
| Null driver reference | Highlight label red | “⚠ Missing driver record.” |
| File I/O block | Non-blocking popup | “Log write failed — retrying in background.” |

---

### 🔹 Controller Layer
| Fault | Handling | Behavior |
|--------|-----------|-----------|
| Invalid match ID | Log error + skip | Controller ignores and continues. |
| Engine not initialized | Log + show alert | UI displays “Engine unavailable.” |
| RacePhase mismatch | Log warning | Controller forces re-sync. |

Example:
```vbnet
Try
    engine.SetWinner(matchId, driverId)
Catch ex As Exception
    ErrorLogger.Log("SetWinner failed: " & ex.Message)
End Try
```

---

### 🔹 Engine Layer (RoundRobin, Random, Ladder)
| Fault | Handling | Example Log |
|--------|-----------|--------------|
| Duplicate pairing attempt | Skip + log | `[WARN] Pairing (D1,D3) already exists.` |
| Lane shuffle failure | Retry with new seed | `[DEBUG] LaneShuffle retry seed=5712` |
| Missing driver | Skip + flag round incomplete | `[ERROR] Missing driver in Round 3 match.` |
| Invalid winner | Log + reject | `[WARN] Winner not found in match.` |

---

### 🔹 Repository Layer
| Fault | Handling | Example Log |
|--------|-----------|--------------|
| Constraint violation | Retry once | `[Repository] Retry insert after constraint violation.` |
| Serialization error | Skip + log | `[Serializer] Failed encoding Match {GUID}.` |
| DB unavailable | Retry 3x then fallback | `[DB] Reconnect attempt 2/3 failed.` |
| JSON decode fail | Continue with defaults | `[Repository] Deserialization fallback (defaults used).` |

---

## 🧩 Non-Blocking UI Alerts

Instead of message boxes that halt execution, RC Drag Manager uses **soft notification banners** or **status bar messages**.  

Format:
```
⚠ Warning: Some results could not be saved. Check logs for details.
```

Duration: 5 seconds  
Color coding:
- Yellow = Warning  
- Red = Error  
- Blue = Info  

---

## 💾 Log File Rotation

- Logs rotate when exceeding 2 MB.  
- Old logs renamed:
  ```
  app_2025-10-12_01.log
  app_2025-10-12_02.log
  ```
- Maximum 10 files retained.  
- Optionally exported via `SettingsRepository.ExportLogs()`.

---

## 🧩 Lane Shuffle Diagnostics

When pairing generation occurs, the engine logs lane assignments with reproducible seeds:

```
[DEBUG] [LaneShuffle] Seed=3271 | L=DriverA | R=DriverB
```

If a seed collision or serialization error occurs:
```
[WARN] [LaneShuffle] Collision detected — regenerating seed.
```

These logs can be replayed deterministically by reseeding the randomizer with `LaneSeed`.

---

## 🧩 Recovery & Fallback Behavior

| Failure | Recovery Action |
|----------|----------------|
| **Lost DB connection** | Retry ×3, fallback to file save. |
| **Session save interrupted** | Auto-create `.backup` JSON. |
| **Corrupt match file** | Skip corrupted match, continue loading. |
| **UI crash in round render** | Rebind UI from repository state. |
| **Engine crash** | Log stack trace and restart phase. |

---

## 🧩 Log Viewer Integration

Future UI option:  
`Menu → Tools → View Logs`  
Displays last 500 log lines with filters by:
- Date  
- Severity  
- Component  

---

## 🧱 Adjacent Docs

| File | Purpose |
|------|----------|
| `07_Repository_Contracts.md` | Error handling within repository methods. |
| `08_UI_UX_Surface_Map.md` | Visual message handling. |
| `06_SQLite_Schema.md` | Database fault context. |
| `13_Project_Status_Summary.md` | Known issue tracking. |

---

## ✅ Summary

RC Drag Manager’s error and logging system provides:
- **Full transparency** through structured logs.  
- **UI-safe fault handling** without blocking.  
- **Automatic retries** and recoverable persistence.  
- **Deterministic replayability** using `LaneSeed` diagnostics.

Every major subsystem (UI, controller, engine, repository) logs independently with consistent formatting — guaranteeing post-event traceability and stable session recovery.

---
