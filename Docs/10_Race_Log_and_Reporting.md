# RC Drag Manager — Race Log & Reporting System  
**File:** 10_Race_Log_and_Reporting.md  
**Version:** 1.00  
**Status:** ✅ Stable (ChatGPT-Pack Ready)  
**Last Updated:** 2025-10-12  
**Owner:** Stewart McMillan  
**Source of Truth:** Derived from `MatchEngine_Refactor_Spec.md`, controller logic, and new feature design.

---

## 🤖 How ChatGPT Should Use This Doc

This file defines the **Race Log and Reporting System** used to record every match result during a race session.  
Use it to understand:
- How race events are captured, formatted, and stored.  
- How logs can be exported for post-event reports.  
- How this integrates with controllers and repositories.  

See also:  
- `05_Mode_RoundRobin_Spec.md` — pairing and lane shuffle.  
- `06_SQLite_Schema.md` — table and JSON layout.  
- `07_Repository_Contracts.md` — persistence layer.  
- `09_Error_Handling_Logging.md` — runtime logging policies.

---

## 🎯 Purpose

Create a permanent, human-readable **trace of every race** — who raced who, which lanes they ran, who won, and when.  
This log becomes the foundation for post-event reporting, auditing, and replay.

---

## 🧱 Overview

| Layer | Responsibility |
|--------|----------------|
| **Controller (`RaceController`)** | Triggers race-log writes whenever a winner is set. |
| **Repository (`RaceLogRepository`)** | Handles DB and file persistence of race events. |
| **Form 1 UI** | Optionally displays live race log during event. |
| **Report Exporter** | Builds text/CSV summaries for after-race reports. |

Two storage paths are maintained:  
1. **SQLite Table:** `RaceLogs` (structured, queryable).  
2. **Flat File:** `RaceLog_[SessionName].txt` in `C:\Temp\RaceLogs\`.

---

## 🗂️ 1. Database Schema

| Column | Type | Description |
|---------|------|-------------|
| `LogId` | INTEGER PK AUTOINCREMENT | Unique log entry ID. |
| `SessionId` | TEXT (FK) | Linked session ID. |
| `RoundNumber` | INTEGER | Round number (1 – 3). |
| `MatchId` | TEXT (FK) | Match record ID. |
| `LeftDriver` | TEXT | Left-lane driver name or GUID. |
| `RightDriver` | TEXT | Right-lane driver name or GUID. |
| `Winner` | TEXT | Winner name or GUID. |
| `LeftLaneColor` | TEXT | Default “Blue”. |
| `RightLaneColor` | TEXT | Default “Red”. |
| `LaneSeed` | INTEGER | Random seed used for lane assignment. |
| `Timestamp` | TEXT (ISO 8601) | Completion time. |
| `Notes` | TEXT | Optional notes (BYE, rerun, etc.). |

---

## 🗂️ 2. Flat File Storage

Each session automatically writes to a log file:

```
C:\Temp\RaceLogs\RaceLog_<SessionName>_<yyyy-MM-dd>.txt
```

Example header:

```
=== RC DRAG MANAGER — RACE LOG ===
Event: QMDRA Spring Classic
Session ID: a3e4-52e1-… 
Date: 2025-10-12
```

Then one block per round:

```
ROUND 1
--------
Match 1 | Left: Driver A | Right: Driver B | Winner: Driver A | L=Blue | R=Red | Seed=3021 | Time=14:02:12
Match 2 | Left: Driver C | Right: Driver D | Winner: Driver D | Seed=4091 | Time=14:05:47
Match 3 | Left: Driver E | Right: (BYE) | Winner: Driver E | Time=14:06:00
```

---

## 🧩 3. Controller Integration

When a race result is confirmed:

```vbnet
Sub SetWinner(matchId As Guid, winnerId As Guid)
    engine.SetWinner(matchId, winnerId)
    Dim match = matchRepo.GetById(matchId)
    raceLogRepo.AppendLog(match)
End Sub
```

`AppendLog()` constructs both DB record and file line.

---

## 🧩 4. Repository Contract

```vbnet
Public Interface IRaceLogRepository
    Sub AppendLog(match As Match)
    Function GetLogsBySession(sessionId As Guid) As List(Of RaceLogEntry)
    Sub ExportToText(sessionId As Guid, filePath As String)
End Interface
```

### Behavior
- Writes to DB first; appends to file second.  
- Automatically groups entries by round.  
- Timestamps = UTC by default.  
- On write failure → fallback to file-only mode.

---

## 🧩 5. Data Structure

```vbnet
Class RaceLogEntry
    Property LogId As Integer
    Property SessionId As Guid
    Property RoundNumber As Integer
    Property MatchId As Guid
    Property LeftDriver As String
    Property RightDriver As String
    Property Winner As String
    Property LaneSeed As Integer
    Property Timestamp As DateTime
    Property Notes As String
End Class
```

---

## 🧩 6. Export Formats

### Text Export
Default readable format (used by flat file).  
Example:

```
Round 2 | Driver D (Left) vs Driver B (Right) | Winner: Driver D | Seed=2910 | Time=14:22:30
```

### CSV Export
Comma-delimited version for Excel or Google Sheets.

```
Round,LeftDriver,RightDriver,Winner,LaneSeed,Time,Notes
1,Driver A,Driver B,Driver A,3021,14:02:12,
1,Driver C,Driver D,Driver D,4091,14:05:47,
1,Driver E,BYE,Driver E,,14:06:00,BYE
```

### JSON Export
Machine-readable version embedded in `RaceSession.SerializedLogs`.

```json
[
  {
    "RoundNumber": 1,
    "LeftDriver": "Driver A",
    "RightDriver": "Driver B",
    "Winner": "Driver A",
    "LaneSeed": 3021,
    "Timestamp": "2025-10-12T14:02:12Z"
  }
]
```

---

## 🧩 7. Reporting Template

When a session completes, RC Drag Manager can generate:

```
=== RACE SUMMARY REPORT ===
Event: QMDRA Spring Classic
Date: 2025-10-12

Top Performers:
1️⃣ Driver A – 3 Wins (6 Points)
2️⃣ Driver D – 2 Wins (5 Points)
3️⃣ Driver C – 1 Win (3 Points)

Best Lane Win Rate:
Left Lane (Blue): 60 %
Right Lane (Red): 40 %

Session Duration: 00:23:47
Generated by RC Drag Manager v1.0.0
```

Export locations:
- Text Report → `C:\Temp\RaceReports\`
- CSV → same folder with `.csv` extension.

---

## 🧩 8. Error Handling

| Condition | Handling | Example Log |
|------------|-----------|-------------|
| File I/O fail | Retry ×3 → fallback to DB | `[RaceLog] Write failed, retrying.` |
| Missing Driver | Log warning, skip entry | `[RaceLog] Missing driver reference.` |
| DB constraint | Auto-increment retry | `[Repository] Constraint violation — retry.` |
| Serialization error | JSON skip + warning | `[Serializer] Failed encoding RaceLogEntry.` |

---

## 🧱 Adjacent Docs

| File | Purpose |
|------|----------|
| `06_SQLite_Schema.md` | Adds `RaceLogs` table. |
| `07_Repository_Contracts.md` | Repository layer contract. |
| `08_UI_UX_Surface_Map.md` | Optional “Race Log View” UI. |
| `09_Error_Handling_Logging.md` | Logging fallback rules. |
| `13_Project_Status_Summary.md` | Development status tracking. |

---

## ✅ Summary

The **Race Log System** ensures every race in RC Drag Manager is fully traceable and exportable.  
Each match generates an entry detailing:
- Round & match metadata  
- Lanes and random seed  
- Winner and timestamp  
- Optional notes and diagnostics  

Logs can be stored in the DB, exported to file, or replayed via JSON, providing complete transparency and a ready-made data source for official race reports.

---
