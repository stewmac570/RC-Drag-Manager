# RC Drag Manager — Multi-Class Event Feature Specification

**Version:** 1.0  
**Date:** March 2026  
**Scope:** New feature — multi-class event support for Round Robin events  
**Prerequisite reading:** ARCHITECTURE.md, DOMAIN-MODEL.md, DATA-LAYER.md, RACE-FLOW.md

---

## 1. Overview

This feature adds the ability to run a single event containing multiple racing classes simultaneously. Each class runs its own independent Round Robin bracket through to its own Finals champion. The Race Director manages all classes from a single race console using a tabbed interface.

### 1.1 Scope Constraints

- Multi-class events support **Round Robin race type only** (Standard and QMDRA variants).
- Pro Ladder and Random Draw race types are **not** available in multi-class mode.
- All existing single-class event functionality is unchanged.
- The feature is a new entry point — a new button on the Landing Page — not a modification to the existing New Event flow.

---

## 2. Data Model

### 2.1 New Domain Object: `MultiClassEvent`

Create `Domain/MultiClassEvent.cs`:

```csharp
public class MultiClassEvent
{
    public int Id { get; set; }                        // DB-assigned PK after save. 0 before first save.
    public string EventName { get; set; }              // Free-text event name
    public DateTime EventDate { get; set; }            // Date of event
    public List<RaceSession> ClassSessions { get; set; } = new List<RaceSession>();
    // One RaceSession per class. Each is a fully self-contained RaceSession object,
    // identical in structure to a single-class session. RaceSession.ClassType holds
    // the class name for that slot.
}
```

**Key points:**
- `MultiClassEvent` is the unit of persistence, save, and load.
- Each `RaceSession` in `ClassSessions` is structurally identical to a single-class `RaceSession`. No changes to `RaceSession` are required.
- `RaceSession.ClassType` serves as the class name (free-text, set at setup).
- `RaceSession.RoundRobinVariant` and `RaceSession.RoundsToRun` are set independently per class.
- `MultiClassEvent.EventName` and `MultiClassEvent.EventDate` are the shared event-level fields. Each child `RaceSession.EventName` is set to the same value for consistency.

### 2.2 New Database Table: `MultiClassEvents`

Add to `DatabaseInitializer.InitializeDatabase()`:

```sql
CREATE TABLE IF NOT EXISTS MultiClassEvents (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    EventName   TEXT,
    EventDate   TEXT,
    ClassCount  INTEGER,
    EventData   TEXT    -- full JSON-serialized MultiClassEvent object
);
```

- `EventName`, `EventDate`, and `ClassCount` are scalar copies for the session list view.
- `EventData` is the source of truth — the full `MultiClassEvent` serialized to JSON.
- Every save is an INSERT (append-only), consistent with existing `RaceSessionRepository` behaviour.
- There is no UPDATE path (consistent with existing architecture).

### 2.3 New Repository: `MultiClassEventRepository`

Create `Repositories/MultiClassEventRepository.cs`:

| Method | Signature | Notes |
|--------|-----------|-------|
| `SaveEvent` | `SaveEvent(MultiClassEvent evt)` | INSERT new row; sets `evt.Id`; serializes full object to JSON |
| `GetAllEvents` | `List<MultiClassEventSummary> GetAllEvents()` | Returns scalar summary rows — no JSON deserialization |
| `LoadEvent` | `MultiClassEvent LoadEvent(int id)` | SELECT `EventData`, deserialize JSON → `MultiClassEvent` |
| `DeleteEvent` | `DeleteEvent(int id)` | DELETE by Id |

Serialization uses the same `System.Text.Json` options as `RaceSessionRepository`:

```csharp
var opts = new JsonSerializerOptions {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};
string json = JsonSerializer.Serialize(evt, opts);
```

### 2.4 New ViewModel: `MultiClassEventSummary`

Create `ViewModels/MultiClassEventSummary.cs`:

```csharp
public class MultiClassEventSummary
{
    public int Id { get; set; }
    public string EventName { get; set; }
    public DateTime EventDate { get; set; }
    public int ClassCount { get; set; }
}
```

Used by `LoadSessionForm` to display multi-class events in the session list.

### 2.5 Stats Rules

Stats are written per-class as each class's Finals match resolves — not held until the whole event ends.

| Stat | Rule |
|------|------|
| `TotalWins` | Incremented per match win, per class. Same as today — fires during `TournamentCompleted` for that class. |
| `TotalLosses` | Same as `TotalWins`. |
| `EventsEntered` | Incremented once per class the driver is entered in. A driver in 2 classes = +2. Incremented at session start (consistent with existing behaviour — `SessionSetupForm` already does this at Start Race). |
| `EventsWon` | Incremented once per class won. A driver winning 2 classes in one event = +2. |

---

## 3. New UI: Multi-Class Event Setup Form

### 3.1 Entry Point

Add a new button to `LandingPageForm`: **"New Multi-Class Event"**.

On click:
```csharp
var setup = new MultiClassSetupForm(_connectionString);
if (setup.ShowDialog() == DialogResult.OK)
{
    var multiEvent = setup.MultiClassEventResult;
    var form = new MultiClassRaceForm(multiEvent, _connectionString);
    form.Show();
}
```

The existing "New Event" button and its flow are **unchanged**.

### 3.2 `MultiClassSetupForm`

A new form: `UI/Forms/Session/MultiClassSetupForm.cs`.

#### Layout

```
┌─────────────────────────────────────────────────────┐
│  Event Name: [___________________]                  │
│  Event Date: [date picker       ]                   │
│                                                     │
│  Classes in this event:                             │
│  ┌──────────────────────────────────────────────┐  │
│  │ Class Name    │ Variant  │ Rounds │ Drivers   │  │
│  │ Open          │ Standard │ 3      │ 8         │  │
│  │ Stock 2WD     │ QMDRA    │ 5      │ 6         │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
│  [+ Add Class]                [Remove Selected]    │
│                                                     │
│  [Configure Selected Class ▼]                       │
│                                                     │
│  ─────────────────────────────────────────────────  │
│  [Cancel]                          [Start Race »]  │
└─────────────────────────────────────────────────────┘
```

#### Behaviour

**Class list panel:**
- Displays all classes added so far as rows in a ListView.
- Columns: Class Name, RR Variant, Rounds to Run, Driver Count.
- Selecting a row enables "Remove Selected" and "Configure Selected Class".

**"+ Add Class" button:**
- Opens `MultiClassConfigDialog` (see §3.3) with blank fields.
- On OK: validates the class name is not already in the list (case-insensitive). If duplicate, shows error: `"A class named '{name}' already exists in this event. Class names must be unique."` and does not add.
- Adds the new class as a row in the list.

**"Remove Selected" button:**
- Removes the selected class row from the list.
- No confirmation prompt required.

**"Configure Selected Class" button:**
- Opens `MultiClassConfigDialog` pre-populated with the selected class's current settings.
- On OK: updates the row in the list.

**"Start Race »" button:**
- No minimum class count validation — one class is valid.
- For each class: if driver count is 0, shows a warning: `"Class '{name}' has no drivers. Add at least one driver or remove the class."` Blocks start.
- If any class has 1 driver, it is allowed — a BYE will fill the bracket.
- Calls `DriverRepository.IncrementEventsEntered(driverId)` for each driver in each class they are entered in (consistent with existing SessionSetupForm behaviour).
- Builds the `MultiClassEvent` object and sets `DialogResult = OK`.

**Building the `MultiClassEvent`:**

```csharp
var evt = new MultiClassEvent {
    EventName = txtEventName.Text.Trim(),
    EventDate = dtpEventDate.Value.Date
};

foreach (var classConfig in _classList)
{
    var session = new RaceSession {
        EventName = evt.EventName,
        EventDate = evt.EventDate,
        RaceType = "Round Robin",
        ClassType = classConfig.ClassName,
        RoundRobinVariant = classConfig.Variant,        // "Standard" or "QMDRA"
        RoundsToRun = classConfig.RoundsToRun,          // null for Standard
        DriverEntries = classConfig.DriverEntries       // List<RaceSessionDriverEntry>
    };
    evt.ClassSessions.Add(session);
}
```

### 3.3 `MultiClassConfigDialog`

A new modal dialog: `UI/Forms/Session/MultiClassConfigDialog.cs`.

Collects configuration for a single class slot. Reused for both Add and Edit.

#### Fields

| Field | Control | Notes |
|-------|---------|-------|
| Class Name | TextBox | Free-text. Required. Trimmed. |
| RR Variant | RadioButton group | "Standard" / "QMDRA" |
| Rounds to Run | NumericUpDown | Visible only when QMDRA selected. Min=1. |
| Driver roster | CheckedListBox | All drivers from DB. Each row shows Driver Name. |
| Dial-In (per driver) | Inline editable column or secondary dialog | See §3.4 |

#### Driver list population

- Load all drivers from `DriverRepository.GetAllDrivers()`.
- Show all drivers regardless of car class — this is free-text class naming, there is no filter.
- A driver can be checked in multiple classes across the setup form.

#### Dial-In per driver

Each driver row in the roster has an associated `DialIn` value (nullable double). The Race Director can set this independently per driver per class. Default value: the `DefaultDialIn` from the driver's first car, if present; otherwise null.

Suggested implementation: a small edit button or inline numeric cell per driver row that opens a simple `AddEditQualTimeDialog`-style prompt.

The `DialIn` value is stored in `RaceSessionDriverEntry.DialIn` for that class's session.

### 3.4 `RaceSessionDriverEntry` construction

When building `DriverEntries` for a class session in `MultiClassConfigDialog`:

```csharp
foreach (var checkedDriver in checkedDrivers)
{
    var entry = new RaceSessionDriverEntry {
        DriverID = checkedDriver.Id,
        DriverName = checkedDriver.Name,
        CarID = checkedDriver.Cars.FirstOrDefault()?.Id ?? 0,
        CarName = checkedDriver.Cars.FirstOrDefault()?.CarName ?? "",
        ClassType = classConfig.ClassName,
        DialIn = classConfig.DialInOverrides.GetValueOrDefault(checkedDriver.Id, 
                     checkedDriver.Cars.FirstOrDefault()?.DefaultDialIn),
        QualifyingTime = checkedDriver.QualTime,
        Seed = null
    };
    session.DriverEntries.Add(entry);
}
```

---

## 4. New UI: Multi-Class Race Console

### 4.1 `MultiClassRaceForm`

A new top-level form: `UI/Forms/Main/MultiClassRaceForm.cs`.

This form is the race console for multi-class events. It hosts the existing `Form1` logic per class using a tab control, with the bracket view and all existing controls rendered inside the active tab.

#### Architecture approach

Rather than embedding `Form1` directly (which has a Designer and deep WinForms wiring), the implementation should extract the reusable race console panel from `Form1` into a `RaceConsolePanel` UserControl, then:
- `Form1` hosts one `RaceConsolePanel` (unchanged externally).
- `MultiClassRaceForm` hosts one `RaceConsolePanel` per class inside a `TabControl`.

If extracting to a UserControl is too disruptive in the first pass, an acceptable alternative is to host one `Form1` instance per class as an MDI child, with the tab strip in `MultiClassRaceForm` acting as a switcher that shows/hides the child forms. The spec describes the UserControl approach as the target; the MDI fallback is acceptable for a first implementation.

#### Layout

```
┌─────────────────────────────────────────────────────────────┐
│  [Event Name]  [Event Date]                    [Save Event] │
├─────────────────────────────────────────────────────────────┤
│  [ Open ●  ]  [ Stock 2WD ✓ ]  [ Mod 4WD  ]               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   ← existing Form1 bracket view for the active class →     │
│                                                             │
│   (all existing controls: bracket ListView, winner buttons, │
│    Generate Bracket, Generate Next Round, Winners list,     │
│    Edit Result, Buy Back, etc.)                             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

#### Tab strip

One tab per class in `MultiClassEvent.ClassSessions`, in the order they were created.

**Tab label format:** `"{ClassName}"` — no truncation. If the name is long, allow horizontal scrolling on the tab strip.

**Tab visual states:**

| State | Visual | Condition |
|-------|--------|-----------|
| Active | Blue background / bold text | Currently selected tab |
| Pending | Orange/amber background | Active class's current round has unresolved matches |
| Round complete — waiting | Green background | All matches in current round resolved; class is waiting for LB gate |
| Default/neutral | Standard WinForms tab colour | No matches in progress (e.g. between rounds, not yet started) |

Tab state is updated whenever a match result is submitted or a round is advanced in any class.

**Tab switching enforcement:**

Tab switching is disabled for all non-active tabs when the active class has unresolved matches in the current revealed round. Specifically:

- Subscribe to each class `RaceController`'s `CanAdvanceChanged` event.
- When `CanAdvanceChanged` fires with `canAdvance = false` (matches still pending), set `tabControl.Enabled = false` for all tabs except the active one. Alternatively, intercept the `TabControl.Selecting` event and cancel it if the active class has pending matches.
- When `CanAdvanceChanged` fires with `canAdvance = true` (round complete), re-enable tab switching.

Implementation note: intercept `TabControl.Selecting`:

```csharp
private void tabControl_Selecting(object sender, TabControlCancelEventArgs e)
{
    if (e.TabPageIndex == tabControl.SelectedIndex) return; // same tab
    var activeController = GetControllerForTab(tabControl.SelectedIndex);
    if (activeController.HasPendingMatchesInCurrentRound())
    {
        e.Cancel = true;
    }
}
```

`HasPendingMatchesInCurrentRound()` is a new method on `RaceController` (see §5.2).

### 4.2 Per-class `RaceController` instances

`MultiClassRaceForm` creates one `RaceController` per class session at construction:

```csharp
_controllers = new List<RaceController>();
foreach (var session in _multiEvent.ClassSessions)
{
    var controller = new RaceController(session, _connectionString);
    // subscribe to events...
    _controllers.Add(controller);
}
```

Each controller is entirely independent. They do not share state.

### 4.3 LB Gate enforcement

The RR→LB transition is gated: all classes must complete all their RR rounds before any class can start the Losers Bracket phase.

**Implementation:**

- Each `RaceController` raises `CanOfferBuybackChanged` when its RR rounds are complete.
- In `MultiClassRaceForm`, intercept this event. When it fires for a class, check whether all other classes have also completed their RR rounds.
- If not all classes are done: suppress the buyback/LB prompt for the triggering class. Set that class's tab to green (waiting). Do not call `controller.GenerateLosersBracket()` yet.
- When the last class completes its RR rounds: release the gate for all waiting classes simultaneously. For each class that was waiting, trigger the buyback/LB flow in sequence (active tab first, or prompt the director to visit each class tab).

**Gate state tracking in `MultiClassRaceForm`:**

```csharp
private HashSet<int> _rrCompleteClassIndexes = new HashSet<int>();

private void OnCanOfferBuybackChanged(int classIndex, bool canOffer)
{
    if (canOffer)
    {
        _rrCompleteClassIndexes.Add(classIndex);
        UpdateTabState(classIndex); // set green
    }

    if (_rrCompleteClassIndexes.Count == _controllers.Count)
    {
        // All classes done — release gate
        foreach (var idx in _rrCompleteClassIndexes)
            ReleaseClassToLbPhase(idx);
        _rrCompleteClassIndexes.Clear();
    }
}
```

**Abandoned class exclusion:**

A class whose RR matches are all resolved but which was never explicitly advanced (e.g. Race Director stopped interacting with it) must not hold the gate open indefinitely.

A class is considered "effectively complete" for gate purposes if:
- Its `RaceController` has reported `CanOfferBuybackChanged = true`, OR
- Its `RaceController` reports all RR matches are resolved AND the class's `RaceType` is still `"Round Robin"` (i.e. it completed all rounds, the `PushAdvanceState` has fired, but the buyback prompt was never acted on).

The gate evaluates "effectively complete" count, not strictly "actively waiting" count. If a class stalls, `MultiClassRaceForm` polls or rechecks on tab switch and round-advance events.

Alternatively: add an explicit `IsRrComplete` property to `RaceController` (see §5.2) that `MultiClassRaceForm` can read directly.

### 4.4 Event Completion

When each class's Finals match resolves:

1. `TournamentCompleted` fires on that class's `RaceController`.
2. `MultiClassRaceForm` handles it identically to how `Form1` handles it today for a single class: stats are written, a per-class completion popup is shown.
3. That class's tab moves to a "Completed" visual state (suggested: grey out or show a trophy icon — implementer's discretion; not a named state in the spec but should be visually distinct from "green/waiting").

When **all** classes have fired `TournamentCompleted`:

4. `MultiClassRaceForm` shows the combined event summary dialog (see §4.5).

**Tracking completion:**

```csharp
private HashSet<int> _completedClassIndexes = new HashSet<int>();

private void OnTournamentCompleted(int classIndex, RaceSummary summary)
{
    // Write stats (same as Form1 today)
    WriteStatsForClass(summary);
    
    // Show per-class popup
    ShowClassCompletionPopup(summary);
    
    _completedClassIndexes.Add(classIndex);

    if (_completedClassIndexes.Count == _controllers.Count)
        ShowCombinedEventSummary();
}
```

### 4.5 Combined Event Summary Dialog

A new dialog or a `ScrollableTextDialog` instance showing:

```
═══════════════════════════════════════
  [Event Name] — [Event Date]
  FINAL RESULTS
═══════════════════════════════════════

  Open Class
  ──────────
  Champion:      [Driver Name]
  Runner-Up:     [Driver Name]
  3rd Place:     [Driver Name]

  Stock 2WD
  ──────────
  Champion:      [Driver Name]
  Runner-Up:     [Driver Name]
  3rd Place:     [Driver Name]

  ...

═══════════════════════════════════════
```

Data sourced from each class's `RaceSummary` object (already produced by `TournamentCompleted`).

The existing `ScrollableTextDialog` can be reused with a formatted text string.

### 4.6 Save Button

`MultiClassRaceForm` has a **Save Event** button in its toolbar. Save is always available — no blocking conditions.

On click:
1. For each class controller, call `controller.SaveSession()` to flush current match results and revealed rounds back into the `RaceSession` object (same as existing `RaceController.Persistence.cs` behaviour).
2. Call `MultiClassEventRepository.SaveEvent(_multiEvent)` — this INSERT creates a new row. `_multiEvent.Id` is updated.
3. Show a toast or brief status label: `"Event saved."`.

---

## 5. Changes to Existing Code

### 5.1 `LandingPageForm`

Add one new button: **"New Multi-Class Event"**.

Wire `btnNewMultiClassEvent_Click` as described in §3.1. No other changes to `LandingPageForm`.

### 5.2 `RaceController` — new members

Add the following to `RaceController.cs` (or a new partial file `RaceController.MultiClass.cs`):

```csharp
/// <summary>
/// Returns true if there are unresolved matches in the currently revealed round.
/// Used by MultiClassRaceForm to enforce tab switching rules.
/// </summary>
public bool HasPendingMatchesInCurrentRound()
{
    var matches = EngineGetMatches()
        .Where(m => _revealedRounds.Contains(m.RoundLabel))
        .ToList();
    return matches.Any(m => !_matchResult.HasResult(m.MatchId) && 
                            !ByePolicy.IsBye(m.Driver1) && 
                            !ByePolicy.IsBye(m.Driver2) == false);
    // i.e. returns true if any non-BYE match in the current round has no result
}

/// <summary>
/// Returns true if all RR rounds have been resolved (all matches have results).
/// Used by MultiClassRaceForm for gate evaluation.
/// </summary>
public bool IsRrComplete()
{
    if (_session.RaceType != "Round Robin") return true; // already past RR
    var allMatches = EngineGetMatches();
    return allMatches.All(m => _matchResult.HasResult(m.MatchId) || 
                               ByePolicy.IsBye(m.Driver1) || 
                               ByePolicy.IsBye(m.Driver2));
}
```

### 5.3 `DatabaseInitializer`

Add the `MultiClassEvents` table creation to `InitializeDatabase()`:

```csharp
const string createMultiClassEvents = @"
    CREATE TABLE IF NOT EXISTS MultiClassEvents (
        Id          INTEGER PRIMARY KEY AUTOINCREMENT,
        EventName   TEXT,
        EventDate   TEXT,
        ClassCount  INTEGER,
        EventData   TEXT
    );";
ExecuteNonQuery(conn, createMultiClassEvents);
```

This is safe to add — `CREATE TABLE IF NOT EXISTS` will no-op on existing databases.

### 5.4 `LoadSessionForm`

The existing `LoadSessionForm` lists single-class sessions. Options for multi-class events:

**Recommended approach:** Add a second ListView or a second tab to `LoadSessionForm` for multi-class events. On load, query both `RaceSessionRepository.GetAllSessions()` and `MultiClassEventRepository.GetAllEvents()`. Display them in separate sections with a clear label ("Single-Class Events" / "Multi-Class Events").

On selecting a multi-class event row and clicking Load:

```csharp
var evt = _multiClassRepo.LoadEvent(selectedSummary.Id);
var form = new MultiClassRaceForm(evt, _connectionString);
form.Show();
this.Close();
```

### 5.5 No changes required to

- `RaceSession` (domain object) — structurally unchanged.
- `RaceSessionRepository` — unchanged; single-class sessions unaffected.
- `IRaceEngine` and all engine adapters — unchanged.
- `RoundRobinEngine`, `RoundRobinRanker` — unchanged.
- `LosersBracketBuilder`, `RandomBracket` — unchanged.
- `DriverRepository` — unchanged; stat increment methods are called the same way, just called once per class per driver.
- `Form1` — unchanged if the UserControl extraction approach is used. If the MDI fallback approach is used, minor changes may be needed to support hosted mode.

---

## 6. File Manifest — New Files

| File | Type | Purpose |
|------|------|---------|
| `Domain/MultiClassEvent.cs` | Domain object | Parent event container |
| `Repositories/MultiClassEventRepository.cs` | Repository | Save/load/delete multi-class events |
| `ViewModels/MultiClassEventSummary.cs` | ViewModel | Summary row for load screen |
| `UI/Forms/Session/MultiClassSetupForm.cs` | Form | Event creation — class list builder |
| `UI/Forms/Session/MultiClassSetupForm.Designer.cs` | Form | Auto-generated layout |
| `UI/Forms/Session/MultiClassConfigDialog.cs` | Dialog | Per-class config: name, RR variant, rounds, drivers, dial-ins |
| `UI/Forms/Session/MultiClassConfigDialog.Designer.cs` | Dialog | Auto-generated layout |
| `UI/Forms/Main/MultiClassRaceForm.cs` | Form | Race console — tabbed multi-class view |
| `UI/Forms/Main/MultiClassRaceForm.Designer.cs` | Form | Auto-generated layout |
| `Controllers/RaceController.MultiClass.cs` *(optional)* | Partial class | `HasPendingMatchesInCurrentRound()`, `IsRrComplete()` |

---

## 7. File Manifest — Modified Files

| File | Change |
|------|--------|
| `Repositories/DatabaseInitializer.cs` | Add `MultiClassEvents` table creation |
| `UI/Forms/Session/LandingPageForm.cs` | Add "New Multi-Class Event" button and handler |
| `UI/Forms/Session/LandingPageForm.Designer.cs` | Layout for new button |
| `UI/Forms/Session/LoadSessionForm.cs` | Add multi-class event listing and load path |
| `UI/Forms/Session/LoadSessionForm.Designer.cs` | Layout for multi-class section |
| `Controllers/RaceController.cs` | Add `HasPendingMatchesInCurrentRound()` and `IsRrComplete()` (or in new partial file) |

---

## 8. Test Coverage

The following test cases should be added to `RCDragManagerProd.Tests`:

### `MultiClassEventRepositoryTests.cs`

- Save a `MultiClassEvent` with 2 classes → load it back → verify both class sessions are intact.
- Save the same event twice → two rows exist in DB.
- Delete an event → it no longer appears in `GetAllEvents()`.
- `GetAllEvents()` returns correct scalar fields (EventName, ClassCount).

### `MultiClassSetupFormTests.cs` (if testable without UI)

- Duplicate class name is blocked.
- Single class is allowed.
- Zero-driver class blocks Start Race with correct error message.
- `EventsEntered` is incremented once per class per driver.

### `MultiClassRaceFormTests.cs` / integration tests

- Tab switching blocked when active class has pending matches.
- Tab switching allowed when active class round is complete.
- LB gate: class 1 completes RR → LB not started. Class 2 completes RR → both classes released to LB.
- Abandoned class (all matches resolved, never advanced) does not block the LB gate.
- `TournamentCompleted` fires per class; combined summary shown after all classes fire.
- Stats: driver in 2 classes → `EventsEntered` +2. Driver wins both → `EventsWon` +2.

---

## 9. Behavioural Summary (Quick Reference)

| Behaviour | Rule |
|-----------|------|
| Race type | Round Robin only (Standard or QMDRA, per class) |
| Class naming | Free-text; unique within event; case-insensitive duplicate check |
| Minimum classes | 1 (valid; runs like a single-class RR event) |
| Minimum drivers per class | 1 (BYEs fill bracket) |
| Driver in multiple classes | Allowed; same car; dial-in set independently per class |
| Tab switching | Blocked while active class has unresolved matches in current round |
| Tab colours | Blue=active, Orange=pending, Green=round complete/waiting, Default=neutral |
| Round advancement | Each class advances independently through RR rounds |
| RR→LB gate | All classes must complete all RR rounds before any class starts LB |
| Abandoned class | Excluded from gate once all its RR matches are resolved |
| LB→Finals | Each class runs independently, no gate |
| Finals | Each class crowns its own champion independently |
| Stats write timing | Per class, when that class's `TournamentCompleted` fires |
| EventsEntered | +1 per class entered (at Start Race) |
| EventsWon | +1 per class won |
| Event completion | Combined summary shown when all classes have a champion |
| Save | Always allowed; append-only INSERT of full `MultiClassEvent` JSON |
| Load | Single row in LoadSessionForm per multi-class event |
