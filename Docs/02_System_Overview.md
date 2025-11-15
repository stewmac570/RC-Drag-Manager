# RC Drag Manager — System Overview  
**File:** 02_System_Overview.md  
**Version:** 1.00  
**Status:** ✅ Stable (ChatGPT-Pack Ready)  
**Last Updated:** 2025-10-12  
**Owner:** Stewart McMillan  
**Source of Truth:** Derived from verified repository structure, `01_Code_Structure.md`, `PROJECT_STATUS.md`, `MatchEngine_Refactor_Spec.md`, and Randomized Mode spec.

---

## 🧭 How ChatGPT Should Use This Doc

This file is the **conceptual map** of RC Drag Manager — describing how all major layers, engines, and runtime objects interact.  
Use it to:
- Understand the system’s full **control flow** (UI → Controller → Engine → Repository → SQLite).  
- Ground reasoning about **session lifecycle** and **mode differences**.  
- Support generation or review of related documentation (e.g., controller contracts, schema, or UI flow).  
- Avoid code guessing — refer to this document before analyzing logic.  

For detailed file purposes, see: `01_Code_Structure.md`  
For mode logic, see: `04_Mode_Randomized_Bracket_Spec.md` and (future) `05_Mode_RoundRobin_Spec.md`.

---

## 🎯 Purpose

To provide a top-down overview of how RC Drag Manager operates as a desktop application:
- What the user experiences (UI flow).  
- How the internal systems coordinate (controller, engines, repositories).  
- How the three supported **race modes** — *Pro Ladder*, *Randomized Bracket*, and *Round Robin* — fit together under a unified architecture.

---

## 🚫 Non-Goals

- This document does **not** contain source code or class-level definitions.  
- It does **not** detail SQLite schema (see `06_SQLite_Schema.md`).  
- It does **not** define API interfaces (see `03_Controller_Engine_Contracts.md`).  
- It does **not** replace per-mode specifications.

---

## 🧱 System Architecture Overview

### Application Type
- **Platform:** Windows Desktop (.NET Framework 4.8)  
- **Architecture:** WinForms (multi-form app)  
- **Database:** SQLite (`race_data.db`)  
- **Namespace:** `RCDragManagerProd`

### High-Level Layers

| Layer | Description | Key Components |
|-------|-------------|----------------|
| **UI Layer** | All Windows Forms and dialogs controlling race flow, session setup, and driver management. | `LandingPageForm`, `SessionSetupForm`, `Form1`, `DriverManagerForm`, etc. |
| **Controller Layer** | Orchestrates communication between UI, race engines, and repositories. | `RaceController.cs` |
| **Engine Layer** | Encapsulates race logic for each mode via adapters. | `MatchEngine`, `ProLadderEngineAdapter`, `RandomEngineAdapter`, `RoundRobinEngineAdapter` |
| **Domain Layer** | Core data objects representing drivers, cars, sessions, and match results. | `RaceSession`, `Driver`, `Car`, `MatchResult`, `ProLadder` |
| **Repository Layer** | Handles persistence to SQLite and data initialization. | `DriverRepository`, `CarRepository`, `RaceSessionRepository`, `DatabaseInitializer` |
| **Utility / Logging Layer** | Shared helpers, configuration, and log writers. | `Logger`, `AppSettings`, `AssetPath`, `DictEx` |

---

## ⚙️ Control Flow Summary

```
[User]
   ↓
LandingPageForm  →  SessionSetupForm  →  RaceController
                                           ↓
                                   RaceEngineFactory
                                           ↓
                               (ProLadder / Random / RR Engine)
                                           ↓
                                   MatchEngine façade
                                           ↓
                                     SQLite Repositories
                                           ↓
                                   Persistent race_data.db
```

Each layer passes clean, typed objects — **no direct database access from UI or engines.**

---

## 🏁 Race Modes Overview

RC Drag Manager supports **three race engines** under a unified interface (`IRaceEngine`):

| Mode | Description | Core Logic File | Notes |
|------|--------------|----------------|-------|
| **Pro Ladder** | NHRA-style seeded elimination following official ladders (3–32 drivers). | `ProLadder.cs` + `ProLadderEngineAdapter.cs` | Deterministic, seed-based bracket. |
| **Randomized Single Elimination** | Blind draw brackets with optional BYEs and Round 1 buybacks. | `RandomBracket.cs` + `RandomEngineAdapter.cs` | Fair, repeat-avoidant random pairing. |
| **Round Robin** | Every driver races all others; ranked by points and tie-breakers. | `RoundRobinEngine.cs` + `RoundRobinEngineAdapter.cs` | Multi-round scoring, used in “heads-up” style events. |

Each engine conforms to the shared **`IRaceEngine`** contract and plugs into the **`MatchEngine` façade**, which selects the active engine based on session type.

---

## 🔄 Session Lifecycle

### 1. Launch / Landing
- `Program.cs` opens **LandingPageForm**, the main entry menu.  
- Users can **start a new session**, **load a saved event**, or **manage drivers**.

### 2. Session Setup
- `SessionSetupForm` collects session metadata:
  - Event name, date, race type, and class.
  - Driver roster selection from the database.
- On confirmation, a **`RaceSession`** object is created.

### 3. Controller Wiring
- `RaceController` instantiates the correct engine using `RaceEngineFactory`.
- All race flow commands (Next Round, Set Winner, etc.) route through the controller, **never directly from the UI**.

### 4. Race Execution
- Active engine generates pairings (`GeneratePairings()`).
- Matches are displayed in `Form1` (the race control UI).
- Race Director selects winners manually (NHRA-compliant manual flow).
- Results are persisted via repositories.

### 5. Round Progression
- `Form1` tracks round state through the engine.
- “Next Round” triggers a new bracket build or result phase.
- For Random and RR modes, repeat prevention or scoring systems manage advancement.

### 6. Save / Resume
- Entire session (drivers, matches, results) saved via `RaceSessionRepository`.
- On reload, the session rehydrates all match data and engine state.

---

## 🧩 Data Model & Persistence Summary

| Object | Responsibility | Stored In |
|---------|----------------|-----------|
| `Driver` | Name, Qual Time, Stats, Cars | `Drivers` table |
| `Car` | Car info and class type | `Cars` table |
| `RaceSession` | Event metadata and current race state | Serialized in `RaceSessionRepository` |
| `MatchResult` | Per-match outcome and winner tracking | Embedded in session data |
| `ProLadder` | Ladder seed map definitions | Code only (not DB) |

All writes go through repositories; **no form or engine performs direct SQL**.  
`DatabaseInitializer` ensures schema creation and upgrades on startup.

---

## 📊 Logging & Configuration

- Logs written via `Logger.cs` to:  
  `%APPDATA%\RC_Drag_Manager\app.log`
- Controlled through `AppSettings.json` or `AppSettings.cs`
- Future phases include configurable log levels and optional console output.

---

## 🔐 Key Architectural Rules

- **Manual Round Control:** No automatic advancement; Race Director approves every round.  
- **NHRA Compliance:** Pro Ladder logic must remain deterministic and hand-driven.  
- **No direct DB writes from UI or Engine.**  
- **Session integrity:** Every persisted session must reconstruct its full race state deterministically on load.  
- **Engines are pluggable:** Adding new modes (e.g., Chicago Shootout) requires only a new adapter implementing `IRaceEngine`.  

---

## 📚 Adjacent Docs

| File | Purpose |
|------|----------|
| `01_Code_Structure.md` | Canonical folder map and class descriptions |
| `03_Controller_Engine_Contracts.md` | Defines APIs between UI, Controller, and Engines |
| `04_Mode_Randomized_Bracket_Spec.md` | Randomized bracket full specification |
| `05_Mode_RoundRobin_Spec.md` | Round Robin logic specification (pending) |
| `06_SQLite_Schema.md` | Database table definitions and relationships |
| `07_Repository_Contracts.md` | CRUD responsibilities and data boundaries |
| `08_UI_UX_Surface_Map.md` | User interface layout and form purposes |
| `09_Error_Handling_Logging.md` | Logging and exception-handling policies |
| `13_Project_Status_Summary.md` | Development phase tracking and current build notes |

---

## ✅ Summary

RC Drag Manager is a modular WinForms desktop system that unifies three distinct race engines under a consistent session framework.  
The app’s design prioritizes:
- Manual race director control (NHRA compliance)  
- Data persistence via SQLite  
- Deterministic, event-driven architecture  
- Pluggable mode expansion  

This document serves as the **architectural grounding reference** for all ChatGPT-based analysis and document generation.

---
