# MultiClassConfigDialog Driver Picker — Research

> **Read-only research.** No source code was changed. This document is input
> for filing two UX issues against the Class Configuration dialog:
> (1) replace the Car/Class/State dropdown filter row with a single name
> search box, and (2) make the driver ListView column headers sortable.
>
> **Files inspected:**
> - `src/RCDragManagerProd/UI/Forms/Session/MultiClassConfigDialog.Designer.cs`
> - `src/RCDragManagerProd/UI/Forms/Session/MultiClassConfigDialog.cs`
> - `src/RCDragManagerProd/UI/Forms/Session/MultiClassSetupForm.cs` (parent)
> - `src/RCDragManagerProd/Domain/RaceSession.cs` (`RaceSessionDriverEntry`)
>
> `MultiClassConfigDialog` has exactly two source files (one `.cs`, one
> `.Designer.cs`) — no other partial files.

---

## Current Layout

The dialog uses **absolute pixel positioning** throughout. The only
`TableLayoutPanel` is `pnlCardRow` (the three race-type cards); there is no
`FlowLayoutPanel`. Everything else — including the driver picker section — is
placed by explicit `Location`/`Size`.

### Static controls (declared in `.Designer.cs`)

| Control | Type | Location | Size | Notes |
|---------|------|----------|------|-------|
| `btnAddNewDriver` | Button | (20, 310) | 140 × 30 | **Must stay exactly here.** Text "Add New Driver". |
| `lblDrivers` | Label | (20, 348) | AutoSize | "Drivers (check to include):" |
| `lvDrivers` | ListView | (20, 368) | 860 × 280 | `View.Details`, `FullRowSelect=true`, `CheckBoxes=true`. No Anchor (absolute). |
| `lblDialInOverride` | Label | (20, 658) | AutoSize | "Override Dial-In:" |
| `txtDialInOverride` | TextBox | (145, 655) | 110 × — | `Enabled=false` until a driver row is selected. |
| `btnOk` | Button | (690, 720) | 85 × 30 | |
| `btnCancel` | Button | (790, 720) | 85 × 30 | |

Form: `ClientSize` 900 × 770, `FixedDialog`, `CenterParent`, `AutoScroll=false`.

### `lvDrivers` columns

Declared in `.Designer.cs` (5 columns):

| Idx | Header | Width | Type |
|----:|--------|------:|------|
| 0 | Driver | 200 | text |
| 1 | Car | 200 | text |
| 2 | Class Type | 150 | text |
| 3 | Dial-In | 100 | **numeric** (`F3`, e.g. `9.800`) |
| 4 | Override Dial-In | 115 | **numeric** (`F3`) |

A **6th column is added at runtime**, not in the Designer:

| Idx | Header | Width | Added where |
|----:|--------|------:|-------------|
| 5 | State | 70 | `CreateFilterControls()` in `.cs`, lines 131–132: `if (lvDrivers.Columns.Count == 5) lvDrivers.Columns.Add("State", 70, HorizontalAlignment.Left);` |

> ⚠ **This is the single most important gotcha for Change 1.** The State
> column is created as a side effect of the filter-row setup. Every row built
> in `PopulateDriverList` adds 6 cells (Name + 5 subitems incl. State), so the
> column **must** continue to exist. If `CreateFilterControls()` is removed
> wholesale, the State column add must be relocated (to the constructor or the
> Designer) or the State data will have no column to display.

### Filter row (built entirely in code — `CreateFilterControls()`)

The three filter combos and their labels are **not** in the Designer. They are
created in `CreateFilterControls()` and added directly to the form's `Controls`
(each with `BringToFront()`), positioned **relative to `btnAddNewDriver`**:

- `y = btnAddNewDriver.Top` (= 310); `x = btnAddNewDriver.Right + 8` (= 168).
- Constants: `comboW = 120`, `labelToBox = 4`, `groupGap = 12`.
- Label widths are measured at runtime via `TextRenderer.MeasureText(...)`.

| Control | Field | Approx Location | Size | Notes |
|---------|-------|-----------------|------|-------|
| "Car:" label | `lblCar` (local) | (168, 316) | AutoSize | |
| Car combo | `cmbFilterCar` | (~198, 312) | 120 × — | `DropDownStyle=DropDownList` |
| "Class:" label | `lblClass` (local) | (~330, 316) | AutoSize | |
| Class combo | `cmbFilterClass` | (~370, 312) | 120 × — | items: (All), Heads Up, Bracket, Dial In |
| "State:" label | `lblState` (local) | (~502, 316) | AutoSize | |
| State combo | `cmbFilterState` | (~545, 312) | 120 × — | states distinct from drivers |

The three combos are private fields on the dialog (`cmbFilterCar`,
`cmbFilterClass`, `cmbFilterState`); the labels are method-local. All three
combos wire `SelectedIndexChanged += FilterChanged`.

Net effect: the picker row reads
`[Add New Driver]  Car:[▼]  Class:[▼]  State:[▼]` at y≈310, with the driver
list (`lvDrivers`) directly below at y=368.

---

## Current Data Flow

### Where drivers come from
`FillFilterCombos()` calls `_allDrivers = _driverRepo.GetAllDrivers()` and
caches the full list in the `_allDrivers` field. **This method does double
duty:** it loads `_allDrivers` *and* populates the three combo boxes (car
names, fixed class list, distinct states). Any refactor that removes the
combos must preserve the `_allDrivers` load.

### Fields available per driver
Resolved per driver as `var car = driver.Cars?.FirstOrDefault();` — i.e. only
the **first** car is shown/used:

| Display column | Source |
|----------------|--------|
| Driver | `driver.Name` |
| Car | `car?.CarName` |
| Class Type | `car?.ClassType` |
| Dial-In | `car?.DefaultDialIn` (`F3`) |
| Override Dial-In | `_dialInOverrides[driver.Id]` (`F3`) if set, else blank |
| State | `driver.State` |

`item.Tag = driver.Id` on every row. `RaceSessionDriverEntry` (the OK result
type) has **no State field** — State is display/filter-only; it is not carried
into the built entries.

### How the dropdowns filter the list
`FilterChanged` (any combo change) → `PopulateDriverList(null)`.
`PopulateDriverList`:
1. Reads the three combo selections (defaulting to `"(All)"`).
2. `BeginUpdate()`, sets `_suppressRosterEvents = true`, clears `lvDrivers`.
3. Loops `_allDrivers`, `continue`-skipping rows that fail the state, class, or
   car filter (each compared `OrdinalIgnoreCase` against the first car).
4. Builds the `ListViewItem` (+5 subitems), sets `item.Checked =
   _checkedDriverIds.Contains(driver.Id)`.
5. `EndUpdate()`, `_suppressRosterEvents = false` in a `finally`.

So **filtering is purely a view operation** — it rebuilds the visible rows from
`_allDrivers`; it never mutates the selection.

### How "Override Dial-In" works
- `_dialInOverrides` is a `Dictionary<int, double?>` keyed by driver Id.
- `lvDrivers.SelectedIndexChanged` → `LvDrivers_SelectedIndexChanged`: if a row
  is selected, enables `txtDialInOverride` and shows the current override
  (else clears + disables it).
- `txtDialInOverride.Leave` → `TxtDialInOverride_Leave`: reads the selected
  row, parses the text; empty removes the override and clears
  `item.SubItems[4].Text`; a valid `double` stores it and writes
  `item.SubItems[4].Text` (the **Override Dial-In** column, index 4).
- On OK, the override is applied only for **Dial-In** class type (Heads Up →
  null; Bracket → `FixedDialIn`; Dial-In → override else `car.DefaultDialIn`).

---

## Selection Model

- **Source of truth is `_checkedDriverIds` (`HashSet<int>`), not the
  ListView.** `LvDrivers_ItemChecked` adds/removes the driver Id as the user
  toggles checkboxes (guarded by `_suppressRosterEvents` so repopulation
  doesn't fire spurious toggles).
- Because selection lives in the HashSet, **a driver checked and then filtered
  out of view remains selected.** This is already correct today and is the key
  reason the search-box change is low-risk.
- On OK (`BtnOk_Click`): iterates `_checkedDriverIds` (not the visible items),
  looks each driver up in `_allDrivers`, and builds `BuiltDriverEntries`
  (`List<RaceSessionDriverEntry>`). The parent (`MultiClassSetupForm`) reads
  `dlg.BuiltDriverEntries` plus the scalar properties (`ClassName`, `RaceType`,
  `ClassType`, `FixedDialIn`, `Variant`, `RoundsToRun`) into its `ClassConfig`.
- Edit flow: `MultiClassSetupForm` passes a `MultiClassConfigDialogValues` in;
  `PopulateDriverList(existing)` seeds `_checkedDriverIds` (and, for Dial-In
  classes, `_dialInOverrides`) from the existing entries.

---

## Sort Behaviour (today)

- `lvDrivers.Sorting` — **not set** (default `SortOrder.None`).
- `ColumnClick` — **not wired**.
- `ListViewItemSorter` / any `IComparer` — **does not exist** anywhere in the
  dialog. Rows appear in `_allDrivers` order (repository order).

---

## Proposed Change 1 — Name Search Box

**Goal:** replace the Car/Class/State filter combos with one TextBox that
filters the list by driver **Name** (case-insensitive "contains") as the user
types. `btnAddNewDriver` stays exactly where it is. Empty box = show all.
Checked state is preserved.

### Approach
1. In `CreateFilterControls()` (rename to e.g. `CreateSearchControl()`):
   - Stop creating `cmbFilterCar`/`cmbFilterClass`/`cmbFilterState` and their
     labels.
   - Create one `TextBox txtSearch` at the same slot
     (`Left = btnAddNewDriver.Right + 8`, `Top` aligned with the button),
     widened to fill the row out to the list's right edge (~`lvDrivers.Right`),
     and add it to `Controls` with `BringToFront()`.
   - **Relocate the State-column add** (`lvDrivers.Columns.Add("State", …)`) so
     it still runs (State is being kept — it's a sort target). Move it here or
     to the constructor / Designer.
2. Remove the `cmbFilterCar/Class/State` private fields.
3. Split `FillFilterCombos()`: keep the `_allDrivers = _driverRepo
   .GetAllDrivers()` load (rename to `LoadDrivers()`); delete the combo-fill
   code. Update the two callers (`ctor` and `BtnAddNewDriver_Click`).
4. Replace `FilterChanged` with a `txtSearch.TextChanged` handler that calls
   `PopulateDriverList(null)`.
5. In `PopulateDriverList`, replace the three `OrdinalIgnoreCase` combo filters
   with one name filter:
   `if (!string.IsNullOrWhiteSpace(search) && driver.Name.IndexOf(search,
   StringComparison.OrdinalIgnoreCase) < 0) continue;`

### Cue / placeholder text
WinForms `TextBox` has no managed placeholder on .NET 4.8. Options:
- **Native cue banner** via P/Invoke (`SendMessage` `EM_SETCUEBANNER`
  `0x1501`) — shows "Search drivers…" greyed when empty/unfocused. Cleanest;
  ~5 lines of interop.
- A static `Label` "Search:" before the box (no interop). Simplest.
- Managed grey-text-on-focus hack (more code, more bugs). Not recommended.

### Files affected
- **`MultiClassConfigDialog.cs` only.** All filter controls live in code
  today, so no Designer change is strictly required. (Optional: declare
  `txtSearch` and the State column in the Designer for tidiness.)

### Difficulty
**Low.** Single file; mostly deletion plus one TextChanged handler and a
one-line filter. The only non-trivial bit is remembering to keep `_allDrivers`
loading and to keep the State column.

### Risks
- **State column regression (high-likelihood if missed):** it's added inside
  the method being gutted — relocate the add. (Called out above.)
- **`_allDrivers` load regression:** `FillFilterCombos()` is the only place the
  roster is loaded; don't delete that line with the combo code.
- **Selection preservation:** already safe — selection is the `HashSet`, not
  the view. No change needed, but the issue should assert it as an acceptance
  test (check a driver, type a search that hides them, press OK, confirm they
  appear in `BuiltDriverEntries`).
- **Override Dial-In flow:** untouched by this change; the search box only
  changes which rows are visible. Low risk.
- No height change → no impact on the separate sizing concern for this dialog
  (it is the CRITICAL form in the UI sizing audit; this change neither helps
  nor worsens that).

---

## Proposed Change 2 — Sortable Column Headers

**Goal:** clicking a header sorts ascending; clicking the same header again
flips to descending. Works for all six columns. Dial-In / Override Dial-In sort
**numerically**. Checked state preserved across sorts.

### Approach
1. Add a nested `IComparer` (e.g. `private sealed class DriverColumnSorter :
   IComparer`) holding `Column` (int) and `Order` (`SortOrder`).
   - `Compare(x, y)` reads `((ListViewItem)x).SubItems[Column].Text`.
   - For numeric columns (index 3 and 4) `double.TryParse` both sides; compare
     as doubles. For text columns use
     `string.Compare(..., StringComparison.OrdinalIgnoreCase)`.
   - Apply `Order` (negate result for descending).
2. Assign `lvDrivers.ListViewItemSorter = _sorter;` once (e.g. in the ctor).
3. Wire `lvDrivers.ColumnClick`: if the clicked column == current column, flip
   `Order`; else set column and default to `Ascending`. Then `lvDrivers
   .Sort();`.
4. **Empty numeric cells:** decide a rule (recommend: blanks sort last in both
   directions, or treat blank as the lowest value). Needs a one-line decision —
   see Open Questions.

### Visual sort indicator
WinForms Details ListView has **no built-in sort glyph**. Two options:
- **Header text arrow (recommended, Low):** append `" ▲"` / `" ▼"` to the
  active column's header `Text` and strip it from the others on each
  `ColumnClick`. Reliable, no interop.
- **Native header glyph (Medium):** P/Invoke `HDM_SETITEM` with
  `HDF_SORTUP`/`HDF_SORTDOWN` on the header control. More authentic, more code.
  Per the brief, treat as **low priority** — flag and skip if it balloons.

### Files affected
- **`MultiClassConfigDialog.cs` only** (nested comparer class + ColumnClick
  handler + one assignment). No Designer change needed.

### Difficulty
**Low** for sort + numeric handling + text-arrow indicator.
**Medium** only if the native header glyph is required.

### Risks
- **Auto-sort during repopulation:** once `ListViewItemSorter` is set, the
  ListView re-sorts on every `Items.Add`. `PopulateDriverList` already wraps
  adds in `BeginUpdate()/EndUpdate()`, so this batches; perf is fine for
  realistic rosters. Verify the `_suppressRosterEvents`/`ItemChecked`
  interplay still holds (sorting reorders items, it doesn't recreate them, so
  `Tag`/`Checked` survive — selection model is safe).
- **Override Dial-In editing across sort:** `TxtDialInOverride_Leave` edits
  `lvDrivers.SelectedItems[0].SubItems[4]`. Sorting reorders item objects but
  keeps references and selection, so editing still targets the right driver.
  If the user is sorted *by* Override Dial-In, the row won't re-position until
  the next ColumnClick — acceptable. Low risk, but worth an acceptance test:
  sort by a column, edit an override, confirm it lands on the correct driver.
- **Blank-value ordering** for numeric columns is the only genuine design
  decision (Open Questions).
- Selection preservation across sort: safe (HashSet source of truth).

---

## Recommended Filing Order

File as **two separate issues** — they are independent, touch different code
paths, and have different acceptance tests:

1. **Issue A — Name search box** (file first). It deletes the filter-combo code
   and simplifies `PopulateDriverList`, which leaves a cleaner surface for the
   sort work. Difficulty **Low**.
2. **Issue B — Sortable columns** (file second). Purely additive on top of A.
   Difficulty **Low** (Medium if native glyph is mandated).

Both are confined to `MultiClassConfigDialog.cs`. They *could* ship in one PR,
but two issues keep the acceptance criteria crisp (search-preserves-selection
vs. numeric-sort-correctness). If Stew prefers one unit of work, sequence them
A → B in the same branch.

---

## Open Questions for Stew

1. **Cue text style:** native cue banner ("Search drivers…", needs ~5 lines of
   P/Invoke) vs. a plain "Search:" label vs. nothing? *(Recommend native cue
   banner; label is the zero-risk fallback.)*
2. **Keep the State column?** It was added as a side effect of the filter row.
   The brief lists State as a sort target, so the assumption is **keep it** —
   please confirm. If State is dropped, both the column and the per-row State
   cell come out together.
3. **Search scope:** Name only (per brief). Confirm we are *not* also matching
   Car or Class text.
4. **Blank numeric ordering:** where do empty Dial-In / Override Dial-In cells
   go when sorting those columns — always last, or treated as lowest value?
5. **Default sort on open:** keep current repository order, or default to
   Driver ascending once sorting exists?
6. **Sort indicator:** is the header-text arrow (▲/▼) acceptable, or is the
   native Windows header glyph required? *(Brief says native is low priority —
   recommend the text arrow.)*
7. **Post-add behaviour:** after "Add New Driver", should the search box clear
   (so the new driver is visible) or retain the current term? *(Today the list
   just repopulates; with search, clearing avoids "I added a driver and can't
   see them".)*
