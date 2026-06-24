# Time Fields Research — Set Time, QualifyingTime, DialIn

Read-only investigation of how `Set Time`, `QualifyingTime`, and `DialIn`
flow through the desktop app. No code changes made.

---

## 1. What does the "Set Time" button do?

**Control:** `btnSetQualTime` — `Form1.Designer.cs:30,76,253-262`. Text =
`"Set Time"`. Sits on the driver-setup panel of `Form1`.

**Click handler:** `Form1.Events.cs:174-194` — `btnSetQualTime_Click`.

```csharp
if (lvDrivers.SelectedItems.Count > 0) {
    string selectedName = lvDrivers.SelectedItems[0].Text;
    var driver = drivers.FirstOrDefault(d => d.Name == selectedName);
    if (driver != null) {
        var qualDialog = new AddEditQualTimeDialog(driver.Name, driver.QualTime);
        if (qualDialog.ShowDialog() == DialogResult.OK) {
            driver.QualTime = qualDialog.QualifyingTime;
            UpdateDriverList();
        }
    }
}
```

Steps:
1. Reads selected driver from `lvDrivers` (the driver-roster ListView).
2. Looks the driver up in Form1's local `drivers` `List<Driver>`.
3. Opens `AddEditQualTimeDialog` — a small modal with one `NumericUpDown`
   (`UI/Forms/Drivers/AddEditQualTimeDialog.cs`).
4. On OK: writes the new value back to the **local `Driver` object's
   `QualTime`** property.
5. Calls `UpdateDriverList()` to refresh the ListView.

**Observations:**
- The handler **does not** call the `RaceController`, **does not** touch
  `_session.DriverEntries.QualifyingTime` directly, and **does not** push a
  live update.
- The change persists only because the save path
  (`RaceController.Persistence.cs:79-89`) rebuilds `DriverEntries` from
  `_drivers` (which references the same `Driver` objects) at save time:
  ```csharp
  _session.DriverEntries.Add(new RaceSessionDriverEntry {
      DriverID = d.Id, DriverName = d.Name, QualifyingTime = d.QualTime
  });
  ```
- **The button has no `Enabled` gate.** It stays clickable throughout the
  entire session — before, during, and after `GenerateBracket()`. By
  contrast, `btnGenerateBracket` is gated by
  `_controller.HasBracketStarted` (`Form1.UI.cs:38`).
- `MultiClassRaceForm` does not have its own Set Time button — each tab
  hosts a `Form1` instance (`MultiClassRaceForm.cs:96-104`), so the button
  appears on every class tab.

---

## 2. Where is `QualifyingTime` stored and displayed?

### Storage (three layers)

| Layer | Field | File:line |
|---|---|---|
| Domain (in-memory driver) | `Driver.QualTime` (double?) | `Domain/Drivers.cs:40` |
| Domain (session entry) | `RaceSessionDriverEntry.QualifyingTime` (double?) | `Domain/RaceSession.cs:72` |
| Database | `Drivers.QualTime` (REAL) | `Repositories/DatabaseInitializer.cs:23` |

### Set into `DriverEntries`

`SessionSetupForm.Events.cs:219-243` — only populated when
`classType == "Heads Up"`:
```csharp
if (classType == "Heads Up")  qualTime = er.driver.QualTime;
else if (classType == "Dial-In") dialIn = er.car.DefaultDialIn;
else if (classType == "Bracket Class") dialIn = fixedDial;
```
For Dial-In and Bracket Class, `QualifyingTime` stays `null`.

### Hydrated into Form1's `drivers` list when an existing session loads

`Form1.cs:60-67`:
```csharp
drivers = currentSession.DriverEntries
    .Select(e => new Driver { Id = e.DriverID, Name = e.DriverName, QualTime = e.QualifyingTime })
    .ToList();
```

### Persisted back

`RaceController.Persistence.cs:86` — at save time, `DriverEntries` are
rebuilt with `QualifyingTime = d.QualTime`.

### Displayed

- **`lvDrivers`** (driver-roster ListView on Form1) — `Form1.UI.cs:31`,
  formatted as `"0.000"` or `"—"`. **This is on the setup panel, not during
  a race.** It is also the order key for the list (`Form1.UI.cs:22-25`).
- **`DriverManagerForm`** — `DriverManagerForm.UI.cs:80` shows `"Qual Time"`
  in the driver detail pane.
- **Not shown** in the race console (`lvPairings`, `lvWinners`, the winner
  buttons), or in the live scoreboard payload, or in `MultiClassRaceForm`
  outside of the embedded Form1's `lvDrivers`.

### Database update path

`DriverRepository.UpdateQualifyingTime(int driverId, double qualTime)` —
`Repositories/DriverRepository.cs:413-426`. Called from
`DriverManagerForm.Events.cs:287` (the standalone Driver Manager screen),
not from Form1's `btnSetQualTime`.

---

## 3. Where is `DialIn` stored? Is it displayed during a race?

### Storage

| Layer | Field | File:line |
|---|---|---|
| Domain (session entry) | `RaceSessionDriverEntry.DialIn` (double?) | `Domain/RaceSession.cs:71` |
| Domain (session-level) | `RaceSession.FixedDialIn` (double?) | `Domain/RaceSession.cs:18` |
| Domain (per-car) | `Car.DefaultDialIn` (double?) | `Domain/Car.cs:20` |

There is **no `DialIn` column on the `Drivers` DB table** — DialIn lives on
`Cars` (`DefaultDialIn`) and on the session entry (per-event copy).

### Set into `DriverEntries`

`SessionSetupForm.Events.cs:226-238` — populated for `Dial-In` class (from
`er.car.DefaultDialIn`) or `Bracket Class` (from session-level
`fixedDial`).

### Yes, displayed during a race

**On the winner buttons** (`btnWinner1` / `btnWinner2`):
`Form1.Display.cs:151-155` (in `OnNextMatchReady`):
```csharp
double? leftDialIn  = leftDriverId  > 0 ? _controller.GetDriverDialIn(leftDriverId)  : null;
double? rightDialIn = rightDriverId > 0 ? _controller.GetDriverDialIn(rightDriverId) : null;
btnWinner1.Text = currentLeft  + FormatDialIn(leftDialIn);
btnWinner2.Text = currentRight + FormatDialIn(rightDialIn);
```
`FormatDialIn` (`Form1.Display.cs:264-268`) appends `" [4.123]"` to the
button text.

**On the live scoreboard** — `LiveMatchDto.LeftDriverDialIn` /
`RightDriverDialIn` (`Integration/LiveRaceUpdateDto.cs:28-29`), populated
in `RaceController.LiveUpdate.cs:89-90`.

It is **not** displayed in `lvDrivers`, `lvPairings`, or `lvWinners` (no
column for it).

---

## 4. Existing UI for editing time fields after `GenerateBracket()`?

### `QualifyingTime` — yes, but only via the Set Time button

- `btnSetQualTime` has no `Enabled` gate (point 1 above), so it remains
  clickable post-bracket.
- Editing affects the same `Driver` reference held by the engine (drivers
  are passed by reference into `_controller.GenerateBracket(...)`).
- However, **engines that use `QualTime` for seeding only read it at
  bracket-generation time** (see point 5). A post-bracket QualTime edit
  will NOT reseed the existing bracket. It will only show up if a new
  bracket is generated, or via the save path.
- There is **no edit-from-bracket** UI for QualifyingTime — the user has
  to go back to the driver list, select the driver, and click Set Time.

### `DialIn` — yes, two paths

1. **Right-click on a winner button during a race** —
   `Form1.cs:46-47` registers `MouseUp` → `ShowEditDialInForButton(isLeft)`
   (`Form1.Events.cs:469-478`). That opens a small dialog
   (`ShowEditDialInDialog`, lines 480-561) and on OK calls
   `_controller.UpdateDriverDialIn(driverId, newDialIn)`. This path **does
   write to `_session.DriverEntries.DialIn`** (controller-mediated, with
   lock — `RaceController.DialIn.cs:30-41`) and queues a live update.
2. **Driver self-update from the live scoreboard** — POST `/api/dialin`
   with optional PIN (ENH-02 / #165). The desktop polls back via
   `LiveApiClient.GetDialInUpdatesAsync` and `PollAndApplyAsync` applies
   the changes to `_session.DriverEntries`.

Dial-in edits are **locked** when `Generate Next Round` is clicked —
`Form1.Events.cs:258` calls `_controller.LockDialIn()`. The lock is honoured
by both the right-click edit path and the polling path.

There is **no equivalent post-bracket QualifyingTime edit path** — Set
Time always edits the local driver-list copy, never the controller.

---

## 5. How is `QualifyingTime` used in bracket generation?

### Seeding only — Pro Ladder / `MatchEngine`

`RaceEngines/MatchEngine.cs:21-22,36`:
```csharp
.OrderBy(d => d.QualTime.HasValue ? 0 : 1)
.ThenBy(d => d.QualTime ?? double.MaxValue)
...
int timed = allDrivers.Count(d => d.QualTime.HasValue);
```
Lower QualTime → better seed; drivers without a time fall to the back.

### Other engines do not use QualifyingTime

- `RoundRobinEngine` — does not reference `QualTime`.
- `LosersBracketBuilder` / `RandomBracket` — do not reference `QualTime`
  (Random is purely random with lane-fairness; LB uses RR pairing
  history).

### Display ordering

- `Form1.UI.cs:22-25` orders `lvDrivers` by QualTime ascending. Display
  only.

### Summary

QualifyingTime is **read once at bracket generation** (Pro Ladder) for
seeding, and **continuously for display** in the driver list. It does not
affect Round Robin or Random Bracket modes at all.

---

## 6. How is `DialIn` used?

### Not passed to any race engine

A grep of `RaceEngines/`, `RoundRobinMode/`, `RandomMode/`, and the
brackets shows **no `DialIn` references** in any engine. DialIn is purely
metadata — it does not affect pairings, seeding, BYE assignment, or
results in this codebase. (Real-world NHRA dial-in racing applies dial-ins
at the track via handicap start timing, which is outside the app's
scope.)

### Stored, displayed, synced

| Use | Where |
|---|---|
| Stored on session entry | `RaceSession.DriverEntries[i].DialIn` |
| Displayed on winner buttons during race | `Form1.Display.cs:154-155` (via `FormatDialIn`) |
| Displayed on live scoreboard | `LiveMatchDto.LeftDriverDialIn` / `RightDriverDialIn` |
| Displayed on live scoreboard match cards | `RCDragLiveServer/Controllers/PublicLiveController.cs` (DialInBadge) |
| Updated from RD (right-click) | `Form1.Events.cs:469-561` → `RaceController.UpdateDriverDialIn` |
| Updated from driver (live form) | `RCDragLiveServer/Controllers/DriverDialInController.cs` → polled back |
| Locked after `Next Round` | `RaceController.DialIn.cs:43-46` (`LockDialIn`) |

---

## Cross-cutting observations (not code changes — just things noticed)

1. **`btnSetQualTime` updates Form1's local `drivers` list, not the
   controller.** The change only flows into `_session.DriverEntries`
   through the save path. Since engines hold the same `Driver` references
   by reference (not by copy), live edits do mutate engine driver objects
   — but no engine re-seeds on a QualTime change post-generation.

2. **Asymmetry with DialIn.** DialIn has a controller-mediated update path
   (`UpdateDriverDialIn`), a lock, and live broadcast. QualifyingTime has
   none of these.

3. **No way to edit QualifyingTime "in the moment" during a race** — only
   via the setup-panel button against the `lvDrivers` selection. No
   right-click on a winner button, no in-bracket editor. This is
   asymmetric with DialIn's right-click affordance.

4. **DialIn is class-conditional at session creation** but is shown on
   every class's winner buttons regardless of class type. For Heads Up
   (which uses QualTime, not DialIn), the button text simply has no
   bracketed dial-in suffix because `DialIn` is `null` and `FormatDialIn`
   returns empty string.
