# RC Drag Manager — Session Setup Audit

**Note:** this audit covers the legacy WinForms UI. The current event flow lives in the WPF project (`Windows/LandingWindow`, `Windows/SetupWindow`, `Views/RaceConsoleView`).

## Scope

This audit covers the full "create event" flow from the landing page through to the race console opening. Files examined:

- `UI/Forms/Session/LandingPageForm.cs`
- `AppServices/MultiClassSetupService.cs` (`ValidateCanStart`, `BuildDriverEntries`, `StartEvent`) + `UI/Forms/Session/MultiClassSetupForm.cs`
- `Domain/RaceSession.cs`
- `UI/Forms/Main/Form1.cs` + `.UI.cs` + `.WinnerButtons.cs` + `.Display.cs`

---

## 1. LandingPageForm.cs

### What the user sees / does

The landing page presents five buttons: **Create Race Session**, **Load Saved Event**, **Driver Lists**, **Settings**, and **Exit**.

### Two separate paths to Form1

There are two distinct code paths that open the race console:

**Path B — "Create Race Session" (btnCreateRaceSession_Click)**

```csharp
var setup = new MultiClassSetupForm(_connStr);
if (setup.ShowDialog() == DialogResult.OK)
{
    var multiEvent = setup.MultiClassEventResult;
    var form = new MultiClassRaceForm(multiEvent, _connStr);
    form.Show();
}
```

The standard path. Opens `MultiClassSetupForm` modally; on OK, takes the configured `MultiClassEvent` from `setup.MultiClassEventResult`. The setup logic lives in `AppServices/MultiClassSetupService.cs` (`ValidateCanStart`, `BuildDriverEntries`, `StartEvent`), not in the form.

**Path C — Load Event (btnLoadEvent_Click)**

Loads a previously saved session from the DB. Not part of the "create" flow but follows the same Form1 open pattern.

### Hardcoded assumptions

- A new controller is always created fresh; there is no controller reuse across forms.

---

## 2. Session Setup (retired `SessionSetupForm`)

The form described below was retired. The current WinForms setup is `MultiClassSetupForm` + `MultiClassConfigDialog` (one configuration dialog per class) over `AppServices/MultiClassSetupService.cs` (`ValidateCanStart`, `BuildDriverEntries`, `StartEvent`). See `MULTI-CLASS-EVENT-SPEC.md` for the shipped multi-class flow.

### What the user sees / does

The setup form collects:

1. **Event Name** — free-text field
2. **Event Date** — date picker
3. **Race Type** — combo box: `"Pro Ladder"`, `"Random Draw"`, `"Round Robin"`
4. **Class** — three mutually exclusive radio buttons: **Heads Up**, **Bracket Class**, **Dial-In**
5. **Fixed Dial-In** — numeric field shown only when Bracket Class is selected
6. **Round Robin options** — shown only for Round Robin: variant (`Standard` / `QMDRA`) and round count
7. **Driver list** — filtered by the selected class; drivers checked into the event roster
8. **Start Race** button

### Data collected and stored

`BtnStartRace_Click` builds the `RaceSession`:

| Field | Source |
|-------|--------|
| `EventName` | Text box |
| `EventDate` | Date picker |
| `RaceType` | Combo box value (normalized) |
| `ClassType` | Derived from the selected radio button (`"Heads Up"` / `"Bracket Class"` / `"Dial-In"`) |
| `FixedDialIn` | Numeric field — only set for Bracket Class, otherwise `null` |
| `RoundRobinVariant` | Selector on RR panel (`"Standard"` / `"QMDRA"`) |
| `RoundsToRun` | Spinner on RR panel (QMDRA only) |
| `DriverEntries` | One `RaceSessionDriverEntry` per checked driver |

Two spellings are in play: the session-level `ClassType` uses `"Bracket Class"` and `"Dial-In"`, while the driver-list filter matches the car record's `ClassType` with `"Bracket"` and `"Dial In"` (a space). The two sets do not line up — a latent mismatch.

Per-driver `DialIn` assignment in `DriverEntries`:

| Class | What goes into `DialIn` |
|-------|------------------------|
| Heads Up | `null` (qualifying time copied to `QualifyingTime` instead) |
| Dial-In | `car.DefaultDialIn` from the car record |
| Bracket Class | `fixedDial` (the session-level fixed value) |

**Side effect on Start Race**: each selected driver's `EventsEntered` is bumped **immediately** when Start Race is clicked — before the race runs. (The retired form did this manually via `EventsEntered += 1` + `UpdateDriver`; today `MultiClassSetupService.StartEvent` calls `DriverRepository.IncrementEventsEntered`.) Cancelling after clicking Start Race still increments the counter.

### Hardcoded assumptions

1. **Single class per session**: The three class radio buttons are mutually exclusive. There is no mechanism to select drivers from different classes for the same session.
2. **Single FixedDialIn per session**: One `double?` covers all Bracket Class drivers equally.
3. **Class filter is binary**: `RefreshDriverList()` filters drivers using `car.ClassType == "Heads Up"` (etc.). A driver whose cars span multiple classes will only appear under each class separately. A driver **can** be entered in more than one class; entries are de-duplicated within a class only.
4. **EventsEntered incremented on setup, not on completion**: Stats are bumped before any racing happens. If the event is abandoned, the increment is not reversed.
5. **Race type is session-wide**: One race type covers all drivers. There is no per-class race type.

### UI limitations for multi-class support

- The class selection UI is three radio buttons — structurally prevents multi-class selection without a UI redesign (e.g., checkboxes or a per-driver class column).
- The driver list panel shows one class's drivers at a time. Showing drivers from multiple classes simultaneously would require a redesigned roster table with a class column.
- The Fixed Dial-In field is a single value. Multi-class would require per-class or per-driver dial-in overrides.

---

## 3. RaceSession (Domain Model)

### What gets stored

`RaceSession` has one class field:

```csharp
public string ClassType { get; set; }
```

This is a single string. It is set once at session creation and is never updated during the race.

```csharp
public double? FixedDialIn { get; set; }
```

Also a single value. Used only for Bracket Class events; `null` for all other class types.

`DriverEntries` is `List<RaceSessionDriverEntry>`. Each entry has its own `ClassType` (copied from the car record) and `DialIn`. So the data model **does** store per-driver class information at the entry level. The live roster (`RaceController.LiveUpdate.cs`) reads the per-entry `ClassType`; `Form1`'s driver hydration does not.

### What does NOT get stored

- No concept of "classes within a session" — no `List<string> ClassTypes`.
- No per-class race type, round structure, or engine configuration.
- No per-class bracket or result set.

### Serialization

`RaceSession` is serialized in full to JSON by `System.Text.Json` and stored in `RaceSessions.SessionData`. `ClassType` and `FixedDialIn` are serialized as plain string/double fields. There is no migration concern for existing sessions if these fields are changed additively.

---

## 4. RaceSessionDriverEntry

```csharp
public class RaceSessionDriverEntry
{
    // Unique within a class. Standard classes may leave this as zero; the
    // multi-car RR setup assigns one so two cars from the same driver are
    // distinct competitors in the engine and standings.
    public int RaceEntryId { get; set; }
    public int DriverID { get; set; }
    public string DriverName { get; set; }
    public int CarID { get; set; }
    public string CarName { get; set; }
    public string ClassType { get; set; }   // car's class at session creation time
    public double? DialIn { get; set; }
    public double? QualifyingTime { get; set; }
    public int? Seed { get; set; }
}
```

Each `DriverEntry` carries `ClassType` and `DialIn` — a snapshot of the car's class and dial-in at session creation. This means the data model technically supports drivers having different class values within one session.

---

## 5. Form1 (Race Console)

### What gets used from the session

`Form1`'s constructor projects `DriverEntries` into a `List<Driver>` with a LINQ `Select`, taking `Id = entry.DriverID`, `Name = entry.DriverName` and `QualTime = entry.QualifyingTime` from each entry.

**On this path, only `DriverID`, `DriverName`, and `QualifyingTime` are used.** `CarID`, `CarName`, `ClassType`, and `DialIn` from the entry are not read when `Form1` hydrates its driver list. Other paths do read them: `RaceController.LiveUpdate.cs` builds the live roster from `CarName`, `DialIn` and `ClassType`, and `RaceController.Resume.cs` and `RaceController.RoundFlow.Core.cs` read `CarName` and `DialIn`. The `Driver` objects passed to the engine have no class information at all.

Form1 itself makes no use of `ClassType`; the per-class tab titles in `MultiClassRaceForm` come from `session.ClassType`.

### What the user sees

- A bracket display with all participating drivers in one pool, sorted by qualifying time.
- No class separation in the bracket view.
- No class column in any ListView.
- No per-driver class information shown during the race.

### Hardcoded assumptions

- All drivers in a session are treated as one undivided pool.
- `QualTime` is the only ranking criterion — it is class-agnostic.
- `LaneFairnessManager` tracks lane history per driver ID — no class awareness.
- Stats increments (`IncrementWinsAndLosses`, `IncrementEventsEntered`, `IncrementEventsWon`) operate on Driver IDs only — no class dimension.

### Downstream (engine layer)

`RaceController.GenerateBracket()` receives `List<Driver>` — no class information. All three engine types (`ProLadderEngineAdapter`, `RandomEngineAdapter`, `RoundRobinEngineAdapter`) operate on the flat driver list. `IRaceEngine` has no class parameter. Multi-Car Round Robin adds entry-scoped scheduling alongside the class-scoped controllers: `MultiCarRoundRobinScheduler.Build` turns the `MultiCarRaceEntry` field into an `EngineMatch` fixture keyed by per-entry `RaceEntryId`, so a class can hold several cars from one driver without changing `IRaceEngine`.

---

## Multi-Class Support Summary

### Can the current `RaceSession` model support multiple classes without structural changes?

> **Superseded:** multi-class shipped as parallel per-class tabs under a `MultiClassEvent` parent (`Domain/MultiClassEvent.cs`, `Repositories/MultiClassEventRepository.cs`) — see `MULTI-CLASS-EVENT-SPEC.md`. The assessment below is the pre-implementation view.

**No — not without changes**, though some groundwork exists.

The `RaceSessionDriverEntry` type already carries per-driver `ClassType` and `DialIn`, so the entry-level data model is structurally capable of representing a mixed-class roster. However, everything above and below that level assumes one class:

| Layer | Current constraint |
|-------|--------------------|
| `RaceSession` | Single `ClassType` string; single `FixedDialIn` |
| Setup UI | Three mutually exclusive radio buttons — one class selectable |
| Driver filter | Filters to one class at a time |
| `Form1` hydration | Reads only `DriverID`, `DriverName`, `QualifyingTime` from entries |
| Engine layer | Receives `List<Driver>` — no class information |
| Bracket logic | No per-class bracket, round structure, or result set |
| Stats | Incremented per driver ID — no class dimension |

### What would need to change for multi-class support?

**Data model:**

1. Replace `string ClassType` with `List<string> ClassTypes` (or a structured class-config list) on `RaceSession`.
2. Replace `double? FixedDialIn` with a per-class or per-driver override map.
3. Add a concept of "class bracket" — either multiple engines running in parallel or sequential class runs within one session.

**Setup UI:**

1. Replace the three class radio buttons with checkboxes or a multi-select list.
2. Change `RefreshDriverList()` to include drivers from all selected classes, with a visible class column.
3. Add per-class race type configuration if classes can run different formats.

**Race console:**

1. Change `Form1` hydration to pass `ClassType` and `DialIn` through to the driver objects or match records.
2. Add class-aware bracket views if classes run simultaneously.
3. Or: implement sequential class runs — run class A to completion, then class B — which would require session phasing logic.

**Engine layer:**

1. `IRaceEngine.LoadDrivers()` would need a class parameter, or engines would need to be instantiated per class.
2. `RaceController` would need to manage multiple engine instances or a class-run queue.

**Stats:**

1. `IncrementWinsAndLosses` and `IncrementEventsWon` would need a class dimension if stats are tracked per class.

### Least-invasive path

> **Superseded:** multi-class was implemented as parallel per-class tabs in one console under a `MultiClassEvent` parent, not sequential runs. See `MULTI-CLASS-EVENT-SPEC.md`.

The least disruptive approach would be **sequential class runs**: treat each class as a fully independent session (one `RaceSession` per class), run them consecutively, and save them separately. This requires no structural changes to `RaceSession`, the engine layer, or the race console. The only addition needed is a "next class" workflow on the landing page or a session-linking mechanism in the UI.

A fully parallel multi-class session (all classes on screen simultaneously) would require significant structural work across all layers.

---

## Findings at a Glance

| Finding | File | Notes |
|---------|------|-------|
| One create path: setup form, then the race console | `LandingPageForm.cs` | `btnCreateRaceSession_Click` opens `MultiClassSetupForm`, then `MultiClassRaceForm` |
| Class configuration is per class, not per session | `MultiClassSetupService.cs` | Classes configured per `ClassConfigDto`; single-class constraint is superseded |
| `EventsEntered` incremented on Start Race, not on completion | `MultiClassSetupService.cs` (`StartEvent`) | Abandoned events still count |
| Per-driver `ClassType` stored in entry and read by the live roster | `RaceSessionDriverEntry` | `RaceController.LiveUpdate.cs` filters on it |
| Form1 hydrates only `DriverID`, `DriverName`, `QualTime` | `Form1.cs` | `CarID`, `CarName`, `ClassType`, `DialIn` silently dropped |
| Engine layer has no class concept | All `IRaceEngine` impls | `List<Driver>` is class-agnostic |
| Stats increments have no class dimension | `RaceConsoleService`, `MultiClassRaceService`, `MultiClassSetupService`, `RaceController.Stats` | Per-driver ID only. `IncrementWinsAndLosses`: RaceConsoleService + MultiClassRaceService + RaceController.Stats. `IncrementEventsWon`: RaceConsoleService + MultiClassRaceService. `IncrementEventsEntered`: MultiClassSetupService + RaceConsoleService |
