# RCDM × Portatree Integration — Architecture & Design

> Status: planned design, not implemented in the current desktop app or live server as of 2026-06-06. Current code has no Portatree watcher, Paradox reader, timing result model, timing persistence table, or live timing DTO fields. Use this document as a design plan only.
*Last updated: May 2026*

---

## Goal

RC Drag Manager (RCDM) manages the event — drivers, bracket, dial-ins, points, live scoreboard.
Portatree Eliminator Competition runs the actual race — Christmas tree, reaction times, ETs, MPH.

The integration connects them so:
1. RCDM shows the operator what entry numbers and dial-ins to type into the Eliminator for each pairing
2. The Eliminator runs the race and records the result
3. The result flows back into RCDM automatically — bracket advances, timing data stored, live scoreboard updates
4. OBS overlay displays real-time race results for streaming

---

## Architecture Overview

```
RCDM (bracket master)
  │
  ├─ "Now Racing" panel → operator reads entry numbers + dial-ins
  │    └─ operator manually types into Eliminator touchscreen
  │
  └─ Portatree Bridge Service (new, in-process)
       │
       ├─ FileSystemWatcher on C:\Res2024\{date}\{date}.db
       │    └─ Paradox table watcher (pypxlib or raw Paradox reader)
       │
       ├─ On new row detected:
       │    ├─ Read pair of rows (same RaceNumber, left + right lane)
       │    ├─ Look up CarNumber → RCDM Driver via EntryMap
       │    ├─ Determine winner (Win=1 row)
       │    └─ Call RaceController.SubmitWinner(winnerDriverId)
       │
       └─ Extend LiveRaceUpdateDto with timing fields
            └─ RCDragLiveServer pushes to OBS browser source
```

---

## Portatree Results Database Schema

**File:** `C:\Res2024\{YYMMDD}\{YYMMDD}.db` (Paradox format, created per event)
**Written by:** ElimComp.exe with AutoSave ON (real-time after each run)
**Key fields for integration:**

| Field | Type | Notes |
|-------|------|-------|
| `RaceNumber` | Long | Sequential run counter. Both lanes (left + right) share the SAME RaceNumber. Two rows per race. |
| `CarNumber` | Alpha(6) | Entry number typed by operator — links to RCDM EntryMap |
| `Type` | Short | Race type: 1=Time Trial, 2=Qualification, 4=Elimination |
| `Cat` | Short | Category number |
| `Round` | Short | Round number |
| `Time` | Timestamp | When result was recorded |
| `DialIn` | Long | Dial-in × 10000 (e.g. 2.580 → 25800) |
| `RT` | Long | Reaction time × 10000 (e.g. 0.035 → 350) |
| `60Foot` | Long | 60ft ET × 10000 |
| `330Foot` | Long | 330ft ET × 10000 |
| `660Foot` | Long | 660ft ET × 10000 |
| `990Foot` | Long | 990ft ET × 10000 |
| `1320Foot` | Long | 1320ft ET × 10000 (the main elapsed time) |
| `Mph1` | Long | Mid-track MPH × 10000 |
| `Mph2` | Long | Trap MPH × 10000 |
| `Win` | Short | 1 = this lane won, 0 = lost |
| `Red` | Short | 1 = red light (foul start) |
| `BO` | Short | 1 = breakout (ran quicker than dial-in) |
| `DisQ` | Short | 1 = disqualified |
| `OverDial` | Long | Over/under dial × 10000 (negative = under = breakout) |
| `eMOV` | Long | Margin of victory × 10000 |
| `RacerName` | Alpha(20) | Racer name (populated from racer.db if entry matched) |
| `CatName` | Alpha(25) | Category name |

**Scaling rule:** All time/speed Long fields are stored as integer × 10000. To get the display value: `displayValue = rawLong / 10000.0`. Example: RT=350 → 0.0350s, 1320Foot=92430 → 9.2430s.

**Row pattern per race:**
```
RaceNumber=1, CarNumber="007", Win=1, RT=350, 1320Foot=92430 ...  ← left lane winner
RaceNumber=1, CarNumber="012", Win=0, RT=412, 1320Foot=94180 ...  ← right lane loser
```

---

## Portatree Racer Database Schema

**File:** `C:\Portatree\racer.db` (Paradox, 76 fields, same schema as race.db roster)

Key fields for integration:

| Field | Notes |
|-------|-------|
| `EntryNumber` | Alpha(6) — the number typed at the tower |
| `RacerNumber` | Alpha(6) — internal roster number |
| `FirstName` | Alpha(20) |
| `LastName` | Alpha(20) |
| `DefaultDial` | Long × 10000 — stored default dial-in |

The `EntryNumber` in racer.db corresponds to the `CarNumber` written to the results db. This is the join key.

---

## RCDM Data Gaps (Confirmed)

From CC's audit of the current codebase:

**`MatchResult.cs`** — stores only `(Driver Winner, Driver Loser)` per matchId. No timing fields.

**`MatchResultSave`** — stores only `MatchId`, `WinnerDriverId`, `LoserDriverId`. No timing fields.

**`RaceSession`** — no RT, ET, MPH, DialIn-result, or timing fields at session or match level.

**`LiveRaceUpdateDto`** / **`LiveWinnerDto`** — no timing fields. `LiveMatchDto` has `LeftDriverDialIn` / `RightDriverDialIn` (double?) but nothing for post-race actuals.

**Conclusion:** RCDM has no existing timing data model. All timing storage is new work.

---

## Build Phases

### Phase 1 — Results Bridge (core, build first)

**What it does:** Portatree result → RCDM bracket advance + timing storage

**New components required:**

1. **`PortatreeTimingResult`** (new domain class)
   ```csharp
   public class PortatreeTimingResult
   {
       public int RaceNumber { get; set; }
       public string CarNumber { get; set; }      // entry number
       public int DriverId { get; set; }          // resolved from EntryMap
       public double DialIn { get; set; }
       public double RT { get; set; }
       public double ET { get; set; }             // 1320Foot / 10000.0
       public double MPH { get; set; }            // Mph2 / 10000.0
       public double OverUnder { get; set; }
       public double MOV { get; set; }
       public bool Win { get; set; }
       public bool RedLight { get; set; }
       public bool Breakout { get; set; }
       public bool Disqualified { get; set; }
       public DateTime RecordedAt { get; set; }
   }
   ```

2. **`PortatreeEntryMap`** (new, per-session)
   - Simple `Dictionary<string, int>` mapping CarNumber string → RCDM DriverId
   - Populated at session start by operator assigning entry numbers to drivers
   - Persisted as part of `RaceSession` JSON (new field `List<EntryAssignment>`)

3. **`PortatreeResultWatcher`** (new service)
   - `FileSystemWatcher` on the dated results `.db` file path
   - On `Changed` event: read new rows since last `RaceNumber` seen
   - Pairs the two rows for the same `RaceNumber`
   - Resolves `CarNumber` → `DriverId` via `EntryMap`
   - Calls `RaceController.SubmitWinner(winnerDriverId, loserDriverId)`
   - Stores `PortatreeTimingResult` pair in new `MatchTimingStore`

4. **`MatchTimingStore`** (new, in-memory + persisted)
   - Stores `PortatreeTimingResult` per matchId
   - Serialized as `List<PortatreeTimingResult>` in `RaceSession` JSON
   - New SQLite table `MatchTimingResults` for permanent storage post-session

5. **`PortatreeParadoxReader`** (new utility)
   - Pure C# Paradox file reader (no BDE dependency — BDE is not reliable in all deployments)
   - Reads `.db` file header for field descriptors
   - Reads records sequentially by row index
   - Returns `List<Dictionary<string, object>>` rows
   - Tested against `C:\Res2024\240913\240913.db` and `C:\Portatree\racer.db`

6. **Config additions to `appsettings.json`:**
   ```json
   "PortatreeEnabled": false,
   "PortatreeResultsPath": "C:\\Res2024",
   "PortatreeAutoDetectDate": true
   ```
   When `PortatreeEnabled = false`, the watcher never starts. Existing manual winner-entry path unchanged.

**New UI element on Form1 / MultiClassRaceForm:**
- "Entry Numbers" panel: shows current pairing with assigned entry numbers + dial-ins
- Operator reads this and types into the Eliminator touchscreen
- Small "Assign Entries" button opens a modal at session start for mapping drivers to entry numbers

**Guard — active match sync:**
RCDM's bracket has a concept of "currently active match" via `RaceController.PushNextMatch()`. The watcher must only fire `SubmitWinner` if the incoming `CarNumber` pair matches the current active match's entry assignments. If no match found (stale run, test pass, wrong category) — log and ignore, do NOT advance the bracket.

---

### Phase 2 — OBS Overlay (build second)

**What it does:** Live race result data visible in OBS as a browser source

**Changes to `LiveRaceUpdateDto`:**

Add timing fields to `LiveWinnerDto`:
```csharp
public class LiveWinnerDto
{
    public string RoundLabel { get; set; }
    public string WinnerName { get; set; }
    public string LoserName { get; set; }
    // NEW:
    public double? WinnerRT { get; set; }
    public double? WinnerET { get; set; }
    public double? WinnerMPH { get; set; }
    public double? WinnerDialIn { get; set; }
    public double? LoserRT { get; set; }
    public double? LoserET { get; set; }
    public double? LoserMPH { get; set; }
    public double? LoserDialIn { get; set; }
    public bool WinnerRedLight { get; set; }
    public bool LoserRedLight { get; set; }
    public bool WinnerBreakout { get; set; }
    public bool LoserBreakout { get; set; }
    public double? MOV { get; set; }
}
```

Add a `LiveLastRunDto` to `LiveRaceUpdateDto` for the most recent completed run (for live commentary panel):
```csharp
public LiveLastRunDto LastRun { get; set; }
```

**RCDragLiveServer side:**
- Add a new endpoint or extend existing update endpoint to serve `LastRun` and enhanced `Winners`
- OBS browser source HTML page polls this endpoint every ~500ms
- Page shows: driver names, dial-ins, RT, ET, MPH, win/red/BO flags, MOV
- Styled for readability at 1080p (dark background, large numbers)

---

### Phase 3 — Entry Number Display Panel (build third)

**What it does:** Shows operator what to type into the Eliminator for the current pairing

**New UI panel on Form1 (pnlBottom or pnlRail area):**
```
┌─────────────────────────────────────────┐
│  NOW RACING                             │
│  LEFT:  007  Dave Smith    dial: 2.580  │
│  RIGHT: 012  Mike Jones    dial: 2.640  │
└─────────────────────────────────────────┘
```

This panel updates automatically when `NextMatchReady` fires, pulling from the active `EntryMap`.

**"Assign Entry Numbers" dialog** (new, shown at session start when `PortatreeEnabled = true`):
- DataGridView: Driver Name | Entry Number (editable) | Dial-In
- Pre-fills entry numbers sequentially (001, 002, 003…)
- Operator can override any number
- Saves to `RaceSession.EntryAssignments`

---

## Data Flow Diagram (Race Day)

```
1. RCDM session starts
   → EntryMap created: Dave Smith = 007, Mike Jones = 012, etc.
   → PortatreeResultWatcher starts watching C:\Res2024\260528\260528.db

2. RCDM generates bracket: Match 1 = Dave Smith (L) vs Mike Jones (R)
   → "Now Racing" panel shows: LEFT 007 2.580 | RIGHT 012 2.640
   → Operator reads panel, types into Eliminator touchscreen

3. Eliminator runs race
   → AutoSave writes two rows to 260528.db (RaceNumber=1)

4. FileSystemWatcher fires
   → Reader detects 2 new rows for RaceNumber=1
   → CarNumber "007" → DriverId 3 (Dave Smith)
   → CarNumber "012" → DriverId 7 (Mike Jones)
   → Win=1 on "007" row
   → Guard check: active match is Match 1, expected entries 007 + 012 ✓
   → RaceController.SubmitWinner(winnerId=3, loserId=7)
   → MatchTimingStore.Store(matchId=1, leftResult, rightResult)

5. RCDM bracket advances normally (same path as manual entry)
   → LiveApiClient pushes updated DTO to stewmacrc.com
   → DTO now includes timing data in LiveWinnerDto

6. OBS browser source polls stewmacrc.com
   → Overlay updates with Dave Smith RT=0.035 ET=9.243 MPH=143.2 WIN
```

---

## Paradox Reader — Implementation Notes

Do NOT use BDE/ODBC from C#. BDE is a 32-bit COM server and requires registration. Instead, write a pure C# Paradox reader:

**Paradox file structure (relevant parts):**
- Bytes 0–1: record size
- Bytes 2–3: header size
- Byte 4: file type (3 = indexed, 0 = non-indexed)
- Bytes 5–6: max table size
- Bytes 7–8: number of records
- Bytes 9–10: next block
- Byte 21: number of fields
- Field descriptor array starts at byte 120 (for Paradox 7 format): each descriptor is 1 byte type + 1 byte size + 1 byte offset
- Field name table follows header

The CC research pass already confirmed `pypxlib 2.5` reads these files correctly. For the C# port, implement the same header parsing logic. The scaling factor for all Long timing fields is **÷ 10000**.

Reference: `C:\Res2024\240913\240913.db` is the test fixture (0 rows but correct schema). When the box arrives and a real race is run, this file will have data to test against.

---

## Open Questions Before Implementation

1. **Results path detection:** Does ElimComp create `C:\Res2024\{YYMMDD}\{YYMMDD}.db` automatically, or does the operator create the folder first? Need to verify path creation on first run with the box.

2. **FileSystemWatcher vs polling:** Paradox `.db` files have companion `.PX`, `.VAL`, `.XG0`, `.YG0` index files that ElimComp may write in a burst after the main `.db`. Watch the `.db` file only but add a 200ms debounce before reading to let the write complete.

3. **Paradox file locking:** ElimComp holds the file open while running. C# reader must open with `FileShare.ReadWrite` or the watcher read will fail while ElimComp is active. Test this scenario before shipping.

4. **PortatreeEnabled feature flag:** Confirm `appsettings.json` is the right config surface (consistent with `LiveBroadcastEnabled` already there) vs `App.config`. Current pattern in RCDM uses `AppSettings.cs` loading from `%APPDATA%\RC_Drag_Manager\appsettings.json`.

5. **New DB table vs JSON blob:** `MatchTimingResults` as a new SQLite table is cleaner for querying post-event stats. Add migration via `DatabaseInitializer` with `CREATE TABLE IF NOT EXISTS` pattern already used in the project.

6. **Multi-class events:** `MultiClassEvent` has multiple `RaceSession` objects. Each class needs its own `EntryMap` and its own results file watch path. Design the watcher as per-session, not per-event.

---

## Files To Create / Modify

### New files
- `src/RCDragManagerProd/Integration/PortatreeTimingResult.cs`
- `src/RCDragManagerProd/Integration/PortatreeEntryMap.cs`
- `src/RCDragManagerProd/Integration/PortatreeResultWatcher.cs`
- `src/RCDragManagerProd/Integration/PortatreeParadoxReader.cs`
- `src/RCDragManagerProd/UI/Forms/Session/AssignEntryNumbersDialog.cs`

### Modified files
- `src/RCDragManagerProd/Domain/RaceSession.cs` — add `List<EntryAssignment> EntryAssignments`
- `src/RCDragManagerProd/Domain/MatchResult.cs` — no change needed (timing is separate store)
- `src/RCDragManagerProd/Repositories/DatabaseInitializer.cs` — add `MatchTimingResults` table
- `src/RCDragManagerProd/Integration/LiveRaceUpdateDto.cs` — add timing fields to `LiveWinnerDto`, add `LiveLastRunDto`
- `src/RCDragManagerProd/Config/AppSettings.cs` — add `PortatreeEnabled`, `PortatreeResultsPath`
- `src/RCDragManagerProd/UI/Forms/Main/Form1.cs` — add "Now Racing" panel wiring
- `src/RCDragManagerProd/Controllers/RaceController.cs` — expose hook for watcher to call SubmitWinner

### Test files to add
- `src/RCDragManagerProd.Tests/PortatreeParadoxReaderTests.cs` — test against fixture `.db` files
- `src/RCDragManagerProd.Tests/PortatreeResultWatcherTests.cs` — test guard logic, entry map lookup, scaling

---

## Prerequisites Before Starting Phase 1

1. Portatree box physically connected and ElimComp confirmed communicating (COM port INI fixed — see PORTATREE-SETUP.md)
2. At least one real race run through ElimComp with AutoSave ON, producing a populated results `.db` file
3. That file copied to `C:\Res2024\{date}\{date}.db` for use as a test fixture
4. Confirm pypxlib row reads match the actual data visible in ElimComp's Race Screen (sanity check the scaling factor)

---

## Suggested CC Prompt Sequence (Phase 1)

1. **Research pass** — read `PortatreeParadoxReader` skeleton, existing `LiveApiClient`, `AppSettings`, `DatabaseInitializer` before writing any code
2. **Implement `PortatreeParadoxReader`** — pure C# Paradox reader, tested against fixture files
3. **Implement `PortatreeResultWatcher`** — FileSystemWatcher + debounce + guard logic
4. **Add `EntryAssignment` to `RaceSession`** + `AssignEntryNumbersDialog`
5. **Wire into `RaceController`** — `SubmitWinner` hook from watcher
6. **Add `MatchTimingResults` table** to `DatabaseInitializer`
7. **Extend `LiveRaceUpdateDto`** with timing fields
8. **Add "Now Racing" panel** to Form1

Each step is a separate CC prompt with its own research pass. Never implement more than one step per prompt.
