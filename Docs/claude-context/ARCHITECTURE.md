# RC Drag Manager — Architecture

## Solution Structure

```
src/RCDragManagerProd/
├── AppServices/                UI↔controller seam: RaceConsoleService,
│                               MultiClassSetupService, DriverManagerService,
│                               LoadSessionService, EventSettingsService, … (15 files)
├── Config/                     AppSettings.cs — JSON-backed settings loader
├── Controllers/                RaceController (partial classes) + LaneFairnessManager
├── Domain/                     Core entities: Driver, Car, RaceSession, MatchResult, ProLadder, etc.
│   └── Ladders/                ProLadder partial files L03–L24 (one file per field size)
├── Helpers/                    AssetPath, MatchLookupHelper
├── Integration/                LiveApiClient, LiveRaceUpdateDto (live feed HTTP client)
├── Logging/                    Logger.cs, LogWriter.cs
├── Properties/                 AssemblyInfo, Resources, Settings
├── RaceEngines/                IRaceEngine, MatchEngine, adapters, RaceEngineFactory
├── RandomMode/                 RandomBracket, RandomMatch, RandomMatchEngine,
│                               LosersBracketEngine, LosersBracketBuilder
├── Repositories/               DatabaseInitializer, DriverRepository,
│                               CarRepository, RaceSessionRepository,
│                               MultiClassEventRepository, DbDate
├── RoundRobinMode/             RoundRobinEngine, RoundRobinMatch, RoundRobinRanker,
│                               MultiCarRoundRobinScheduler, MultiCarOpeningRoundPlanner
│   └── RoundRobinScorecardLogger/   Debug, Formatter, Logger, Writer
├── UI/Forms/                   All WinForms — organized by functional area
│   ├── Cars/                   AddCarDialog, SelectCarDialog
│   ├── Common/                 ScrollableTextDialog, SettingsForm, RaceDialogs
│   ├── Drivers/                AddDriverAndCarDialog, AddDriverDialog, AddEditQualTimeDialog,
│   │                           DriverManagerForm, DriverStatsForm, EditDriverDialog
│   ├── Main/                   Form1 (partial), Form1.Designer, Form1.Display,
│   │                           Form1.Events, Form1.UI, Form1.WinnerButtons,
│   │                           MultiClassRaceForm, QRCodeDialog
│   ├── Results/                BuybackDriverSelectionForm, EditWinnerDialog
│   └── Session/                LandingPageForm, LoadSessionForm,
│                               MultiClassSetupForm (partial: main, Designer),
│                               MultiClassConfigDialog
├── Utils/                      DictEx (extension methods)
└── ViewModels/                 MatchResultSave, PairingRow, RaceSessionSummary, WinnerRow,
                                MultiClassEventSummary, RaceResultsPresentation,
                                ClassCompletionPresentation, EventCompletionPresentation
```

---

## Layer Breakdown

### 1. UI Layer (`RCDragManagerProd.WPF/` + legacy `UI/Forms/`)

The primary UI is **WPF** (`src/RCDragManagerProd.WPF/`, v2.0.0+): top-level windows in `Windows/`, the console UserControl in `Views/RaceConsoleView`, themed dialogs in `Dialogs/`, and `ThemeManager` for dark/light theming. WPF views are **logic-free** — they bind to the extracted AppServices (`RaceConsoleService`, `LoadSessionService`, `MultiClassSetupService`, …) and subscribe to `RaceController` events.

The original **WinForms** UI in `UI/Forms/` is legacy but still builds. Its forms follow the same rule: respond to controller events, forward user actions to the controller, never touch the database or engine directly.

Key legacy WinForms forms:

| Form | Purpose |
|------|---------|
| `LandingForm` | Main menu: New Session, Load Session, Manage Drivers (file `LandingPageForm.cs`) |
| `MultiClassSetupForm` | Event setup: event details plus one or more classes (race type, class type, roster, qual times, dial-ins) |
| `MultiClassConfigDialog` | Per-class configuration dialog used during setup (race type, class type, driver picker, dial-in overrides) |
| `Form1` | Race control console: bracket display, winner entry, round advancement |
| `DriverManagerForm` | CRUD for the persistent driver + car registry |
| `LoadSessionForm` | List and select a saved session to resume |
| `BuybackDriverSelectionForm` | Pick which losers enter the Losers Bracket |
| `EditWinnerDialog` | Override the winner of a completed match (active round only) |
| `DriverStatsForm` | View lifetime stats for a single driver |
| `ScrollableTextDialog` | Reusable scrollable text popup (used for RR standings scorecard) |

`Form1` is split across six partial files:
- `Form1.cs` — main event wiring, controller subscription
- `Form1.Designer.cs` — auto-generated layout
- `Form1.Display.cs` — bracket list rendering
- `Form1.WinnerButtons.cs` — winner button state management
- `Form1.UI.cs` — general UI helpers
- `Form1.Events.cs` — event handlers

`MultiClassSetupForm` has two parts:
- `MultiClassSetupForm.cs` — core setup logic
- `MultiClassSetupForm.Designer.cs` — auto-generated layout

`MultiClassConfigDialog` — per-class configuration dialog invoked from the setup form during the event setup flow.

### 2. Application Services Layer (`AppServices/`)

The seam between the UIs and the controller: a view calls a service, and each service either wraps a repository or drives a `RaceController`. 15 files.

| Component | Purpose |
|-----------|---------|
| `RaceConsoleService` | UI-independent command and state seam for the race console |
| `RaceConsoleViewModel` | Data contract the race console renders from (no engine-internal types) |
| `RaceConsoleViewModelBuilder` | Builds the console snapshot from a `RaceController` (title, current/on-deck/in-the-hole, next action) |
| `LoadSessionService` | Loading saved races/events for the Load screen |
| `MultiClassSetupService` | Race setup and class-config operations for multi-class setup |
| `MultiClassRaceService` | Stat persistence for multi-class events |
| `SessionRosterService` | Validation and construction helpers for the in-memory session roster |
| `RaceRosterService` | Roster building for a race; writes new drivers to the DB first to get real ids |
| `DriverManagerService` | Driver/car CRUD for the Driver Manager screen |
| `DriverStatsService` | Match-history queries for the Driver Stats screen |
| `EventSettingsService` | Pure rules for what may change mid-event (per-event Settings tab) |
| `EventCompletionPresentationBuilder` | Builds the end-of-event board from each class's saved archive |
| `RaceResultsPresentationBuilder` | Builds `RaceResultsPresentation` (phases, rounds, match cards, standings) |
| `ClassCompletionPresentationBuilder` | Builds the class-complete board (champion, runner-up, 3rd) |
| `RosterAddResult` | Result object for one `SessionRosterService.AddOrUpdate` call |

### 3. Controller Layer (`Controllers/`)

`RaceController` is the **orchestrator** between the UI and the race engines. It holds all mutable race state and emits C# events that the UI subscribes to. It is a `sealed partial class` split across 18 files:

| File | Responsibility |
|------|----------------|
| `RaceController.cs` | State fields, event declarations, constructor |
| `RaceController.Session.cs` | `Reset()`, `SetBuybackDrivers()` |
| `RaceController.MultiClass.cs` | Cross-class gates (`HasPendingMatchesInCurrentRound`, `IsRrComplete`) and the RR vs multi-car identity mapping (`OwnerDriverId`, `GetStatResults`) |
| `RaceController.Resume.cs` | `RestoreFromSave()`, `ReplayResults()` — rebuild engine/controller state from a saved snapshot |
| `RaceController.RoundFlow.Core.cs` | `GenerateBracket()`, `AdvanceRound()`, `PushNextMatch()`, `PushAdvanceState()` |
| `RaceController.RoundFlow.Finals.cs` | `InjectFinal4Bracket()`, `StartFinals()`, `StartFinalsTop3NoBuyback()`, `InjectFinalsAllAdvance()` |
| `RaceController.RoundFlow.Losers.cs` | `GenerateLosersBracket()`, `StartLosersBracket()` |
| `RaceController.RoundFlow.View.cs` | `BuildCurrentBracketRows()`, display helpers |
| `RaceController.RoundFlow.Defer.cs` | `PushCurrentMatchToEndOfRound()`, `ApplyRaceOrder()` — live-only match deferral, never persisted |
| `RaceController.Results.cs` | `SubmitWinner()`, `EditWinnerInActiveRound()`, `GetEligibleBuybackDrivers()` |
| `RaceController.ResultSnapshots.cs` | `CaptureCurrentResultSnapshot()`, `CaptureRoundRobinResultSnapshot()` — builds the saved results archive |
| `RaceController.Persistence.cs` | `SaveSession()` — serializes state into the session object |
| `RaceController.SaveClose.cs` | `SaveProgress()` (resumable checkpoint) and `CloseRace()` (mark finished) |
| `RaceController.Logging.cs` | `TryLogCompletedRound()`, scorecard helpers |
| `RaceController.Stats.cs` | `PersistTournamentStats()`, `PersistMatchStats()` — driver stat writes via `DriverRepository` |
| `RaceController.LiveUpdate.cs` | `QueueLiveUpdate()` — optional live feed push |
| `RaceController.DialIn.cs` | `UpdateDriverDialIn()`, `LockDialIn()`, `StartDialInPolling()` — dial-in edits and live-site polling |
| `RaceController.EngineCalls.cs` | Thin wrapper methods (`EngineGetMatches`, `EngineSetWinner`, etc.) to isolate engine type-casts |

`LaneFairnessManager` — tracks left/right lane assignment history to balance lane fairness across drivers.

`IStandingsDialogService` — interface for showing the RR standings popup, injected at construction time (allows test substitution).

### 4. Race Engine Layer (`RaceEngines/`)

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

### 5. Domain / Business Logic Layer (`Domain/`, `RandomMode/`, `RoundRobinMode/`)

Pure logic, no UI or database dependencies:

| Component | Purpose |
|-----------|---------|
| `Driver` | Core entity |
| `Car` | Child entity of Driver |
| `RaceSession` | Serializable session state object |
| `MatchResult` | In-memory winner/loser store, keyed by matchId |
| `ProLadder` | Partial class providing `GetLadder(n)` — returns NHRA bracket template |
| `ProLadder.LadderMatch` | Bracket edge definition: MatchId, Seed1, Seed2, FromMatch1, FromMatch2, RoundLabel |
| `RandomBracket` | Stateless helpers for generating random bracket rounds |
| `RandomMatch` | Match node for random/LB brackets |
| `RandomMatchEngine` | Full random bracket state machine (stores matches + results) |
| `LosersBracketEngine` | Simple single-elim runner used internally (callback-based) |
| `LosersBracketBuilder` | Builds the `List<RandomMatch>` tree for the Losers Bracket, with rematch avoidance |
| `RoundRobinEngine` | Circle-method round-robin scheduler + results store |
| `RoundRobinRanker` | Points-based ranking: Win=4, Loss=1, BYE=2, plus one composite `TotalScore` = points + 0.1 per opponent you **beat** that is level on points + 0.001 × the points of the drivers you beat. Sorted on `TotalScore` desc (ties by driver id); no separate H2H tiebreak |
| `RoundRobinMatch` | Match record for RR |
| `MultiCarRoundRobinScheduler` | Car-aware RR fixture builder: pairs entries two at a time (keyed on `RaceEntryId`), byes for an odd field, exhausting fresh pairings before repeating a race or pairing two cars owned by the same driver |
| `MultiCarOpeningRoundPlanner` | Re-seeds the first buyback round (`LB-R1`) so two entries owned by one driver do not meet while an outside-driver pairing is available |
| `ByePolicy` | `IsBye(Driver d)` — true if `d == null` |
| `RoundLabels` | Normalizes round label strings and provides sort keys |

### 6. Repository / Data Layer (`Repositories/`)

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
    └─► LandingForm(connStr)
            │
            ├─► DriverManagerForm ──► DriverRepository / CarRepository
            │
            └─► MultiClassSetupForm(connStr) ──► MultiClassSetupService
                    │                                  └─► DriverRepository
                    │  (modal; on OK LandingForm opens the console)
                    └─► MultiClassRaceForm(event, connStr)
                            │   builds one RaceController(session) per class
                            └─► Form1(controller)
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
                                            │   WinnersUpdated, CanAdvanceChanged,
                                            │   CanPickWinnerChanged,
                                            │   CanOfferBuybackChanged, CanStartFinalsChanged,
                                            │   TournamentCompleted
                                            │
                                            └─► RaceSessionRepository (save/load)
                                                DriverRepository (stat updates)
```

### Multi-Class Events

An event owns one or more classes. Each class is its own `RaceSession` (own race type,
bracket, roster and console tab); `MultiClassEvent` is the parent. The pieces:

| Component | Role |
|-----------|------|
| `MultiClassEventRepository` | Persists the parent event: `SaveEvent`, `LoadEvent`, `GetAllEvents`, `DeleteEvent` |
| `MultiClassSetupService` | Builds the event from the setup UI (`StartEvent`, class config) |
| `RaceController.MultiClass.cs` | Cross-class gates (`HasPendingMatchesInCurrentRound`, `IsRrComplete`) and identity mapping (`OwnerDriverId`) |
| `MultiClassRaceWindow` | WPF console: one `RaceConsoleView` per class tab |
| `MultiClassRaceForm` | Legacy WinForms console: one `Form1` per class |

### Key Architectural Rules

- **No direct DB access from UI or engine.** All SQL goes through repositories.
- **No auto-advancement.** Every round transition requires explicit user action.
- **Controller emits events; UI only subscribes.** Forms never call engine methods directly.
- **Engine state is opaque to the UI.** The UI only sees `EngineMatch` DTOs and `PairingRow` / `WinnerRow` view models.
- **Engines are pluggable.** Adding a new race mode requires implementing `IRaceEngine` and registering a string key in `RaceEngineFactory`.
