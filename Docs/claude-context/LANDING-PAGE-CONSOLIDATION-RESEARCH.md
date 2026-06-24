# Landing Page Consolidation — Research

Read-only investigation of the three event-creation paths exposed on
`LandingForm` (formerly `LandingPageForm`) and what would be needed to
collapse them. No code changes made.

---

## QUESTION 1 — Quick Session path (`btnNewEvent`)

### Trace

`LandingPageForm.cs:37-43`:
```csharp
private void btnNewEvent_Click(object sender, EventArgs e)
{
    Logger.Log("[QUICK] Launching Quick Session → RaceController(new RaceSession())");
    var controller = new RaceController(new RaceSession());   // empty quick session
    var mainForm = new Form1(controller);
    mainForm.Show();
}
```

The button creates an empty `RaceSession` (no event name, no date, no class
type, no driver entries, no race-type selection), wraps it in a new
`RaceController`, and shows a standalone `Form1`.

### What's missing vs. Create Race Session path

Comparing to `btnCreateSession_Click` (`LandingPageForm.cs:45-63`), Quick
Session skips the entire `SessionSetupForm` step. So a Quick Session has:

| Field | Create Race Session | Quick Session |
|---|---|---|
| `RaceSession.EventName` | from text box | "" (blank — Form1 falls back to `"Quick Session"` literal at `Form1.cs:54-56`) |
| `RaceSession.EventDate` | from date picker | `default(DateTime)` |
| `RaceSession.RaceType` | from `cmbRaceType` (Pro Ladder / Random / RR) | `null` (set later when GenerateBracket is called from Form1's own `cmbRaceType`) |
| `RaceSession.ClassType` | from setup form | `null` |
| `RaceSession.DriverEntries` | populated from selected drivers + cars | empty list — user has to add drivers via Form1's `txtName` + `txtTime` form |
| `FixedDialIn` | optional (Bracket Class) | not set |
| `RoundRobinVariant` / `RoundsToRun` | configurable | defaults (`"Standard"`, null) |

In Quick Session, **driver entries get set only as a side effect** of
`RaceController.SaveSession()` rebuilding `DriverEntries` from the
in-memory `drivers` list (`RaceController.Persistence.cs:79-89`).

### Live broadcast behaviour

`RaceController.LiveUpdate.cs:23` and `:533`, plus
`RoundFlow.Core.cs:169` substitute the literal `"Quick Session"` for the
`EventName` when blank. So Quick Session events do show up on the live
feed with that placeholder name and the same `EventId` as any other
session (`RaceSession` constructor always assigns a new GUID at
`RaceSession.cs:50`).

### `btnSaveAndClose` short-circuit for Quick Session

`Form1.Events.cs:292-299`:
```csharp
if (currentSession == null)
{
    MessageBox.Show("Quick Session completed. No session file saved.");
    Close();
    return;
}
```

Note: `currentSession` is hydrated from `_controller.Session` at
`Form1.cs:52`, which is the empty `RaceSession` (not null). So this
guard does **not actually fire** for Quick Session — `currentSession`
is non-null. The "Quick Session completed" branch is dead code in the
current flow. The full save path runs even for Quick Session, writing
an INSERT row to the `RaceSessions` table.

### Other references

- `LandingPageForm.cs:39, 41` — the click handler itself.
- `LandingPageForm.Designer.cs:12, 31, 58-63` — designer registration.
- `Form1.cs:56`, `RaceController.LiveUpdate.cs:23`,
  `RaceController.RoundFlow.Core.cs:169, 533` — the literal string
  `"Quick Session"` is used as a fallback `EventName`.
- **No test references** — searched for `btnNewEvent`, `Quick Session`,
  `new RaceSession()` in tests; only `PairingHistorySerializationTests.cs`
  constructs `new RaceSession()` directly, and that's a domain-level
  serialisation test that would survive the button being deleted.

---

## QUESTION 2 — Create Race Session path (`btnCreateSession`)

### Trace

`LandingPageForm.cs:45-63`:
```csharp
var setup = new SessionSetupForm(_driverRepo);
if (setup.ShowDialog() == DialogResult.OK)
{
    var rs = setup.RaceSessionResult;
    var controller = new RaceController(rs);
    var mainForm = new Form1(controller);
    mainForm.Show();
}
```

`SessionSetupForm.Events.cs:206-243` builds a `RaceSession` with:
- `EventName`, `EventDate` from the form
- `RaceType` from `cmbRaceType` — three options
  (`SessionSetupForm.Designer.cs:79`): `"Pro Ladder"`, `"Random Draw"`,
  `"Round Robin"`
- `ClassType` (Heads Up / Dial-In / Bracket Class) — drives whether
  `QualifyingTime` or `DialIn` is populated per entry
  (`SessionSetupForm.Events.cs:224-229`)
- `DriverEntries` populated from the selected event roster
- `RoundRobinVariant` / `RoundsToRun` if Round Robin

Form1 then opens with the populated session.

### What standalone Form1 does that MultiClassRaceForm does not replicate

| Concern | Standalone Form1 | MultiClassRaceForm equivalent |
|---|---|---|
| **Save and Close** | `Form1.Events.cs:292-323` saves via `_controller.SaveSession()` + `sessionRepository.SaveSession(currentSession)` + `_controller.RecomputeEventsWon(...)` + closes the form. | Each hosted Form1's `btnSaveAndClose_Click` runs the same controller/session save, **then** also persists the parent multi-class event via `_multiClassEventRepo.SaveEvent(_multiClassEvent)` (`Form1.Events.cs:304-315`). Hosted mode raises `HostedSaveAndCloseCompleted` instead of closing — the parent `MultiClassRaceForm` closes itself (`MultiClassRaceForm.cs:115-118`). |
| **TournamentCompleted popup + stats** | `Form1.Events.cs:65-87` shows the per-event winner/runner-up popup and calls `_controller.PersistTournamentStats(...)` — increments EventsEntered for all participants, EventsWon for the winner, TotalWins/TotalLosses for each match. | Suppressed in hosted mode via `if (IsHostedMode) return;` (`Form1.Events.cs:70`). `MultiClassRaceForm.OnClassTournamentCompleted` (`MultiClassRaceForm.cs:235-282`) runs instead — does its own `IncrementWinsAndLosses` per match + `IncrementEventsWon` for the winner. EventsEntered is incremented **up front** in `MultiClassSetupForm.BtnStartRace_Click` (line 155), not on completion. |
| **Per-match stats** (`PersistMatchStats` / `PersistEventWon`) | Fires from `Form1.WinnerButtons.cs:281, 286` on every Winner1/Winner2 click. | Same — these calls have **no `IsHostedMode` gate**, so they fire in hosted mode too. (Result: stats are written *twice* end-to-end in both standalone and multi-class — once per match, once at completion. Pre-existing behaviour, not a difference between paths.) |
| **Buy-back popup** | `Form1.Events.cs:35-43` `OnCanOfferBuybackChanged` shows "Round-Robin complete. Click 'Buy Back'..." popup when `enabled && !IsHostedMode`. | Suppressed in hosted mode. `MultiClassRaceForm.CheckAndReleaseRrGate` (`MultiClassRaceForm.cs:207-231`) runs instead — waits for **all classes** to finish RR, then shows a single combined popup. |
| **Buy-back flow itself** | `Form1.Events.cs:336-399` — `btnGenerateLosersBracket_Click` → `BuybackDriverSelectionForm` → `_controller.GenerateLosersBracket(selectedDrivers)` (with single-driver direct-promotion at line 374). | Same — each hosted Form1 runs its own buy-back independently. The only multi-class addition is the gate at the parent level (don't show buy-back per-class until all classes are RR-complete). |
| **Finals flow** | `Form1.Events.cs:45-63` `OnCanStartFinalsChanged` shows "Finals Ready" popup when `enabled && !_finalsPopupShown`. **No `IsHostedMode` gate.** | The popup **still fires** in hosted mode (no suppression). `MultiClassRaceForm` doesn't intercept Finals state — finals are run per-class via the embedded Form1. |
| **Live broadcast** | `_controller.StartDialInPolling()` in `Form1` ctor (`Form1.cs:93`) and `StopDialInPolling()` on close (line 110). Push happens via `QueueLiveUpdate` triggered by controller actions. One controller, one push stream, one EventId. | Each tab has its own `RaceController` (`MultiClassRaceForm.cs:51-54`), so each starts/stops its own dial-in polling and pushes its own stream. With N classes, N concurrent push streams to `/api/update` — each carries the **same `EventId`** if `MultiClassSetupForm.BtnStartRace_Click` reuses the same RaceSession across classes (see Q3). |
| **`cmbRaceType` selection** | Persisted across reset: `Form1.Events.cs:286-289` re-applies `currentSession.RaceType` after `Reset()`. | Hosted Form1 also has this code; works the same. But MultiClass setup hardcodes `RaceType = RaceTypes.RoundRobin` (see Q3) — so the dropdown is effectively pre-selected to RR with no other option. |

The **one piece of logic standalone exclusively owns** is the
`OnTournamentCompleted` → `PersistTournamentStats` path and the
"Quick Session no save" guard. Both have parallel implementations on the
multi-class side.

---

## QUESTION 3 — New Multi-Class Event path (`btnNewMultiClassEvent`)

### Trace

`LandingPageForm.cs:94-109`:
```csharp
var setup = new MultiClassSetupForm(_connStr);
if (setup.ShowDialog() == DialogResult.OK)
{
    var multiEvent = setup.MultiClassEventResult;
    var form = new MultiClassRaceForm(multiEvent, _connStr);
    form.Show();
}
```

`MultiClassSetupForm.BtnStartRace_Click` (`MultiClassSetupForm.cs:135-183`):
1. Validates that every class has at least one driver. **No minimum class
   count check** — `_classList.Count == 0` would build an empty event.
2. Calls `IncrementEventsEntered` for **every driver in every class** —
   *up front, before any race has been run*.
3. Builds a `MultiClassEvent` with one `RaceSession` per class. **Hardcodes
   `RaceType = RaceTypes.RoundRobin`** for every session
   (`MultiClassSetupForm.cs:172`). Pro Ladder and Random Draw are
   **inaccessible** through the multi-class path.
4. Each session gets its own `EventId` (auto-generated by
   `RaceSession` ctor at `RaceSession.cs:50`) — note these are *different*
   per class. The `EventName` and `EventDate` are shared across the
   sessions.

### `MultiClassRaceForm.BuildTabs` flow

`MultiClassRaceForm.cs:86-113`:
```csharp
for (int i = 0; i < _controllers.Count; i++) {
    var session = _multiEvent.ClassSessions[i];
    var tab = new TabPage(session.ClassType);
    var form1 = new Form1(_controllers[i]);
    form1.IsHostedMode = true;
    form1._multiClassEvent = _multiEvent;
    form1._multiClassEventRepo = _multiClassRepo;
    form1.TopLevel = false;
    form1.FormBorderStyle = FormBorderStyle.None;
    form1.Dock = DockStyle.Fill;
    tab.Controls.Add(form1);
    form1.HostedSaveAndCloseCompleted += OnHostedSaveAndCloseCompleted;
    form1.Show();
    tabControl.TabPages.Add(tab);
    _classRaceForms.Add(form1);
}
```

Each tab embeds a borderless top-level-`false` Form1 instance with:
- Its own `RaceController` (constructed in
  `MultiClassRaceForm.cs:49-54`)
- Hosted-mode flag enabled
- Pointers to the parent `MultiClassEvent` and `MultiClassEventRepository`

### Differences from standalone (concise)

- **Save**: hosted Form1's save also persists the multi-class event row
  + raises an event so the parent form closes. Standalone closes itself.
- **Stats**: pre-race EventsEntered + post-race per-match W/L + post-race
  EventsWon, all done by the parent. Standalone does it all on
  TournamentCompleted via `PersistTournamentStats`.
- **Buy-back gating**: parent waits for all classes RR-complete, then a
  single combined popup. Standalone shows the popup immediately for that
  class.
- **Finals popup**: **NOT suppressed in hosted mode** — it still fires
  per-class. (Possibly intentional, since each class runs its own finals
  independently.)
- **Live broadcast**: N independent push streams from N controllers, each
  with its own `EventId`. The desktop side does not coordinate them; the
  server-side store buckets by `EventName` (per
  `RCDragLiveServer/Services/InMemoryLiveRaceStateStore.cs:73-75`), so
  multiple class-streams with different `EventId`s but the same
  `EventName` collapse into one event bucket on the server with one
  class entry per `ClassType`.
- **Race type**: hardcoded to Round Robin.

---

## QUESTION 4 — Can MultiClassRaceForm run a single class?

### Code-path inspection

- **`MultiClassSetupForm`**: no minimum class count. The user could click
  Add Class once, fill the dialog, then Start Race with one class.
  Validation only checks that each class has ≥1 driver
  (`MultiClassSetupForm.cs:138-148`).
- **`MultiClassRaceForm.BuildTabs`**: loops over `_controllers.Count`. One
  iteration produces one tab containing one hosted Form1 with the same
  border-less, dock-fill setup as the multi-class case.
- **`UpdateAllTabStates`** / **`tabControl_Selecting`**: gracefully handle
  any tab count, including one.
- **`CheckAndReleaseRrGate`** (lines 207-231): `_rrCompleteClassIndexes
  .Count == _controllers.Count` triggers the "all RR complete" popup. With
  one class, this fires immediately when that one class finishes RR. The
  message reads "All classes have completed Round Robin..." which is
  technically correct but slightly odd phrasing for a single class.
- **`OnClassTournamentCompleted`** (lines 235-282): when all classes
  complete, calls `ShowCombinedEventSummary`. With one class, the summary
  shows just that class — works but headed `FINAL RESULTS` over a single
  block.

### What a single-class multi-class event would look like in practice

- LandingForm → "New Multi-Class Event" → MultiClassSetupForm
- Add one class (e.g. "Heads Up", RR, drivers)
- Start Race → MultiClassRaceForm with **one tab**
- The tab shows the same Form1 UI as standalone, just embedded in a tab
  control with a single page
- Buy-back ready popup says "All classes have completed Round Robin"
  (singular phrasing slightly off)
- On completion, "Class Complete" popup fires, then immediately the
  combined "Event Complete — Final Results" dialog shows just that class

### Constraints

1. **Race type forced to Round Robin.** Cannot run a single Pro Ladder or
   Random Draw event through this path without also changing
   `MultiClassSetupForm`.
2. **Pre-race EventsEntered increment.** A driver who registers in setup
   then doesn't actually race still gets their EventsEntered bumped (and
   not rolled back if the user cancels mid-event).
3. **Cosmetic phrasing**: "All classes have completed Round Robin" with
   N=1 sounds wrong but is harmless.

There are **no functional blockers** to running MultiClassRaceForm with
exactly one class. The setup form already permits it.

---

## QUESTION 5 — What would break if Quick Session and Create Race Session were removed?

### All references to `btnNewEvent`

| File | Line | What |
|---|---|---|
| `UI/Forms/Session/LandingPageForm.cs` | 37, 39, 41 | Click handler |
| `UI/Forms/Session/LandingPageForm.Designer.cs` | 12, 31, 58-63 | Designer button registration |

No production code outside the designer/handler references `btnNewEvent`.
**No test references.** The string `"Quick Session"` is used as a
**display-only fallback** for blank `EventName` in three places
(`Form1.cs:56`, `RaceController.LiveUpdate.cs:23`,
`RaceController.RoundFlow.Core.cs:169, 533`); those would remain valid
even if the button were removed (any blank-name session would still
display "Quick Session"). There's also one dead branch
(`Form1.Events.cs:292-299`) that was Quick-Session-specific and is
already unreachable today.

### All references to `btnCreateSession`

| File | Line | What |
|---|---|---|
| `UI/Forms/Session/LandingPageForm.cs` | 45 | Click handler |
| `UI/Forms/Session/LandingPageForm.Designer.cs` | 13, 32, 65-70 | Designer button registration |

No test references. The Create Session click handler depends on:
- `SessionSetupForm` — used **only** by this handler. Removing this path
  removes the only entry point to `SessionSetupForm`.
- `Form1` constructor with a populated session — still used by Quick
  Session (if kept) and by Load Saved Event for single-class loads
  (`LandingPageForm.cs:73`).

### Production features only reachable through standalone Form1

| Feature | Where | Replicable in MultiClass? |
|---|---|---|
| **Pro Ladder race type** | `SessionSetupForm.Designer.cs:79`, `Form1.Designer.cs:509-512`, `MatchEngine.cs` | ❌ Multi-class hardcodes RR. Migration would require adding a per-class race-type dropdown to `MultiClassConfigDialog` and removing the hardcode in `MultiClassSetupForm.cs:172`. |
| **Random Draw race type** | Same | ❌ Same as Pro Ladder. |
| **Heads Up class** | `SessionSetupForm.Events.cs:224-225` | Multi-class has its own `MultiClassConfigDialog` flow that builds `RaceSessionDriverEntry` directly — need to check whether it sets `QualifyingTime` for Heads Up. (Not investigated in this pass.) |
| **Bracket Class with `FixedDialIn`** | `SessionSetupForm.Events.cs:217, 228-229` | Same — depends on whether `MultiClassConfigDialog` supports `FixedDialIn`. |
| **Standalone Form1 reset behaviour** (`Form1.Events.cs:286-289`) | Re-applies `currentSession.RaceType` after Reset | Already works in hosted Form1. |

### Things to verify before removing the buttons

1. **Race type parity.** Either preserve standalone for Pro Ladder /
   Random Draw, or extend `MultiClassConfigDialog` /
   `MultiClassSetupForm.cs:166-179` to allow per-class `RaceType` and
   `FixedDialIn`, removing the RR hardcode.
2. **Heads Up qualifying time**. Confirm `MultiClassConfigDialog` builds
   `RaceSessionDriverEntry.QualifyingTime` for Heads Up classes (the
   path SessionSetupForm uses at `SessionSetupForm.Events.cs:225`).
3. **EventsEntered timing**. Multi-class increments EventsEntered up
   front; standalone does it on `TournamentCompleted`. After collapsing,
   pick one and stick to it (or your stats accounting will diverge).
4. **Live broadcast EventId per-class**. If you collapse to multi-class
   for *all* events, every event becomes N concurrent streams (one per
   class). Confirm the server-side bucketing by `EventName` is what
   you want (it currently is — see
   `InMemoryLiveRaceStateStore.cs:73-75`).
5. **"Quick Session" fallback string**. Either remove the literal
   fallbacks (`Form1.cs:56`, `RaceController.LiveUpdate.cs:23`, etc.) or
   leave them as harmless dead-string defenders.
6. **Dead-code cleanup**: `Form1.Events.cs:292-299` (the
   `currentSession == null` branch) becomes provably dead.
7. **`SessionSetupForm`** can be deleted entirely if Create Race Session
   is removed (no other references). Same for `SessionSetupForm.Events.cs`,
   `SessionSetupForm.Designer.cs`.
8. **Tests**: only `PairingHistorySerializationTests.cs` uses
   `new RaceSession()` directly — domain-level, unaffected. No UI tests
   to update.

### Summary

**Nothing tested or production-critical is unique to the standalone path
*except* race-type parity (Pro Ladder, Random Draw) and the
`SessionSetupForm.ClassType`-driven population of `QualifyingTime` /
`DialIn` / `FixedDialIn` per class type.** If multi-class setup is
extended to cover those, the standalone path can be removed cleanly.

---

## QUESTION 6 — Load Saved Event

### Trace

`LandingPageForm.cs:65-77` opens `LoadSessionForm` as a modal. Two
divergent paths:

1. **Single-class session selected** — `LoadSessionForm.btnLoad_Click`
   sets `LoadedSession` and returns `DialogResult.OK`
   (`LoadSessionForm.cs:171-172`). Back in `LandingPageForm.cs:71-74`:
   ```csharp
   var loaded = load.LoadedSession;
   var controller = new RaceController(loaded);
   var mainForm = new Form1(controller);
   mainForm.Show();
   ```
   Standalone Form1.

2. **Multi-class event selected** —
   `LoadSessionForm.LoadSelectedMultiClassEvent` (`LoadSessionForm.cs:182-215`):
   ```csharp
   var evt = _multiClassRepo.LoadEvent(selectedId);
   var form = new MultiClassRaceForm(evt, _connectionString);
   form.Show();
   Close();
   ```
   Opens MultiClassRaceForm directly **from inside the load dialog**,
   bypassing `LandingPageForm`'s OK branch. The parent `LandingPageForm`
   sees the dialog close with no `DialogResult.OK` and does nothing.

### Would loaded sessions still work if standalone path was removed?

**Single-class loaded sessions would be orphaned.** Today they go through
`new Form1(controller)`. If standalone Form1 is removed, single-class
loads need to be migrated to MultiClassRaceForm with one tab.

The simplest migration:
- Wrap any loaded `RaceSession` in a `MultiClassEvent` with one
  `ClassSession`, then open `MultiClassRaceForm`.
- This requires no changes to the persistence schema — the existing
  `RaceSessions` rows stay valid; you'd just route them through the
  multi-class viewer.

Caveats:
- `MultiClassEventRepository.LoadEvent` and `SaveEvent` would need to
  tolerate "single-class" multi-events (probably already does — loading
  a multi-class with one class is a subset of loading any multi-class).
- The `RaceType` on a loaded session might not be Round Robin (e.g. an
  old Pro Ladder session). MultiClassRaceForm itself doesn't gate on
  RaceType — it just passes the session to the embedded Form1 — so a
  loaded Pro Ladder session would keep working, **provided** the
  hardcode in `MultiClassSetupForm` is only for *new* multi-class
  events, not a general restriction. Verify this stays true.

### Open question for the user

If the long-term goal is "everything goes through multi-class", consider
whether `LoadSessionForm` should drop its two-tab UI and present a single
unified list of "events" (each with 1+ classes). That's a larger UX
change but eliminates the divergent load paths as well.
