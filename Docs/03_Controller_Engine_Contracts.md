# RC Drag Manager — Controller & Engine Contracts  
**File:** 03_Controller_Engine_Contracts.md  
**Version:** 1.00  
**Status:** ✅ Stable (ChatGPT-Pack Ready)  
**Last Updated:** 2025-10-12  
**Owner:** Stewart McMillan  
**Source of Truth:** Derived from verified repo structure, `01_Code_Structure.md`, `MatchEngine_Refactor_Spec.md`, and `PROJECT_STATUS.md`.

---

## 🤖 How ChatGPT Should Use This Doc

This document defines the **control and execution contracts** connecting the UI layer to the race-logic engines.  
Use it to reason about:
- The runtime flow between forms, controller, and engine adapters.  
- The responsibilities of `RaceController`, `RaceEngineFactory`, `MatchEngine`, and adapters.  
- The shared `IRaceEngine` interface and transition states via `RacePhase`.  

When generating or reviewing related docs (like error handling, logging, or engine specs), treat this as the **definitive guide** to how race flow is orchestrated.

See also:  
- `02_System_Overview.md` (big-picture flow)  
- `04_Mode_Randomized_Bracket_Spec.md` (mode logic example)  
- `06_SQLite_Schema.md` (persistence reference)

---

## 🎯 Purpose

To clearly describe how:
- The **UI layer** (Forms) sends commands.  
- The **Controller** orchestrates session logic and engine selection.  
- The **Engines** execute race logic through unified adapters.  
- The **MatchEngine façade** manages multi-phase transitions.  

This ensures consistent GPT reasoning and future extensibility (e.g., adding new race types).

---

## 🧩 Scope

Covers:
- `RaceController.cs`  
- `RaceEngineFactory.cs`  
- `IRaceEngine` contract  
- Engine Adapters (`ProLadderEngineAdapter`, `RandomEngineAdapter`, `RoundRobinEngineAdapter`)  
- `MatchEngine` façade  
- `RacePhase` enum  

Does **not** cover UI details or persistence — see `08_UI_UX_Surface_Map.md` and `07_Repository_Contracts.md`.

---

## ⚙️ Core Architecture Overview

```
[UI: Forms]
   │
   ▼
 RaceController
   │
   ▼
 RaceEngineFactory ──► IRaceEngine (Adapter)
   │                       │
   │                       ├─► ProLadderEngineAdapter
   │                       ├─► RandomEngineAdapter
   │                       └─► RoundRobinEngineAdapter
   │
   ▼
 MatchEngine (façade) ──► RacePhase transitions
   │
   ▼
 SQLite Repositories
```

Every engine adapter implements **`IRaceEngine`**, providing a uniform API for bracket generation, result entry, and round advancement.

---

## 🧠 Component Responsibilities

### 🔹 `RaceController.cs`

| Aspect | Description |
|---------|--------------|
| **Role** | Central orchestrator for all session logic. |
| **Responsibilities** | - Initialize engines through `RaceEngineFactory`.<br>- Maintain a live reference to `RaceSession`.<br>- Route all UI actions (`Next Round`, `Set Winner`, `Save`) through adapters.<br>- Coordinate session persistence via repositories.<br>- Publish UI events (`BracketRedrawn`, `CanAdvanceChanged`, etc.). |
| **Key Methods** | `StartSession()`, `SetWinner()`, `AdvanceRound()`, `SaveSession()`, `LoadSession()` |
| **Dependencies** | `RaceEngineFactory`, `IRaceEngine`, `RaceSessionRepository`, `Logger` |
| **Design Rule** | Never embeds race logic — always delegates to the engine. |

---

### 🔹 `RaceEngineFactory.cs`

| Aspect | Description |
|---------|--------------|
| **Role** | Responsible for selecting and returning the correct race engine adapter. |
| **Input** | `RaceSession.RaceType` or equivalent mode identifier. |
| **Output** | Instance of an `IRaceEngine` implementation. |
| **Typical Logic** |  
```csharp
switch(session.RaceType)
{
    case RaceType.ProLadder:   return new ProLadderEngineAdapter(session);
    case RaceType.Random:      return new RandomEngineAdapter(session);
    case RaceType.RoundRobin:  return new RoundRobinEngineAdapter(session);
    default:                   throw new NotSupportedException("Unknown mode");
}
``` |
| **Design Rule** | The factory must not contain race logic — only instantiation. |

---

## 🧩 Unified Engine Interface — `IRaceEngine`

This interface guarantees that all race types expose the same core lifecycle methods.

```csharp
public interface IRaceEngine
{
    object GetCurrentMatch();                // Returns current match DTO (PairingRow, etc.)
    void   SetWinner(Guid driverA, Guid? driverB, Guid winner);
    void   Advance();                        // Progresses to the next round/phase
    bool   IsComplete { get; }               // True when all rounds finished
}
```

| Property | Description |
|-----------|-------------|
| `GetCurrentMatch()` | Returns engine-specific match data for UI display. |
| `SetWinner()` | Registers winner for a match, updates internal result state. |
| `Advance()` | Generates next round or transitions to next RacePhase. |
| `IsComplete` | Indicates when event logic is fully resolved. |

The controller treats all engines identically via this interface.

---

## 🔧 Engine Adapter Layer

Each adapter implements `IRaceEngine`, connecting `RaceController` to its specialized engine logic.

| Adapter | Backing Engine | Function | Key Notes |
|----------|----------------|-----------|------------|
| **ProLadderEngineAdapter** | `MatchEngine` | NHRA-style deterministic brackets using fixed seed maps. | Manual round advancement required. |
| **RandomEngineAdapter** | `RandomMatchEngine` / `RandomBracket` | Random blind draw brackets, repeat-avoidant pairing, Round-1 buybacks. | Auto-calculates BYEs; respects pairing history. |
| **RoundRobinEngineAdapter** | `RoundRobinEngine` | Generates schedule ensuring all drivers race each other. | Provides standings and tiebreakers. |

**Design Constraints**
- Adapters must be **stateless** beyond their session reference.  
- All communication with data layers goes through `RaceController`.  
- No adapter may perform direct SQLite queries.  

---

## 🧩 `MatchEngine` Façade

Acts as a single entry point managing engine delegation and **phase transitions**.

| Aspect | Description |
|---------|--------------|
| **Role** | Facade for managing multiple phases (Round Robin → Losers Bracket → Pro Ladder). |
| **Responsibilities** | - Maintain current `RacePhase`.<br>- Route all `IRaceEngine` calls to the active engine.<br>- Handle transitions when a phase completes.<br>- Provide consistent lifecycle methods to `RaceController`. |
| **Dependencies** | `RoundRobinEngine`, `LosersBracketEngine`, `ProLadderEngine`. |
| **Design Pattern** | *Façade* + *Strategy* combination — phase-aware delegation. |

### Example Internal Flow
```csharp
switch (Phase)
{
    case RacePhase.RoundRobin:
        _active = _roundRobin;
        if (_roundRobin.IsComplete)
            TransitionTo(RacePhase.LosersBracket);
        break;

    case RacePhase.LosersBracket:
        _active = _losers;
        if (_losers.IsComplete)
            TransitionTo(RacePhase.ProLadder);
        break;

    case RacePhase.ProLadder:
        _active = _proLadder;
        if (_proLadder.IsComplete)
            Phase = RacePhase.Complete;
        break;
}
```

**Public Methods**
```csharp
void Advance()            // Delegates to active engine, updates Phase if needed
void SetWinner(...)       // Routed to active engine
object GetCurrentMatch()  // Returns phase-specific DTO
bool IsComplete           // True when Phase == Complete
```

---

## 🧮 `RacePhase` Enum

Defines which sub-engine the façade should delegate to.

```csharp
public enum RacePhase
{
    RoundRobin,
    LosersBracket,
    ProLadder,
    Complete
}
```

| Phase | Description |
|--------|-------------|
| **RoundRobin** | Qualification or seeding stage. |
| **LosersBracket** | Secondary elimination for non-qualified drivers. |
| **ProLadder** | Final NHRA-style seeded elimination. |
| **Complete** | Tournament concluded — no further advancement. |

---

## 🔄 UI Interaction Flow (Simplified)

```
[Form1 / SessionSetupForm]
        │
        ▼
   RaceController.StartSession()
        │
        ▼
   RaceEngineFactory.Select()
        │
        ▼
      IRaceEngine Adapter
        │
        ▼
   MatchEngine (if multi-phase)
        │
        ▼
  Repositories.SaveResults()
```

All user actions (next round, set winner, etc.) travel this same route, keeping logic isolated and testable.

---

## 🧱 Architectural Principles

| Principle | Enforcement |
|------------|-------------|
| **Single Source of Logic** | Engines own race rules; controllers only route. |
| **Manual Director Control** | Round advancement always triggered from UI (NHRA compliance). |
| **Pluggable Engines** | Adding new race types requires only a new adapter implementing `IRaceEngine`. |
| **Strict Persistence Boundaries** | Engines never write directly to SQLite. |
| **Predictable Transitions** | Each engine signals completion; `MatchEngine` manages phase hand-off. |
| **Event-Driven UI Updates** | Controller emits events for UI refresh; no polling or thread coupling. |

---

## 📚 Adjacent Docs

| File | Purpose |
|------|----------|
| `02_System_Overview.md` | System architecture & lifecycle overview |
| `04_Mode_Randomized_Bracket_Spec.md` | Randomized engine design |
| `05_Mode_RoundRobin_Spec.md` | Round Robin logic (planned) |
| `06_SQLite_Schema.md` | Database mapping for driver/session storage |
| `07_Repository_Contracts.md` | Persistence API definitions |
| `09_Error_Handling_Logging.md` | Logging and exception policy |
| `13_Project_Status_Summary.md` | Implementation progress tracking |

---

## ✅ Summary

The Controller & Engine layer defines a **modular orchestration pipeline** where:
- The **Controller** handles commands and persistence.
- The **Factory** chooses the correct adapter.
- The **Adapter** implements the common `IRaceEngine` contract.
- The **MatchEngine façade** coordinates multi-phase flow.  

This design provides full flexibility for future expansion while ensuring NHRA compliance and deterministic session replay.

---
