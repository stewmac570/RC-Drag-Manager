# MultiClassConfigDialog — Current State Research (for BUG-15 / #249)

> **Read-only research.** No source code changed. This documents the **current
> state of `main`** (after the overnight routine merged #240, #241, #242, #243)
> and what BUG-15 needs to do.
>
> **Files inspected** (current `main`):
> - `src/RCDragManagerProd/UI/Forms/Session/MultiClassConfigDialog.Designer.cs`
> - `src/RCDragManagerProd/UI/Forms/Session/MultiClassConfigDialog.cs`
> - `src/RCDragManagerProd/app.manifest` (added by #240)

---

## ⚠ The task premise needs correcting before anything else

The brief assumed BUG-14 "made the form taller without fixing the problem,"
that the filter combos are still present, and that the DPI work (#240) is still
open. **All three are now out of date.** Since the last research task, the
overnight routine merged a large batch:

| Commit | PR | Issue | What it did |
|--------|----|-------|-------------|
| `a9436c2` | #244 | #240 | **DPI awareness** — added `app.manifest` (System DPI) + `AutoScaleMode.Dpi` to every form |
| `18cdbc6` | #245 | #241 | **BUG-14 attempt** — `pnlContent` (AutoScroll) + docked `pnlButtonBar` + `OnLoad` screen-clamp |
| `91c6a7e` | #246 | #242 | **ENH-13** — replaced Car/Class/State combos with a name **search box** |
| `f6499fb` | #247 | #243 | **ENH-14** — **sortable** column headers (numeric-aware, ▲/▼ glyphs) |

Consequences for BUG-15:
1. **BUG-14 did NOT increase the form height.** `ClientSize` is still `900×770`
   — unchanged from before BUG-14 (verified by diff, below). The "form got
   taller" belief is incorrect.
2. **The filter combos are gone.** ENH-13 replaced them with `txtSearch`
   (created at runtime in `CreateSearchControl()`). Do not plan to remove combos
   — they no longer exist.
3. **Sorting already exists** (ENH-14). The State column is now created in
   `CreateSearchControl()` (relocated there by ENH-13).
4. **The app is already DPI-aware** (System level) — #240 is merged, not open.
5. **BUG-14 already added a fit-to-screen mechanism** (`OnLoad` clamp + docked
   button bar + AutoScroll). On paper this should keep OK/Cancel reachable — see
   "OK/Cancel Visibility Analysis," which is the crux of this report.

---

## Form-Level Properties (Current)

| Property | Value | Notes |
|----------|-------|-------|
| `ClientSize` | **900 × 770** | Unchanged by BUG-14. |
| `Size` | not set explicitly | Derived = ClientSize + FixedDialog chrome. |
| `MinimumSize` | **not set** | ← BUG-15 must set this. |
| `MaximumSize` | not set | |
| `FormBorderStyle` | **FixedDialog** | ← BUG-15 must change to `Sizable`. |
| `MaximizeBox` | **false** | ← BUG-15 → true. |
| `MinimizeBox` | **false** | ← BUG-15 → true. |
| `StartPosition` | CenterParent | Keep. |
| `AutoScaleMode` | **Dpi** | Set by #240. `AutoScaleDimensions = (96,96)`. Keep. |
| `AutoScroll` (form) | false | Scrolling is on `pnlContent` instead. |
| `AutoScrollMinSize` | not set | |

**DPI state (from #240):** `app.manifest` declares
`<dpiAware>true</dpiAware>` + `<dpiAwareness>system</dpiAwareness>` (System DPI
awareness — the manifest comment explicitly chose System over PerMonitorV2 as
the safe option). The csproj references the manifest. So on a 14" 1080p panel at
150% the form is **DPI-scaled** (770 logical → ~1155 physical), not
bitmap-stretched.

---

## Control Inventory

Two containers sit directly on the form; everything else lives inside them.

### Form root

| Control | Type | Dock | Size | Notes |
|---------|------|------|------|-------|
| `pnlContent` | Panel | **Fill** | fills above bar | `AutoScroll = true`, `Padding (0,0,0,10)`. Added first. |
| `pnlButtonBar` | Panel | **Bottom** | Height 56 | Added last so it claims the bottom edge. |

### Inside `pnlButtonBar`

| Control | Location | Size | Anchor | Class |
|---------|----------|------|--------|-------|
| `btnOk` | (690, 13) | 85 × 30 | **Top \| Right** | CORRECT for the bar |
| `btnCancel` | (790, 13) | 85 × 30 | **Top \| Right** | CORRECT for the bar |

### Inside `pnlContent` (absolute positions; anchor = Top\|Left unless noted)

| Control | Location | Size | Anchor | Class |
|---------|----------|------|--------|-------|
| `lblClassName` | (20, 20) | AutoSize | Top\|Left | CORRECT (fixed top-left) |
| `txtClassName` | (110, 17) | W 220 | Top\|Left | CORRECT |
| `pnlCardRow` (TableLayoutPanel) | (20, 55) | 860 × 70 | **Top\|Left\|Right** | CORRECT — 3 equal % columns |
| ↳ `pnlCardProLadder` / `pnlCardRandomDraw` / `pnlCardRoundRobin` | — | Dock=Fill | — | All three present (see note) |
| `pnlRrConfig` (Panel) | (20, 135) | 860 × 70 | **Top\|Left\|Right** | CORRECT |
| ↳ `lblRrRounds` (15,20), `nudRoundsToRun` (75,17 W60), `chkBuybackRace` (165,19), `lblBuybackHint` (165,45) | — | — | — | inside pnlRrConfig |
| `grpClassType` (GroupBox) | (20, 235) | 860 × 62 | Top\|Left | **WRONG** → should be Top\|Left\|Right |
| ↳ `rbHeadsUp` (20,28), `rbBracket` (130,28), `rbDialIn` (265,28), `lblFixedDialIn` (355,31), `txtFixedDialIn` (450,28 W100) | — | — | — | inside grpClassType |
| `btnAddNewDriver` | (20, 310) | 140 × 30 | Top\|Left | CORRECT — **keep exactly here** |
| `txtSearch` (runtime, `CreateSearchControl`) | (168, 314) | W ~712 | Top\|Left | **WRONG** → should be Top\|Left\|Right |
| `lblDrivers` | (20, 348) | AutoSize | Top\|Left | CORRECT |
| `lvDrivers` (ListView) | (20, 368) | 860 × 280 | Top\|Left | **WRONG** → should be Top\|Bottom\|Left\|Right |
| `lblDialInOverride` | (20, 658) | AutoSize | Top\|Left | **WRONG** → should be Bottom\|Left |
| `txtDialInOverride` | (145, 655) | W 110 | Top\|Left | **WRONG** → should be Bottom\|Left |

`lvDrivers` columns: Driver 200, Car 200, Class Type 150, Dial-In 100,
Override Dial-In 115, **State 70** (State added at runtime in
`CreateSearchControl`). Total ≈ **835px**.

**Race format cards — all three confirmed present.** `pnlCardProLadder`,
`pnlCardRandomDraw`, and `pnlCardRoundRobin` are all created and added to
`pnlCardRow` (columns 0/1/2). Stew's "Round Robin might be missing" was a
screenshot crop — it is present in the Designer.

---

## OK/Cancel Visibility Analysis

**At the default `ClientSize` 900×770 on a screen tall enough to show it:**
OK/Cancel are **inside** the client area. `pnlButtonBar` (Dock=Bottom, 56px)
sits at y≈714–770; the buttons at y≈727 (714+13) with height 30 end at ≈757 <
770. The content above (`pnlContent`) needs ≈687px (lowest control is
`txtDialInOverride`, bottom ≈677, +10 padding) which fits in the ≈714px above
the bar, so at default size there is no scrollbar and nothing clips.

**The failure case is only when the rendered form is taller than the screen.**
At 150% the form is ~1155px physical tall; usable height on a 14" 1080p panel is
~1010px. Without intervention the button bar (form bottom) would be below the
screen.

**But BUG-14 added intervention** — an `OnLoad` override (`.cs` lines 96–110):

```
Rectangle workingArea = Screen.FromControl(this).WorkingArea;
int newWidth  = Math.Min(Width,  workingArea.Width);
int newHeight = Math.Min(Height, workingArea.Height);
if (newWidth != Width || newHeight != Height) Size = new Size(newWidth, newHeight);
// …then re-clamps Location so the whole window is on-screen.
```

On load this shrinks the window to the working area (≈1010px tall), leaving
`pnlButtonBar` docked at the (now on-screen) bottom and `pnlContent` scrolling
its overflow.

**Conclusion — and the central open question:** *Statically, the merged
BUG-14 + #240 code looks like it should keep OK/Cancel reachable on a 14" laptop
at 150%* (DPI-aware form + OnLoad clamp + docked button bar + AutoScroll). Yet
Stew verified the buttons are still off-screen. The static code does **not**
explain that failure. The most likely explanations, in order:

1. **Stale / failed build.** #249 itself notes the **test project has
   pre-existing build errors**. If the verified app was produced by building the
   *solution* (which fails on the test project), the run may have used an **old
   binary predating #244/#245**. This must be ruled out first — rebuild
   `RCDragManagerProd` directly and re-test before assuming the code is wrong.
2. **Perception.** The clamp can blow the form up to full working-area height
   (~1010px) with a scrollbar — which can *look* broken even though the buttons
   are technically reachable.
3. **A genuine runtime bug** in the clamp/scroll/DPI interaction that is not
   visible from static reading (needs live debugging).

Either way, BUG-15's directive (Sizable + MinimumSize + clean anchors + smaller
default) is a **more deterministic** design than the OnLoad-clamp + AutoScroll
approach, and is worth doing regardless of which explanation holds.

---

## What BUG-14 (#241) Changed

Diff of `MultiClassConfigDialog.Designer.cs` between the pre-BUG-14 commit
(`a9436c2`, which is the #240 DPI merge) and the BUG-14 merge (`18cdbc6`):

- **Added** two container panels: `pnlContent` (Dock=Fill, **AutoScroll=true**,
  bottom padding 10) and `pnlButtonBar` (Dock=Bottom, Height=56).
- **Moved** OK/Cancel out of the form and into `pnlButtonBar`, repositioned from
  `(690,720)`/`(790,720)` to `(690,13)`/`(790,13)`, and **anchored them
  Top\|Right** within the bar.
- **Re-parented** all former top-level controls into `pnlContent`.
- In the `.cs`: **added the `OnLoad` screen-clamp** (lines 96–110).

What BUG-14 did **NOT** do:
- ❌ Did **not** change `ClientSize` (still 900×770).
- ❌ Did **not** add `MinimumSize`.
- ❌ Did **not** change `FormBorderStyle` (still FixedDialog).
- ❌ Did **not** set `MaximizeBox`/`MinimizeBox` (still false).
- ❌ Did **not** anchor `lvDrivers`, `grpClassType`, or the override row.

So BUG-14's gap is exactly the BUG-15 scope: it added scroll + a docked bar +
a load-time clamp, but left the form **fixed-size and non-resizable** with
**unanchored body controls**. **Keep** BUG-14's docked button bar — it is the
right structure. The decision is whether to also keep its AutoScroll +
OnLoad-clamp safety nets (see recommendations).

---

## Anchor Audit

| Control | Current | Class | Proposed for BUG-15 |
|---------|---------|-------|---------------------|
| `pnlContent` / `pnlButtonBar` | Dock Fill / Bottom | CORRECT | keep |
| `btnOk` / `btnCancel` | Top\|Right (in bar) | CORRECT | keep (effectively Bottom\|Right of form) |
| `lblClassName` / `txtClassName` | Top\|Left | CORRECT | keep |
| `pnlCardRow` | Top\|Left\|Right | CORRECT | keep |
| `pnlRrConfig` | Top\|Left\|Right | CORRECT | keep |
| `grpClassType` | Top\|Left | **WRONG** | **Top\|Left\|Right** |
| `btnAddNewDriver` | Top\|Left | CORRECT | keep (do not move) |
| `txtSearch` | Top\|Left | **WRONG** | **Top\|Left\|Right** (stretch to list edge) |
| `lblDrivers` | Top\|Left | CORRECT | keep |
| `lvDrivers` | Top\|Left | **WRONG** | **Top\|Bottom\|Left\|Right** (fill central area) |
| `lblDialInOverride` | Top\|Left | **WRONG** | **Bottom\|Left** |
| `txtDialInOverride` | Top\|Left | **WRONG** | **Bottom\|Left** |

> ⚠ **AutoScroll vs Anchor=Bottom conflict.** `lvDrivers` (Top\|Bottom) and the
> override row (Bottom) are inside `pnlContent`, which currently has
> `AutoScroll = true`. Anchoring controls to the bottom **inside an AutoScroll
> panel** is the classic WinForms fiddly combo (bottom anchors to the visible
> client edge, not the virtual scroll extent, which can strand or jiggle
> controls). Cleanest resolution: **set `pnlContent.AutoScroll = false`** and let
> a sensible `MinimumSize` guarantee everything fits; long driver lists scroll
> via the ListView's own row scrollbar. See recommendations.

---

## Recommended Implementation Targets

- **FormBorderStyle:** `Sizable`
- **MaximizeBox:** `true`
- **MinimizeBox:** `true`
- **StartPosition:** `CenterParent` (keep)
- **AutoScaleMode:** `Dpi` (keep — already set by #240; specify MinimumSize in
  logical 96-DPI units so it scales)
- **MinimumSize:** **≈ 900 × 650** (Stew's suggested starting point is good).
  Practical floor is ~820 × 620: the fixed header block runs to `lvDrivers.Top`
  (y≈368), the override row + 56px button bar add ~96, so below ~620 client
  height the driver list becomes unusably short. Width below ~820 forces a
  horizontal scrollbar on the 835px-wide column set.
- **Default ClientSize:** **≈ 900 × 660** (down from 900×770). Justification:
  660 × 1.5 = **990px** physical at 150%, comfortably under the ~1010px usable
  area; width 900 × 1.5 = 1350 < 1920. The content's natural height (~743 incl.
  bar) exceeds 660, so `lvDrivers` (anchored Top\|Bottom) absorbs the difference
  — at 660 the list is ~196px tall and scrolls its rows; at MinimumSize ~120px.
- **`pnlContent.AutoScroll`:** recommend **false** (rely on MinimumSize +
  anchors + the ListView's native row scroll). Alternative: leave `true` as a
  belt-and-suspenders safety net, but only if the Anchor=Bottom interplay is
  tested carefully.
- **`OnLoad` screen-clamp:** **may be kept** as a harmless safety net (it only
  fires once, on load), or removed now that Sizable + MinimumSize do the job
  deterministically. Recommend keeping it for one release, then removing.

---

## Race Format Card Row — Design Decision Needed

**Already resolved in the current code — no change needed.** `pnlCardRow` is a
`TableLayoutPanel` with three equal 33.33% columns, the cards `Dock=Fill` within
their columns, and the panel is anchored `Top|Left|Right`. So **the three cards
already stretch evenly across the width** as the form widens (Option A).

- **Option A — stretch evenly (current):** recommended. Keep it; it already
  behaves correctly under resize, and all three cards stay visible at
  MinimumSize.
- **Option B — fixed-width left-aligned:** not recommended; would be a
  regression from the current responsive behaviour for no benefit.

---

## Risks Flagged

1. **AutoScroll + Anchor=Bottom** (`pnlContent` is AutoScroll; `lvDrivers` /
   override need Bottom anchors). The cleanest fix is to turn `pnlContent`
   AutoScroll off. If kept on, test bottom-anchored controls at small sizes.
2. **`txtSearch` is created at runtime** with a fixed `Width = lvDrivers.Right -
   left` and no anchor. Anchoring it must be done at creation in
   `CreateSearchControl()` (in the `.cs`), **not** in the Designer — easy to miss.
3. **DPI units.** `MinimumSize` must be set in logical (96-DPI) units so
   `AutoScaleMode.Dpi` scales it; a physically-sized MinimumSize would be wrong
   at 150%.
4. **Horizontal scrollbar at MinimumSize.** Column total ≈835px; at a ~820–900
   wide form the list may show a horizontal scrollbar. Acceptable, or narrow
   columns — Stew's call.
5. **OnLoad clamp vs Sizable.** The existing clamp sets `Size` on load; with
   Sizable this is fine, but if kept it should not fight a user maximizing the
   window (it won't — it only runs once, before the user interacts).
6. **Interaction with merged ENH-13/ENH-14.** Anchoring changes must not disturb
   the search box wiring or the `ListViewItemSorter`/ColumnClick behaviour
   (both live in the `.cs`, not the Designer, so low risk).

---

## Open Questions for Stew

1. **Did your "still broken" test use a fresh build of `RCDragManagerProd`?**
   The merged code (BUG-14 clamp + #240 DPI) looks like it should keep OK/Cancel
   on-screen. Because the **solution build fails on the test project**, a stale
   binary is the most likely explanation. Please confirm the verified app was
   built from current `main` (project-only build) — it changes whether BUG-15 is
   "finish the job" or "the clamp genuinely doesn't work and needs live
   debugging."
2. **Keep or remove the `OnLoad` screen-clamp?** Recommend keep-for-now (safety
   net), remove later. Your call.
3. **Keep or remove `pnlContent.AutoScroll`?** Recommend remove (cleaner anchors,
   deterministic). OK to rely on MinimumSize + the ListView's own row scroll?
4. **Default size 900×660 and MinimumSize 900×650 acceptable?** This shrinks the
   visible list area vs today; the list scrolls its rows to compensate.
5. **Horizontal scrollbar on the driver list at minimum width** — acceptable, or
   should columns be narrowed to fit ~820px?
6. **Scope confirmation:** ENH-13 (search) and ENH-14 (sort) are already merged.
   BUG-15 is purely the border/anchors/size structural fix — no touching the
   search or sort code. Confirm.
