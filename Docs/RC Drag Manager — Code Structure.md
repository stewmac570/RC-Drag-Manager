# RC Drag Manager — Code Structure (Current, ChatGPT Canonical)

> Purpose: This document is the **single source of truth** for ChatGPT about the RC Drag Manager codebase.  
> Audience: ChatGPT / GPT-based tools only (not engineer-facing).  
> Scope: High-level architecture, verified folder map, component responsibilities, and file→purpose matrix.

---

## How ChatGPT Should Use This Doc

- **Grounding:** Treat the folder map and file matrices in this doc as canonical.
- **Context order:** Prefer this file over older “Code Structure v1/v2/v3/v4” documents.
- **When unclear:** Ask for the specific file content; do not assume missing behavior.
- **Exclude build output:** Ignore `bin/`, `obj/`, `packages/`, `Installer/`.

---

## Repository Layout (authoritative)

Root level:
C:.
| .gitignore
| RCDragManagerProd.csproj.user
| RCDragManagerProd.sln
|
+---Docs
| ... (project documentation; not relevant to runtime)
|
+---Installer
| --- (Inno Setup scripts and payloads; not application logic)
|
+---packages
| --- (NuGet package caches; not application logic)
|
---src
---RCDragManagerProd
| App.config
| packages.config
| Program.cs
| RCDragManagerProd.csproj
| RCDragManagerProd.sln
|
+---Assets
| rcdrag_logo 2.ico
| Reto logo trans full 256.png
| Reto logo trans full.png
| retro trans icon.ico
|
+---Config
| AppSettings.cs
|
+---Controllers
| RaceController.cs
|
+---Domain
| Car.cs
| Drivers.cs
| MatchResult.cs
| ProLadder.cs
| RaceSession.cs
|
+---Helpers
| AssetPath.cs
| MatchLookupHelper.cs
|
+---Logging
| Logger.cs
|
+---RaceEngines
| IRaceEngine.cs
| MatchEngine.cs
| ProLadderEngineAdapter.cs
| RaceEngineFactory.cs
| RandomEngineAdapter.cs
| RoundRobinEngineAdapter.cs
|
+---RandomMode
| LosersBracketBuilder.cs
| LosersBracketEngine.cs
| RandomBracket.cs
| RandomMatch.cs
| RandomMatchEngine.cs
|
+---Repositories
| CarRepository.cs
| DatabaseInitializer.cs
| DriverRepository.cs
| RaceSessionRepository.cs
|
+---RoundRobinMode
| RoundRobinEngine.cs
| RoundRobinMatch.cs
| RoundRobinRanker.cs
| RoundRobinScorecardLogger.cs
|
+---UI
| ---Forms
| AddCarDialog.cs
| AddCarDialog.Designer.cs
| AddDriverAndCarDialog.cs
| AddDriverAndCarDialog.Designer.cs
| AddDriverDialog.cs
| AddDriverDialog.Designer.cs
| AddEditQualTimeDialog.cs
| AddEditQualTimeDialog.Designer.cs
| BuybackDriverSelectionForm.cs
| BuybackDriverSelectionForm.Designer.cs
| DriverManagerForm.cs
| DriverManagerForm.Designer.cs
| DriverManagerForm.resx
| DriverStatsForm.cs
| DriverStatsForm.Designer.cs
| EditDriverDialog.cs
| EditDriverDialog.Designer.cs
| EditWinnerDialog.cs
| EditWinnerDialog.Designer.cs
| Form1.cs
| Form1.Designer.cs
| Form1.resx
| LandingPageForm.cs
| LandingPageForm.Designer.cs
| LandingPageForm.resx
| LoadSessionForm.cs
| LoadSessionForm.Designer.cs
| LoadSessionForm.resx
| ScrollableTextDialog.cs
| SelectCarDialog.cs
| SelectCarDialog.Designer.cs
| SessionSetupForm.cs
| SessionSetupForm.Designer.cs
| SessionSetupForm.resx
| SettingsForm.cs
|
+---Utils
| DictEx.cs
|
---ViewModels
MatchResultSave.cs
PairingRow.cs
RaceSessionSummary.cs
WinnerRow.cs

markdown
Copy code

**Note:** `bin/` and `obj/` exist both at root and under `src/...` as build outputs; ignore them for architecture.

---

## Architectural Overview

- **Application type:** Windows Forms (.NET Framework 4.8), single-exe desktop app.
- **Primary namespace:** `RCDragManagerProd` (assumed for all top-level code).
- **Data store:** SQLite (bundled `System.Data.SQLite.dll`). The working DB is typically `race_data.db` (found in build output).
- **High-level layers:**
  - **UI/Forms:** Interaction surfaces for race setup, driver/car management, session control, and results.
  - **Controllers:** Orchestrate UI actions, call engines and repositories.
  - **Domain:** Core entities: `RaceSession`, `Drivers`, `Car`, `MatchResult`, `ProLadder`.
  - **RaceEngines:** Strategy layer; implementations for Randomized Bracket, Round Robin, and Pro Ladder via adapters to a shared `IRaceEngine` contract.
  - **Mode Modules:** `RandomMode/` & `RoundRobinMode/` hold mode-specific algorithms and helpers.
  - **Repositories:** SQLite CRUD and initialization.
  - **Helpers/Utils:** Glue code (paths, lookups, small utilities).
  - **Logging:** Simple logging abstraction.
  - **Config:** App settings binding.

---

## Startup & Core Flow

1. **Entry**: `Program.cs` → initializes WinForms app and launches main form(s).
2. **Session Setup**: `SessionSetupForm` + `RaceController`:
   - Load or create a `RaceSession` (via `RaceSessionRepository`).
   - Select mode (Random, Round Robin, Pro Ladder).
   - Engine factory picks engine: `RaceEngineFactory` → (`RandomEngineAdapter`, `RoundRobinEngineAdapter`, or `ProLadderEngineAdapter`) that all implement `IRaceEngine`.
3. **Data Access**:
   - `Repositories/*Repository.cs` handle persistence through SQLite.
   - `DatabaseInitializer.cs` ensures schema presence.
4. **Running Events**:
   - Engine produces matches/pairings (`RandomMatchEngine`, `RoundRobinEngine`, or `MatchEngine`/adapters).
   - UI forms (`StartRace`, `LoadSession`, dialogs) display and mutate state.
   - Results logged via `RoundRobinScorecardLogger` and persisted via repos.
5. **Results & Summaries**:
   - `ViewModels/*` capture summaries (`RaceSessionSummary`, `PairingRow`, `WinnerRow`, `MatchResultSave`) for UI and storage.
6. **Assets & Settings**:
   - Icons in `/Assets`.
   - `AppSettings.cs` binds/encapsulates config values.
   - `AssetPath.cs` resolves runtime asset/db paths.
7. **Logging**:
   - `Logging/Logger.cs` provides diagnostic/event logging (scope TBD by implementation).

---

## Component Responsibilities (by folder)

### Config
- **AppSettings.cs** — Centralized configuration model or accessors for runtime options.

### Controllers
- **RaceController.cs** — Orchestrates session lifecycle:
  - Bridges UI actions ↔ engines ↔ repositories.
  - Applies business rules for starting/continuing sessions.

### Domain
- **RaceSession.cs** — Aggregate root for a racing event/session.
- **Drivers.cs** — Driver entity (identity, stats, eligibility).
- **Car.cs** — Car entity (class, owner/driver link).
- **MatchResult.cs** — Single match outcome (winner, times, notes).
- **ProLadder.cs** — Domain structure for seeded “Pro Ladder” bracket.

### Helpers
- **AssetPath.cs** — Robust path resolution to assets/DB, safe for different run contexts.
- **MatchLookupHelper.cs** — Utilities to locate/derive match pairing relationships.

### Logging
- **Logger.cs** — Logging facility (file/console/trace), used by controllers and engines.

### RaceEngines
- **IRaceEngine.cs** — Common interface (methods like `GeneratePairings`, `RecordResult`, `NextRound`, etc.).
- **RaceEngineFactory.cs** — Returns engine implementation based on session mode.
- **MatchEngine.cs** — Shared logic for match generation/advancement (base/utility for adapters).
- **RandomEngineAdapter.cs** — Adapter aligning Random Mode classes with `IRaceEngine`.
- **RoundRobinEngineAdapter.cs** — Adapter aligning Round Robin with `IRaceEngine`.
- **ProLadderEngineAdapter.cs** — Adapter aligning Pro Ladder with `IRaceEngine`.

### RandomMode
- **RandomMatchEngine.cs** — Core algorithm to build rounds by randomization.
- **RandomMatch.cs** — Data structure for randomized pairings/matches.
- **RandomBracket.cs** — Manages bracket state for random mode.
- **LosersBracketEngine.cs** — Handles lower bracket logic (if double-elim semantics are present).
- **LosersBracketBuilder.cs** — Constructs losers bracket from current state.

### RoundRobinMode
- **RoundRobinEngine.cs** — Generates schedule/matrix ensuring each racer meets all others (as configured).
- **RoundRobinMatch.cs** — Represents a single RR match pairing.
- **RoundRobinRanker.cs** — Computes standings from results.
- **RoundRobinScorecardLogger.cs** — Writes scorecards/summary logs.

### Repositories (SQLite)
- **DatabaseInitializer.cs** — Creates tables/seeds sample data if needed.
- **CarRepository.cs** — CRUD for `Car`.
- **DriverRepository.cs** — CRUD for `Drivers`.
- **RaceSessionRepository.cs** — Persist/restore `RaceSession` with matches & results.

### UI/Forms
- **LandingPageForm** — Entry/landing surface.
- **SessionSetupForm** — Session configuration (mode, participants, rules).
- **LoadSessionForm** — Open prior sessions from SQLite.
- **Form1** — Legacy/main shell or utility form.
- **DriverManagerForm / DriverStatsForm** — Manage drivers and view stats.
- **Add*/Edit* dialogs** — CRUD dialogs for drivers/cars, qualifiers, winners, etc.
- **ScrollableTextDialog** — Utility window for long text.
- **SettingsForm** — App preferences.

### Utils
- **DictEx.cs** — Dictionary extensions and misc. helpers.

### ViewModels
- **RaceSessionSummary.cs** — Aggregated session summary for UI/export.
- **PairingRow.cs** — A row for pairing grids (UI binding).
- **WinnerRow.cs** — A row for winners/advancement (UI binding).
- **MatchResultSave.cs** — Serialization model for saving results.

---

## File → Purpose Matrix (quick reference)

| Path | Type | Purpose |
|------|------|---------|
| `Program.cs` | Entry | WinForms entry point; app bootstrap, initial form. |
| `Controllers/RaceController.cs` | Orchestration | Connects UI to engines and repos; session lifecycle. |
| `RaceEngines/IRaceEngine.cs` | Contract | Abstraction for all race modes. |
| `RaceEngines/RaceEngineFactory.cs` | Factory | Selects engine based on mode. |
| `RaceEngines/MatchEngine.cs` | Core Logic | Common helper/base for engine behaviors. |
| `RaceEngines/*Adapter.cs` | Adapter | Bridges specific mode engines to `IRaceEngine`. |
| `RandomMode/*` | Mode Logic | Randomized bracket + losers bracket mechanics. |
| `RoundRobinMode/*` | Mode Logic | Round-robin schedule, ranking, and logging. |
| `Domain/*` | Entities | Core domain (drivers, cars, sessions, results, ladders). |
| `Repositories/*Repository.cs` | Data | SQLite persistence for domain aggregates. |
| `Repositories/DatabaseInitializer.cs` | Data | Schema creation/migrations seed. |
| `Helpers/AssetPath.cs` | Infra | Path handling for assets & DB. |
| `Logging/Logger.cs` | Infra | Logging abstraction. |
| `UI/Forms/*` | UI | Windows Forms and dialogs for user workflows. |
| `ViewModels/*` | UI Model | Bindable models and save formats. |
| `Config/AppSettings.cs` | Config | Strongly-typed settings or configuration adapter. |
| `Utils/DictEx.cs` | Utility | General helpers & extensions. |

---

## Data Model & Storage Notes

- **Database**: SQLite; runtime file typically `race_data.db` (debug copy exists under `/bin/Debug`).
- **Repositories**: Encapsulate all SQL access; **do not** query DB from UI or engines directly.
- **Transactions**: If not present, future enhancement: wrap multi-step updates in transactions.
- **Migrations**: `DatabaseInitializer` is responsible for ensuring tables exist; if schema evolves, add idempotent upgrade code.

---

## Session Lifecycle (UI + Engine)

1. Create/Load session in **SessionSetupForm**.
2. **RaceController** instantiates engine via **RaceEngineFactory**.
3. Engine generates initial pairings (per mode).
4. UI records results via **RaceController** → **Repositories**.
5. Engine advances bracket/rounds until completion.
6. **ViewModels** produce summaries/exports; **Logger** records key steps.

---

## Mode Semantics Snapshot

- **Randomized Single Elimination**  
  - Uses `RandomMatchEngine`, `RandomBracket`, optional `LosersBracket*` for consolation/double-elim.
  - Seeding is randomized unless overridden by future features.

- **Round Robin**  
  - `RoundRobinEngine` builds match matrix; `RoundRobinRanker` computes standings by win/loss (and optionally time/points).
  - `RoundRobinScorecardLogger` persists printable reports.

- **Pro Ladder**  
  - `ProLadderEngineAdapter` leverages domain `ProLadder` to seed bracket per rules.

---

## Extensibility Points

- Add a new mode: implement `IRaceEngine`, create `*Engine` and (optionally) an adapter; register in `RaceEngineFactory`.
- Add new persistence: add repository class & modify `DatabaseInitializer`.
- Add export/report: add a ViewModel and a UI action; call into repositories for data.

---

## Non-Goals / Ignored Folders

- `bin/`, `obj/`, `packages/`, and `Installer/` contain **build artifacts or distribution payloads** and **must not** be used as architectural references.

---

## AI Context Usage Notes

- This file is the **canonical** context document for ChatGPT.
- Prefer this file over any older “Code Structure v2/v3/v4” docs.
- When referencing behavior, cite the **folder + file** from this doc before asking for code.
- For large queries, pair this with:
  - `MatchEngine_Refactor_Spec.md` (algorithms)
  - `RC Drag Manager — Randomized Bracket Mode Full Design Specification.md`
  - DevLog summary (`_PROJECT_STATUS_SUMMARY.md`) for roadmap/state.

---

## Maintenance Checklist (when code changes)

- [ ] Update the **Repository Layout** section if folders/files move.
- [ ] Update **Component Responsibilities** for added/removed classes.
- [ ] If a new mode is added, document it under **Mode Semantics** and update **Factory** notes.
- [ ] Regenerate **File → Purpose Matrix** for any renamed files.
- [ ] Ensure DevLog summary reflects new milestones.
