# RC Drag Manager — Architecture

## Solution Structure

```
src/RCDragManagerProd/
├── Config/                     AppSettings.cs — JSON-backed settings loader
├── Controllers/                RaceController (partial classes) + LaneFairnessManager
├── Domain/                     Core entities: Driver, Car, RaceSession, MatchResult, ProLadder, etc.
│   └── Ladders/                ProLadder partial files L03–L24 (one file per field size)
├── Helpers/                    AssetPath, MatchLookupHelper
├── Integration/                LiveApiClient, LiveRaceUpdateDto (live feed HTTP client)
├── Logging/                    Logger.cs
├── Properties/                 AssemblyInfo, Resources, Settings
├── RaceEngines/                IRaceEngine, MatchEngine, adapters, RaceEngineFactory
├── RandomMode/                 RandomBracket, RandomMatch, RandomMatchEngine,
│                               LosersBracketEngine, LosersBracketBuilder
├── Repositories/               DatabaseInitializer, DriverRepository,
│                               CarRepository, RaceSessionRepository
├── RoundRobinMode/             RoundRobinEngine, RoundRobinMatch, RoundRobinRanker
│   └── RoundRobinScorecardLogger/   Debug, Formatter, Logger, Writer
├── UI/Forms/                   All WinForms — organized by functional area
│   ├── Cars/                   AddCarDialog, SelectCarDialog
│   ├── Common/                 ScrollableTextDialog
│   ├── Drivers/                AddDriverAndCarDialog, AddDriverDialog, AddEditQualTimeDialog,
│   │                           DriverManagerForm, DriverStatsForm, EditDriverDialog
│   ├── Main/                   Form1 (partial), Form1.Designer, Form1.Display,
│   │                           Form1.WinnerButtons, Form1.UI
│   ├── Results/                BuybackDriverSelectionForm, EditWinnerDialog
│   └── Session/                LandingPageForm, LoadSessionForm,
│                               SessionSetupForm (partial: main, UI, Events)
├── Utils/                      DictEx (extension methods)
└── ViewModels/                 MatchResultSave, PairingRow, RaceSessionSummary, WinnerRow
```

---

## Layer Breakdown

### 1. UI Layer (`UI/Forms/`)

All Windows Forms. Forms are **logic-free** — they respond to controller events and forward user actions to the controller. They never touch the database or engine directly.

Key forms:

| Form | Purpose |
|------|---------|
| `LandingPageForm` | Main menu: New Session, Load Session, Manage Drivers |
| `SessionSetupForm` | Event setup: race type, class, roster, qual times, seeds |
| `Form1` | Race control console: bracket display, winner entry, round advancement |
| `DriverManagerForm` | CRUD for the persistent driver + car registry |
| `LoadSessionForm` | List and select a saved session to resume |
| `BuybackDriverSelectionForm` | Pick which losers enter the Losers Bracket |
| `EditWinnerDialog` | Override the winner of a completed match (active round only) |
| `DriverStatsForm` | View lifetime stats for a single driver |
| `ScrollableTextDialog` | Reusable scrollable text popup (used for RR standings scorecard) |

`Form1` is split across five partial files:
- `Form1.cs` — main event wiring, controller subscription
- `Form1.Designer.cs` — auto-generated layout
- `Form1.Display.cs` — bracket list rendering
- `Form1.WinnerButtons.cs` — winner button state management
- `Form1.UI.cs` — general UI helpers

`SessionSetupForm` is split across three partial files:
- `SessionSetupForm.cs` — core logic
- `SessionSetupForm.UI.cs` — UI helpers
- `SessionSetupForm.Events.cs` — event handlers

### 2. Controller Layer (`Controllers/`)

`RaceController` is the **orchestrator** between the UI and the race engines. It holds all mutable race state and emits C# events that the UI subscribes to. It is a `sealed partial class` split across 9 files:

| File | Responsibility |
|------|----------------|
| `RaceController.cs` | State fields, event declarations, constructor |
| `RaceController.Session.cs` | `Reset()`, `SetBuybackDrivers()` |
| `RaceController.RoundFlow.Core.cs` | `GenerateBracket()`, `AdvanceRound()`, `PushNextMatch()`, `PushAdvanceState()` |
| `RaceController.RoundFlow.Finals.cs` | `InjectFinal4Bracket()`, `StartFinals()`, `StartFinalsTop3NoBuyback()`, `InjectFinalsAllAdvance()` |
| `RaceController.RoundFlow.Losers.cs` | `GenerateLosersBracket()`, `StartLosersBracket()` |
| `RaceController.RoundFlow.View.cs` | `BuildCurrentBracketRows()`, display helpers |
| `RaceController.Results.cs` | `SubmitWinner()`, `EditWinnerInActiveRound()`, `GetEligibleBuybackDrivers()` |
| `RaceController.Persistence.cs` | `SaveSession()` — serializes state into the session object |
| `RaceController.Logging.cs` | `TryLogCompletedRound()`, scorecard helpers |
| `RaceController.LiveUpdate.cs` | `QueueLiveUpdate()` — optional live feed push |
| `RaceController.EngineCalls.cs` | Thin wrapper methods (`EngineGetMatches`, `EngineSetWinner`, etc.) to isolate engine type-casts |

`LaneFairnessManager` — tracks left/right lane assignment history to balance lane fairness across drivers.

`IStandingsDialogService` — interface for showing the RR standings popup, injected at construction time (allows test substitution).

### 3. Race Engine Layer (`RaceEngines/`)

Defines the pluggable engine abstraction:

| Component | Purpose |
|-----------|---------|
| `IRaceEngine` | Interface every engine must implement (lifecycle + results + state) |
| `EngineMatch` | Neutral DTO used by the controller and UI — no engine types leak up |
| `RaceEngineFactory` | Static factory: maps string race type → `IRaceEngine` implementation |
| `MatchEngine` | **Legacy** Pro Ladder engine used directly by `Form1` before the adapter layer |
| `ProLadderEngineAdapter` | `IRaceEngine` wrapping `ProLadder` + `MatchEngine` |
| `RandomEngineAdapter` | `IRaceEngine` wrapping `RandomMatchEngine`; also supports `InjectMatches()` for the LB |
| `RoundRobinEngineAdapter` | `IRaceEngine` wrapping `RoundRobinEngine`; exposes `SetRoundsToRun()`, `GetStandings()`, `GetTopRankedDrivers()` |

### 4. Domain / Business Logic Layer (`Domain/`, `RandomMode/`, `RoundRobinMode/`)

Pure logic, no UI or database dependencies:

| Component | Purpose |
|-----------|---------|
| `Driver` | Core entity |
| `Car` | Child entity of Driver |
| `RaceSession` | Serializable session state object |
| `MatchResult` | In-memory winner/loser store, keyed by matchId |
| `ProLadder` | Partial class providing `GetLadder(n)` — returns NHRA bracket template |
| `ProLadder.LadderMatch` | Bracket edge definition: Seed1, Seed2, FromMatch1, FromMatch2, RoundLabel |
| `RandomBracket` | Stateless helpers for generating random bracket rounds |
| `RandomMatch` | Match node for random/LB brackets |
| `RandomMatchEngine` | Full random bracket state machine (stores matches + results) |
| `LosersBracketEngine` | Simple single-elim runner used internally (callback-based) |
| `LosersBracketBuilder` | Builds the `List<RandomMatch>` tree for the Losers Bracket, with rematch avoidance |
| `RoundRobinEngine` | Circle-method round-robin scheduler + results store |
| `RoundRobinRanker` | Points-based ranking: Win=4, Loss=1, BYE=2; tiebreaks by H2H then Opponent Strength |
| `RoundRobinMatch` | Match record for RR |
| `ByePolicy` | `IsBye(Driver d)` — true if `d == null` |
| `RoundLabels` | Normalizes round label strings and provides sort keys |

### 5. Repository / Data Layer (`Repositories/`)

All database access is isolated here. No form or engine ever touches SQL directly.

| Component | Purpose |
|-----------|---------|
| `DatabaseInitializer` | `InitializeDatabase(connStr)` — idempotently creates all tables |
| `DriverRepository` | Full CRUD for Drivers + their Cars; also stat increment methods |
| `CarRepository` | Lightweight standalone Car access (partially used alongside DriverRepository) |
| `RaceSessionRepository` | `SaveSession`, `LoadSession`, `GetAllSessions`, `DeleteSession` |

---

## How Components Connect

```
Program.cs
    │
    └─► DatabaseInitializer.InitializeDatabase()
    └─► LandingPageForm(connStr)
            │
            ├─► DriverManagerForm ──► DriverRepository / CarRepository
            │
            └─► SessionSetupForm ──► DriverRepository
                    │
                    └─► Form1(session, connStr)
                            │
                            └─► RaceController(session)
                                    │
                                    ├─► RaceEngineFactory.Create(raceType)
                                    │       └─► ProLadderEngineAdapter
                                    │           RandomEngineAdapter
                                    │           RoundRobinEngineAdapter
                                    │
                                    ├─► [Events] → Form1 subscribes
                                    │   BracketRedrawn, NextMatchReady,
                                    │   CanAdvanceChanged, CanPickWinnerChanged,
                                    │   CanOfferBuybackChanged, CanStartFinalsChanged,
                                    │   TournamentCompleted
                                    │
                                    └─► RaceSessionRepository (save/load)
                                        DriverRepository (stat updates)
```

### Key Architectural Rules

- **No direct DB access from UI or engine.** All SQL goes through repositories.
- **No auto-advancement.** Every round transition requires explicit user action.
- **Controller emits events; UI only subscribes.** Forms never call engine methods directly.
- **Engine state is opaque to the UI.** The UI only sees `EngineMatch` DTOs and `PairingRow` / `WinnerRow` view models.
- **Engines are pluggable.** Adding a new race mode requires implementing `IRaceEngine` and registering a string key in `RaceEngineFactory`.
