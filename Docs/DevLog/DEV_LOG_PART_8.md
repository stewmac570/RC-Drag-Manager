---- DEV LOG PART 8 ----
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

Ensures schema for Drivers, Cars, RaceSessions (fix for â€œno such table: RaceSessionsâ€).

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
Dev Log â€” UI cleanup (Form1) â€” 2025-08-16

Goal

Stop Form1 and Designer â€œfightingâ€. Make Designer the single source of truth for UI. Keep logic + logging in Form1.cs only.

What changed

Designer ownership restored

All controls, columns, layout, anchors, sizes, fonts, event hookups moved/kept in Form1.Designer.cs.

Fixed form canvas: AutoScaleMode=None, ClientSize=1200x600, FixedSingle, no maximize.

Form1.cs trimmed to logic

Removed runtime layout code (ApplyLayout14InchGrid, FixAnchors14, DPI tweaks, column width fiddling, resize handlers).

Removed any control instantiation or Controls.Add(...).

Kept only event handlers and controller wiring.

UI behavior (unchanged or improved)

Next match panel: sets winner buttonsâ€™ text/tags; auto-disables BYE side.

Pairings/Winners ListViews: Designer defines columns; code only rebuilds items (adds grey round headers + rows).

Buttons gating:

CanAdvanceChanged â†’ enables â€œGenerate Next Roundâ€.

CanOfferBuybackChanged â†’ enables â€œBuy Backâ€ + info popup.

CanStartFinalsChanged â†’ re-enables â€œGenerate Bracketâ€ for Finals + info popup.

Generate Bracket click flow:

Finals pending â†’ starts Finals.

Losers Bracket phase â†’ starts LB from stored buybacks.

Otherwise â†’ generates initial bracket from cmbRaceType.

Session save/reset:

Reset clears lists/labels; re-enables Generate Bracket; restores race type when applicable.

Save writes driver entries + calls controller SaveSession(); persists via repository.

Logging

Kept and focused logs: bracket generation, BYE guards, winners list rebuild, button state changes, popups, results, errors.

How to edit UI now

Use Visual Studio Designer for all movement/size/font/anchors.

Fonts & sizes: select control â†’ Properties â†’ Font / Size (Formâ€™s Font acts as base; controls can reset to inherit).

Files touched

Form1.cs: logic-only, no UI creation/layout.

Form1.Designer.cs: full UI initialization, layout, fonts, event hookups, fixed form size/scaling.
------------------------------------------------------------------------------
RC Drag Manager â€” Dev Log (cleanup & fixes)
Repo / Structure

Standardized solution folders in project root:

Assets/, Config/, Controllers/, Domain/, Helpers/, Logging/, RaceEngines/, RandomMode/, RoundRobinMode/, Repositories/, UI/, Utils/, ViewModels/, Properties/.

Moved Docs â†’ docs/, Installer â†’ installer/.

Prepared plan to move whole project under src/RCDragManagerProd (new branch: feature/move-to-src).

Namespaces / Using

Unified WinForms to RCDragManagerProd.UI.Forms.

Fixed missing/ambiguous using across files (Domain, Repositories, Logging, RaceEngines, RoundRobinMode).

Resolved MatchResultSave ambiguity by using fully-qualified names (Domain vs ViewModels) where needed.

Assets / Resources

Created Helpers/AssetPath.cs to resolve Assets\... paths reliably.

Moved logos to Assets/ and patched Resources.resx references.

Added tracing in AssetPath via Logger.Log.

Logging + Settings

New Config/AppSettings.cs (persisted JSON in AppData).

Logger honors AppSettings.IsLoggingEnabled.

Default: Debug = ON, Release = OFF.

Program.cs: AppSettings.Load() on startup; fatal handler shows log path.

Database / Persistence

DatabaseInitializer added; ensures all tables (incl. RaceSessions) exist.

Called initializer at app start and in repositories opening connections.

Fixed Quick Session â€œno such table: RaceSessionsâ€ by creating table before save.

Session / Stats

Form1:

Winner buttons now call _controller.SubmitWinner(...), then UpdateDriverStats(winner, loser) with DB bump (Wins/Losses).

On TournamentCompleted: +EventsEntered for roster; +EventsWon for winner (with DB update + logging).

DriverManagerForm:

Details grid shows computed â€œEvents Wonâ€ from saved sessions (not stale DB field).

Added ComputeEventsWonFromHistory() using RaceSessionRepository + ladder map; works for Pro Ladder and Final-4 after Round Robin.

Compile / Runtime Fixes

Fixed ?? operator misuse (List vs IEnumerable) by normalizing repository returns to concrete lists.

Restored Logger symbol errors by centralizing Logging\Logger.cs and correct namespace imports.

Resolved missing type errors (Driver/Car/LadderMatch/Engine* etc.) after namespace consolidation.

UI Polish

Winners panel: grouped by round with headers; stable ordering (R1.., SF, F, LB rounds).

â€œEdit Resultâ€ picker dialog to change winners for active round only (BYE guarded).

Next-match label shows â€œOn Deck / In The Holeâ€ preview.

Git / Branches

Branch feature/structure-cleanup-1 committed & merged (folder tidy, fixes).

Next: create feature/move-to-src, move project under src/RCDragManagerProd, update solution, push PR.

Suggested commit titles already used / to use

repo: structure cleanup (folders + namespaces)

logging: settings-backed logger (Release OFF by default)

db: add DatabaseInitializer + ensure RaceSessions

ui: driver stats computed from saved sessions

ui: winners panel + edit result flow

repo: move project under src/RCDragManagerProd (next)
------------------------------------------------------------------------------

