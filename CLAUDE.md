# RC Drag Manager — Claude Code Context

This file is the entry point for Claude Code. Read this first, then read the
docs listed below before writing any code.

---

## Project in One Sentence

A Windows desktop app (C# / .NET 4.8 / WinForms / SQLite) that lets a Race
Director run NHRA-style RC drag racing tournaments. One operator, one machine,
no network, no auto-advancement — every step is a manual click.

---

## Documentation Index

All docs live in `Docs/claude-context/`. Read them in this order when working
on this codebase:

| File | Read when |
|------|-----------|
| `PROJECT-OVERVIEW.md` | Always — tech stack, solution structure, build steps |
| `ARCHITECTURE.md` | Always — layer breakdown, component connections, key rules |
| `DOMAIN-MODEL.md` | Always — every entity, property, and relationship |
| `DATA-LAYER.md` | Touching DB or repositories — schema, serialization, quirks |
| `RACE-FLOW.md` | Touching race logic — step-by-step event flow, bracket types |
| `CODEBASE-MAP.md` | Finding files — every source file with one-line description |
| `TESTING.md` | Writing tests or refactoring UI — standard commands, coverage expectations, headless test helpers |
| `TECHNICAL-DEBT.md` | Before any refactor — known issues, closed bugs, weaknesses |
| `CURRENT-SESSION-SETUP-AUDIT.md` | Touching session setup or multi-class work |
| `MULTI-CLASS-EVENT-SPEC.md` | Working on the multi-class feature — full specification |

---

## How to Build

```
Solution: src/RCDragManagerProd/RCDragManagerProd.sln
```

Open in Visual Studio 2022. NuGet packages restore automatically.
Build → Rebuild Solution.
Output: `src/RCDragManagerProd/bin/Debug/` or `bin/Release/`.

Do not use `dotnet build` — this is .NET Framework 4.8, not .NET Core/5+.
Use MSBuild or Visual Studio only.

---

## How to Run Tests

Test project: `src/RCDragManagerProd.Tests/`

Run via Visual Studio Test Explorer → Run All.
Tests use an in-memory SQLite connection string. No external setup required.
All tests must pass before committing.

---

## Architecture Rules — Do Not Violate

These are non-negotiable. The codebase enforces them consistently and any
deviation will be caught in code review.

1. **No direct DB access from UI or engines.** All SQL goes through a
   repository class in `Repositories/`. Forms and engines never hold a
   connection or write SQL.

2. **No business logic in Forms.** Forms subscribe to controller events and
   forward user actions to the controller. They do not compute, validate,
   or make decisions.

3. **Controller emits events; UI only subscribes.** Forms never call engine
   methods directly. The call chain is: Form → Controller → Engine.

4. **Engine state is opaque to the UI.** UI sees only `EngineMatch` DTOs and
   `PairingRow` / `WinnerRow` view models. Never pass engine-internal types
   upward.

5. **No auto-advancement.** Every round transition requires an explicit user
   action. Do not add any timer, background task, or automatic progression.

6. **Engines are pluggable via `IRaceEngine`.** Adding a new race mode means
   implementing `IRaceEngine` and registering a key in `RaceEngineFactory`.
   Do not add switch/if-else on race type strings outside the factory and
   controller.

7. **Stats go through `DriverRepository` only.** Never write directly to the
   `Drivers` table from a form or controller. Use `IncrementWinsAndLosses`,
   `IncrementEventsEntered`, `IncrementEventsWon`.

---

## Key Conventions

- **Round labels are strings**, not enums. Canonical values defined in
  `RoundLabels.cs`. Always use `RoundLabels` constants — never hardcode
  `"SF"`, `"F"`, `"RR1"`, etc. as raw strings.

- **BYE = null driver.** `ByePolicy.IsBye(driver)` is the only test. Never
  check `driver == null` directly in logic code.

- **`RaceSession.RaceType` mutates** during an event:
  `"Round Robin"` → `"Losers Bracket"` → `"Finals"`. Code that reads
  `RaceType` must handle all three values.

- **`RaceController` is a sealed partial class** split across 11 files.
  Adding new methods to the controller means either adding to the most
  relevant existing partial file or creating a new partial file named
  `RaceController.{Concern}.cs`.

- **All saves are INSERT, never UPDATE.** `RaceSessionRepository` and
  (new) `MultiClassEventRepository` are append-only. This is by design.

- **`System.Text.Json` only** — no Newtonsoft.Json. For any type that
  `System.Text.Json` cannot serialize (e.g. `HashSet<(int,int)>`), add a
  serialization backing store property and mark the original `[JsonIgnore]`.
  See `RaceSession.PairingHistory` / `PairingHistoryRaw` as the pattern.

---

## Current Active Feature

**Multi-Class Event Support** — see `MULTI-CLASS-EVENT-SPEC.md` for the full
specification and `MULTI-CLASS-IMPLEMENTATION-PLAN.md` for the sequenced
task list.

Summary: add a new `MultiClassEvent` parent object, a new setup form, a new
tabbed race console (`MultiClassRaceForm`), and a new repository. All changes
are additive — no existing classes, forms, or tables are modified except where
explicitly listed in the spec §5 "Changes to Existing Code".

---

## What Not to Touch

Unless the spec explicitly says otherwise, do not modify:

- `RaceSession` domain object
- `RaceSessionRepository`
- `IRaceEngine` and all engine adapters
- `RoundRobinEngine`, `RoundRobinRanker`
- `LosersBracketBuilder`, `RandomBracket`
- `Form1` and its partial files
- Any existing test file (add new ones; don't change passing tests)
