# RC Drag Manager — Codebase Map

All source files in `src/RCDragManagerProd/`, organised by folder. The WPF UI
lives in `src/RCDragManagerProd.WPF/` (see the section at the end and the WPF
UI section of `CLAUDE.md`).

---

## AppServices/

UI-agnostic services extracted from the forms (issues #284–#291); both the
legacy WinForms UI and the WPF UI bind to these.

| File | Description |
|------|-------------|
| `RaceConsoleService.cs` | Race-console orchestration: winner submission, primary action, buybacks, edit-result validation, save/close, completion stats (`RecordTournamentCompletion`, `RecomputeEventsWon`) |
| `RaceConsoleViewModel.cs` / `RaceConsoleViewModelBuilder.cs` | Console state snapshot + builder |
| `LoadSessionService.cs` | Lists/loads saved sessions and multi-class events with typed failure results |
| `MultiClassSetupService.cs` | Multi-class event setup validation and construction |
| `MultiClassRaceService.cs` | Multi-class coordination helpers (per-class completion stats) |
| `SessionRosterService.cs` | Roster validation/sync between the console grid and the session |
| `DriverManagerService.cs` / `DriverStatsService.cs` | Driver registry and stats screens' logic |
| `RaceResultsPresentationBuilder.cs` / `ClassCompletionPresentationBuilder.cs` | Build result/ladder presentations from a saved session |
| `EventSettingsService.cs` | Rules behind the per-event Settings tab: pure functions over class state deciding what may change mid-event |
| `RaceRosterService.cs` | Roster service for the race console: add existing drivers, drop drivers from the race (driver DB untouched) |
| `RosterAddResult.cs` | Typed result for roster additions (Success, Error, Driver, WasExisting) |
| `EventCompletionPresentationBuilder.cs` | Builds the end-of-event board from each class's saved `RaceResultsArchive` |

---

## Root

| File | Description |
|------|-------------|
| `Program.cs` | App entry point: initializes settings, DB, global exception handlers, opens `LandingForm` (in `LandingPageForm.cs`) |
| `RCDragManagerProd.csproj` | Project file: .NET Framework 4.8, NuGet references, build targets |
| `RCDragManagerProd.sln` | Solution file for the two app projects in `src/` (no Tests project); the repo-root `RCDragManagerProd.sln` adds the test project |
| `App.config` | App configuration: supported runtime, app settings (logging, log path, live update), assembly binding redirects |
| `app.manifest` | Application manifest: Windows compatibility / DPI settings |
| `packages.config` | NuGet package references. A clean rebuild needs `System.Resources.Extensions` restored or the WinForms build fails with `MSB3822` (see `CLAUDE.md`) |
| `lib/System.Resources.Extensions.dll` | Checked-in assembly referenced by `HintPath` |

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
| `RaceController.EngineCalls.cs` | `EngineGetMatches()`, `EngineSetWinner()`, `EngineHasWinner()`, etc. — logging + null-safe engine-call adapters |
| `RaceController.LiveUpdate.cs` | `QueueLiveUpdate()`, `BuildLiveRaceUpdateDto()`, `BroadcastLiveSnapshot()` — optional HTTP live feed push |
| `RaceController.DialIn.cs` | Dial-in state: `GetDriverDialIn()`, `UpdateDriverDialIn()`, lock/unlock, live-site poll timer, `DialInsChanged` event |
| `RaceController.Resume.cs` | `RestoreFromSave()` — rebuilds engine state from a saved session (mid-event resume) |
| `RaceController.MultiClass.cs` | Multi-class coordination helpers (`IsRrComplete()`, pending-match queries) |
| `RaceController.SaveClose.cs` | `SaveProgress()` / close-race orchestration |
| `RaceController.ResultSnapshots.cs` | Captures completed-result snapshots for results-only viewing |
| `RaceController.RoundFlow.Defer.cs` | `PushCurrentMatchToEndOfRound()` — "more time" deferral |
| `RaceController.Stats.cs` | Legacy stats helpers: `PersistMatchStats()` and `PersistEventWon()` remain the Form1 path, `PersistTournamentStats()` is unused; completion stats go through `RaceConsoleService` |
| `LaneFairnessManager.cs` | Tracks lane (left/right) history per driver; `ShouldSwap(key, driver1Id, driver2Id)` decides whether to swap the pairing |
| `IStandingsDialogService.cs` | Interface for showing the RR standings popup; default impl uses `ScrollableTextDialog` |

---

## Domain/

| File | Description |
|------|-------------|
| `Drivers.cs` | `Driver` entity: Id, Name, QualTime, TotalWins, TotalLosses, EventsEntered, EventsWon, Seed, State, Cars |
| `Car.cs` | `Car` entity: Id/CarID alias, DriverId, CarName, ClassType, DefaultDialIn |
| `RaceSession.cs` | `RaceSession` (full session state), `RaceSessionDriverEntry` (per-driver snapshot), `MatchResultSave` (serializable result record) |
| `MultiClassEvent.cs` | Parent object for multi-class events: EventName, EventDate, `ClassSessions` (one `RaceSession` per class) |
| `MultiCarRaceEntry.cs` | One car competing in a multi-car RR class; engine identity is the entry, not the person |
| `RaceResults.cs` | `RaceResultsArchive` and its phase/match/standings snapshot types |
| `ResumeSnapshot.cs` | Bracket-structure + phase snapshot for mid-event resume (`SavedMatch` children) |
| `MatchResult.cs` | In-memory result store: `SetWinner`, `GetWinner`, `GetLoser`, `HasResult`, `ClearFromMatch`, `IsTournamentComplete` |
| `ByePolicy.cs` | `IsBye(Driver d)` — true if `d == null` |
| `RoundLabels.cs` | Round label normalization (`"R1"`, `"SF"`, `"F"`, `"LB-R1"`, `"LB-F"`, `"RR1"`, …), compare/sort keys |
| `RaceTypes.cs` | Race-type string constants (RoundRobin, MultiCarRoundRobin, ...) + `IsRoundRobinFormat()` |
| `ProLadder.cs` | Partial class shell (empty body) |
| `ProLadder.Structures.cs` | `ProLadder.LadderMatch` class: MatchId, Seed1, Seed2, FromMatch1, FromMatch2, RoundLabel |
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
| `Logger.cs` | Static facade: `Log` (info) + `Debug` (gated by `AppSettings.VerboseLogging`); respects `AppSettings.EnableLogging` |
| `LogWriter.cs` | Shared lock-guarded writer: one file handle, AutoFlush, 5 MB roll to `app.log.1` in `%APPDATA%\RC_Drag_Manager` |

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
| `DatabaseInitializer.cs` | Idempotent schema creation: `CREATE TABLE IF NOT EXISTS` for Drivers, Cars, RaceSessions, MultiClassEvents |
| `DriverRepository.cs` | Full CRUD for Drivers + Cars; stat increment methods; `ComputeEventsWonFromSavedSessions()` |
| `CarRepository.cs` | Lightweight Car-only CRUD (partial overlap with DriverRepository) |
| `RaceSessionRepository.cs` | `SaveSession()` (INSERT first save / UPDATE after), `GetAllSessions()` (summary list), `TryLoadSession(id)` (typed load result), `DeleteSession(id)` |
| `MultiClassEventRepository.cs` | Same pattern for `MultiClassEvent` parent objects (`SaveEvent`/`GetAllEvents`/`LoadEvent`/`DeleteEvent`) |
| `DbDate.cs` | Invariant-culture format/parse for stored EventDate strings |

---

## RoundRobinMode/

| File | Description |
|------|-------------|
| `RoundRobinMatch.cs` | Match data class: MatchId, RoundLabel, Driver1, Driver2 |
| `RoundRobinEngine.cs` | Circle-method scheduler: `GenerateMatches()`, `GetMatches()`, `SetWinner()`, `GetStandings()`, `GetTopN()`, `GetTopRankedDrivers()` |
| `RoundRobinRanker.cs` | `Rank()` — computes a `DriverRankResult` list sorted by TotalScore desc, then DriverId (TotalScore = points + 0.1 × head-to-head + 0.001 × beaten-drivers score) |
| `MultiCarRoundRobinScheduler.cs` | Builds the multi-car RR fixture from `MultiCarRaceEntry` lists; exhausts fresh pairings before repeating |
| `MultiCarOpeningRoundPlanner.cs` | Seeds the opening buyback round so two car entries owned by the same driver don't meet |

### RoundRobinMode/RoundRobinScorecardLogger/

| File | Description |
|------|-------------|
| `RoundRobinScorecardLogger.cs` | `Log()` — entry point for generating and displaying standings |
| `RoundRobinScorecardFormatter.cs` | Rebuilds aggregates from `RoundRobinEngineAdapter` + `MatchResult` and returns the scorecard as a string |
| `RoundRobinScorecardDebug.cs`, `RoundRobinScorecardWriter.cs` | Empty partial-class shells (no members) |

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
| `RaceDialogs.cs` | Shared race-day message-box helpers (issue #258) |
| `SettingsForm.cs` | App settings form: enable logging, log file path, enable live broadcast to the public site |

### Drivers/

| File | Description |
|------|-------------|
| `AddDriverDialog.cs` / `.Designer.cs` | Modal: add a driver by name, with a qualifying time that is captured and validated |
| `AddDriverAndCarDialog.cs` / `.Designer.cs` | Combined modal: add driver + first car in one step |
| `AddEditQualTimeDialog.cs` / `.Designer.cs` | Modal to set or edit a driver's qualifying time |
| `EditDriverDialog.cs` / `.Designer.cs` | Modal to edit driver name and state |
| `DriverManagerForm.cs` / `.Designer.cs` | Driver registry shell: constructor + service wiring |
| `DriverManagerForm.Events.cs` | CRUD event handlers for the driver manager |
| `DriverManagerForm.UI.cs` | Grid/panel updates for the driver manager |
| `DriverStatsForm.cs` / `.Designer.cs` | View lifetime stats for a single selected driver |

### Main/

| File | Description |
|------|-------------|
| `Form1.cs` | Race console: controller subscription + session save |
| `Form1.Designer.cs` | Auto-generated layout: ListViews, buttons, labels, panel sizing |
| `Form1.Events.cs` | Event handlers: bracket generation, buyback, finals wiring |
| `Form1.Display.cs` | `RedrawFullBracket()`, `OnWinnersUpdated()`, `OnNextMatchReady()`, `ApplyByeButtonStyle()` — populates the bracket and winners ListViews |
| `Form1.WinnerButtons.cs` | `HandleWinnerClick()`, `ShowWinnerPicker()`, `UpdateDriverStats()`, `BumpEventWon()` |
| `Form1.UI.cs` | General UI helpers: enabling/disabling controls, label updates |
| `MultiClassRaceForm.cs` / `.Designer.cs` | WinForms multi-class race form hosting one console per class (legacy) |
| `QRCodeDialog.cs` | "Public Site QR" dialog: scannable QR for the public live scoreboard site |

### Results/

| File | Description |
|------|-------------|
| `EditWinnerDialog.cs` / `.Designer.cs` | Modal to override the winner of the active round's resolved match |
| `BuybackDriverSelectionForm.cs` / `.Designer.cs` | Checkbox list of eligible drivers for the Losers Bracket; confirms selection |

### Session/

| File | Description |
|------|-------------|
| `LandingPageForm.cs` / `.Designer.cs` | Main menu with five buttons: "Create Race Session", "Load Saved Event", "Driver Lists", "Settings", "Exit" |
| `LoadSessionForm.cs` / `.Designer.cs` | Lists saved sessions; user picks one to resume (opens `MultiClassRaceForm`) |
| `MultiClassSetupForm.cs` / `.Designer.cs` | Session setup: race type, class config, roster build, session object creation |
| `MultiClassConfigDialog.cs` / `.Designer.cs` | Per-class configuration dialog used during setup (race type, RR variant, rounds) |

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
| `MultiClassEventSummary.cs` | Summary row for the Load Event list (Id, EventName, EventDate, ClassCount) |
| `RaceResultsPresentation.cs` | Results presentation model: phase/round/match cards, scoring legend, standings rows |
| `ClassCompletionPresentation.cs` | Per-class completion presentation |
| `EventCompletionPresentation.cs` | End-of-event board: one row per class with champion and runner-up (replaces the old ASCII block) |
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

Unit and integration tests using MSTest 4 (MSTest meta-package), targeting
`net48`. Around 60 test files, one per class under test, at the project root,
plus reusable seams in `Helpers/`.

| File | Description |
|------|-------------|
| `Helpers/TestDriverFactory.cs` | Driver packs (`CreateProLadderPack`, `CreateProLadderByePack`, `CreateRoundRobinPack(n)`) |
| `Helpers/TestSessionFactory.cs` | `RaceSession` / `MultiClassEvent` builders |
| `Helpers/NoOpStandingsDialogService.cs` | `IStandingsDialogService` no-op stub |
| `Helpers/RecordingStandingsDialogService.cs` | Records `Show(...)` calls |
| `Helpers/RecordingSessionStore.cs` | `IRaceSessionStore` double counting `Persist()` calls |

Notable files include `RandomEngineAdapterTests` (in `Test1.cs`),
`RaceSessionRepositoryTests` (temp-file SQLite), `MultiClassFeatureTests`
(multi-class gates incl. the 11 skipped placeholders), and
`WindowSizingStandardTests` (parses WPF XAML to enforce the window sizing
standard).

Test DB fixtures use a temp-file SQLite database (deleted on dispose), never
`:memory:`.

---

## src/RCDragManager.CodeStats/

Standalone static analysis tool (separate solution). Not part of the main app runtime.

Scans the main project's source files and generates Markdown/JSON reports of class structure, dependencies, events, repositories, and UI controls. Used for documentation and architecture analysis.

| Folder | Description |
|--------|-------------|
| `Models/` | Data models: `ClassInfo`, `MethodInfo`, `EventInfo`, `RepositoryInfo`, `UIControlInfo`, `DependencyInfo`, `ClassRelationInfo`, `ProjectMap` |
| `Modules/` | Scanners and exporters: `ClassScanner`, `MethodScanner`, `EventScanner`, `RepositoryScanner`, `UIControlScanner`, `ClassRelationAnalyzer`, `DependencyGraphAnalyzer`, `CircularDependencyDetector`, `ProjectMapBuilder`, `JsonExporter`, `MarkdownExporter`, `UIEventMapExporter` |
| `Program.cs` | Entry point: orchestrates scan and export |

> `src/ProjectAnalysis/` holds this tool's **generated output** (ProjectMap.json,
> Methods.md, …). A second divergent copy sits at
> `src/RCDragManager.CodeStats/ProjectAnalysis/`. Both are point-in-time
> snapshots and their line numbers drift from the source: regenerate before
> trusting either, and prefer reading the code directly.

---

## src/RCDragManagerProd.WPF/ (current UI — v2.0.0)

The shipped WPF app. Views hold no race logic; they bind to the AppServices
above and subscribe to `RaceController` events. See the "WPF UI" section of
`CLAUDE.md` for conventions (themed dialogs, `Brush.*` resources, Dispatcher
marshalling).

| Path | Description |
|------|-------------|
| `App.xaml(.cs)` | Startup: settings, theme, DB init, global exception handler, opens `LandingWindow` |
| `Windows/` | Top-level windows: Landing, Setup, LoadSession, DriverManager, DriverStats, RaceConsole, MultiClassRace, Settings, LiveScoreboard |
| `Views/RaceConsoleView.xaml(.cs)` | One class's race console; hosted standalone or one-per-tab in `MultiClassRaceWindow` |
| `Views/EventSettingsView.xaml(.cs)` | The event settings tab (first tab of every event): class reset with typed confirmation, buybacks per class, live theme switching |
| `Dialogs/` | Themed modal dialogs incl. `MessageDialog` (the dark `MessageBox` replacement), results/buyback/edit dialogs |
| `ViewModels/` | INotifyPropertyChanged view models + display-row types |
| `Resources/Theme.xaml` | Brushes (`Brush.*` bound to `C.*` colours), radii, font sizes |
| `Resources/Styles.xaml` | Control styles (buttons, grids, title-bar traffic lights) |
| `ThemeManager.cs` | Dark/light palette (`C.*` colour dictionary swap at runtime) |
| `WindowSizing.cs` | Work-area clamping, borderless-maximize constraints, Win11 rounded corners |
| `WpfStandingsDialogService.cs` | `IStandingsDialogService` implementation using themed dialogs |
| `Converters.cs` | Value converters used by the XAML views |
