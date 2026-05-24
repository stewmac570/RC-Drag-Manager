# UI Sizing Audit — Target 1920x1080 (14" Laptop)

> **Read-only audit.** No source code was changed. This document is source
> material for filing UI-fix issues later. Every form under
> `src/RCDragManagerProd/UI/Forms/` was inspected from its `.Designer.cs`
> (or, for hand-coded forms, the `.cs` constructor).
>
> **Target:** 1920×1080 on a modern 14" laptop. Usable client area after the
> Windows taskbar (~40px) and a form title bar (~30px) is roughly
> **1920 × 1010**.
>
> **Critical environmental fact:** Windows sets **150% display scaling by
> default** on a 14" 1920×1080 panel, and the app is **not DPI-aware** (see
> Cross-Cutting Observations §1). That means every form is bitmap-stretched
> 1.5× before it reaches the screen. A window whose total height exceeds
> ~673px at 100% will have its bottom edge pushed off-screen at 150%. This is
> the single fact that explains the reported "OK/Close off-screen" symptom and
> it colours every severity rating below.

---

## Summary

| Severity | Count |
|----------|-------|
| CRITICAL | 1 |
| HIGH     | 0 |
| MEDIUM   | 2 |
| LOW      | 5 |
| OK       | 11 |
| **Total**| **19** |

**Reference numbers used throughout** (DPI-unaware bitmap scaling; window
height ≈ ClientSize.Height + ~39px for title bar + fixed border):

| Form | Client H | Window H @100% | @125% | @150% | Fits 1920×1010? |
|------|---------:|---------------:|------:|------:|-----------------|
| MultiClassConfigDialog | 770 | ~809 | ~1011 | ~1214 | 100% only — clips ≥125% |
| MultiClassRaceForm | 790 | ~829 | ~1036 | ~1244 | 100% only — clips ≥125% (but resizable) |
| MultiClassSetupForm | 600 | ~639 | ~799 | ~959 | up to 150% |
| LandingForm | 600 | ~639 | ~799 | ~959 | up to 150% |
| LoadSessionForm | 560 | ~599 | ~749 | ~899 | up to 150% (but resize breaks layout) |
| DriverManagerForm | 600 | ~639 | ~799 | ~959 | up to 150% |
| DriverStatsForm | 600 | ~639 | ~799 | ~959 | up to 150% |
| Form1 | 561 | ~600 | ~750 | ~900 | up to 150% (resizable) |
| QRCodeDialog | 580 | ~619 | ~774 | ~929 | up to 150% |
| ScrollableTextDialog | 640 | ~679 | ~849 | ~1019 | ~150% borderline (resizable) |
| All small dialogs (≤300) | ≤300 | ≤339 | — | — | comfortably |

---

## Findings — Ordered by Severity

### [CRITICAL] MultiClassConfigDialog

**File:** `src/RCDragManagerProd/UI/Forms/Session/MultiClassConfigDialog.Designer.cs`
**Current Size:** 900 × 770 (ClientSize)
**Border:** FixedDialog
**StartPosition:** CenterParent
**MinimumSize:** not set
**AutoScroll:** false
**AutoScaleMode:** not set (defaults to None for a top-level form)

**Problem:**
This is the dialog reached by **Landing → Create Race Session →
MultiClassSetupForm → Add Class** — i.e. the exact "when creating a new race"
path in the reported bug. It is the tallest dialog in the app at 770px client
height, it is `FixedDialog` (the user cannot resize it), and `AutoScroll` is
off (there is no scrollbar escape hatch). The OK / Cancel buttons sit at the
very bottom (Y=720). The moment the dialog renders taller than the screen —
which happens under normal 14"-laptop conditions — those two buttons are
unreachable and the user cannot complete or cancel class setup.

**Specific evidence:**
- `btnOk` Location (690, 720), Size 85×30 → bottom edge at client Y=750.
- `btnCancel` Location (790, 720), Size 85×30 → bottom edge at client Y=750.
- Both sit only 20px above the 770px client floor. There is no margin for any
  growth.
- **At exactly 1920×1080 / 100% scaling it does fit** (window ≈809px < 1010px
  usable) — stated honestly.
- **At 125% scaling** (a common 14" setting) the window is ~1011px tall —
  already past the ~1010px usable height; OK/Cancel land under the taskbar.
- **At 150% scaling** (Windows default for a 14" 1080p panel) the window is
  ~1214px tall — OK/Cancel are ~200px below the bottom of the screen.
- **At 1366×768 (older 14", 100%)** the 809px window exceeds the ~728px work
  area outright; the buttons are off-screen regardless of DPI.
- Because the form is FixedDialog with no AutoScroll, the user has **no way**
  to reach the buttons (can't resize, can't scroll, can't maximize).

**Existing issue overlap:** None open. (BUG-10 / #195 covered
*MultiClassRaceForm*, a different form, and is closed. ENH-09b/09c/09d
#221–#223 added controls to this dialog but did not address its overall
height.)

**Suggested fix:**
Two layers. (1) Immediate: bring the dialog within a 14"-safe height — reduce
the driver `ListView` height and/or compress the stacked sections so total
client height is ≤ ~650px, and keep OK/Cancel anchored to the bottom. (2)
Robust: change the border to `Sizable`, set a sensible `MinimumSize`, turn
`AutoScroll = true`, and `Anchor` the OK/Cancel buttons to Bottom-Right so
they remain visible (and scrollable to) at any size or DPI. The app-wide
DPI-awareness fix (Cross-Cutting §1) is the real root-cause remedy and would
prevent this class of bug everywhere.

**Difficulty:** Medium (proper resize/scroll + anchors). A Trivial stopgap
(shrink height, move buttons up) exists if a quick patch is wanted first.

---

### [MEDIUM] MultiClassRaceForm

**File:** `src/RCDragManagerProd/UI/Forms/Main/MultiClassRaceForm.Designer.cs`
**Current Size:** 1200 × 790 (ClientSize)
**Border:** Sizable
**StartPosition:** CenterScreen
**MinimumSize:** set (900 × 600)
**AutoScroll:** false
**AutoScaleMode:** Font (the only form that sets it)

**Problem:**
This is the hosted race console (tabbed, hosts `Form1` instances). It is the
best-behaved large form — `tabControl` is `Dock=Fill`, `lblStatus` is anchored
Top-Right, it has a `MinimumSize`, and it is resizable and maximizable. At
1920×1080 / 100% it is comfortable (window ≈829px < 1010px). The MEDIUM rating
is for two reasons: (a) the **default launch size** (790px client) is taller
than a 14" panel can show at 125%/150% scaling, so on first open the bottom of
the tab content can be clipped until the user maximizes; and (b) a
**documentation discrepancy** that needs reconciling (see below).

**Specific evidence:**
- `ClientSize` 1200×790 → window ~829px → ~1244px at 150% (clipped on first
  open). Mitigated because the form is resizable/maximizable and
  `MinimumSize` 900×600 → ~900px at 150%, which fits.
- `tabControl` `Dock=Fill`, `lblStatus` `Anchor=Top,Right` — content reflows
  correctly when resized. No clipped columns observed at the designer level
  (BUG-10 / #195, which reported clipped ListView columns here, is closed).

**⚠ Handover discrepancy (flag only, not proposing a change):**
SESSION-HANDOVER.md states *"MultiClassRaceForm must remain FixedSingle with
MaximizeBox=false."* The actual designer is `FormBorderStyle.Sizable` with
`MaximizeBox = true` (lines 53–54). This looks like the result of the BUG-10
fix that made the form resizable. The current resizable state is *better* for
14" laptops, so per the audit constraints I am only flagging the mismatch —
the handover note appears stale and should be reconciled before anyone
"restores" FixedSingle on the basis of that note.

**Existing issue overlap:** BUG-10 / #195 (closed) addressed the column/stretch
behaviour. No open issue.

**Suggested fix:**
Lower the default `ClientSize` height to ~700–720 so it fits a 14" panel
before maximizing (content already docks, so nothing else needs to move).
Separately, update SESSION-HANDOVER.md to record that the form is intentionally
Sizable/Maximizable now.

**Difficulty:** Trivial (one size change) + a doc edit.

---

### [MEDIUM] LoadSessionForm

**File:** `src/RCDragManagerProd/UI/Forms/Session/LoadSessionForm.Designer.cs`
**Current Size:** 900 × 560 (ClientSize)
**Border:** not set → defaults to **Sizable**
**StartPosition:** CenterScreen
**MinimumSize:** not set
**AutoScroll:** false

**Problem:**
The form is resizable (default Sizable border) but its controls use **absolute
positioning with no Anchor and no Dock at the form level**, so resizing breaks
the layout: the `tabControl` stays a fixed 800×460 and the Load/Delete/Cancel
buttons stay pinned to absolute coordinates. Shrinking the window clips the
buttons; growing it leaves dead space and a too-small list. This is the classic
"Sizable but controls don't follow" defect. It fits fine at the target size on
first open, but any resize degrades it, and there is no `MinimumSize` to stop
the user shrinking it until the buttons disappear.

**Specific evidence:**
- Form border never set → Sizable; no `MinimumSize`.
- `tabControl` Location (50,20) Size 800×460 — absolute, not Dock/Anchor.
- `btnDelete` (50, 500) 90×40; `btnLoad` (644, 500) 90×40; `btnCancel`
  (762, 500) 90×40 — all absolute, **no Anchor**. Bottom edge at client Y=540
  (inside 560), so visible on first open, but they do not move on resize.
- The inner ListViews *are* `Dock=Fill` within their tab pages (good), so only
  the form-level layout is the problem.
- At 1366×768 the 599px window fits; no DPI clipping until ≥150%.

**Existing issue overlap:** None.

**Suggested fix:**
Either make it behave like a fixed dialog (set `FormBorderStyle =
FixedSingle`), or make it properly resizable: `Anchor` the buttons to
Bottom-Right / Bottom-Left, `Anchor` the `tabControl` Top-Left-Right-Bottom (or
host it in a Dock=Fill panel above a Dock=Bottom button strip), and set a
`MinimumSize` (~700×450).

**Difficulty:** Low.

---

### [LOW] MultiClassSetupForm

**File:** `src/RCDragManagerProd/UI/Forms/Session/MultiClassSetupForm.Designer.cs`
**Current Size:** 900 × 600 (ClientSize)
**Border:** FixedSingle
**StartPosition:** CenterScreen
**MinimumSize:** not set
**AutoScroll:** false

**Problem:**
The first screen of the "Create Race Session" flow. Fits the target
comfortably (window ~639px; fits even at 150% → ~959px). FixedSingle so the
user can't break it by resizing. The only concerns are robustness: absolute
positioning with no `Anchor`, no `AutoScaleMode`, so at ≥175% scaling or with
larger system fonts the bottom Start Race / Cancel buttons could clip.

**Specific evidence:**
- `btnStartRace` (570, 540) 140×40 → bottom Y=580 (inside 600). Visible.
- `btnCancel` (720, 540) 140×40 → bottom Y=580 (inside 600). Visible.
- No Anchor on any control; class-management buttons (btnAddClass/Edit/Remove)
  pinned at absolute X=762.
- Fits at 1366×768 (639px window < 728px work area).

**Existing issue overlap:** None.
**Suggested fix:** Add `AutoScaleMode = Dpi` (or `Font`) and `Anchor` the
bottom buttons to Bottom-Right; optionally a small `MinimumSize`. Mostly a
hardening pass — no layout restructure needed.
**Difficulty:** Low.

---

### [LOW] DriverManagerForm

**File:** `src/RCDragManagerProd/UI/Forms/Drivers/DriverManagerForm.Designer.cs`
**Current Size:** 900 × 600 (ClientSize)
**Border:** FixedSingle
**StartPosition:** CenterScreen
**MinimumSize:** not set
**AutoScroll:** false

**Problem:**
Fits the target (window ~639px; fits to 150%). The content panel and all
buttons use absolute positioning with no `Anchor`; `pnlContent` is a fixed
494×560 and the right-hand button column is pinned at absolute X=722. It works
fine at 100–150% but won't reflow and would clip the right column at ≥175%
scaling or on a narrower window. Internally the panel does use Dock (Top/Top/
Fill) for its three List-area controls, which is good.

**Specific evidence:**
- Right column buttons at X=722, widths 150 → right edge X=872 (inside 900).
- `pnlContent` Location (220,20) Size 494×560 → right edge X=714, bottom Y=580.
- No `Anchor` on buttons or panel; FixedSingle so size is locked at 900×600.

**Existing issue overlap:** None.
**Suggested fix:** Add `AutoScaleMode = Dpi`. Optionally anchor the right-hand
button column to Top-Right and let `pnlContent` `Anchor`/`Dock` so the form
could later be made resizable. Low urgency.
**Difficulty:** Low.

---

### [LOW] DriverStatsForm

**File:** `src/RCDragManagerProd/UI/Forms/Drivers/DriverStatsForm.Designer.cs`
**Current Size:** 900 × 600 (ClientSize)
**Border:** FixedDialog
**StartPosition:** CenterScreen
**MinimumSize:** not set
**AutoScroll:** false

**Problem:**
Simple read-only stats view. Fits the target and to 150% (window ~639px).
Absolute positioning, no `AutoScaleMode`. The Close button is comfortably
inside the client area. Only a hardening nit.

**Specific evidence:**
- `btnClose` (762, 510) 90×40 → bottom Y=550 (inside 600). Visible.
- `lvMatches` (50, 90) 800×400 — absolute, no Anchor (won't grow, but form is
  fixed so it never needs to).
- Fits at 1366×768.

**Existing issue overlap:** None.
**Suggested fix:** Add `AutoScaleMode = Dpi`; nothing else required.
**Difficulty:** Trivial.

---

### [LOW] QRCodeDialog

**File:** `src/RCDragManagerProd/UI/Forms/Main/QRCodeDialog.cs` *(hand-coded,
no Designer file)*
**Current Size:** 500 × 580 (ClientSize)
**Border:** FixedDialog
**StartPosition:** CenterParent
**MinimumSize:** not set
**AutoScroll:** false
**AutoScaleMode:** explicitly None

**Problem:**
Fits the target and to 150% (window ~619px → ~929px). Explicitly sets
`AutoScaleMode = None`, so the QR image and Close button are bitmap-scaled with
the rest of the form (acceptable here — it's a single image + one button).
Would only clip at ≥175%. Polish only.

**Specific evidence:**
- `_btnClose` (190, 510) 120×40 → bottom Y=550 (inside 580). Visible.
- `_pictureBoxQr` (50, 80) 400×400; `_lblInstruction` (10,12) 480×58.
- Fits at 1366×768.

**Existing issue overlap:** None (this form was added by ENH-05 / #213, closed).
**Suggested fix:** Optional — reconsider `AutoScaleMode = None` if the app
moves to DPI-awareness, so the QR stays crisp. No action needed at the target.
**Difficulty:** Trivial.

---

### [LOW] EditWinnerDialog

**File:** `src/RCDragManagerProd/UI/Forms/Results/EditWinnerDialog.Designer.cs`
**Current Size:** 300 × 110 (ClientSize)
**Border:** not set → defaults to **Sizable**
**StartPosition:** not set → defaults to **WindowsDefaultLocation**
**MinimumSize:** not set
**AutoScroll:** false

**Problem:**
Tiny dialog (combo + OK/Cancel) that fits everywhere. Two minor defects: it has
no `FormBorderStyle` (so it's user-resizable for no reason, and its controls
aren't anchored, so resizing strands them), and no `StartPosition`, so it opens
at a Windows default location rather than centered on its parent — inconsistent
with every other dialog.

**Specific evidence:**
- `btnOK` (40, 60) 80×30 → bottom Y=90 (inside 110). Visible.
- `btnCancel` (180, 60) 80×30 → bottom Y=90. Visible.
- No border style, no StartPosition; buttons unanchored.

**Existing issue overlap:** None.
**Suggested fix:** Set `FormBorderStyle = FixedDialog` and `StartPosition =
CenterParent`. One-line each.
**Difficulty:** Trivial.

---

## OK — No Action Needed

These forms fit the 1920×1080 target with margin, keep their primary buttons
well inside the client area, and survive 14"-laptop scaling (≤150%) without
clipping. Listed with key facts for completeness; none warrant an issue.

| Form | File | ClientSize | Border | StartPosition | Primary buttons (Y / bottom) | Notes |
|------|------|-----------:|--------|---------------|------------------------------|-------|
| LandingForm | `Session/LandingPageForm.Designer.cs` | 900×600 | FixedSingle | CenterScreen | menu buttons 100–380 | Static main menu; logo StretchImage; no AutoScaleMode (cross-cutting). |
| Form1 | `Main/Form1.Designer.cs` | 884×561 | (default) | CenterScreen | docked button rails | **Reference layout** — panels + TableLayoutPanels + anchored ListViews; MinimumSize 900×600; hosted in MultiClassRaceForm tabs. Nit: ClientSize 884×561 < MinimumSize 900×600; AutoScaleMode=None. |
| ScrollableTextDialog | `Common/ScrollableTextDialog.cs` | 820×640 | Sizable | CenterParent | Close anchored Top-Right; RichTextBox Dock=Fill | Resizable + maximizable; ~1019px at 150% but user can resize. Well-built. |
| SettingsForm | `Common/SettingsForm.cs` | ~460×250 (Width/Height) | FixedDialog | CenterParent | Save/Cancel Top=150 / ~173 | Sets outer Width/Height not ClientSize (minor code nit); fits easily. |
| AddCarDialog | `Cars/AddCarDialog.Designer.cs` | 450×300 | FixedDialog | CenterParent | OK/Cancel 180 / 220 | Small modal. |
| SelectCarDialog | `Cars/SelectCarDialog.Designer.cs` | 350×300 | FixedDialog | CenterParent | OK/Cancel 240 / 270 | Small modal (legacy). |
| AddDriverDialog | `Drivers/AddDriverDialog.Designer.cs` | 400×200 | FixedDialog | CenterParent | OK/Cancel 120 / 155 | Small modal. |
| AddDriverAndCarDialog | `Drivers/AddDriverAndCarDialog.Designer.cs` | 450×280 | FixedDialog | CenterParent | OK/Cancel 200 / 240 | Small modal. |
| AddEditQualTimeDialog | `Drivers/AddEditQualTimeDialog.Designer.cs` | 400×200 | FixedDialog | CenterParent | Save/Cancel 130 / 160 | Small modal. |
| EditDriverDialog | `Drivers/EditDriverDialog.Designer.cs` | 450×250 | FixedDialog | CenterParent | OK/Cancel 150 / 190 | Small modal. |
| BuybackDriverSelectionForm | `Results/BuybackDriverSelectionForm.Designer.cs` | 284×260 | FixedDialog | CenterParent | Confirm/NoBuyback 225 / 248 (anchored Bottom) | Buttons correctly anchored. Minor: `btnConfirm` is 75px wide for the caption "Confirm Buybacks" — text likely truncated (cosmetic, not sizing). |

---

## Cross-Cutting Observations

These recur across forms and are candidates for project-wide standards.

**1. The app is DPI-unaware — this is the root cause of the reported bug.**
There is no `app.manifest`, no `<dpiAware>`/`<dpiAwareness>` entry, no
`SetProcessDPIAware`/`SetProcessDpiAwarenessContext` call in `Program.cs`, and
no `System.Windows.Forms.ApplicationConfigurationSection` `DpiAwareness` in
`App.config`. On a 14" 1920×1080 laptop (Windows default **150% scaling**) the
OS bitmap-stretches the entire app 1.5×. Any window taller than ~673px at 100%
loses its bottom edge. This single gap is why `MultiClassConfigDialog`'s
OK/Cancel go off-screen. **Highest-leverage fix:** add a DPI-aware manifest (or
the App.config setting) *and* set a consistent `AutoScaleMode` across forms
(`Dpi` or `Font`) so WinForms re-lays-out instead of being stretched. This
would fix the symptom class everywhere, not just in one dialog.

**2. `AutoScaleMode` is set on only 3 of 19 forms, inconsistently.**
`MultiClassRaceForm` = `Font`; `Form1` and `QRCodeDialog` = `None` (explicit);
the other 16 don't set it at all (top-level default is effectively None). With
no app-level DPI awareness this is moot today, but it must be made consistent
as part of any DPI fix or the forms will scale differently from one another.

**3. Almost no form sets `MinimumSize`.** Only `Form1` (900×600) and
`MultiClassRaceForm` (900×600) do. The two resizable-by-accident forms
(`LoadSessionForm`, `EditWinnerDialog` — both default Sizable) have no
`MinimumSize`, so the user can shrink them until controls vanish.

**4. Absolute positioning with no `Anchor` is the dominant layout style.**
Only `Form1`, `MultiClassRaceForm`, `BuybackDriverSelectionForm`, and
`ScrollableTextDialog` use Dock/Anchor meaningfully. Everywhere else, controls
are pinned to fixed pixel coordinates. For FixedDialog/FixedSingle forms this
is acceptable; it becomes a bug the moment a form is (or becomes) resizable or
is bitmap-scaled past its design size.

**5. Tall fixed dialogs with bottom buttons and no `AutoScroll` have no escape
hatch.** `MultiClassConfigDialog` (770) is the acute case; if any future change
or DPI step pushes content below the visible area, the user is stuck. A default
of `AutoScroll = true` on data-entry dialogs would be cheap insurance.

**6. Two default-`Sizable` forms appear unintentional.** `LoadSessionForm` and
`EditWinnerDialog` never set `FormBorderStyle`, so they inherit Sizable. Given
neither anchors its controls, this is almost certainly an oversight rather than
a design choice.

**7. Documentation drift (not a sizing bug, but found during the audit):**
- `CODEBASE-MAP.md` still lists `SessionSetupForm` (`.cs` / `.UI.cs` /
  `.Events.cs` / `.Designer.cs`) and `LandingPageForm`'s "New Multi-Class
  Event" button. `SessionSetupForm` has been **deleted** (retired by ENH-11 /
  #219) — no such files exist anymore. The map also says `RaceController` is
  split across 9 files while `ARCHITECTURE.md` says 11.
- `SESSION-HANDOVER.md` says `MultiClassRaceForm` "must remain FixedSingle with
  MaximizeBox=false," but the form is actually Sizable / MaximizeBox=true (the
  BUG-10 fix). See the MultiClassRaceForm finding.
These are flagged for awareness; updating them is outside this read-only audit.

---

## Recommended Issue Filing Order

CRITICAL first; within each severity, lowest difficulty first.

1. **File issue: BUG-XX — MultiClassConfigDialog — OK/Cancel off-screen when
   adding a class on a 14" laptop (new-race path)** — CRITICAL. Reduce dialog
   height to a 14"-safe size *and* make it Sizable + AutoScroll with anchored
   OK/Cancel. (Difficulty: Medium; Trivial stopgap available.)
2. **File issue: ENH-XX — Make the app DPI-aware (manifest + consistent
   AutoScaleMode)** — root-cause fix for the whole symptom class; would also
   prevent #1 recurring. Highest leverage; affects all forms. (Difficulty:
   Medium — app-wide, needs a manifest/App.config change plus an AutoScaleMode
   pass and re-test of each form.)
3. **File issue: ENH-XX — MultiClassRaceForm — lower default launch height to
   fit 14" before maximizing; reconcile stale FixedSingle handover note** —
   MEDIUM. (Difficulty: Trivial + doc edit.)
4. **File issue: BUG-XX — LoadSessionForm — anchor controls / set FixedSingle +
   MinimumSize so resize doesn't strand the buttons** — MEDIUM. (Difficulty:
   Low.)
5. **File issue: ENH-XX — EditWinnerDialog — set FixedDialog border +
   CenterParent start position** — LOW. (Difficulty: Trivial.)
6. **File issue: ENH-XX — DriverStatsForm — add AutoScaleMode (DPI hardening)**
   — LOW. (Difficulty: Trivial.)
7. **File issue: ENH-XX — MultiClassSetupForm — AutoScaleMode + anchor bottom
   buttons** — LOW. (Difficulty: Low.)
8. **File issue: ENH-XX — DriverManagerForm — AutoScaleMode + anchor right-hand
   button column** — LOW. (Difficulty: Low.)
9. **File issue: CHORE-XX — Update CODEBASE-MAP.md / SESSION-HANDOVER.md drift
   (SessionSetupForm removed; MultiClassRaceForm is Sizable; controller file
   count)** — docs only. (Difficulty: Trivial.)

> Items 5–8 (LOW) are best folded into the DPI-awareness work (item 2): if the
> app gains a consistent `AutoScaleMode` and a manifest, most of the LOW
> hardening nits are resolved in that same pass and may not need separate
> issues.
