# RC Drag Manager — Session Handover
*Generated: May 2026*

---

## What Was Accomplished This Session

### Issues Filed and Resolved
- **9 original issues** filed (#159–#167) covering live scoreboard bugs and enhancements
- **Automated issue-fix routine** set up in Claude Code Desktop — runs hourly, builds, tests, raises and merges PRs unattended
- The routine worked overnight and closed 5 issues automatically (#159, #160, #162, #164, #166)
- Cross-repo support added to the routine — it now works across both RC-Drag-Manager and RCDragLiveServer

### New Issues Filed This Session
| # | Title | Priority | Status |
|---|-------|----------|--------|
| #202 | BUG-11: Stale server state not flushed on new session | High | Open |
| #211 | BUG-12: Single class events skip landing page | High | Open |
| #212 | BUG-13: No landing page / branding on stewmacrc.com | High | Open |
| #213 | ENH-05: QR code generator in app | Medium | Open |
| #215 | ENH-06: Visible dial-in edit panel on Form1 | Medium | Open |
| #216 | ENH-08: Remove Quick Session button | Medium | Open |
| #217 | ENH-09: Extend MultiClass to support all race types | High | Blocked (replaced by 09a-09d) |
| #218 | ENH-10: Reroute Load Saved Event through MultiClassRaceForm | High | Blocked (needs ENH-09) |
| #219 | ENH-11: Remove Create Race Session / retire SessionSetupForm | Medium | Blocked (needs ENH-09 + 10) |
| #220 | ENH-09a: Remove RaceType hardcode in MultiClassSetupForm | High | Open |
| #221 | ENH-09b: Add race type selector to MultiClassConfigDialog | High | Open |
| #222 | ENH-09c: Add class type selector to MultiClassConfigDialog | High | Open |
| #223 | ENH-09d: Add QMDRA/RoundsToRun config to MultiClassConfigDialog | Medium | Open |

### Research Completed
- **ISSUE-PRIORITY.md** — CC assessed all issues and recommended order
- **TIME-FIELDS-RESEARCH.md** — CC traced QualifyingTime and DialIn through the full codebase
- **LANDING-PAGE-CONSOLIDATION-RESEARCH.md** — CC traced all three landing page paths and identified what blocks consolidation

---

## Automated Routine — Current State

**Location:** Claude Code Desktop → Routines → rc-drag-issue-fixer
**Schedule:** Hourly
**Folder:** `C:\Users\Stewart McMillan\source\repos\RC-Drag-Manager`

**Both repos the routine works across:**
- `C:\Users\Stewart McMillan\source\repos\RC-Drag-Manager`
- `C:\Users\Stewart McMillan\source\repos\RCDragLiveServer`

**How it works:**
1. Fetches all open issues ordered by priority (high bugs first, down to low enhancements)
2. Skips issues labelled `blocked`, `wip`, `on-hold`, `skip`
3. Checks dependency `Depends On` sections — skips if dependencies not yet merged
4. Creates a branch (`fix/` for bugs, `feature/` for enhancements)
5. Implements the fix, builds, runs tests
6. If pass: merges PR, comments on issue, closes issue
7. If fail: raises WIP PR, comments with error output, leaves issue open

**Labels used for priority sorting:**
- `priority:high` + `bug` → first
- `priority:high` + `enhancement` → second
- `priority:medium` + `bug` → third
- `priority:medium` + `enhancement` → fourth
- `priority:low` + `bug` → fifth
- `priority:low` + `enhancement` → last

**To add new issues to the routine:** Just create the issue with the right labels. The routine picks it up automatically on the next run.

**To block an issue:** `gh issue edit {number} --repo stewmac570/RC-Drag-Manager --add-label "blocked"`
**To unblock:** `gh issue edit {number} --repo stewmac570/RC-Drag-Manager --remove-label "blocked"`

---

## Landing Page — Current State and Problems

The landing page currently has these buttons in this order:
1. Quick Session ← broken, creates empty session, should be removed
2. Create Race Session ← legacy standalone Form1 path
3. Load Saved Event
4. Driver Lists
5. Settings
6. New Multi-Class Event ← the correct hosted path

### The Core Problem
There are two ways to create a race and they behave differently:
- **Create Race Session** → SessionSetupForm → standalone Form1 (not hosted)
- **New Multi-Class Event** → MultiClassSetupForm → MultiClassRaceForm hosting Form1 tabs

The hosted path (MultiClassRaceForm) is the correct one — it supports tab color coding, Save and Close, and multiple classes. But it currently only supports Round Robin. ENH-09a through 09d will extend it to support all race types, after which the standalone path can be retired.

### Desired End State
1. **Create Race Session** (renamed from New Multi-Class Event)
2. **Load Saved Event**
3. **Driver Lists**
4. **Settings**
5. **Exit**

Quick Session and the old Create Race Session both gone. One entry point, one flow.

---

## UI/UX Problems Identified — For Next Session

These are the UI/UX issues that need a dedicated focused session:

### Landing Page
- Wrong button order
- Two race creation entry points (confusing)
- Quick Session button is broken and shouldn't exist
- Desired order: Create Race Session → Load Saved Event → Driver Lists → Settings → Exit

### MultiClassRaceForm / Form1 Layout
- Form layout breaks at different screen sizes (BUG-10 / #210 filed)
- Columns cut off in Current Round Pairings and Match Winners ListViews
- Form does not stretch cleanly to screen size
- The hosted Form1 inside MultiClassRaceForm needs proper anchor/dock settings

### Race Console (Form1)
- Dial-in times hidden behind right-click — not discoverable (ENH-06 / #215)
- Qualifying times section exists but dial-in has no equivalent visible panel
- Set Time button updates local driver list only, not controller-mediated

### Live Scoreboard (stewmacrc.com)
- Landing page inconsistent — sometimes bypassed for single class events (BUG-12/13)
- Stale results from previous session showing (BUG-11)
- Various layout and ordering issues partially addressed by overnight routine runs

---

## Dependencies Chain

```
ENH-09a (#220) ← no dependencies, safe to run now
    └── ENH-09b (#221) ← needs 09a merged
            └── ENH-09c (#222) ← needs 09b merged
                    └── ENH-09d (#223) ← needs 09c merged
                            └── ENH-10 (#218) ← needs all 09a-09d merged
                                    └── ENH-11 (#219) ← needs ENH-10 merged
```

ENH-08 (#216), ENH-06 (#215), ENH-05 (#213) are all independent and can run in any order.

---

## Repos

| Repo | URL | Purpose |
|------|-----|---------|
| RC-Drag-Manager | github.com/stewmac570/RC-Drag-Manager | Desktop app (C# .NET 4.8 WinForms) |
| RCDragLiveServer | github.com/stewmac570/RCDragLiveServer | Live scoreboard server + frontend (ASP.NET Core 8) |

---

## Next Session Focus — UI/UX

The next session should be dedicated to:

1. **Mockup the desired landing page** — get agreement on layout before writing any code
2. **Mockup the desired MultiClassRaceForm layout** — agree on how it should look at different screen sizes
3. **Mockup the dial-in panel on Form1** — where does it live, what does it look like
4. **Create properly scoped UI issues** for CC to implement
5. **Confirm ENH-09a has merged** and unblock ENH-09b before starting that session

Start the next session by pulling up this handover and the current open issues list.
