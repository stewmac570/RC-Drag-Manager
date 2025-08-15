# PROJECT_STATUS_DEVLOG_FULL.md

# ✅ RC Drag Manager — Master Project Reference (Full Development History + Recovery Locked)

---

##1️⃣ Project Overview

### Project Name:
**RC Drag Manager**

### Purpose:
RC Drag Manager is a Windows Forms C# application designed to manage NHRA-style RC drag racing brackets. It allows race directors to manage drivers, cars, and events following strict Pro Ladder logic. The application fully automates bracket creation, match resolution, and race session management for various class types including Heads Up, Bracket Class, and Dial-In formats.


🟢 Start new feature branch protocol:

- Confirm main branch clean.
- Git checkout new branch:
  git checkout -b feature/<task-name>
- Build isolated code in branch.
- After full testing: commit, push, open PR.
- Merge PR after GPT validated.
- Append completed branch log entry into PROJECT_STATUS_DEVLOG_FULL.md.

---

## 2️⃣ Repository & Git Status (Post-Git Recovery)

- ✅ Full Git recovery completed (June 7, 2025)
- ✅ Repository `main` fully reset and stabilized
- ✅ All development history merged into single synced baseline
- ✅ `.gitignore` applied (`.vs/`, `bin/`, `obj/`)
- ✅ All Designer files fully restored and synced
- ✅ Namespace fully standardized under: `RCDragManagerProd`
- ✅ Active remote repo: https://github.com/stewmac570/RC-Drag-Manager

---

## 3️⃣ Full Architecture Summary

| Layer            | Description                               |
|-------------------|-------------------------------------------|
| UI Layer          | Windows Forms (WinForms)                 |
| Business Logic    | Bracket generation & race flow logic     |
| Data Layer        | SQLite persistence (Drivers/Cars)        |
| Session Engine    | RaceSession object manages sessions      |

---

## 4️⃣ Code Structure Summary (Post Recovery)

| File | Purpose |
|------|---------|
| `Program.cs` | App entry point |
| `LandingForm.cs` | Main landing page |
| `SessionSetupForm.cs` | Full session creation UI |
| `DriverManagerForm.cs` | Driver and car management UI |
| `AddDriverAndCarDialog.cs` | Unified add driver + car entry |
| `EditDriverDialog.cs` | Edit driver name and state |
| `AddCarDialog.cs` | Add/edit cars |
| `MatchEngine.cs` | Core race bracket state engine |
| `ProLadder.cs` | NHRA Pro Ladder ruleset (3–10 drivers implemented) |
| `RaceSession.cs` | Full race session configuration object |
| `MatchResult.cs` | Match result tracking engine |
| `DriverRepository.cs` | SQLite data layer for drivers |
| `CarRepository.cs` | SQLite data layer for cars |
| All `.Designer.cs` files | Fully linked to form files, namespace fixed |

---

## 5️⃣ Fully Completed Features

- ✅ Driver management (add/edit/delete)
- ✅ Car management (add/edit/delete per driver)
- ✅ Unified add-driver-and-car workflow (`AddDriverAndCarDialog`)
- ✅ SQLite persistence operational (`race_data.db`)
- ✅ NHRA Pro Ladder engine (3–10 drivers fully functional)
- ✅ Session setup (event name, date, class type, roster building)
- ✅ Driver filtering per class type (Heads Up, Dial, Index)
- ✅ Session handoff pipeline fully implemented
- ✅ Bracket pairing & round advancement logic (strict NHRA style)
- ✅ Next-up pairing UI functional
- ✅ Manual winner selection with live bracket updates
- ✅ Manual match result editing (`EditWinnerDialog`)
- ✅ Fully modular GPT-compliant code architecture
- ✅ UI resource branding (transparent logo embedded)

---

## 6️⃣ Partial / In-Progress Work

- 🚧 NHRA Pro Ladder expansion (11–32 drivers templates loaded but not fully parsed)
- 🚧 Random draw bracket logic (UI wired, backend pending)
- 🚧 Round robin logic (UI placeholder only)
- 🚧 Session save/load system (pending)
- 🚧 Seed confirmation logic stubbed in SessionSetupForm

---

## 7️⃣ Outstanding Task List

| Category | Task |
|----------|------|
| Ladder Logic | Complete full `ProLadder.cs` hardcoded expansion (11–32 drivers) |
| Session Persistence | Build session save/load using JSON or SQLite |
| Randomizer | Build full random draw pairing engine |
| Round Robin | Build full round robin engine |
| Statistics | Driver win/loss/event tracking |
| Reporting | Historical session reporting, event summaries |
| Settings | Implement Settings page functionality |
| Validation | Form validation & error handling on SessionSetupForm |
| DB Versioning | Build upgradeable DB version system |
| Code Refactor | Merge CarRepository into DriverRepository |
| AI Expansion | GPT-powered AI seed balancing, class population, suggestions |

---

## 8️⃣ Permanent GPT Locked Development Rules

🚫 Strict **DO NOTS**:

- ❌ Do NOT auto-advance rounds
- ❌ Do NOT auto-resolve BYEs
- ❌ Do NOT allow MatchEngine to control round progression
- ❌ Do NOT modify ProLadder.cs unless explicitly requested
- ❌ Do NOT introduce dynamic pairings into Pro Ladder
- ❌ Do NOT reintroduce `GetNextPlayableRound()` or `RefreshBracketState()`

✅ GPT Code Boundaries:

- ✅ All bracket advancement stays **manual** (Form1 driven)
- ✅ Race Director fully controls bracket flow
- ✅ Pro Ladder pairing strictly follows NHRA standards
- ✅ MatchEngine holds state — Form1 controls UI race flow
- ✅ Full `.cs` file delivery always for new work (no snippets)
- ✅ Full file headers, comments, clean block structure
- ✅ Production-grade, maintainable, scalable code quality

---

## 9️⃣ Historical Development Log (Full)

---

### 🔧 Branch: `feature/session-wiring`

- ✅ Built full SessionSetupForm implementation
- ✅ Added event details entry (name, race type, date)
- ✅ Added class selection (Heads Up, Dial, Index)
- ✅ Implemented full driver roster building with Add/Remove flows
- ✅ Integrated live driver list filtering by class type
- ✅ Wired session creation pipeline into RaceSession object
- ✅ Fully populated RaceSession with correct DriverEntry objects

---

### 🔧 Branch: `feature/form1-race-engine-restore`

- ✅ Rebuilt MatchEngine into fully manual NHRA race logic
- ✅ Removed all auto-advancement logic (old GPT auto-pairing logic fully deleted)
- ✅ Eliminated `RefreshBracketState()` and `GetNextPlayableRound()`
- ✅ BYEs included but require manual advancement
- ✅ Manual race control now locked to Form1 UI only
- ✅ Race rounds controlled by Generate Next Round button exclusively
- ✅ MatchResult engine rebuilt for full manual result storage
- ✅ Strict NHRA race flow fully enforced

---

### 🔧 Branch: `feature/driver-manager-add-driverandcar`

- ✅ Unified driver + car creation into AddDriverAndCarDialog
- ✅ Eliminated original AddDriverDialog flow
- ✅ Simplified all driver creation to match SessionSetup behavior
- ✅ Added EditDriverDialog (allows Name and State edit; disables QualTime edits here)
- ✅ Applied database schema update to add `State` field (fully backward compatible)
- ✅ Simplified Edit Car and Delete Car flows using in-list selection
- ✅ Removed legacy SelectCarDialog dependency

---

### 🔧 Branch: `feature/driver-save-button`

- ✅ Updated Save Changes button logic inside DriverManagerForm
- ✅ Added “Save and Close” behavior to reassure users that edits are committed
- ✅ Simplified UI button flow and prevented accidental data loss confusion
- ✅ Left repository save logic untouched (auto-saves on edit)
- ✅ No DB changes — UI-level change only

---

### 🔧 Branch: `feature/ui-driver-form1-tweaks`

- ✅ Applied AddDriverAndCarDialog into SessionSetupForm
- ✅ Fully integrated class type logic into dialog:
  - Heads Up
  - Dial-In (editable)
  - Index (fixed dial-in)
- ✅ Fully eliminated Qualifying Time entry from Session Setup (moved to Race UI only)
- ✅ Applied Form1 ListView FullRowSelect for improved bracket row behavior
- ✅ Full visual UI alignment across all forms

---

### 🔧 Branch: `feature/recovery-clean`

- ✅ Full Git recovery after repository corruption
- ✅ Complete repo rebuild and `main` branch reset
- ✅ All development work merged and stabilized into safe working baseline
- ✅ Fully linked Designer files, resource images, namespaces and project structure fully functional
- ✅ Official GPT master working baseline as of `2025-06-07`

---

# 🔐 Recovery Locked — Project Fully Synchronized

✅ **RC Drag Manager — Master Development Log + Recovery Completed**

✅ This file becomes permanent project baseline for:

- All GPT code generation
- Future development reference
- Branch work validation

---

### 🔧 Branch: `feature/qualifying-time-work`

- ✅ Added Qualifying Time editor inside DriverManagerForm.
- ✅ Created new `AddEditQualTimeDialog` reusable WinForms dialog.
- ✅ Fully integrated `Set Qual Time` button into Driver Manager UI.
- ✅ Extended `DriverRepository` with `UpdateQualifyingTime()` method for SQLite update.
- ✅ Designer files updated for new button placement (`btnSetQualTime`).
- ✅ Fully compatible with future race session seeding logic.
- ✅ Preserved namespace alignment and Designer stability.
- ✅ Git branch safely merged via Pull Request #8 into `main`.
- ✅ Repository now clean and fully synchronized for continued development.

🔧 RC Drag Manager — Development Summary (Race Director Build)
✅ Major Changes Completed:
🏁 Session Setup Logic
Fully wired QualifyingTime and DialIn into RaceSession object.

Class Types:

Heads Up → uses QualifyingTime.

Dial-In → uses Car.DefaultDialIn.

Bracket Class (Index) → uses FixedDialIn entered at session start.

SessionSetupForm fully stable and integrated.

🏁 MatchEngine Control Upgrade
Form1 no longer manually tracks rounds.

MatchEngine is now sole authority for:

Seed logic

Ladder structure

Round labels (R1, SF, F, etc)

Match dependencies

Bracket state

🏁 Race Director Flow Logic
Full manual round reveal system:

"Generate Bracket" shows only R1.

"Generate Next Round" reveals subsequent rounds manually.

Director holds 100% round control.

Matches are only selectable for rounds that have been revealed.

🏁 Pairing Display
Pairings list fully groups matches by round.

Proper round label ordering (R1 → R2 → QF → SF → F).

Fully dynamic across all bracket sizes (3 to 32 drivers).

🏁 Winners List Display
Winners list grouped and ordered exactly like pairings list.

Round headers displayed:

diff
Copy
Edit
---- R1 ----  
Driver 1  
Driver 2  
---- F ----  
Driver X
🏁 Save and Close Button (Placeholder)
Save and Close button added to Form1.

Placeholder implemented:

Displays messagebox until database persistence logic is built.

Fully wired up for future event save/load system.

✅ Code Files Touched:
SessionSetupForm.cs

Form1.cs

Form1.Designer.cs

MatchEngine.cs (used unchanged, fully state-driven)

MatchResult.cs (used unchanged, fully state-driven)

🔒 Project State
✅ Fully stable.

✅ NHRA Pro Ladder compliant.

✅ Fully race-director driven.

✅ Fully merge committed to main.


# ✅ Race Session Save/Load Engine Development Log

## 🔨 Feature: Persistent Race Sessions (Save, Load, Resume, Delete)

### ✅ New Features Implemented
- Added full **RaceSessionRepository.cs** to handle database I/O for sessions.
- Sessions now fully serialized as JSON blob to SQLite for fast save/load.
- Sessions include:
  - Event Name
  - Event Date
  - Race Type
  - Class Type
  - Fixed DialIn
  - DriverEntries (driver ID, name, car, qualifying time, dial-in, seeds)
  - SavedResults (full bracket winners: MatchId → DriverId)
  - SavedRevealedRounds (full bracket round progression)

### ✅ UI Changes

#### LoadSessionForm:
- Built full LoadSessionForm with:
  - Full session listing (`ListView`)
  - Load button (resume race)
  - Delete button (delete session)
  - Cancel button (close without action)
- UI redesigned to match global 900x600 window sizing
- Button layout standardized: Delete → Load → Cancel (bottom-right alignment)
- Fully wired event handling

#### LandingPageForm:
- Integrated LoadSessionForm into `btnLoadEvent_Click()`
- Now fully launches Form1 passing loaded RaceSession to resume race mid-bracket

#### Form1:
- Fully updated to accept restored RaceSession object
- Fully rebuilds bracket state:
  - Driver roster
  - MatchEngine re-initialization
  - MatchResults restored
  - Revealed rounds restored
- Saves full bracket state after every round to DB

### ✅ Database Changes

- `RaceSessions` table created dynamically via `EnsureTableExists()` if missing.
- Repository logic:
  - `INSERT` on new session
  - `UPDATE` on existing session (Id-based)
- Full delete functionality wired into `DeleteSession()`

### ✅ Testing & Debugging Tools Added

- Temporary path logging added to repository constructor for full DB path tracking.
- Full debug-level tracing of database location to ensure correct file used.

### ✅ Bugs Encountered & Resolved

- 🐞 Early development versions inserted multiple duplicate records due to missing Id handling.
- 🐞 Data contract evolved during build; some DB files contained invalid SessionData blobs.
- 🐞 After DB wipe, missing `EnsureTableExists()` caused silent save failures (fixed).
- 🐞 Final ListView column rebuild corrected internal state preventing empty loads.

---

## ✅ Outcome

- **Session Save/Load/Delete is now 100% stable, fully functional.**
- Race sessions can be saved mid-bracket, closed, reloaded, and resumed without data loss.
- Fully surgical recovery process completed.
- Foundation now stable for future:
  - Web scoreboard
  - Session cloning
  - Multi-session management
  - Long-term persistence and debugging tools


### 🔧 Branch: feature/form1-ui-tweak-pass

- ✅ Added new "Set Qual Time" button to Form1 UI under driver list
- ✅ Wired Set Qual Time button to `AddEditQualTimeDialog` for editing individual qualifying times mid-session
- ✅ Corrected Edit Driver button wiring to call `EditDriverDialog` (constructor parameters aligned)
- ✅ Standardized Form1 UI height to 900x600 to match all other forms
- ✅ Repositioned Reset Race, Edit Result, Save and Close, and Up Next label for visual alignment after height change
- ✅ Fixed Generate Next Round button logic:
  - Disabled until all matches in current round have been resolved
  - Prevents accidental advancement before race director selects winners
- ✅ Fixed Save and Close logic for Quick Session mode:
  - Allows clean close without session data present
  - Displays clear message if session was not tied to database session object
- ✅ Preserved full NHRA Pro Ladder logic, manual race control, and GPT locked rules
- ✅ No other forms or files modified outside Form1.cs and Form1.Designer.cs

✅ Fully merge committed to main after full GPT validation.
✅ Clean branch scope validated for recovery tracking.


Branch: feature/pro-ladder-expansion
✅ Fully expanded ProLadder.cs logic from 11 to 16 car fields.

✅ Bracket logic strictly follows official NHRA Pro Ladder structure (validated against Proladder9-16.pdf).

✅ All ladder expansions built match-by-match using race director controlled mappings.

✅ Corrected prior logic mismatches between seeds and match references.

✅ Fully maintained NHRA-compliant RoundLabel sequence (R1, R2, SF, F).

✅ No changes made to MatchEngine, Form1, or UI logic — expansion isolated strictly to ProLadder.cs.

✅ Expansion fully tested against race director workflow for:

11-car bracket

12-car bracket

13-car bracket

14-car bracket

15-car bracket

16-car bracket

✅ Fully compatible with existing MatchResult, SessionSave, LoadSession system.

✅ Locked clean commit for future expansion 17–32 cars.

✅ That entry is safe to append directly into Section 9️⃣ Historical Development Log (Full) inside your master project file.

 Branch: feature/pro-ladder-17-24
✅ Added full ladder structure for 17-car and 18-car Pro Ladder brackets.

✅ Introduced support for new round label "R3" to handle additional elimination stages.

✅ Updated ProLadder.cs with:

GetLadder17() using official NHRA seeding for 17 drivers

GetLadder18() using official NHRA seeding for 18 drivers

All match references (FromMatch1, FromMatch2) and RoundLabel values fully mapped

✅ Manual round flow preserved:

R1 → R2 → R3 → SF → F

✅ Adjusted internal round label sorter (GetRoundOrder) to include "R3" for UI/order logic

✅ Verified alignment with original NHRA documents from Proladder9–16.pdf and Proladder17–24.pdf



🔧 Branch: feature/pro-ladder-19
✅ Created new Pro Ladder structure for 19-car NHRA elimination bracket

✅ Verified against official ladder layout from uploaded PDF and race director hand-marked bracket

✅ Added GetLadder19() to ProLadder.cs with correct:

R1–R3 mappings

All BYEs and MatchId sequences

RoundLabels: "R1", "R2", "R3", "SF", "F"

✅ Added GetLadder20() as next bracket entry with full match mappings

✅ Manual round reveal structure preserved (no auto-advancement)

✅ Fully compatible with MatchEngine, Form1, and RaceSession pipeline

✅ Branch safely committed and pushed
✅ Ready for pull request and merge into main


### 🔧 Branch: feature/20-to-24-driverladder

- ✅ Added full ladder structure for 19–24 driver Pro Ladder brackets.
- ✅ Verified all brackets against official NHRA seedings from Proladder17–24.pdf.
- ✅ Implemented the following methods in `ProLadder.cs`:
  - `GetLadder20()`
  - `GetLadder21()`
  - `GetLadder22()`
  - `GetLadder23()`
  - `GetLadder24()`
- ✅ Correctly mapped all MatchId, Seed1, Seed2, FromMatch1, FromMatch2 fields.
- ✅ Applied RoundLabel sequence: R1 → R2 → R3 → SF → F where applicable.
- ✅ Preserved full NHRA compliance and round progression logic.
- ✅ UI and engine compatibility confirmed with existing 3–19 driver support.
- ✅ Ready for extension to 25–32 driver ladders.

✅ Branch: feature/form1-random-ui
📦 Summary of Work Completed
Added ComboBox for Race Type (Pro Ladder, Randomized, Round Robin)

Only visible in Quick Session mode

Default: “Pro Ladder”

Stopped Auto-Starting Brackets

Bracket no longer auto-generates on session start

User must click Generate Bracket manually

Replaced Current Round Pairings UI

ListBox → ListView with 3 columns: M#, Driver 1, Driver 2

Clean alignment + visual round headers (e.g., “Round 1”)

Replaced Match Winners UI

ListBox → ListView with columns: M#, Loser, Winner

Round headers show in second column

Fixed BYE handling

Replaced all “TBD” placeholders with "BYE" when a driver is missing

Displayed consistently in brackets, buttons, labels

Extended MatchResult to track losers

SetWinner(matchId, winner, loser) stores both drivers

Supports GetLoser(matchId)

Old 2-arg SetWinner(...) still supported for session restore

Updated RaceSession saving

MatchResultSave now includes LoserDriverId

Data stored and restored cleanly across sessions

Merge conflict resolved

Final result uses "BYE" (uppercase) consistently


--------------------


Summary of Work — Dialog Cleanup + DriverManager Fix
🧱 Affected Files:
AddCarDialog.cs

AddCarDialog.Designer.cs

DriverManagerForm.cs

✅ Fixed AddCarDialog
Cleaned AddCarDialog.cs to expose:

csharp
Copy
Edit
public Car NewCar { get; private set; }
Confirmed default constructor exists:

csharp
Copy
Edit
public AddCarDialog() { InitializeComponent(); }
Restored radio button logic (rbHeadsUp, rbDial, rbIndex)

Confirmed btnOK_Click builds NewCar correctly

Confirmed dial-in logic based on class type

Fixed and matched all control declarations in AddCarDialog.Designer.cs

Correctly wired all event handlers (btnOK_Click, ClassTypeChanged)

✅ Fixed DriverManagerForm.cs
Fully cleaned and replaced btnAddCar_Click:

Now correctly uses new AddCarDialog() for new car creation

Calls dlg.NewCar and adds to selectedDriver.Cars

Fully cleaned and replaced btnEditCar_Click:

Retrieves selected car correctly

Calls AddCarDialog(car) and applies edited data

Eliminated all CS1503, CS1061, CS0103 errors

Standardized all method logic with proper in-scope variables

Final file compiles clean and matches your architecture

✅ Build Status:
✔️ All errors resolved
✔️ All dialogs working
✔️ Final rebuild successful
✔️ Ready to check in

------------------------------
 Branch: feature/driver-stats
✅ Added new Driver Stats button to DriverManagerForm (enabled on driver selection)

✅ Created new WinForm: DriverStatsForm with separate .cs and .Designer.cs

✅ UI matches LoadSessionForm styling (900×600, top summary, detailed table view)

✅ Top summary includes: Wins, Losses, Events Entered, Events Won

✅ Unified ListView displays match history per session:

Columns: Event Name, Date, Round, Opponent, Result

Automatically shows "BYE" when driver had no opponent

✅ Reads from RaceSession.SavedResults and DriverEntries

✅ Uses MatchLookupHelper.cs to resolve RoundLabel by MatchId without active session

✅ Fully supports both active and historical bracket inspection

✅ All code modular, non-intrusive, and compatible with existing SQLite session structure

✅ Branch pushed and ready for merge.
✅ Feature tested and UI confirmed stable.

----------------------------------------

✅ Feature: Random-Draw Bracket (and mixed Pro-Ladder fixes)
Status: COMPLETE & STABLE

Area	Work completed in this session
Random-draw first-round generator	• RandomBracket.GenerateFirstRound() incorporated.
• Shuffling, BYE allocation, correct MatchId assignment.
Random-draw round-by-round engine	• RandomMatchEngine created and wired.
• GenerateNextRound() avoids repeat pairings and auto-resolves BYEs.
btnGenerateBracket_Click	• Refactored: single isRandom flag.
• Random branch shuffles & loads randomEngine, adds revealedRounds.Add("R1").
• Pro-Ladder branch seeds, initialises MatchEngine, detects real first-round label (R1/SF/…) and adds to revealedRounds.
RedrawFullBracket	• Guarantees lvPairings has columns (fix for blank list on Quick-Session forms).
• Separate logic for Pro-Ladder vs Random modes.
IsRandomMode() helper	• Returns true if race-type string contains “random” (case-insensitive). Eliminates hard-coded "Randomized".
UpdateNextUp	• Re-written to use IsRandomMode.
• Handles null randomEngine and BYE names.
ProcessMatchWinner	• Re-written with IsRandomMode.
• Skips stat updates when loser is BYE.
• Supports both engines and session-launch path.
UpdateButtonStates	• Guard added: if randomEngine == null or revealedRounds.Count == 0 → leave buttons disabled (fixes crash after Reset).
Reset Race	• randomEngine = null; added; full UI clear; buttons reset.
Columns added once	• RedrawFullBracket() initialises ListView columns when none exist (Create-Race-Session launch fix).
Winner list & “Generate Next Round” state	• Correct enabling logic after each round; final round disables button.
Crash fixes	• Handled empty revealedRounds (Sequence contains no elements).
• Guarded against null engines in all methods.
UI feedback	• lblNext shows “Up Next: --” until bracket generated; “All matches resolved.” at end.

Testing covered

Quick-Session → Pro-Ladder (3 – 16 drivers) ✓

Quick-Session → Random-Draw ✓

Create-Race-Session → Pro-Ladder ✓

Create-Race-Session → Random Draw (“Random Draw” label) ✓

Reset Race on both modes ✓

BYE auto-advancement & BYE stats skip ✓

Next logical tasks

Persist RandomMatchEngine state to session save/load (future).

UI polish (column widths, scroll auto-scroll).

Extend Pro-Ladder templates 25 – 32 drivers.

Feel free to paste the table (or adapt) into PROJECT_STATUS_DEVLOG_FULL.md under a new branch entry, e.g.:

arduino
Copy
Edit
### 🔧 Branch: feature/random-draw-final-stabilise
✅ Full random-draw bracket engine completed and integrated …
—end of feature.

--------------------------------------------------------

Round-Robin feature — work completed in this chat

New engine files

RoundRobinEngine.cs – generates 3 rounds of pairings (max 1 BYE/round, no rematches), tracks every result in DriverMatchResult.

RoundRobinRanker.cs – weights points (R1 4.0 / R2 3.5 / R3 3.0), ranks by points → wins → H2H → opp-strength → random.

LosersBracketEngine.cs – single-elimination bracket for all non-top-3 drivers; BYEs auto-advanced.

MatchEngine draft update

Added SessionType.RoundRobin and RacePhase enum.

Façade logic routes calls to the active engine (Round-Robin → Losers Bracket → Pro Ladder) and detects completion.

Git actions

Created branch round-robin-engin.

Committed new files (RoundRobinEngine, RoundRobinRanker, LosersBracketEngine) with message
“MVP: add round-robin engines and ranker”.

Pushed branch to origin; ready for Pull Request.

Next-phase planning

Produced detailed MatchEngine_Refactor_Spec.md outlining extraction of ProLadderEngine, IMatchEngine interface, façade design, UI wiring, unit-test matrix.

Provided prompt to start a fresh chat for the refactor.

This log entry captures all code additions, git commits, and the roadmap delivered in this session.

-------------------------------------------------

1. Initial Wiring of Round Robin Mode
Connected "Round Robin" as a valid race mode option in btnGenerateBracket_Click.

RoundRobinEngine was instantiated and LoadDrivers(...) + GenerateMatches() were called.

Round labels like "R1" were added to revealedRounds.

2. Initial RoundRobinEngine Setup
Refactored RoundRobinEngine to store match data as:

csharp
Copy
Edit
List<(Driver Driver1, Driver Driver2, string RoundLabel, int MatchId)> matches;
Implemented GenerateMatches() with logic for:

Avoiding rematches across rounds

Up to 3 rounds max

Assigning BYEs if odd number of drivers remain

3. Resolved Early Crashes
Fixed an issue where the app crashed when only one pairing existed due to:

Incorrect use of placeholder ResolveDrivers(LadderMatch) instead of correct overload.

Cleaned up UpdateEventWinnerStats() to handle all 3 race types with proper logic per engine.

4. Confirmed Matching Logic Runs Without Crash
Multiple iterations ensured the btnNextRound_Click and match processing doesn’t crash.

❌ What’s Still NOT Working
🔴 Only One or Two Matches Appear
GenerateMatches() often results in just one pairing + one BYE, even with 6 drivers.

Expected: 3 unique pairings in Round 1 alone with 6 drivers, no BYEs.

🔴 Round Robin UI Not Fully Wired
Winner buttons on Form1 do not always respond when Round Robin mode is active.

The current ResolveDrivers(...) and match detection don’t connect to UI correctly for Round Robin.

🔴 UI Hangs or Locks
Under certain states, GenerateMatches() loops infinitely if no unpaired opponents exist (bad exit condition).

Caused UI lockups during testing with higher driver counts or bad match logic.

⚠️ Problems Identified
Your current RoundRobinEngine.GenerateMatches() rotates drivers endlessly if no opponent is found.

Form1 UI isn’t using the correct match list filtering logic when drawing Round Robin matches.

Conflicts between MatchEngine-based logic and new RoundRobinEngine logic are still unresolved in Form1.

📌 Next Step (As You Suggested)
We need to:

Start a clean chat

Upload Form1.cs, MatchEngine.cs, RoundRobinEngine.cs, RaceSession.cs, and others as needed

Get a precise implementation plan — no code, no guesses — just a surgical set of changes to get Round Robin fully functional and UI-integrated.

-----------------------------------------------------------------------------------

Round-Robin “single-pair” bug – what we fixed
Area	Change	Why it matters
Driver.cs	• Added static _nextRuntimeId + auto-assign logic so every Driver gets a unique runtime ID unless one is loaded from storage.	Round-Robin pairing relied on IDs; duplicates caused missing pairings.
Form1.cs – Generate Bracket	• Unified UI refresh after generation.
• Added full Round-Robin branch.	All first-round pairs now appear.
Form1.cs – ProcessMatchWinner	• New Round-Robin branch.
• Correct 2-param call for Randomized engine.
• Shared UI refresh.	Winner buttons now record results in every race type.
Form1.cs – GetNextHiddenRound & UpdateButtonStates	• Both methods now query the active engine (Pro Ladder / Randomized / Round Robin).	Generate Next Round enables exactly when it should.
RoundRobinEngine.cs	• Re-implemented GenerateMatches() with “circle method” scheduling (3 rounds, no rematches, ≤1 BYE/round).	Fixed the “two drivers disappear after R1” issue.
Form1.cs – Save & Close logic	• Replaced single Pro-Ladder loop with three branches that pull results from the correct engine.
• Uses your MatchResultSave model.	Stopped NullReferenceException when saving Round-Robin or Randomized sessions.
Misc. UI refresh	Ensured every engine change calls RedrawFullBracket, UpdateNextUp, UpdateWinnersList, and UpdateButtonStates.	Keeps the interface in sync after any action.

Outcome

Round-Robin sessions now:

generate complete pairings for 3 rounds,

let you pick winners,

unlock Generate Next Round when all current matches are resolved,

save/restore correctly without crashing.

Pro Ladder and Randomized modes continue to work exactly as before.

----------------------------------------------------------------

Branch: feature/phase2-task1-ranker
Date: 2025-06-22
Developer: Stewart McMillan
Focus: Round Robin support + universal BYE protection for match UI

🎯 Purpose of Task
Resolve critical UX/UI bug where winner buttons remained enabled when a driver was paired against a BYE in all race modes — including Round Robin, Randomized Bracket, and Pro Ladder.

Previously, if a race contained an odd number of drivers, the BYE pairing could:

Cause crashes when the user selected the BYE driver

Allow invalid match resolutions

Mismatch display logic between "Next up", button labels, and internal state

🔧 Fixes and Features Implemented
1️⃣ Fixed Button Enable Logic for BYE
Updated UpdateNextUp() (in Form1.cs) to:

Automatically disable winner buttons for BYE drivers

Enable only the driver that is racing against BYE

Fully disable both buttons if both drivers are null or the match is resolved

csharp
Copy
Edit
btnWinner1.Enabled = d1 != null && d1.Name != "BYE";
btnWinner2.Enabled = d2 != null && d2.Name != "BYE";
2️⃣ Updated All Race Modes (Pro Ladder, Random, Round Robin)
Ensured button state logic is consistent across all formats:

🔁 Randomized Bracket Mode

🔃 Round Robin Engine

🏁 NHRA-style Pro Ladder

The UpdateNextUp() method now contains separate logic branches per race mode, but uses the same rule for disabling invalid matches.

3️⃣ Resolved Crashes in UpdateDriverStats()
Bug: pressing a button for a BYE match caused a null reference exception in UpdateDriverStats() because winner or loser was null.

Fix: added a safe check at the top of the method:

csharp
Copy
Edit
if (winner == null || loser == null) return;
4️⃣ Re-enabled driver buttons only when valid
UI no longer permits clicking "Driver vs BYE" pairings.

If both sides are null, display shows Next: -- and disables both buttons.

🆕 New or Updated Files
File	Status	Purpose
Form1.cs	Modified	Full BYE detection logic, button control
MatchEngine.cs	Modified	Updated driver resolution + error resilience
RandomMatchEngine.cs	New	Engine to support randomized match format
RoundRobinMatchResult.cs	New	DTO to hold round-robin result records
RoundRobinRanker.cs	New	Planned future module (empty shell for now)
RandomBracket.cs	Modified	Driver resolution logic reused across race types

✅ Outcome
🟢 No crashes when BYE drivers are present

🟢 Button behavior is now predictable and consistent

🟢 Code handles missing or invalid drivers gracefully

🟢 All race formats respect the new logic

🟢 Full match flow now clean across quick sessions and created events

🟢 Stable version committed to feature/phase2-task1-ranker

📝 Commit Message Used
pgsql
Copy
Edit
Phase 2 - Task 1: Add RoundRobinRanker and fix BYE button disable logic
- Prevents crashes from BYE matches
- Ensures UI winner buttons are only active for valid drivers
- Updates Form1.cs UpdateNextUp to handle all modes

------------------------------------------------------------

 Branch: feature/roundrobin-rank-logic
Date: 2025-06-22
Developer: Stewart McMillan
Task: Round Robin Phase 2 – Task 2 (Ranking Engine)

✅ Purpose:
Implement complete Round Robin scoring and ranking engine with support for:

Win / Loss / BYE point logic per round

H2H tiebreak

Opponent Strength as third tiebreak

Stable deterministic sorting fallback

✅ Features Added:

RoundRobinRanker.cs rewritten to compute:

TotalPoints using scoring table:

R1: Win 4.0 / Loss 1.0 / BYE 2.0

R2: Win 3.5 / Loss 0.75 / BYE 1.5

R3: Win 3.0 / Loss 0.5 / BYE 1.0

Wins, Losses

Defeated opponents list

OpponentStrength (sum of opponents’ total points)

Rank ordering:

TotalPoints → Wins → H2H → OppStrength → DriverId

Added fallback logic to replace unsupported .GetValueOrDefault() with .ContainsKey(...) for full .NET Framework 4.7.2 compatibility

✅ Classes Updated:

RoundRobinRanker.cs — now stable and testable

Added OpponentStrength field to DriverRankResult model

Updated sorting logic to handle ties and BYE-only rounds cleanly

✅ Git Actions:

Created new branch feature/roundrobin-rank-logic

Replaced LINQ extension method for .NET 4.7.2 compatibility

Committed and pushed finalized logic

PR created: "Final Round Robin Ranking Logic – Full Points, Tiebreaks, .NET 4.7.2 Fix"

✅ Status:
Feature complete and merged to main. Ready for Task 3 (UI display of standings post-R3).

-----------------------------------------------------

✅ Round Robin Core Functionality (Completed & Confirmed Working)
RoundRobinEngine.cs:

GenerateMatches() successfully creates 3-round pairing schedule.

SetWinner() and HasWinner() implemented with internal result tracking.

GetResults() returns RoundRobinMatchResult with points and match details.

RoundRobinMatchResult.cs:

Model updated to include WinnerId, LoserId, MatchId, RoundLabel, Driver1Id, Driver2Id.

✅ Form1 Integration
🆕 RaceType Handling:

btnGenerateBracket_Click() correctly initializes roundRobinEngine.

Only "R1" is revealed at start.

🆕 Match Display:

RedrawFullBracket() renders each round if revealed.

Match entries display Driver1 and Driver2 using real names (confirmed visually).

🆕 Winner Selection & Result Tracking:

ProcessMatchWinner(bool winner1) routes to SetWinner() for Round Robin.

Uses roundRobinEngine.HasWinner() to detect unresolved matches.

After setting winner, UI updates (buttons, list, etc).

🆕 Standings & Stats:

UpdateEventWinnerStats() evaluates top winner at end of Round Robin (most wins).

UpdateDriverStats() tracks wins/losses in DB.

✅ UI Workflow Verified
Generate → Round 1 shows 3 matches.

Select winners → Round 2 reveals.

Select winners → Round 3 reveals.

Final standings display in Match Winners box.

“Next Up” updates correctly per match.

🚫 Before Bug: Pairings Were Stable
Pairings in lvPairings (left panel) stayed fixed throughout match resolution.

Match order and pairing layout did not change as winners were selected.

-------------------------------------------------------

Feature Branch: feature/roundrobin-final4-buyback
Scope: End-to-end Round-Robin → Buyback → No-rematch Losers Bracket → Final-4 Pro‐Ladder integration

Work Completed
Buyback UI

Added BuybackDriverSelectionForm (checkbox list + “Confirm Buybacks” / “No Buyback” buttons).

Modal returns selected drivers or skips directly to 4th-place injection.

Losers Bracket Engine Hook-up

Wired “Generate Losers Bracket” button to build a single-elimination tree via LosersBracketBuilder.Build(entrants, history, offset).

Stored pairing history to prevent rematches.

Introduced inLosersPhase flag to switch Form1 into LB mode.

Round-Robin & Pro-Ladder Coexistence

Updated RedrawFullBracket() to render all Round-Robin rounds and any revealed LB rounds in one combined view.

Enhanced UpdateNextUp() so winner buttons drive LB matches when inLosersPhase is true.

Patched ProcessMatchWinner() to record LB results, auto-advance BYEs, and auto-reveal the next LB round.

Adjusted UpdateButtonStates() to enable Next Round once each LB round is fully resolved, and to re-enable “Generate Next Round” / LB logic in the correct order.

Final-4 Injection

After the last LB round resolves, extracted the LB champ, combined with Round-Robin top-3, re-seeded by QualTime, and re-initialized MatchEngine for a 4-driver Pro-Ladder (Semi 1, Semi 2, Final).

Switched currentSession.RaceType to “Pro Ladder” to avoid falling back into Round-Robin.

Reset Logic

Enhanced btnReset_Click to clear inLosersPhase, randomEngine, revealedRounds, and pairing history—returning to a clean slate.

Persistence & Branch Management

Tested full RR → LB → Final-4 flow locally.

Committed & pushed all changes (Form1.cs, Form1.Designer.cs, BuybackForm.cs/.Designer, LosersBracketBuilder.cs, project file) to the feature branch.

Next Steps
Pull Request & Code Review

Automated/Manual QA covering:

RR rounds 1–3 → Buyback selector → LB R1→R2→Final, then Semis & Final.

“No Buyback” shortcut path.

Reset cycle & Save/Load persistence.

Persistence of LB bracket in RaceSession for Save / Load (future).

Polish & UX tweaks (e.g. clearer round labels, timing, styling).

---------------------------------------------------------------


# RC Drag Manager — Project Status (June 25, 2025)

## ✅ Summary of Work Completed (in this session)

### 🧠 Logic and Engine Improvements

- **Winner resolution logic stabilized**
  - Correctly fixes cases where a driver defeated another but then reverted to a BYE.
  - `RandomMatchEngine.SetWinner(...)` now explicitly back-resolves loser's identity to avoid nulls or BYEs post-selection.

- **Losers Bracket Auto-Round Generation Removed**
  - Removed automatic reveal of the next Losers Bracket round from `ProcessMatchWinner()`.
  - Manual reveal is now required via the “Generate Next Round” button.

- **Pop-up for Top 3 Round Robin Winners Restored**
  - After all 3 RR rounds and final result are entered, a popup displays the Top 3 drivers.
  - This message instructs the user to generate the Losers Bracket.

- **UI Locking Enforced Between Rounds**
  - Disabled winner buttons when a round is completed but before the next is triggered.
  - Prevents false/misleading button states.

### 🛠️ UI / Form1.cs Fixes

- **Reset Race Bug Fixed**
  - Previously reverted the race type to Pro Ladder — now it fully resets state without applying default race logic.

- **Manual Advancement Mode Restored**
  - Post-RR or LB rounds, user must manually click “Generate Next Round”.
  - No more auto-advance.

- **Buyback Selection Dialog Logic Respected**
  - After confirming buyback drivers, next round does not auto-reveal.
  - This keeps round progression consistent across all modes.

- **Correct Button References**
  - Fixed unknown `btnDriver1` errors by replacing with `btnSelectDriver1` / `btnSelectDriver2`.

- **Match Rendering Sync**
  - UI now reflects winner states and locks controls until next valid user action.

---

## 🧩 Outstanding Issues / Known Bugs

- **Extra BYEs still appearing** under some LB conditions (low driver count, non-power-of-two brackets).
  - Needs deeper validation in `LosersBracketBuilder.cs`.

- **UI state drift after long sessions**:
  - Some late-round buttons remain active visually, despite being disabled in logic.
  - May need a centralized `DisableWinnerButtons()` utility.

---

## 📌 Next Steps

1. **Fix BYE Overpopulation Bug**
   - Patch logic in `LosersBracketBuilder.Build(...)` to avoid generating ghost matches when player counts are low or uneven.

2. **Centralize Round Completion Checks**
   - Add a shared method like `IsCurrentRoundComplete()` to reduce redundant `GetMatches().Where(...).Any(...)` code.

3. **Improve Final-4 Injection**
   - Add clearer UI transition from LB winner → Pro Ladder finals injection.

4. **Enhance Session Persistence (planned)**
   - Save/load state structure: driver list, round progress, and win/loss history.

5. **Add Developer Logging (optional)**
   - Show internal bracket creation output for debugging future bugs.

---

*Last Updated: June 24, 2025*

-------------------------------------
RC Drag Manager – Refactor Work Completed in This Session
(feature/refactor-bracket-controller branch)

1 New Logic-Layer Files
File	Purpose
RaceEngines/IRaceEngine.cs	Single contract every bracket engine implements – pure domain logic.
RaceEngines/ProLadderEngineAdapter.cs	Wraps existing MatchEngine and exposes it through IRaceEngine.
RaceEngines/RaceEngineFactory.cs	Switchboard that returns the correct adapter for a race-type string.
ViewModels/PairingRow.cs	DTO for bracket ListView rows (headers & pairings).
ViewModels/WinnerRow.cs	DTO for winners ListView rows.
Controllers/RaceController.cs	Central state/control class – owns RaceSession, IRaceEngine, events, and all race-flow logic.

(All committed & pushed.)

2 Program & Entry Forms
Program.cs – now creates a blank RaceSession, builds a RaceController, and passes it to new Form1(controller).

LandingPageForm.cs – both the Create and Load paths instantiate a RaceController with the chosen/loaded RaceSession and pass it to Form1.

3 Form1 Refactor
Change	Details
Constructor	Accepts RaceController; stores it; uses _controller.Session in place of the old session param.
Event wiring	• BracketRedrawn → RedrawFullBracket()
• NextMatchReady → updates lblNext, winner-buttons text/tag/enabled
• WinnersUpdated → rebuilds lvWinners
• CanAdvanceChanged → enables btnNextRound
• CanPickWinnerChanged → enables btnWinner1/2
Generate button	Old in-form bracket logic removed; now calls _controller.GenerateBracket(raceType, drivers).
Winner buttons	Call _controller.SubmitWinner(matchId, firstOption).
Next-Round button	Calls _controller.AdvanceRound().
Obsolete helpers	ProcessMatchWinner, in-form engine instances, pairing history logic now redundant (not yet deleted but unused).

4 Git History
Add logic layer & controller.

Wire Program / LandingPage / Form1 to controller.

Hook up UI events & buttons.

Commit after each milestone.
Branch pushed: feature/refactor-bracket-controller.

5 Build / Runtime Status
Build: solution compiles with 0 errors / 0 warnings.

Run-time: Quick Session and loaded sessions can run Pro-Ladder brackets end-to-end:

Add drivers → Generate Bracket.

Pick winners until Next Round enables → advance.

Repeat to final; winners list populates correctly.

Controller events keep UI elements (bracket, next-up, winners, button states) in sync.

6 Outstanding (Future) Work
Area	To-do
Other engines	Implement RandomEngineAdapter, RoundRobinEngineAdapter, update factory.
Losers-Bracket flow	Move existing quick-draw logic into a dedicated adapter or controller extension.
Persistence	Flesh out RaceController.SaveSession() and any repository layer.
Clean-up	Delete unused helper methods/fields in Form1 (old engine refs, pairing history, etc.).
Unit tests	Add tests for each adapter and for RaceController state transitions.
Docs	Update architecture and developer docs with new files & flow diagrams.

7 Suggested Immediate Tests
Pro-Ladder sanity – run 4-, 8-, 16-driver brackets; confirm bracket generation, round progression, and final winner.

UI state – verify buttons are correctly enabled/disabled at each step.

Quick vs. Loaded sessions – ensure both paths behave identically with the new controller.


---------------------------------------------------------------------

✅ Feature: Major Bracket Logic Refactor (feature/refactor-2.0)
Purpose:
Bring all bracket engines (Pro Ladder, Random Draw, Round Robin) under a consistent architecture with clear, reusable adapters. This unifies the race session logic, improves maintainability, and removes duplication across different bracket types.

🗂️ Key Changes:
Pro Ladder Engine:

Confirmed working as the stable base.

Verified it runs cleanly through full bracket rounds with BYEs handled correctly.

Winner buttons auto-disable when facing a BYE.

Random Draw Mode:

Fully separated its logic into RandomMatchEngine.

Added RandomEngineAdapter to comply with the IRaceEngine interface.

Added missing methods (LoadDrivers, GenerateBracket, Reset, GetMatches, SetWinner, HasWinner) to connect properly.

Verified core logic produces correct pairings and respects no-rematch constraints.

Round Robin Mode:

Fully rebuilt with:

RoundRobinEngine.cs — main engine: generates 3 rounds using the circle method.

RoundRobinRanker.cs — ranks drivers with points, wins, opponent strength, and head-to-head.

RoundRobinMatch.cs — simple DTO to represent results.

Removed unnecessary RoundRobinMatchResult.cs file to prevent confusion.

Renamed file for clarity: RoundRobinMatch.cs only stores match details (IDs, winner, loser, round).

Implemented RoundRobinEngineAdapter matching the IRaceEngine contract.

Shared Interfaces:

Updated IRaceEngine to handle all engines consistently:

LoadDrivers()

GenerateBracket()

Reset()

GetMatches()

GetRoundOrder()

SetWinner()

HasWinner()

Added EngineMatch DTO to unify match data structure.

UI & Controller Hooks:

Confirmed the MatchController correctly logs which engine is used (RaceEngineFactory debug statements).

Improved winner buttons to auto-disable for BYEs.

Made sure btnNextRound is only enabled when the current round is fully resolved.

Git & Branch:

Renamed RoundRobinMatchResult.cs → RoundRobinMatch.cs and cleaned up leftover references.

Added/removed files properly.

Committed changes as feature/refactor-2.0.

Set remote upstream and pushed the branch.

Ready for PR merge to main.

📝 Known Limitations / Next Steps:
Round Robin needs real-world test runs to verify all 3 rounds generate correctly and rank accurately.

Add more robust logging and unit tests for bracket engines.

Confirm Quick Session uses the selected bracket type from the race type dropdown.

Future features:

Persistent storage for session save/load.

Export results to CSV/PDF.

Statistics tracking for drivers.

📌 Outcome:
Pro Ladder, Random, and Round Robin now share a unified, modular bracket structure.

BYE handling and match resolution are consistent across all modes.

The entire bracket engine layer is now testable, reusable, and ready for new session features.

Feature Branch: feature/refactor-2.0
Ready for PR: ✅

Stewart McMillan — RC Drag Manager
2025-07-07

-------------------------------------------------------------------------------------

🔧 Branch: feature/save-session-final4

✅ Added full SaveSession() logic for all race modes:
- Pro Ladder
- Randomized Bracket
- Round Robin (R1–R3 match history, driver stats)

✅ Integrated Final-4 logic:
- Preserves Round Robin Top 3
- Captures Losers Bracket results
- Reconstructs Pro Ladder semifinals with re-seeded top 4

✅ Extended RaceSession serialization:
- Stores all match results, revealed rounds, driver entries, pairing history

✅ RaceController:
- SaveSession() pulls final results from correct engine adapter
- Supports RaceType transitions (e.g., RR → Final-4)

✅ UI confirmed stable:
- Form1 correctly disables buttons post-final
- Session can be saved mid or post event without error

✅ All code tested and merged

------------------------------------------------------------------------------
✅ Dev Log Update — Logging System Integration
Feature: feature/logging-system
Date: 2025-08-03
Context: Infrastructure Improvement

🎯 Goal
Implement a configurable logging system that saves logs to a known location for debugging and audit purposes.

🛠️ Work Completed
Area	Details
Logger Class	New Logger static utility class added in RCDragManagerProd.
• Reads settings from App.config.	
• Logs messages only if EnableLogging=true.	
• Creates target directory if missing.	
• Appends timestamped log lines to specified file.	
App.config	Added two keys under <appSettings>:
• EnableLogging = true	
• LogFilePath = %APPDATA%\RC_Drag_Manager\app.log (auto-expanded in code)	
Path Expansion	Custom logic handles %APPDATA% token in .config. Resolves to full roaming path on any system.
Form1.cs	Call to Logger.Log("🔥 Logging system initialized") added in constructor to confirm init.

📁 Result
Logs now saved to:
C:\Users\<YourUser>\AppData\Roaming\RC_Drag_Manager\app.log
------------------------------------------------------------------------------
✅ Dev Log Summary – Round Robin Buyback Refactor
📅 Date: 2025-08-04
🔁 Feature Branch: feature/roundrobin-buyback-restore

🧠 Problem
After completing all 3 rounds in Round Robin mode, no progression or buyback prompt was shown.

Previous logic for Buyback Phase was removed during Form1/UI refactor.

Missing features:

No “Generate Losers Bracket” button.

No buyback driver selection popup.

No promotion to Pro Ladder after RR standings.

✅ Work Completed
Confirmed current RoundRobinEngine & Adapter work correctly.

Verified RoundRobinRanker is now in charge of standings & tiebreak logic.

Reinstated PushAdvanceState() and ensured it fires on winner selection.

Located missing buyback entry point:

PushNextMatch() correctly ends RR but did not emit buyback trigger.

Proposed and outlined full reimplementation of Buyback Phase, including:

UI Button: btnGenerateLosersBracket

Event: CanOfferBuybackChanged

Dialog: BuybackSelectionDialog (not yet coded)

Controller logic: GetEligibleBuybackDrivers() + GenerateLosersBracket()

Enabled logging at key points (match submission, standings, bracket generation).

🧪 Pending Tasks
 Implement and wire BuybackSelectionDialog UI.

 Add btnGenerateLosersBracket to Form1.Designer.cs.

 Final controller integration + test.

 Confirm buyback → ladder flow works with 2–4 drivers.

🧷 Notes
All logic stays modular.

Pro Ladder engine reused after RR.

No database dependencies in this phase.
------------------------------------------------------------------------------
Dev-Log Summary — 2025-08-08
Topic: Fix compile errors and complete Round-Robin → Losers-Bracket flow

1. Compile-time fixes
File	Change
RandomEngineAdapter.cs	• Added InjectMatches(List<RandomMatch>)
• Added default & param ctors
• Made _engine readonly field (no inline new)
• Injected concise logging
RaceController.cs	• Field _losersEngine now IRaceEngine
• New field _selectedDrivers (buy-back list)
• GenerateLosersBracket() now:  • builds adapter, calls InjectMatches, sets _inLosersPhase  • stores _selectedDrivers, fires PushNextMatch()  • logging refined
Form1.cs	• btnGenerateLosersBracket_Click now disables button on first click and forwards selectedDrivers to controller
• Constructor: subscribes to CanOfferBuybackChanged to enable the LB button; sets initial btnGenerateLosersBracket.Enabled = false
PushAdvanceState()	• Buy-back trigger guarded by !_inLosersPhase and RoundRobinEngineAdapter check
LosersBracketEngine.cs	• _rng static; logging improved

2. Buy-back eligibility
GetEligibleBuybackDrivers() now uses RoundRobinEngineAdapter.GetStandings() + GetTopRankedDrivers(3) (no session string dependency).

3. UI behaviour
LB button enabled only once all RR matches resolved, disabled immediately after click.

First LB pairing auto-pushed to UI; “Next Round” button now activates correctly.

4. Logging
Added granular [LB], 🔁, and UI: log entries for bracket generation, injection counts, first-match push, and button state.

Status: Build clean, Round-Robin → Buy-back → Losers-Bracket flow functional; finals phase next on roadmap.
------------------------------------------------------------------------------
To get everything compiling and wire up the Round-Robin → Buyback → Losers-Bracket flow end-to-end, you’ll need changes in four places:

RandomEngineAdapter.cs

Add an InjectMatches(List<RandomMatch> matches) method on your adapter so it can seed in the pre-built losers-bracket matches.

Provide both a default ctor (for “new” LB builds) and a ctor that accepts an existing RandomMatchEngine, and make your _engine field readonly.

Sprinkle in your [LB]-style log calls around the injection and match resolution.

RaceController.cs

Change your _losersEngine field to type IRaceEngine.

Add a List<Driver> _selectedDrivers to hold the buy-back picks.

In GenerateLosersBracket(...) do:

csharp
Copy
Edit
var adapter = new RandomEngineAdapter();
adapter.LoadDrivers(_selectedDrivers);
adapter.InjectMatches(builtLbMatches);
_inLosersPhase = true;
_losersEngine = adapter;
PushNextMatch(); // kick off the first LB pairing
Fire your CanOfferBuybackChanged and BracketRedrawn events at the right spots.

Form1.cs

Wire up btnGenerateLosersBracket_Click to call controller.GenerateLosersBracket(...), then immediately disable the button so you can’t re-trigger it.

In your form’s constructor subscribe to the controller’s CanOfferBuybackChanged event and do btnGenerateLosersBracket.Enabled = e;.

Initialize btnGenerateLosersBracket.Enabled = false; so it only comes alive once Round Robin is fully resolved.

LosersBracketEngine.cs

Make sure any RNG you use is a static readonly Random _rng = new Random(); so you don’t get threading or repeat-seed problems.

Add logging around match build so you see “🔒 Stored X LB drivers” and “📦 Starting Losers Bracket generation…”.

With those in place, your compile errors will vanish and the full RR → buyback → LB flow will actually run. Would you like me to drop in the updated code for one of these files first (say, the adapter), or would you prefer a full diff across all four?
------------------------------------------------------------------------------
Dev-Log Entry — “Buy-back & Losers-Bracket Integration” (2025-08-08)
1. Compile-time fixes

RandomEngineAdapter.cs

Added two constructors (default & parameterized) to accept an injected RandomMatchEngine.

Changed _engine to a readonly field without inline instantiation.

Introduced InjectMatches(List<RandomMatch>) to wrap LoadMatches(...).

Sprinkled concise Logger.Log(...) calls in constructors and injection.

RaceController.cs

Changed _losersEngine field from RandomMatchEngine to IRaceEngine.

Added private field _selectedDrivers to persist buy-back selection.

Updated GenerateLosersBracket(...):

Stores selectedDrivers into _selectedDrivers.

Spins up RandomEngineAdapter, calls InjectMatches, sets _inLosersPhase.

Fires PushNextMatch() immediately after bracket redraw.

Enhanced logging around each major step.

LosersBracketEngine.cs

Made the internal RNG static so the static RunBracket method compiles.

Retained detailed round-by-round and champion logging.

RoundRobinEngineAdapter.cs

Confirmed availability of GetStandings() and GetTopRankedDrivers(int).

No structural changes; used its API to power eligibility logic.

2. Buy-back eligibility & dialog

RaceController.GetEligibleBuybackDrivers()

Dropped the fragile session-string check; keyed off _engine is RoundRobinEngineAdapter.

Retrieved all drivers via GetStandings() + selected top-3 via GetTopRankedDrivers(3).

Logged total count and names of eligible drivers.

3. UI wiring and button flow

Form1.cs

Initialized btnGenerateLosersBracket.Enabled = false on form load.

Subscribed to _controller.CanOfferBuybackChanged (inline lambda) to enable the buy-back button.

In btnGenerateLosersBracket_Click: disabled the button immediately, invoked the buy-back dialog, and passed selectedDrivers to the controller.

PushAdvanceState()

Wrapped the Round-Robin completion trigger in !_inLosersPhase && _engine is RoundRobinEngineAdapter.

Ensured CanOfferBuybackChanged fires only once, once all RR matches resolve.

Post-buyback LB flow

Controller now pushes the first LB match via PushNextMatch() and logs it.

btnNextRound and winner buttons activate correctly for Losers-Bracket rounds.
------------------------------------------------------------------------------
 Branch: feature/losers-bracket-plumbing
Date: 2025-08-08

🛠️ What We Fixed
Compile errors in RaceController

Changed the _losersEngine field from RandomMatchEngine to the shared IRaceEngine interface to eliminate implicit-conversion errors (CS0266).

Updated all assignments so that RandomMatchEngine is wrapped or cast to IRaceEngine (via a new adapter or explicit cast).

Missing methods on RandomMatchEngine

Added a SetExternalMatches(List<RandomMatch>) API so the engine can accept the bracket built by LosersBracketBuilder.Build(...).

Ensured the engine exposes RunBracket(...) via either the LosersBracketEngine or a properly-typed helper.

Scope and naming fixes in Form1

Replaced the nonexistent GetSelectedDrivers() call on the buy-back dialog with its actual SelectedDrivers property.

Fixed uses of eligibleDrivers and selectedDrivers so they’re in-scope and correctly typed.

RaceController.GenerateLosersBracket overhaul

Sanity checks for selectedDrivers count.

Built the new bracket via:

csharp
Copy
Edit
var lbMatches = LosersBracketBuilder.Build(
    selectedDrivers,
    _session.PairingHistory,
    startMatchId:1000
);
Spun up a RandomMatchEngine, injected lbMatches with SetExternalMatches(...), then set _engine = _losersEngine.

Reset UI state: cleared _revealedRounds, added "Losers Bracket R1", and fired BracketRedrawn with BuildCurrentBracketRows().

Added logging at each step (Logger.Log($"…")) to trace flow.

Bridging UI ↔ Controller

In Form1, wired btnGenerateLosersBracket_Click to:

Show BuybackDriverSelectionForm(eligibleDrivers)

Disable the button on first click

Pass dlg.SelectedDrivers into RaceController.GenerateLosersBracket(...)

Row building stays engine-agnostic

BuildCurrentBracketRows() uses the common IRaceEngine.GetMatches() and filters by _revealedRounds.

🚀 Outcome & Next Steps
The Losers-Bracket pipeline now compiles cleanly and integrates end-to-end: session history → bracket builder → engine injection → UI redraw.

Logging at every major action makes it easy to trace bracket generation, engine switching and UI updates.

Next: tie in RunBracket(...) calls or adapter so the bracket actually runs via LosersBracketEngine, and then wire up the “Generate Next Round” button for the new bracket mode.
------------------------------------------------------------------------------
🧩 Feature: Final-4 Race Flow Fix + UI Polish Prep
Branch:

feature/quick-session-edge-cases (✅ completed)

feature/ui-enhancements (🚧 in progress)

✅ Work Completed in This Chat:
🏁 Round Robin → Losers Bracket → Final-4 flow (fully working)
Captured Top-3 from Round Robin using _rrTop3 before engine swap

Injected Losers Bracket with eligible drivers via RandomEngineAdapter

Added .GetWinner() to extract LB champion from final match

Patched to accept any match with "final" in the label

Injected new Pro Ladder Final-4 bracket with correct drivers

Triggered bracket redraw with "SF" round

All race engines confirmed to interoperate correctly

📦 Logging Enhancements
Added detailed Logger.Log() output throughout:

Top-3 capture

LB matches injected

Final-4 injection

Winner extraction

Round transitions

Debug visibility now present at all major race transitions

🚧 New Feature Started: UI/UX Cleanup (feature/ui-enhancements)
Purpose: polish bracket rendering, round headers, user messages, and button flow

Identified initial issue:
🖼️ Final-4 bracket shows 0 rows due to BuildCurrentBracketRows() filtering bug

Planned:

Fix SF/F round redraw bug

Add end-of-round and end-of-race feedback

Improve bracket round labels and user clarity

Log every UI transition and state change

------------------------------------------------------------------------------
Dev Log Summary — UI & UX Polishing Phase (Chat: Final-4 UI Fixes & Flow)
📦 Feature Branch
feature/ui-enhancements

✅ Work Completed
🖼️ Final-4 Bracket Display Fixed
Updated ProLadder.cs → GetLadder4() to use correct round labels:
"SF" instead of "R1" for Final-4 semi-finals.

Verified bracket rendering works with revealedRounds = { "SF", "F" }.

Logged match trace output from Final-4 generation.

🧠 Match Logging + Debug Tracing
Added full logging to BuildCurrentBracketRows():

Skips for hidden/missing rounds

Match tracing with Driver1/2, RoundLabel, HasResult

Header row and pairing row logging

Verified app.log shows accurate flow from Round Robin → LB → Finals.

🏁 Verified Full Race Progression (8 Drivers)
Round Robin:

All 3 rounds logged with wins for Drivers 8, 5, 3

Losers Bracket:

4 drivers via buyback → Driver 4 wins

Final-4:

Finalists: 1, 2, 3 (RR) + 4 (LB winner)

Final Result: Driver 4 defeats Driver 3 in Match 3 (F)

🔄 Final Bracket Rendering Validated
Confirmed correct number of rows: 2 headers + 3 matches = 5 rows.

Confirmed all round transitions logged and visible.

Final UI displayed Driver 4 as overall winner.

🧪 Identified Next Tasks
Improve Generate Bracket button state flow across RR, LB, Finals.

Add popup alerts for race director at key phase transitions:

After RR complete

After LB winner selected

After Finals conclude
------------------------------------------------------------------------------
Dev Log – Buybacks Flow & Losers Bracket Start Logic

Updated btnGenerateLosersBracket_Click in Form1.cs to:

Rename button text to "Buybacks".

Only open the driver selection dialog and store selected drivers — no automatic race start.

Added detailed logging for eligibility, selection, and storage.

Enabled Generate Bracket button after storing buybacks.

Added SetBuybackDrivers(List<Driver>) method in RaceController.cs to store selected buyback drivers in the session for later use.

Began restructuring Generate Bracket flow so that:

Clicking Generate Bracket after buybacks triggers Losers Bracket instead of restarting Round Robin.

Added IsInLosersBracketPhase property to RaceController.cs for phase detection.

Drafted StartLosersBracket() method in RaceController.cs to build matches from stored buybacks and switch engine to RandomEngineAdapter.

Encountered compilation issues due to:

Missing BuybackDrivers and TopDriversSnapshot properties in RaceSession.cs.

Nonexistent SetExternalMatches() method in RandomEngineAdapter (replaced with LoadDrivers() + InjectMatches()).

Event/method name mismatches (RedrawFullBracket → BracketRedrawn).

Missing GenerateBracket(string) overload in RaceController.cs.

Added BuybackDrivers and TopDriversSnapshot properties to RaceSession.cs.

Created wrapper GenerateBracket(string) method in RaceController.cs to call the 2-argument version using session drivers.

Found that _session.Drivers was never set, causing “session driver list is invalid” log message — identified need to assign driver list during setup.

Status:
Buybacks dialog works without auto-starting race. Generate Bracket button re-enabled after buyback selection. Losers Bracket start wiring in progress but currently blocked by driver list assignment and session state handoff.
------------------------------------------------------------------------------
RC Drag Manager — Dev Log (Finals/LB gating, UI lists, scoring)
Date: 2025-08-10 (AEST)
Author: Stewart + assistant pairing

Flow & Gating
Added finals gate: finals no longer auto-inject on LB completion.

RaceController.cs: CanStartFinalsChanged event, _finalsPending flag, IsFinalsPending prop.

Form1.cs: enables Generate Bracket and shows “Finals Ready” popup when LB ends.

Finals start only when Generate Bracket is pressed.

RaceController.cs: StartFinals() calls InjectFinal4Bracket() and drops gate.

Finals reveal sequencing fixed:

InjectFinal4Bracket() now reveals SF only; F is revealed after Generate Next Round.

Preserves revealed Losers rounds when injecting Finals (no list reset).

Losers Bracket (LB)
Start LB shows R1 immediately and pushes first pairing.

RaceController.cs: StartLosersBracket() loads drivers, injects matches, sets _inLosersPhase, reveals "Losers Bracket R1".

LB builder robustness:

Avoid BYE-vs-BYE in R1; no infinite loops; correct odd-carry to next round so LB Final always exists.

LosersBracketBuilder.cs: rebuilt Build(...) with paired iteration, odd-carry BYE match, consolidated id/r1 lists.

LB champion retrieval hardened:

RandomEngineAdapter.GetWinner() falls back to last round by order if “final” label not found.

Unified UI Lists (left/right panes)
Current Round Pairings now shows all phases (RR → LB → Finals) with continuous M#:

Snapshot RR matches/order at RR completion (and fallback snapshot in StartLosersBracket()).

RaceController.cs: BuildCurrentBracketRows() aggregates RR snapshot, LB engine, and Finals engine; assigns MatchNumber = M1..M*.

PairingRow gained MatchNumber; Form1.RedrawFullBracket() uses it (with headers + logging).

Generate Next Round redraws go through the unified builder:

RaceController.cs: replaced AdvanceRound() to always BuildCurrentBracketRows() → BracketRedrawn.

Match Winners ordering fixed & numbered continuously:

Form1.cs: new WinnersUpdated handler with global sort helper GetGlobalRoundOrder().

Explicit ranking: RR R1..Rn (100+n) → LB R1..Rn (200+n) → LB Final (299) → SF (990) → F (1000).

Buyback Eligibility
Corrected eligible list to be all RR entrants minus Top-3 (not just those appearing in standings).

RaceController.cs: GetEligibleBuybackDrivers() derives roster from RR matches; logs roster, Top-3, eligible names.

Sanity: 10 entries → 7 eligible (verified in logs).

Finals Completion & UX
Added tournament completion event and simple OK-only popup (no auto reset/close).

RaceController.cs: RaceSummary DTO, TournamentCompleted event, summary emission in PushAdvanceState() when Final resolved.

Form1.cs: popup shows Event/Bracket/Winner/Runner-up/Matches; leaves session intact.

Added safe Reset() that clears engines/flags/UI when used manually.

Made GetMatch(int) null-safe after reset to prevent NRE.

Round Robin Scoring (auditability)
Exposed shared points schedule for R1/R2/R3:

RoundRobinRanker.cs: public static PointsForRound(string) and legacy GetPoints() delegating to it (with logging of schedule/unknown labels).

Added clear W-L scoreboard at RR completion:

RaceController.cs: LogRoundRobinScoreboard(rr) (names + W-L).

(Prepared) Detailed per-round/per-match scorecard helper (ready to enable next):

RaceController.cs: LogRoundRobinScorecardDetailed(rr) prints per-match points, round subtotals, final totals (not always on by default).

Logging & Diagnostics
Button state changes logged (Generate Bracket / Next Round / Generate Losers Bracket).

Lifecycle snapshots at key transitions: LB pre/post swap, finals gating, revealed rounds, row builds.

[ROWS] BUILD v2 entry logs active engines, snapshot counts, and revealed rounds every redraw.

LB builder logs R1 pairings, odd-carry creation, and total match counts.

Finals inject logs all generated matches with driver names.

Known Follow-ups / Nice-to-haves
Ensure btnNextRound is wired once (avoid duplicate “AdvanceRound completed” logs if double-subscribed).

Optionally enable the detailed RR scorecard at RR completion (per-round subtotals) for race-day clarity.

Integrate MatchResult fully to eliminate any “Winner Mx” placeholders in legacy paths.

Quick Acceptance (verified)
RR → Buyback (correct eligible count) → LB (R1..Final) → Finals gate → SF → Next Round → Final → OK popup.

Left Current Round Pairings lists all rounds continuously, even after LB/Finals transitions.

Right Match Winners ordered RR → LB R1..Final → SF → F with continuous M#.

No freezes on LB start; no auto-starting Finals; no Finals without user gating.
------------------------------------------------------------------------------
2025-08-12 — UI/UX + Round-Robin scoring + Random mode fixes
Features
Round-Robin score popup: Added RoundRobinScorecardLogger (new file) and wired it to show at RR completion. Includes per-round lines and a composite “Score = Pts + Wins×0.01 + H2H×0.001 + SoS×0.000001” so ties are numerically clear.

In-app tie clarity: Popup rows now include driver names and show (Pts, W, H2H, SoS) for transparency.

Auto finals when no Buyback: If <2 eligible Buyback drivers (e.g., 4 racers), controller skips LB and injects Finals with the “wildcard” 4th. User is notified.

Controller (RaceController.cs)
PushAdvanceState():

Logs RR standings, shows popup, snapshots RR, then:

If ≥2 Buyback eligible → enable Buyback button.

Else → auto-advance with wildcard and inject Final-4.

InjectFinal4Bracket(): Cleaned up; supports wildcard when LB absent; preserves LB rounds in left panel; reveals only SF initially.

BuildCurrentBracketRows():

Unified renderer now supports RandomEngineAdapter rounds (R2/R3 stay visible).

Fix for “self-match” display in LB Final: if engine collapses to champ vs champ, we recover (loser, winner) from MatchResult for display.

PushFullRefresh(): Uses unified builder (not the old BuildPairingRows()).

SubmitWinner(): Logs per-round RR scoring once a round fully resolves.

SaveSession(): Hardened (null-safe engines, results, revealed rounds). Writes only resolved/recorded matches; logs summary.

Default raceType: If UI passes blank, default to Round Robin and log.

Round-Robin ranking (RoundRobinMode/RoundRobinRanker.cs)
Loser derivation: If only a winner is stored (non-BYE), infer loser from the pairing.

SoS fix: Strength-of-Schedule sums final points of actual opponents and only for resolved matches.

Logging: [RR-PTS], [RR-OS], and pre/post sort tables for auditability.

Random mode (RaceEngines/RandomEngineAdapter.cs)
BYE fairness: Audits every round and redistributes BYEs so:

BYE goes to drivers with the fewest prior BYEs.

Avoids back-to-back BYEs for the same driver when possible.

Swaps recipients within the round as needed; detailed [RND-BYE] logs.

Next-round builder: Fair BYE selection in GenerateNextRoundFair() (for controller use).

GetWinner(): Replaced ^1 with Count-1 for legacy C# compatibility.

UI/UX
Current Round Pairings stays populated for Random mode across R2/R3.

Name visibility: Scorecard lines show driver names (not just IDs).

Finals gating: Finals button only enabled when LB completes or when we auto-advance.

Bugs fixed
Duplicate BYEs to the same driver across rounds (Random) → fixed with fairness audit.

LB Final displayed as “X vs X” → fixed by expanding from MatchResult.

Null-ref on Save & Close when engines/state were cleared → fixed.

Files touched
Controllers/RaceController.cs

RoundRobinMode/RoundRobinRanker.cs

RoundRobinMode/RoundRobinScorecardLogger.cs (new)

RaceEngines/RandomEngineAdapter.cs

RaceEngines/RoundRobinEngineAdapter.cs (minor wiring)

ViewModels/PairingRow.cs (display support)

MatchResult.cs, RaceSession.cs, Form1*.cs (wiring + UI)

Docs/_PROJECT_STATUS_DEVLOG_FULL.md (updated)

Notes / Next
Optional: expose and call GenerateNextRoundFair() from the Random “Generate Next Round” UI handler (if not already).

If desired, re-run BYE audit on each round reveal to keep fairness bullet-proof after edits/imports.

------------------------------------------------------------------------------
Repo + base project

Full repo recovery and cleanup finished; main stabilized, designers re-linked, namespace unified to RCDragManagerProd, and remote set.

Architecture + folder layout documented (UI, engines, repositories, domain).

Core features in place

Driver + car management, unified “Add driver & car” dialog, SQLite persistence, session setup (event name/date/type), and NHRA Pro Ladder engine (3–10 drivers) are all marked ✅.

Session creation / setup

Built out the entire SessionSetupForm: event details, class selection (Heads Up, Dial, Index), roster building, and live filtering; creates a RaceSession wired with correct DriverEntry objects.

Form1 workflow and race engine control

Race flow was rebuilt to be fully manual NHRA style: old auto-advance logic removed, BYEs kept but require manual advance, “Generate Next Round” now the only way to move rounds, and match results are stored manually.

UI tweaks branch: added “Set Qual Time” below the driver list; fixed “Edit Driver” wiring; standardized Form1 size; aligned bottom controls; tightened “Generate Next Round” enable/disable rules; and clarified Save/Close in Quick Session.

Pro Ladder expansion

NHRA Pro Ladder expanded from 11 to 16 cars with correct seed/match mapping and round labels (R1, R2, SF, F). Compatible with existing save/load.

Save/Load system hardening

Session table auto-created when missing; insert/update flows fixed; debug tooling added; several save/load bugs identified and resolved. Outcome: save/load/delete now “100% stable.”

Runtime signals in logs (examples)

Bracket/UI rebuilds and finals completion are logged (headers/rows added, winner/runner-up, etc.), confirming the wiring during play.

Engine selection and bracket generation for Round Robin / Random Draw are logged during session runs.
------------------------------------------------------------------------------
Branch / scope

Branch: feature/installer-stabilization (pushed)

Commit: 9d8ca13 + .gitignore commit 7973960

Installer (Inno) – final

Fixed arch line (x86 instead of ia32).

Switched to per-user install: DefaultDirName={localappdata}\Programs\RC Drag Manager, PrivilegesRequired=lowest.

Shortcuts use WorkingDir {app}, first-run uses runasoriginaluser.

Desktop shortcut moved to {userdesktop} to avoid UAC/write errors.

Kept %APPDATA%\RC_Drag_Manager dir creation for logs.

App boot + DB

Program.cs:

Builds absolute %APPDATA%\RC_Drag_Manager\race_data.db.

Exposes Program.ConnectionString.

Global exception handlers + fatal message.

DatabaseInitializer.cs:

Ensures schema for Drivers, Cars, RaceSessions (fix for “no such table: RaceSessions”).

Repositories:

DriverRepository + RaceSessionRepository accept either a full connection string or a file path; normalize to Data Source=...;Version=3;.

Implemented SaveSession(session); added GetAllSessions, LoadSession, DeleteSession.

Models / UI wiring

Added RaceSessionSummary (for session list).

LoadSessionForm:

Uses connection string; robust list rebuild; logging; guarded handlers.

LandingPageForm:

Accepts conn string; wires repositories once; launches forms cleanly.

Logging

App.config points to %APPDATA%\RC_Drag_Manager\app.log.

Added consistent repo/UI logging lines for startup, CRUD, errors.

Git hygiene

Expanded .gitignore (VS, Installer/Payload, Installer/output, logs, DB, binaries).

Purged already-tracked payload/output from index.

Feature branch pushed; ready for PR.

Result

Installer builds cleanly, installs without admin, creates per-user shortcuts, launches app.

App uses a writable SQLite DB and logs; sessions can be saved/loaded; schema auto-ensured.

Build errors from missing types/tables resolved.
------------------------------------------------------------------------------
Dev Log — UI cleanup (Form1) — 2025-08-16

Goal

Stop Form1 and Designer “fighting”. Make Designer the single source of truth for UI. Keep logic + logging in Form1.cs only.

What changed

Designer ownership restored

All controls, columns, layout, anchors, sizes, fonts, event hookups moved/kept in Form1.Designer.cs.

Fixed form canvas: AutoScaleMode=None, ClientSize=1200x600, FixedSingle, no maximize.

Form1.cs trimmed to logic

Removed runtime layout code (ApplyLayout14InchGrid, FixAnchors14, DPI tweaks, column width fiddling, resize handlers).

Removed any control instantiation or Controls.Add(...).

Kept only event handlers and controller wiring.

UI behavior (unchanged or improved)

Next match panel: sets winner buttons’ text/tags; auto-disables BYE side.

Pairings/Winners ListViews: Designer defines columns; code only rebuilds items (adds grey round headers + rows).

Buttons gating:

CanAdvanceChanged → enables “Generate Next Round”.

CanOfferBuybackChanged → enables “Buy Back” + info popup.

CanStartFinalsChanged → re-enables “Generate Bracket” for Finals + info popup.

Generate Bracket click flow:

Finals pending → starts Finals.

Losers Bracket phase → starts LB from stored buybacks.

Otherwise → generates initial bracket from cmbRaceType.

Session save/reset:

Reset clears lists/labels; re-enables Generate Bracket; restores race type when applicable.

Save writes driver entries + calls controller SaveSession(); persists via repository.

Logging

Kept and focused logs: bracket generation, BYE guards, winners list rebuild, button state changes, popups, results, errors.

How to edit UI now

Use Visual Studio Designer for all movement/size/font/anchors.

Fonts & sizes: select control → Properties → Font / Size (Form’s Font acts as base; controls can reset to inherit).

Files touched

Form1.cs: logic-only, no UI creation/layout.

Form1.Designer.cs: full UI initialization, layout, fonts, event hookups, fixed form size/scaling.
------------------------------------------------------------------------------

------------------------------------------------------------------------------

