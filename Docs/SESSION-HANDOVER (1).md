# RC Drag Manager — Session Handover
*Generated: May 2026*

---

## What Was Accomplished This Session

### v1.2.0 Released
All outstanding issues from the previous session were resolved by the automated routine overnight. The full ENH-09a → ENH-09b → ENH-09c → ENH-09d → ENH-10 → ENH-11 chain completed. v1.2.0 was tagged and released with installer attached.

**Closed this session (routine + manual):**
- ✅ ENH-09a/b/c/d — MultiClassRaceForm now supports all race types per class
- ✅ ENH-08 — Quick Session button removed
- ✅ ENH-06 — Dial-in panel added to Form1
- ✅ ENH-05 — QR code generator in app
- ✅ BUG-12 — Single class events no longer skip landing page
- ✅ BUG-13 — stewmacrc.com always shows branding/landing page
- ✅ ENH-10 — Load Saved Event rerouted through MultiClassRaceForm
- ✅ ENH-11 — Standalone SessionSetupForm retired; orphaned .resx build error fixed
- ✅ Landing page button shift — buttons shifted up after Quick Session removal
- ✅ Version label updated to v1.2.0 on landing page

### UI/UX Work Completed This Session

#### MultiClassConfigDialog — Race Format Redesign (PR #233, merged)
- Replaced Race Type dropdown with 3 card-style selectors: Pro Ladder, Random Draw, Round Robin
- Cards span full width matching the driver list
- Round Robin card expands inline config panel: Rounds (N) spinner (default 3) + "Buyback race for 4th finals spot" checkbox
- Replaced Standard/QMDRA radio buttons entirely — buyback checkbox maps to Standard (checked) / QMDRA (unchecked) internally
- Controller fix: Standard RR now respects RoundsToRun from session instead of always auto-calculating min(3, n-1)

#### Form1 Layout Refactor (PR #234, merged)
- Introduced proper container panels — pnlHeader, pnlLeft, pnlBottom, pnlRail, tlpMain
- pnlLeft (Dock=Left): driver list and editing controls
- tlpMain (Dock=Fill): TableLayoutPanel 50/50 split — pnlCenter (pairings) and pnlRight (winners)
- pnlRail (Dock=Right): action buttons in TableLayoutPanel with Save & Close pushed to bottom
- pnlBottom (Dock=Bottom): Generate Bracket | race queue rows | Generate Next Round
- ListView columns fixed — no horizontal overflow
- 4px gap between pairings and winners panels
- Edit Match Result button aligned with top of lvWinners
- Driver List controls aligned with pairings/winners header row
- Race Type dropdown (cmbRaceType) removed — dead code since SessionSetupForm retired

---

## Current State

### Open Issues
| # | Title | Priority | Status |
|---|-------|----------|--------|
| #190 | REVIEW-11: LiveApiClient.SendAsync serialises all pushes through global SemaphoreSlim | Low | Open — low priority, not blocking anything |

**Board is essentially clear.**

### Known Remaining UI Rough Edges (not filed as issues yet)
- "Driver List:" label sits a couple of pixels above "Current Round Pairings:" label — minor alignment
- txtName and txtTime input boxes have no placeholder labels — not discoverable which is which
- Bottom-left corner under Generate Bracket is empty grey — looks bare
- Form1 column widths in driver list truncate names — lvDrivers is 224px wide, Name column is only 80px

---

## Automated Routine — Current State

**Location:** Claude Code Desktop → Routines → rc-drag-issue-fixer
**Schedule:** Hourly
**Both repos:**
- `C:\Users\Stewart McMillan\source\repos\RC-Drag-Manager`
- `C:\Users\Stewart McMillan\source\repos\RCDragLiveServer`

**How it works:**
1. Fetches all open issues ordered by priority (high bugs first, down to low enhancements)
2. Skips issues labelled `blocked`, `wip`, `on-hold`, `skip`
3. Checks `Depends On` sections — skips if dependencies not yet merged
4. Creates branch, implements, builds, tests
5. Pass → merges PR, closes issue. Fail → raises WIP PR, comments error, leaves open

**Labels:** `priority:high` + `bug` → first … `priority:low` + `enhancement` → last

**To block:** `gh issue edit {number} --repo stewmac570/RC-Drag-Manager --add-label "blocked"`
**To unblock:** `gh issue edit {number} --repo stewmac570/RC-Drag-Manager --remove-label "blocked"`

---

## Architecture — Current State

### Landing Page (clean)
1. Create Race Session → MultiClassSetupForm → MultiClassRaceForm (hosted Form1 tabs)
2. Load Saved Event → MultiClassRaceForm (hosted)
3. Driver Lists
4. Settings
5. Exit

SessionSetupForm and standalone Form1 path are fully retired.

### Form1 Layout — Panel Structure
```
Form1
├── pnlHeader (Dock=Top, H=50)       — event title
├── pnlBottom (Dock=Bottom, H=170)   — generate bracket | race queue | generate next round
├── pnlRail (Dock=Right, W=116)      — action buttons, save & close at bottom
├── pnlLeft (Dock=Left, W=224)       — driver list + editing controls
└── tlpMain (Dock=Fill, 50/50)
    ├── pnlCenter                    — current round pairings
    └── pnlRight                     — match winners
```

### MultiClassConfigDialog — Race Format Cards
- Pro Ladder | Random Draw | Round Robin (card selector)
- Round Robin expands: Rounds (N, default 3) + Buyback checkbox
- Buyback checked → Variant="Standard", RoundsToRun=N
- Buyback unchecked → Variant="QMDRA", RoundsToRun=N

---

## Key Learnings This Session

- **For pixel-level WinForms layout work: run CC interactively, not through me as middleman.** CC can see the file, make the change, build, and adjust in one loop. I can't see results between changes so I was guessing. Use me for design decisions and prompt structure only.
- **pnlRail/pnlLeft Dock=Right/Left already start below pnlHeader at runtime** — adding Padding(0, 50, 0, 0) double-offsets. For Dock panels, WinForms handles the header offset automatically.
- **Absolutely positioned controls inside a panel ignore Padding** — only docked children respect it.
- **CC stops on exact string mismatch** — prompts using `System.Windows.Forms.Padding` may not match files that use the `using` shorthand `Padding`. Use the unqualified form or instruct CC to match either.

---

## Repos

| Repo | URL | Purpose |
|------|-----|---------|
| RC-Drag-Manager | github.com/stewmac570/RC-Drag-Manager | Desktop app (C# .NET 4.8 WinForms) |
| RCDragLiveServer | github.com/stewmac570/RCDragLiveServer | Live scoreboard server + frontend (ASP.NET Core 8) |

---

## Next Session

Board is clear. Options for next focus:

1. **Tidy remaining Form1 rough edges** — placeholder labels on txtName/txtTime, Driver List label alignment, empty bottom-left corner
2. **Live broadcast push feature** — GUID-based token, toggle button on Form1, designed but not yet implemented
3. **File new issues** for anything above and let the routine handle them overnight

Start next session by checking the open issues list — the routine may have picked up REVIEW-11 overnight.
