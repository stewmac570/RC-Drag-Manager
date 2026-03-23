# RC Drag Manager — Multi-Class Event Implementation Plan

**Feature:** Multi-Class Event Support  
**Spec:** `Docs/claude-context/MULTI-CLASS-EVENT-SPEC.md`  
**Target:** Each phase must build and all tests must pass before starting the next phase.

---

## How to Use This Plan

Work through phases in order. Each phase has:
- A clear **goal** — what exists at the end of this phase
- **Tasks** — specific files to create or modify
- **Verify** — how to confirm the phase is complete before moving on

Do not start Phase N+1 until Phase N is verified. The phases are ordered so
that each one has all its dependencies already in place.

---

## Phase 1 — Domain Object and Database

**Goal:** `MultiClassEvent` exists as a domain object, the DB table exists,
and the repository can save and load a round-trip.

### Tasks

**1.1 — Create `Domain/MultiClassEvent.cs`**

```csharp
using System;
using System.Collections.Generic;

namespace RCDragManagerProd.Domain
{
    public class MultiClassEvent
    {
        public int Id { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public List<RaceSession> ClassSessions { get; set; } = new List<RaceSession>();
    }
}
```

No other changes to any domain object.

---

**1.2 — Create `ViewModels/MultiClassEventSummary.cs`**

```csharp
using System;

namespace RCDragManagerProd.ViewModels
{
    public class MultiClassEventSummary
    {
        public int Id { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public int ClassCount { get; set; }
    }
}
```

---

**1.3 — Add `MultiClassEvents` table to `DatabaseInitializer.cs`**

In `Repositories/DatabaseInitializer.cs`, inside `InitializeDatabase()`, add
after the existing `RaceSessions` table creation:

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

Pattern is identical to existing table creation calls in that method.

---

**1.4 — Create `Repositories/MultiClassEventRepository.cs`**

Model this file on the structure of `RaceSessionRepository.cs`. Key points:

- Constructor accepts a connection string or file path; normalize via the same
  `NormalizeConnString()` pattern used in `RaceSessionRepository`.
- `SaveEvent(MultiClassEvent evt)`: INSERT into `MultiClassEvents`; set
  `evt.Id` from `last_insert_rowid()`; serialize full object to JSON using
  the same `JsonSerializerOptions` as `RaceSessionRepository`.
- `GetAllEvents()`: SELECT scalar columns only (no JSON parse); return
  `List<MultiClassEventSummary>`.
- `LoadEvent(int id)`: SELECT `EventData`; deserialize JSON →
  `MultiClassEvent`. Use `PropertyNameCaseInsensitive = true`.
- `DeleteEvent(int id)`: DELETE by Id.

Full method signatures:

```csharp
public void SaveEvent(MultiClassEvent evt)
public List<MultiClassEventSummary> GetAllEvents()
public MultiClassEvent LoadEvent(int id)
public void DeleteEvent(int id)
```

---

### Verify Phase 1

Run existing test suite — all tests must still pass (no regressions).

Then confirm manually:
- Project builds with zero errors.
- `MultiClassEvent` class exists and is accessible.
- `MultiClassEventSummary` class exists and is accessible.
- `DatabaseInitializer` compiles with the new table statement.
- `MultiClassEventRepository` compiles with all four methods.

Phase 1 test file (`MultiClassEventRepositoryTests.cs`) will be added in
Phase 2 once the repository exists. Writing the tests first is acceptable if
preferred.

---

## Phase 2 — Repository Tests

**Goal:** `MultiClassEventRepository` is proven correct by automated tests.

### Tasks

**2.1 — Create `src/RCDragManagerProd.Tests/MultiClassEventRepositoryTests.cs`**

Use an in-memory SQLite connection string (same pattern as
`RaceSessionRepositoryTests.cs`). Call `DatabaseInitializer.InitializeDatabase()`
on the in-memory connection before each test.

Required test methods:

```csharp
[TestMethod]
public void SaveEvent_SetsIdOnObject()
// After SaveEvent, evt.Id is non-zero.

[TestMethod]
public void SaveAndLoad_RoundTrip_PreservesEventName()
// Save a MultiClassEvent, load it back by Id, EventName matches.

[TestMethod]
public void SaveAndLoad_RoundTrip_PreservesClassSessions()
// Save a MultiClassEvent with 2 class sessions, load it back,
// ClassSessions.Count == 2 and ClassType values match.

[TestMethod]
public void SaveTwice_CreatesTwoRows()
// Save same event object twice → GetAllEvents() returns 2 rows.

[TestMethod]
public void DeleteEvent_RemovesFromGetAllEvents()
// Save, delete, GetAllEvents() returns 0 rows.

[TestMethod]
public void GetAllEvents_ReturnsCorrectClassCount()
// Save event with 3 classes → GetAllEvents()[0].ClassCount == 3.

[TestMethod]
public void SaveAndLoad_RoundTrip_PreservesDriverEntries()
// Save a MultiClassEvent where one class session has 2 DriverEntries,
// load it back, DriverEntries.Count == 2 for that class.
```

### Verify Phase 2

All new tests pass. All existing tests still pass.

---

## Phase 3 — Setup Form (Logic Only, No Designer)

**Goal:** `MultiClassSetupForm` and `MultiClassConfigDialog` exist with full
business logic. Designer/layout files can be minimal placeholders at this
stage — the logic must be correct and testable.

### Tasks

**3.1 — Create `UI/Forms/Session/MultiClassConfigDialog.cs`**

This dialog collects configuration for one class slot. Fields:

- `ClassName` (string) — text box
- `Variant` (string) — `"Standard"` or `"QMDRA"` — radio buttons
- `RoundsToRun` (int?) — numeric up-down, visible only when QMDRA selected
- `DriverEntries` (List<RaceSessionDriverEntry>) — checked list of all drivers
- `DialInOverrides` (Dictionary<int, double?>) — per-driver dial-in values

Public result properties:

```csharp
public string ClassName { get; private set; }
public string Variant { get; private set; }
public int? RoundsToRun { get; private set; }
public List<RaceSessionDriverEntry> BuiltDriverEntries { get; private set; }
```

On OK:
- Validate `ClassName` is not empty. If empty, show error and cancel close.
- Build `BuiltDriverEntries` from checked drivers using dial-in overrides.
  Default dial-in = `driver.Cars.FirstOrDefault()?.DefaultDialIn`.
- Set `DialogResult = DialogResult.OK`.

Constructor signature:

```csharp
public MultiClassConfigDialog(string connectionString, 
                               MultiClassConfigDialogValues existing = null)
```

Where `existing` is null for Add, populated for Edit.

---

**3.2 — Create `UI/Forms/Session/MultiClassSetupForm.cs`**

Public result property:

```csharp
public MultiClassEvent MultiClassEventResult { get; private set; }
```

Internal state:

```csharp
private List<ClassConfig> _classList = new List<ClassConfig>();
```

Where `ClassConfig` is a private inner class or struct holding:
`ClassName`, `Variant`, `RoundsToRun`, `DriverEntries`.

Key methods:

**`AddClass(ClassConfig config)`**
- Check `_classList` for existing name (case-insensitive).
- If duplicate: `MessageBox.Show("A class named '{name}' already exists...")`
  and return without adding.
- Otherwise add to `_classList` and refresh the ListView.

**`RemoveSelectedClass()`**
- Remove the selected row from `_classList` and refresh ListView.

**`BtnStartRace_Click`**
- For each class in `_classList`: if `DriverEntries.Count == 0`, show error
  and abort: `"Class '{name}' has no drivers. Add at least one driver or
  remove the class."`.
- Call `DriverRepository.IncrementEventsEntered(driverId)` for each driver
  in each class they appear in.
- Build `MultiClassEvent`:
  ```csharp
  MultiClassEventResult = new MultiClassEvent {
      EventName = txtEventName.Text.Trim(),
      EventDate = dtpEventDate.Value.Date
  };
  foreach (var cc in _classList)
  {
      var session = new RaceSession {
          EventName = MultiClassEventResult.EventName,
          EventDate = MultiClassEventResult.EventDate,
          RaceType = "Round Robin",
          ClassType = cc.ClassName,
          RoundRobinVariant = cc.Variant,
          RoundsToRun = cc.RoundsToRun,
          DriverEntries = cc.DriverEntries
      };
      MultiClassEventResult.ClassSessions.Add(session);
  }
  ```
- Set `DialogResult = DialogResult.OK`.

---

**3.3 — Designer files**

Create placeholder `MultiClassConfigDialog.Designer.cs` and
`MultiClassSetupForm.Designer.cs`. Minimum viable layout — just enough
controls to wire the logic. Visual polish comes after logic is verified.

---

### Verify Phase 3

- Both forms compile with zero errors.
- Duplicate class name validation works: adding a second class with the same
  name (any case) shows the error message and does not add the class.
- Zero-driver class blocks Start Race with the correct error message.
- A valid setup with 2 classes builds a `MultiClassEvent` with 2
  `ClassSessions`, correct `ClassType` values, and correct `DriverEntries`.
- `EventsEntered` is incremented in the DB for each class a driver is entered
  in (verify by reading back from `DriverRepository.GetDriverById()`).

---

## Phase 4 — RaceController Extensions

**Goal:** `RaceController` exposes the two new methods needed by
`MultiClassRaceForm` for tab enforcement and gate evaluation.

### Tasks

**4.1 — Create `Controllers/RaceController.MultiClass.cs`**

New partial file. Add two public methods:

```csharp
/// <summary>
/// Returns true if there are unresolved non-BYE matches in the currently
/// revealed round. Used by MultiClassRaceForm to enforce tab switching.
/// </summary>
public bool HasPendingMatchesInCurrentRound()
{
    var visibleMatches = EngineGetMatches()
        .Where(m => _revealedRounds.Contains(m.RoundLabel))
        .ToList();

    return visibleMatches.Any(m =>
        !ByePolicy.IsBye(m.Driver1) &&
        !ByePolicy.IsBye(m.Driver2) &&
        !_matchResult.HasResult(m.MatchId));
}

/// <summary>
/// Returns true if all RR matches are resolved (all rounds complete) OR
/// if the session has already advanced past the RR phase.
/// Used by MultiClassRaceForm for LB gate evaluation.
/// </summary>
public bool IsRrComplete()
{
    if (_session.RaceType != "Round Robin") return true;

    var allMatches = EngineGetMatches();
    return allMatches.All(m =>
        _matchResult.HasResult(m.MatchId) ||
        ByePolicy.IsBye(m.Driver1) ||
        ByePolicy.IsBye(m.Driver2));
}
```

---

**4.2 — Add tests for new methods**

Add to a new test file `RaceControllerMultiClassTests.cs` in the test project:

```csharp
[TestMethod]
public void HasPendingMatchesInCurrentRound_ReturnsTrueWhenMatchesUnresolved()
// Create a controller, generate RR bracket, do not submit any winners.
// HasPendingMatchesInCurrentRound() should return true.

[TestMethod]
public void HasPendingMatchesInCurrentRound_ReturnsFalseWhenAllResolved()
// Create a controller, generate RR bracket, submit all winners in round 1.
// HasPendingMatchesInCurrentRound() should return false.

[TestMethod]
public void IsRrComplete_ReturnsFalseWhenMatchesRemain()
// Fresh RR session with unresolved matches. IsRrComplete() returns false.

[TestMethod]
public void IsRrComplete_ReturnsTrueWhenAllMatchesResolved()
// All RR matches resolved. IsRrComplete() returns true.

[TestMethod]
public void IsRrComplete_ReturnsTrueWhenSessionIsLosersPhase()
// Session.RaceType = "Losers Bracket". IsRrComplete() returns true
// (already past RR).
```

### Verify Phase 4

All new tests pass. All existing tests still pass. Project builds.

---

## Phase 5 — Multi-Class Race Console

**Goal:** `MultiClassRaceForm` exists and correctly manages tabbed class
sessions, tab state, tab enforcement, the LB gate, stats writing, and the
combined event summary.

This is the largest phase. Build it in sub-steps.

### Tasks

**5.1 — Create `UI/Forms/Main/MultiClassRaceForm.cs` (skeleton)**

Start with a form that:
- Accepts `MultiClassEvent` and connection string in constructor.
- Creates one `RaceController` per class session.
- Creates a `TabControl` with one tab per class (class name as tab text).
- Embeds a minimal panel in each tab (placeholder — full bracket UI in 5.2).

```csharp
public MultiClassRaceForm(MultiClassEvent multiEvent, string connectionString)
{
    InitializeComponent();
    _multiEvent = multiEvent;
    _connectionString = connectionString;

    _controllers = new List<RaceController>();
    foreach (var session in multiEvent.ClassSessions)
    {
        var controller = new RaceController(session, connectionString);
        _controllers.Add(controller);
        SubscribeToController(controller, _controllers.Count - 1);
    }

    BuildTabs();
}
```

---

**5.2 — Embed race console per tab**

Two implementation options — choose one:

**Option A (preferred): UserControl extraction**
- Extract the bracket ListView, winner buttons, Generate Bracket button,
  Generate Next Round button, Winners ListView, Edit Result, and Buy Back
  button out of `Form1` into a new `RaceConsolePanel` UserControl
  (`UI/Forms/Main/RaceConsolePanel.cs`).
- `Form1` hosts one `RaceConsolePanel` (unchanged externally).
- `MultiClassRaceForm` hosts one `RaceConsolePanel` per tab.

**Option B (fallback): Hosted Form1 instances**
- Create one `Form1` instance per class, configured for hosted mode.
- Show/hide child form content inside tab pages.
- Use this only if Option A proves too disruptive to `Form1`.

Document which option was chosen in a comment at the top of
`MultiClassRaceForm.cs`.

---

**5.3 — Tab state management**

Implement `UpdateTabState(int classIndex)`:

```csharp
private void UpdateTabState(int classIndex)
{
    var tab = tabControl.TabPages[classIndex];
    var controller = _controllers[classIndex];

    if (classIndex == tabControl.SelectedIndex)
    {
        // Active tab — always blue (handled by TabControl selection natively)
        tab.BackColor = SystemColors.Highlight;
    }
    else if (_completedClassIndexes.Contains(classIndex))
    {
        tab.BackColor = Color.LightGray; // completed
    }
    else if (_rrCompleteClassIndexes.Contains(classIndex))
    {
        tab.BackColor = Color.LightGreen; // waiting for gate
    }
    else if (controller.HasPendingMatchesInCurrentRound())
    {
        tab.BackColor = Color.Orange; // matches pending
    }
    else
    {
        tab.BackColor = SystemColors.Control; // default/neutral
    }
}
```

Call `UpdateTabState` for all tabs whenever:
- A match result is submitted in any class.
- A round is advanced in any class.
- The active tab changes.

---

**5.4 — Tab switching enforcement**

Intercept `tabControl.Selecting`:

```csharp
private void tabControl_Selecting(object sender, TabControlCancelEventArgs e)
{
    if (e.TabPageIndex == tabControl.SelectedIndex) return;

    var activeController = _controllers[tabControl.SelectedIndex];
    if (activeController.HasPendingMatchesInCurrentRound())
    {
        e.Cancel = true;
        // Optional: brief status label "Complete all matches before switching class."
    }
}
```

---

**5.5 — LB gate**

Track RR completion per class:

```csharp
private HashSet<int> _rrCompleteClassIndexes = new HashSet<int>();

private void CheckAndReleaseRrGate()
{
    // Rebuild from scratch each call — use IsRrComplete() as source of truth
    _rrCompleteClassIndexes.Clear();
    for (int i = 0; i < _controllers.Count; i++)
    {
        if (_controllers[i].IsRrComplete())
            _rrCompleteClassIndexes.Add(i);
    }

    // Update tab colours
    for (int i = 0; i < _controllers.Count; i++)
        UpdateTabState(i);

    // If all classes are RR-complete, release the gate
    if (_rrCompleteClassIndexes.Count == _controllers.Count)
    {
        foreach (var idx in _rrCompleteClassIndexes.ToList())
            ReleaseClassToLbPhase(idx);
    }
}
```

`ReleaseClassToLbPhase(int classIndex)` switches to that class's tab and
triggers the buyback/LB prompt — the same flow `Form1` runs when
`CanOfferBuybackChanged` fires.

Call `CheckAndReleaseRrGate()` from the `CanAdvanceChanged` and
`CanOfferBuybackChanged` event handlers.

---

**5.6 — Stats and event completion**

```csharp
private HashSet<int> _completedClassIndexes = new HashSet<int>();

private void OnTournamentCompleted(int classIndex, RaceSummary summary)
{
    // Write stats — same as Form1 today
    foreach (var result in summary.MatchResults)
        _driverRepo.IncrementWinsAndLosses(result.WinnerId, result.LoserId);

    foreach (var entry in _multiEvent.ClassSessions[classIndex].DriverEntries)
        _driverRepo.IncrementEventsEntered(entry.DriverID);
    // Note: EventsEntered was already incremented at setup. Do NOT increment again here.
    // Only EventsWon needs updating here.

    _driverRepo.IncrementEventsWon(summary.WinnerId);

    // Per-class popup
    ShowClassCompletionPopup(summary, _multiEvent.ClassSessions[classIndex].ClassType);

    _completedClassIndexes.Add(classIndex);
    UpdateTabState(classIndex);

    if (_completedClassIndexes.Count == _controllers.Count)
        ShowCombinedEventSummary();
}
```

**`ShowCombinedEventSummary()`**

Build a formatted string and show it in `ScrollableTextDialog`:

```
═══════════════════════════════
  [EventName] — [EventDate]
  FINAL RESULTS
═══════════════════════════════

  [ClassName]
  ───────────
  Champion:   [WinnerName]
  Runner-Up:  [RunnerUpName]

  [ClassName]
  ───────────
  Champion:   [WinnerName]
  Runner-Up:  [RunnerUpName]

═══════════════════════════════
```

Data comes from each class's `RaceSummary` (stored when `TournamentCompleted`
fires per class).

---

**5.7 — Save button**

```csharp
private void btnSaveEvent_Click(object sender, EventArgs e)
{
    // Flush each controller's in-progress state back into its session object
    foreach (var controller in _controllers)
        controller.SaveSession(); // existing method on RaceController.Persistence.cs

    // Persist the whole MultiClassEvent
    _multiClassRepo.SaveEvent(_multiEvent);

    lblSaveStatus.Text = "Event saved.";
}
```

---

### Verify Phase 5

- Form opens with correct number of tabs, each labelled with the class name.
- Tab switching is blocked while active class has pending matches.
- Tab colours update correctly: orange when pending, green when waiting for
  gate, default when neutral.
- LB gate: complete class 1 RR → LB not triggered. Complete class 2 RR →
  both classes released to LB.
- Abandoned class (all matches resolved, never advanced) does not block gate
  (IsRrComplete returns true for it).
- Stats written correctly per class on completion.
- Combined summary appears when all classes complete.
- Save button writes a row to `MultiClassEvents` table.

---

## Phase 6 — Landing Page and Load Screen Wiring

**Goal:** The Race Director can launch a multi-class event from the Landing
Page and reload a saved one from the Load screen.

### Tasks

**6.1 — Add button to `LandingPageForm`**

In `LandingPageForm.cs`, add handler:

```csharp
private void btnNewMultiClassEvent_Click(object sender, EventArgs e)
{
    var setup = new MultiClassSetupForm(_connectionString);
    if (setup.ShowDialog() == DialogResult.OK)
    {
        var multiEvent = setup.MultiClassEventResult;
        var form = new MultiClassRaceForm(multiEvent, _connectionString);
        form.Show();
    }
}
```

In `LandingPageForm.Designer.cs`, add `btnNewMultiClassEvent` button.
Suggested label: **"New Multi-Class Event"**.
Place it below the existing "New Event" button.

---

**6.2 — Add multi-class section to `LoadSessionForm`**

In `LoadSessionForm.cs`:

- Add a second `ListView` (or a `TabControl` with two tabs: "Single-Class" /
  "Multi-Class") to show `MultiClassEventSummary` rows.
- Columns: Event Name, Date, Classes.
- On load, call `MultiClassEventRepository.GetAllEvents()` and populate.

On selecting a multi-class row and clicking Load:

```csharp
var evt = _multiClassRepo.LoadEvent(selectedSummary.Id);
var form = new MultiClassRaceForm(evt, _connectionString);
form.Show();
this.Close();
```

---

### Verify Phase 6

- "New Multi-Class Event" button appears on Landing Page.
- Clicking it opens `MultiClassSetupForm`.
- Completing setup opens `MultiClassRaceForm` with correct tabs.
- Saving a multi-class event then opening Load screen shows it as a single row.
- Loading that row reopens `MultiClassRaceForm` with all class state restored.

---

## Phase 7 — Final Polish and Full Regression

**Goal:** All edge cases handled, all tests passing, UI polished enough for
trackside use.

### Tasks

**7.1 — Edge case verification**

Walk through each edge case from spec §9:

| Case | Expected behaviour |
|------|--------------------|
| 1 class in multi-class event | Runs like single-class RR |
| Class with 1 driver | BYE fills bracket, no crash |
| Class with 2 drivers | Valid RR, runs normally |
| Duplicate class name at setup | Blocked with error message |
| Save mid-event | Succeeds; new row in MultiClassEvents |
| Load saved mid-event | State restored for all classes |

**7.2 — Run full test suite**

All tests pass. Zero regressions.

**7.3 — Build in Release mode**

`Build → Rebuild Solution` in Release configuration. Zero errors, zero
warnings introduced by new code.

---

## Summary: File Creation Order

```
Phase 1:  Domain/MultiClassEvent.cs
          ViewModels/MultiClassEventSummary.cs
          Repositories/DatabaseInitializer.cs  (modified)
          Repositories/MultiClassEventRepository.cs

Phase 2:  Tests/MultiClassEventRepositoryTests.cs

Phase 3:  UI/Forms/Session/MultiClassConfigDialog.cs + .Designer.cs
          UI/Forms/Session/MultiClassSetupForm.cs + .Designer.cs

Phase 4:  Controllers/RaceController.MultiClass.cs
          Tests/RaceControllerMultiClassTests.cs

Phase 5:  UI/Forms/Main/MultiClassRaceForm.cs + .Designer.cs
          UI/Forms/Main/RaceConsolePanel.cs (if Option A chosen)

Phase 6:  UI/Forms/Session/LandingPageForm.cs  (modified)
          UI/Forms/Session/LandingPageForm.Designer.cs  (modified)
          UI/Forms/Session/LoadSessionForm.cs  (modified)
          UI/Forms/Session/LoadSessionForm.Designer.cs  (modified)

Phase 7:  Final regression pass — no new files
```
