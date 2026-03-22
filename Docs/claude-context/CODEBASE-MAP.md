# RC Drag Manager — Codebase Map

All source files in `src/RCDragManagerProd/`, organised by folder.

---

## Root

| File | Description |
|------|-------------|
| `Program.cs` | App entry point: initializes settings, DB, global exception handlers, opens `LandingPageForm` |
| `RCDragManagerProd.csproj` | Project file: .NET Framework 4.8, NuGet references, build targets |
| `RCDragManagerProd.sln` | Solution file: references main project + Tests project |

---

## Config/

| File | Description |
|------|-------------|
| `AppSettings.cs` | Loads/saves `AppSettings.json` in `%APPDATA%\RC_Drag_Manager`; exposes `EnableLogging`, `LogFilePath` |

---

## Controllers/

| File | Description |
|------|-------------|
| `RaceController.cs` | Main partial file: state fields (`_engine`, `_session`, `_matchResult`, etc.), event declarations, constructor |
| `RaceController.Session.cs` | `Reset()` (full state wipe) and `SetBuybackDrivers()` |
| `RaceController.RoundFlow.Core.cs` | `GenerateBracket()`, `AdvanceRound()`, `PushNextMatch()`, `PushAdvanceState()` — core bracket flow |
| `RaceController.RoundFlow.Finals.cs` | Finals injection: `InjectFinal4Bracket()`, `StartFinals()`, `StartFinalsTop3NoBuyback()`, `InjectFinalsAllAdvance()` |
| `RaceController.RoundFlow.Losers.cs` | Losers Bracket start: `GenerateLosersBracket()`, `StartLosersBracket()` |
| `RaceController.RoundFlow.View.cs` | `BuildCurrentBracketRows()` — assembles the `PairingRow` list for the bracket ListView |
| `RaceController.Results.cs` | `SubmitWinner()`, `EditWinnerInActiveRound()`, `GetEligibleBuybackDrivers()` |
| `RaceController.Persistence.cs` | `SaveSession()` — collects match results and round state into the `RaceSession` object |
| `RaceController.Logging.cs` | `TryLogCompletedRound()` — emits RR per-round scorecard logs |
| `RaceController.EngineCalls.cs` | `EngineGetMatches()`, `EngineSetWinner()`, `EngineHasWinner()`, etc. — thin adapters isolating engine type casts |
| `RaceController.LiveUpdate.cs` | `QueueLiveUpdate()`, `BuildLiveRaceUpdateDto()` — optional HTTP live feed push |
| `LaneFairnessManager.cs` | Tracks lane (left/right) history per driver; `GetLane()` returns the fairer assignment |
| `IStandingsDialogService.cs` | Interface for showing the RR standings popup; default impl uses `ScrollableTextDialog` |

---

## Domain/

| File | Description |
|------|-------------|
| `Drivers.cs` | `Driver` entity: Id, Name, QualTime, TotalWins, TotalLosses, EventsEntered, EventsWon, Seed, State, Cars |
| `Car.cs` | `Car` entity: Id/CarID alias, DriverId, CarName, ClassType, DefaultDialIn |
| `RaceSession.cs` | `RaceSession` (full session state), `RaceSessionDriverEntry` (per-driver snapshot), `MatchResultSave` (serializable result record) |
| `MatchResult.cs` | In-memory result store: `SetWinner`, `GetWinner`, `GetLoser`, `HasResult`, `ClearFromMatch`, `IsTournamentComplete` |
| `ByePolicy.cs` | `IsBye(Driver d)` — true if `d == null` |
| `RoundLabels.cs` | Round label normalization (`"R1"`, `"SF"`, `"F"`, `"LB-R1"`, `"LB-F"`, `"RR1"`, …), compare/sort keys |
| `ProLadder.cs` | Partial class shell (empty body) |
| `ProLadder.Structures.cs` | `ProLadder.LadderMatch` struct: MatchId, Seed1, Seed2, FromMatch1, FromMatch2, RoundLabel |
| `ProLadder.Ladders.Common.cs` | `ProLadder.GetLadder(n)` — dispatch method returning the template for field size `n` |

### Domain/Ladders/

One file per supported field size (3–24 drivers). Each defines a static partial method returning `List<LadderMatch>` for that specific NHRA Pro Ladder layout.

| File | Description |
|------|-------------|
| `ProLadder.Ladders.L03.cs` | NHRA template: 3-driver bracket |
| `ProLadder.Ladders.L04.cs` | NHRA template: 4-driver bracket |
| `ProLadder.Ladders.L05.cs` | NHRA template: 5-driver bracket |
| … | … (L06 through L23) |
| `ProLadder.Ladders.L24.cs` | NHRA template: 24-driver bracket |

---

## Helpers/

| File | Description |
|------|-------------|
| `AssetPath.cs` | Resolves `Assets\` folder paths relative to the executable; logs path resolution |
| `MatchLookupHelper.cs` | Helper utilities for finding matches by ID within a list |

---

## Integration/

| File | Description |
|------|-------------|
| `LiveApiClient.cs` | HTTP client that POSTs `LiveRaceUpdateDto` to a local live feed server (optional feature) |
| `LiveRaceUpdateDto.cs` | DTO: `EventName`, `EventDate`, `CurrentRound`, `NextUp`, `Matches` (list of `LiveMatchDto`) |

---

## Logging/

| File | Description |
|------|-------------|
| `Logger.cs` | Static logger; writes timestamped lines to `%APPDATA%\RC_Drag_Manager\app.log`; respects `AppSettings.EnableLogging` |

---

## RaceEngines/

| File | Description |
|------|-------------|
| `IRaceEngine.cs` | Interface all engines implement + `EngineMatch` neutral DTO |
| `RaceEngineFactory.cs` | Static factory: maps race type string → `IRaceEngine` (Pro Ladder / Random / Round Robin) |
| `MatchEngine.cs` | Legacy Pro Ladder engine (predates `IRaceEngine`); used internally by `ProLadderEngineAdapter` |
| `ProLadderEngineAdapter.cs` | `IRaceEngine` wrapper for Pro Ladder: delegates to `ProLadder.GetLadder()` + `MatchEngine` |
| `RandomEngineAdapter.cs` | `IRaceEngine` wrapper for random brackets; also supports `InjectMatches()` for pre-built LB match trees |
| `RoundRobinEngineAdapter.cs` | `IRaceEngine` wrapper for Round Robin; adds `SetRoundsToRun()`, `GetStandings()`, `GetTopRankedDrivers()` |

---

## RandomMode/

| File | Description |
|------|-------------|
| `RandomMatch.cs` | Match data class for random/LB brackets: MatchId, Seed1, Seed2, FromMatch1, FromMatch2, RoundLabel |
| `RandomBracket.cs` | Static helpers: `GenerateFirstRound()` (shuffle + BYE), `GenerateNextRound()` (with pairing history avoidance), `ResetByeTracker()` |
| `RandomMatchEngine.cs` | Bracket state machine for random mode: stores `List<RandomMatch>`, resolves drivers, tracks results |
| `LosersBracketEngine.cs` | Standalone single-elim runner (callback-based); not used in the main flow — superseded by `LosersBracketBuilder` |
| `LosersBracketBuilder.cs` | Builds a `List<RandomMatch>` tree for the Losers Bracket: shuffles, pads to power-of-two, R1 with rematch avoidance, subsequent rounds by FromMatch references |

---

## Repositories/

| File | Description |
|------|-------------|
| `DatabaseInitializer.cs` | Idempotent schema creation: `CREATE TABLE IF NOT EXISTS` for Drivers, Cars, RaceSessions; `ALTER TABLE` for new columns |
| `DriverRepository.cs` | Full CRUD for Drivers + Cars; stat increment methods; `ComputeEventsWonFromSavedSessions()` |
| `CarRepository.cs` | Lightweight Car-only CRUD (partial overlap with DriverRepository) |
| `RaceSessionRepository.cs` | `SaveSession()` (INSERT), `GetAllSessions()` (summary list), `LoadSession(id)` (deserialize JSON), `DeleteSession(id)` |

---

## RoundRobinMode/

| File | Description |
|------|-------------|
| `RoundRobinMatch.cs` | Match data class: MatchId, RoundLabel, Driver1, Driver2 |
| `RoundRobinEngine.cs` | Circle-method scheduler: `GenerateMatches()`, `GetMatches()`, `SetWinner()`, `GetStandings()`, `GetTopN()`, `GetTopRankedDrivers()` |
| `RoundRobinRanker.cs` | `Rank()` — computes `DriverRankResult` list sorted by points → wins → H2H → opponent strength |

### RoundRobinMode/RoundRobinScorecardLogger/

| File | Description |
|------|-------------|
| `RoundRobinScorecardLogger.cs` | `Log()` and `BuildScorecard()` — entry points for generating and displaying standings |
| `RoundRobinScorecardFormatter.cs` | Formats `DriverRankResult` list into a readable text table |
| `RoundRobinScorecardDebug.cs` | Debug-level logging helpers for RR scoring |
| `RoundRobinScorecardWriter.cs` | Writes the formatted scorecard to the Logger |

---

## UI/Forms/

### Cars/

| File | Description |
|------|-------------|
| `AddCarDialog.cs` / `.Designer.cs` | Modal dialog to add or edit a car: CarName, ClassType, DefaultDialIn |
| `SelectCarDialog.cs` / `.Designer.cs` | Dropdown dialog to pick a car from a driver's list (legacy; mostly replaced by direct ListView selection) |

### Common/

| File | Description |
|------|-------------|
| `ScrollableTextDialog.cs` | Reusable modal: shows a scrollable text block (used for RR standings scorecard) |

### Drivers/

| File | Description |
|------|-------------|
| `AddDriverDialog.cs` / `.Designer.cs` | Simple modal: add a driver by name only |
| `AddDriverAndCarDialog.cs` / `.Designer.cs` | Combined modal: add driver + first car in one step |
| `AddEditQualTimeDialog.cs` / `.Designer.cs` | Modal to set or edit a driver's qualifying time |
| `EditDriverDialog.cs` / `.Designer.cs` | Modal to edit driver name and state |
| `DriverManagerForm.cs` / `.Designer.cs` | Full driver registry: list all drivers, CRUD for drivers + cars, view stats |
| `DriverStatsForm.cs` / `.Designer.cs` | View lifetime stats for a single selected driver |

### Main/

| File | Description |
|------|-------------|
| `Form1.cs` | Race console: controller subscription, event handlers, session save, buy-back and finals wiring |
| `Form1.Designer.cs` | Auto-generated layout: ListViews, buttons, labels, panel sizing |
| `Form1.Display.cs` | `RebuildPairingsView()`, `RebuildWinnersView()` — populates the bracket and winners ListViews |
| `Form1.WinnerButtons.cs` | Sets winner button text/tags from `NextMatchReady`; disables BYE-side button |
| `Form1.UI.cs` | General UI helpers: enabling/disabling controls, label updates |

### Results/

| File | Description |
|------|-------------|
| `EditWinnerDialog.cs` / `.Designer.cs` | Modal to override the winner of the active round's resolved match |
| `BuybackDriverSelectionForm.cs` / `.Designer.cs` | Checkbox list of eligible drivers for the Losers Bracket; confirms selection |

### Session/

| File | Description |
|------|-------------|
| `LandingPageForm.cs` / `.Designer.cs` | Main menu: New Event, Load Event, Manage Drivers, Exit |
| `LoadSessionForm.cs` / `.Designer.cs` | Lists saved sessions; user picks one to resume (opens Form1 with loaded session) |
| `SessionSetupForm.cs` | Core session setup logic: race type, class, QMDRA config, roster build, session object creation |
| `SessionSetupForm.UI.cs` | UI helpers for session setup: dynamic control visibility |
| `SessionSetupForm.Events.cs` | Event handlers for session setup form controls |
| `SessionSetupForm.Designer.cs` | Auto-generated layout |

---

## Utils/

| File | Description |
|------|-------------|
| `DictEx.cs` | `Dictionary` extension methods (e.g., `GetValueOrDefault` polyfill) |

---

## ViewModels/

| File | Description |
|------|-------------|
| `PairingRow.cs` | Bracket display row: MatchId, RoundLabel, Driver1, Driver2, IsHeader (round heading rows) |
| `WinnerRow.cs` | Winners list row: MatchId, RoundLabel, Winner, Loser |
| `RaceSessionSummary.cs` | Summary record for session list: Id, EventName, EventDate, ClassType, RaceType |
| `MatchResultSave.cs` | Serializable match result: MatchId, WinnerDriverId, LoserDriverId — also defined in Domain/RaceSession.cs |

---

## Properties/

| File | Description |
|------|-------------|
| `AssemblyInfo.cs` | Assembly metadata |
| `Resources.Designer.cs` | Auto-generated embedded resource accessors (logos, icons) |
| `Settings.Designer.cs` | Auto-generated application settings |

---

## src/RCDragManagerProd.Tests/

Unit and integration tests using MSTest v2, targeting `net48`.

| File | Description |
|------|-------------|
| `Test1.cs` | Placeholder / smoke tests |
| `ByePolicyTests.cs` | Tests for `ByePolicy.IsBye()` |
| `ProLadderEngineAdapterTests.cs` | Tests Pro Ladder bracket generation and winner resolution |
| `RaceEngineFactoryTests.cs` | Tests `RaceEngineFactory.Create()` for all known race type strings |
| `RaceControllerFlowTests.cs` | End-to-end controller flow tests (Pro Ladder path) |
| `RaceControllerRandomFlowTests.cs` | End-to-end controller tests for Random draw mode |
| `RaceControllerQmdraFlowTests.cs` | End-to-end controller tests for QMDRA Round Robin mode |
| `RoundRobinEngineAdapterTests.cs` | Tests the RR adapter's round generation and result submission |
| `RoundRobinStandingsTests.cs` | Tests `RoundRobinRanker` points and tiebreaker logic |
| `RaceSessionRepositoryTests.cs` | Tests save/load/delete against an in-memory SQLite DB |
| `DriverRepositoryRegressionTests.cs` | Regression tests for driver CRUD and stat increments |
| `DriverCarEditBugTests.cs` | Tests car edit/delete flows in `DriverRepository` |
| `DriverManagerCarEditFlowTests.cs` | Integration tests for the full driver + car edit workflow |
| `Helpers/TestDriverFactory.cs` | Factory for constructing `Driver` and `Car` test fixtures |
| `Helpers/NoOpStandingsDialogService.cs` | `IStandingsDialogService` no-op stub for controller tests |
| `MSTestSettings.cs` | MSTest configuration |

---

## src/RCDragManager.CodeStats/

Standalone static analysis tool (separate solution). Not part of the main app runtime.

Scans the main project's source files and generates Markdown/JSON reports of class structure, dependencies, events, repositories, and UI controls. Used for documentation and architecture analysis.

| Folder | Description |
|--------|-------------|
| `Models/` | Data models: `ClassInfo`, `MethodInfo`, `EventInfo`, `RepositoryInfo`, `UIControlInfo`, `DependencyInfo`, `ClassRelationInfo`, `ProjectMap` |
| `Modules/` | Scanners and exporters: `ClassScanner`, `MethodScanner`, `EventScanner`, `RepositoryScanner`, `UIControlScanner`, `ClassRelationAnalyzer`, `DependencyGraphAnalyzer`, `CircularDependencyDetector`, `ProjectMapBuilder`, `JsonExporter`, `MarkdownExporter`, `UIEventMapExporter` |
| `Program.cs` | Entry point: orchestrates scan and export |
