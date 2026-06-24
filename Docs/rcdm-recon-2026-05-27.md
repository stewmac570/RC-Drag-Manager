# RC Drag Manager Architectural Recon (2026-05-27)

## 1. Solution structure
- `src/RCDragManagerProd/RCDragManagerProd.csproj`
  Project name: `RCDragManagerProd`
  Target framework: `.NET Framework v4.8` (`<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>`)
  Project type: `WinForms` (`<OutputType>WinExe</OutputType>`, `System.Windows.Forms` reference)
  Purpose: Main desktop application for race session setup, bracket flow, result entry, persistence, and live update publishing.
- `src/RCDragManagerProd.Tests/RCDragManagerProd.Tests.csproj`
  Project name: `RCDragManagerProd.Tests`
  Target framework: `net48`
  Project type: `Test` (MSTest)
  Purpose: Unit/regression tests for race engines, controller flows, repositories, and bracket logic.
- `src/RCDragManager.CodeStats/RCDragManager.CodeStats.csproj`
  Project name: `RCDragManager.CodeStats`
  Target framework: `net8.0`
  Project type: `Console/Exe` (`<OutputType>Exe</OutputType>`)
  Purpose: Auxiliary code-analysis/statistics utility project.

## 2. Top-level folder map
```text
src/
├─ ProjectAnalysis/
├─ RCDragManager.CodeStats/
│  ├─ Models/
│  ├─ Modules/
│  └─ ProjectAnalysis/
├─ RCDragManagerProd/
│  ├─ Assets/
│  ├─ Config/
│  ├─ Controllers/
│  ├─ Domain/
│  │  └─ Ladders/
│  ├─ Helpers/
│  ├─ Integration/
│  ├─ Logging/
│  ├─ RaceEngines/
│  ├─ RandomMode/
│  ├─ Repositories/
│  ├─ RoundRobinMode/
│  │  └─ RoundRobinScorecardLogger/
│  ├─ UI/
│  │  └─ Forms/
│  ├─ Utils/
│  └─ ViewModels/
└─ RCDragManagerProd.Tests/
   └─ Helpers/
```
- `src/ProjectAnalysis/`: Appears to hold analysis artifacts.
- `src/RCDragManager.CodeStats/`: Code statistics utility (`Program`, model and module folders).
- `src/RCDragManagerProd/Assets`: Application icons/images.
- `src/RCDragManagerProd/Config`: Runtime app settings load/save (`AppSettings`).
- `src/RCDragManagerProd/Controllers`: `RaceController` partials for round flow, results, persistence, stats, live updates.
- `src/RCDragManagerProd/Domain`: Core domain objects (`Driver`, `Car`, `RaceSession`, ladder structures).
- `src/RCDragManagerProd/Domain/Ladders`: Pro ladder shape definitions for ladder sizes.
- `src/RCDragManagerProd/Helpers`: Utility helpers (asset paths, match lookup).
- `src/RCDragManagerProd/Integration`: HTTP live update client and DTOs.
- `src/RCDragManagerProd/Logging`: File logger.
- `src/RCDragManagerProd/RaceEngines`: Engine interface/adapters/factory.
- `src/RCDragManagerProd/RandomMode`: Random bracket + losers bracket logic.
- `src/RCDragManagerProd/Repositories`: SQLite repositories + schema initializer.
- `src/RCDragManagerProd/RoundRobinMode`: Round-robin engine, ranking, scorecard logging.
- `src/RCDragManagerProd/UI/Forms`: WinForms UI for session setup, race operation, results, settings.
- `src/RCDragManagerProd/Utils`: Small extension helpers.
- `src/RCDragManagerProd/ViewModels`: UI-facing row/summary models.
- `src/RCDragManagerProd.Tests/Helpers`: Test support classes.

## 3. Data model
- `Driver` — `src/RCDragManagerProd/Domain/Drivers.cs`
  Public properties: `int Id`, `string Name`, `double? QualTime`, `string Notes`, `int TotalWins`, `int TotalLosses`, `int EventsEntered`, `int EventsWon`, `int? Seed`, `string State`, `List<Car> Cars`.
- `Car` — `src/RCDragManagerProd/Domain/Car.cs`
  Public properties: `int Id`, `int CarID`, `int DriverId`, `string CarName`, `string ClassType`, `double? DefaultDialIn`.
- `MultiClassEvent` — `src/RCDragManagerProd/Domain/MultiClassEvent.cs`
  Public properties: `int Id`, `string EventName`, `DateTime EventDate`, `List<RaceSession> ClassSessions`.
- `RaceSession` — `src/RCDragManagerProd/Domain/RaceSession.cs`
  Public properties: `int Id`, `Guid EventId`, `string EventName`, `DateTime EventDate`, `string RaceType`, `string ClassType`, `double? FixedDialIn`, `string RoundRobinVariant`, `int? RoundsToRun`, `List<RaceSessionDriverEntry> DriverEntries`, `List<int[]> PairingHistoryRaw`, `HashSet<(int, int)> PairingHistory`, `List<MatchResultSave> SavedResults`, `List<string> SavedRevealedRounds`, `List<RoundRobinMatch> RoundRobinMatches`, `List<RandomMatch> Matches`, `List<Driver> BuybackDrivers`, `List<Driver> TopDriversSnapshot`, `List<Driver> Drivers`.
- `RaceSessionDriverEntry` — `src/RCDragManagerProd/Domain/RaceSession.cs`
  Public properties: `int DriverID`, `string DriverName`, `int CarID`, `string CarName`, `string ClassType`, `double? DialIn`, `double? QualifyingTime`, `int? Seed`.
- `MatchResultSave` (domain) — `src/RCDragManagerProd/Domain/RaceSession.cs`
  Public properties: `int MatchId`, `int WinnerDriverId`, `int LoserDriverId`.
- `ProLadder.LadderMatch` — `src/RCDragManagerProd/Domain/ProLadder.Structures.cs`
  Public properties: `int MatchId`, `int? Seed1`, `int? Seed2`, `int? FromMatch1`, `int? FromMatch2`, `string RoundLabel`.
- `MatchResult` — `src/RCDragManagerProd/Domain/MatchResult.cs`
  Public properties: None (public behavior class wrapping an internal results dictionary).
- `RandomMatch` — `src/RCDragManagerProd/RandomMode/RandomMatch.cs`
  Public properties: `int MatchId`, `Driver Seed1`, `Driver Seed2`, `int? FromMatch1`, `int? FromMatch2`, `string RoundLabel`.
- `RoundRobinMatch` — `src/RCDragManagerProd/RoundRobinMode/RoundRobinMatch.cs`
  Public properties: `int MatchId`, `Driver Driver1`, `Driver Driver2`, `string RoundLabel`.
- `EngineMatch` — `src/RCDragManagerProd/RaceEngines/IRaceEngine.cs`
  Public properties: `int MatchId`, `Driver Driver1`, `Driver Driver2`, `string RoundLabel`, `int? FromMatch1`, `int? FromMatch2`, `bool HasResult`.
- `DriverRankResult` — `src/RCDragManagerProd/RoundRobinMode/RoundRobinRanker.cs`
  Public properties: `int DriverId`, `int Rank`, `double Points`, `int Wins`, `int Losses`, `int[] DefeatedIds`, `double OpponentStrength`.

## 4. Persistence layer
- Storage mechanism: `SQLite` via raw `System.Data.SQLite` ADO.NET repositories (`DriverRepository`, `CarRepository`, `RaceSessionRepository`, `MultiClassEventRepository`) in `src/RCDragManagerProd/Repositories/`.
- Database path pattern: `%APPDATA%\RC_Drag_Manager\race_data.db` set in `src/RCDragManagerProd/Program.cs` (`ConnectionString = Data Source={dbPath};Version=3;`).
- Connection string behavior: repository constructors accept either full connection string or file path; relative paths normalize under `%APPDATA%\RC_Drag_Manager`.
- Schema definitions: inline SQL in `src/RCDragManagerProd/Repositories/DatabaseInitializer.cs` creating `Drivers`, `Cars`, `RaceSessions`, `MultiClassEvents` (+ index definitions).
- Data shape in DB:
  - `Drivers`/`Cars`: relational rows.
  - `RaceSessions.SessionData`: JSON blob (`System.Text.Json` serialized `RaceSession`).
  - `MultiClassEvents.EventData`: JSON blob (`System.Text.Json` serialized `MultiClassEvent`).
- Seed data: not present.
- Other persistent files:
  - `%APPDATA%\RC_Drag_Manager\appsettings.json` via `src/RCDragManagerProd/Config/AppSettings.cs`.
  - `%APPDATA%\RC_Drag_Manager\app.log` via `src/RCDragManagerProd/Logging/Logger.cs`.

## 5. Race result flow
- Manual result entry UI form:
  - Primary entry point: `src/RCDragManagerProd/UI/Forms/Main/Form1.cs` + `Form1.WinnerButtons.cs` (`btnWinner1_Click`/`btnWinner2_Click` -> `HandleWinnerClick` -> `_controller.SubmitWinner`).
  - Manual correction flow: `btnEditResult_Click` in `src/RCDragManagerProd/UI/Forms/Main/Form1.Events.cs` using in-form winner picker (`ShowWinnerPicker`) and `_controller.EditWinnerInActiveRound`.
  - Additional dialog class exists: `src/RCDragManagerProd/UI/Forms/Results/EditWinnerDialog.cs`.
- Controller/result write path:
  - `RaceController.SubmitWinner` in `src/RCDragManagerProd/Controllers/RaceController.Results.cs` chooses winner/loser, calls engine (`EngineSetWinner`), stores in domain result store (`_matchResult.SetWinner`), updates `_winners`, advances round state, queues live update.
  - Session persistence collects results into `RaceSession.SavedResults` in `src/RCDragManagerProd/Controllers/RaceController.Persistence.cs`.
- File import functionality:
  - `OpenFileDialog`: not present.
  - `StreamReader`: not present.
  - `FileSystemWatcher`: not present.
  - Generic file IO present only for app settings/log files, not race-result ingestion.
- Serial/COM port code:
  - `SerialPort`, `System.IO.Ports`, `BaudRate`, COM listener code: not present.
- Network listening code:
  - `TcpListener`, `HttpListener`, Web API host, SignalR server: not present.
  - Outbound HTTP client exists (`src/RCDragManagerProd/Integration/LiveApiClient.cs`) for live broadcast push/pull.
- Conclusion: no automated result ingestion present.

## 6. Existing extension points
- `IRaceEngine` interface (`src/RCDragManagerProd/RaceEngines/IRaceEngine.cs`) with adapters (`ProLadderEngineAdapter`, `RandomEngineAdapter`, `RoundRobinEngineAdapter`) selected by `RaceEngineFactory`.
- `IStandingsDialogService` interface (`src/RCDragManagerProd/Controllers/IStandingsDialogService.cs`) for standings presentation abstraction.
- Event-based hooks inside controller/UI (C# events), e.g. `RaceController` exposes `BracketRedrawn`, `NextMatchReady`, `WinnersUpdated`, `CanAdvanceChanged`, `TournamentCompleted` and UI subscribes.
- Dependency injection containers/plugin loaders/message buses (MEF, Unity, Autofac, MediatR, custom plugin loader): not present.
- Generic result-provider/data-source interfaces for external timing feeds (`IDataSource`, `IResultProvider`): not present.

## 7. External dependencies
### Data access
- `Stub.System.Data.SQLite.Core.NetFramework` `1.0.119.0`
- `System.Data.SQLite.Core` `1.0.119.0`

### UI
- `QRCoder` `1.6.0`

### Logging
- Nothing found.

### Networking
- Nothing found.

### Testing
- `MSTest` `4.0.1`

### Utility
- `Microsoft.Bcl.AsyncInterfaces` `9.0.5`
- `System.Buffers` `4.5.1`
- `System.IO.Pipelines` `9.0.5`
- `System.Memory` `4.5.5`
- `System.Numerics.Vectors` `4.5.0`
- `System.Resources.Extensions` `4.7.1`
- `System.Runtime.CompilerServices.Unsafe` `6.0.0`
- `System.Text.Encodings.Web` `9.0.5`
- `System.Text.Json` `9.0.5`
- `System.Threading.Tasks.Extensions` `4.5.4`
- `System.ValueTuple` `4.5.0`

### Integration-relevant package flags
- Relevant: `System.Data.SQLite.Core` / `Stub.System.Data.SQLite.Core.NetFramework` (existing DB stack).
- Paradox/BDE/ODBC-specific packages for `.db` timing files: not present.
- Serial integration packages: not present.
- File watcher/inbox integration packages: not present.

## 8. Test coverage of result handling
- Test project: `src/RCDragManagerProd.Tests/RCDragManagerProd.Tests.csproj`.
- Result entry / bracket progression test classes:
  - `RaceControllerFlowTests` — `src/RCDragManagerProd.Tests/RaceControllerFlowTests.cs`
  - `RaceControllerRandomFlowTests` — `src/RCDragManagerProd.Tests/RaceControllerRandomFlowTests.cs`
  - `RaceControllerRRStandardFlowTests` — `src/RCDragManagerProd.Tests/RaceControllerRRStandardFlowTests.cs`
  - `RaceControllerQmdraFlowTests` — `src/RCDragManagerProd.Tests/RaceControllerQmdraFlowTests.cs`
  - `RaceControllerResetTests` — `src/RCDragManagerProd.Tests/RaceControllerResetTests.cs`
  - `RoundRobinEngineAdapterTests` — `src/RCDragManagerProd.Tests/RoundRobinEngineAdapterTests.cs`
  - `ProLadderEngineAdapterTests` — `src/RCDragManagerProd.Tests/ProLadderEngineAdapterTests.cs`
  - `RandomEngineAdapterTests` — `src/RCDragManagerProd.Tests/Test1.cs`
  - `RoundRobinStandingsTests` — `src/RCDragManagerProd.Tests/RoundRobinStandingsTests.cs`
  - `RoundRobinRoundClampingTests` — `src/RCDragManagerProd.Tests/RoundRobinRoundClampingTests.cs`
  - `LosersBracketBuilderTests` — `src/RCDragManagerProd.Tests/LosersBracketBuilderTests.cs`
  - `RandomBracketByeTrackerTests` — `src/RCDragManagerProd.Tests/RandomBracketByeTrackerTests.cs`
  - `MatchResultTests` — `src/RCDragManagerProd.Tests/MatchResultTests.cs`
- Racer management/repository test classes:
  - `DriverRepositoryRegressionTests` — `src/RCDragManagerProd.Tests/DriverRepositoryRegressionTests.cs`
  - `DriverRepositoryStatIncrementTests` — `src/RCDragManagerProd.Tests/DriverRepositoryStatIncrementTests.cs`
  - `DriverCarEditBugTests` — `src/RCDragManagerProd.Tests/DriverCarEditBugTests.cs`
  - `DriverManagerCarEditFlowTests` — `src/RCDragManagerProd.Tests/DriverManagerCarEditFlowTests.cs`
  - `RaceSessionRepositoryTests` — `src/RCDragManagerProd.Tests/RaceSessionRepositoryTests.cs`
  - `PairingHistorySerializationTests` — `src/RCDragManagerProd.Tests/PairingHistorySerializationTests.cs`
  - `MultiClassEventRepositoryTests` — `src/RCDragManagerProd.Tests/MultiClassFeatureTests.cs`
  - `RaceControllerMultiClassTests` — `src/RCDragManagerProd.Tests/MultiClassFeatureTests.cs`
  - `MultiClassLbGateTests` — `src/RCDragManagerProd.Tests/MultiClassFeatureTests.cs`

## 9. Build and deploy notes
From `Installer/installer.iss`:
- Install location pattern: `{localappdata}\Programs\RC Drag Manager` (`DefaultDirName`).
- Files deployed: everything under `Installer/Payload/*` recursively to `{app}` excluding `README.txt`.
- Database bundling behavior:
  - Installer does not explicitly deploy `race_data.db`.
  - Runtime creates `%APPDATA%\RC_Drag_Manager\race_data.db` on first run in `src/RCDragManagerProd/Program.cs` and initializes schema via `DatabaseInitializer`.
- App data folder handling in installer: creates `{userappdata}\RC_Drag_Manager` and notes preserving roaming app data.
- Pre-install action: .NET Framework 4.8+ prerequisite check in `[Code] InitializeSetup` (registry `Release >= 528040`), setup aborts with message if missing.
- Post-install action: optional launch of app (`[Run]` entry with `postinstall`).

## 10. Open questions
1. `src/RCDragManagerProd/App.config` contains `LiveUpdateEnabled`, `LiveUpdateUrl`, and `ApiKey`, while runtime settings are also persisted in `%APPDATA%\RC_Drag_Manager\appsettings.json`; precedence/authoritative config source is not explicit.
2. `src/RCDragManagerProd/UI/Forms/Results/EditWinnerDialog.cs` exists, but edit flow in `Form1` uses a separate in-form dynamic picker (`ShowWinnerPicker`); intended canonical edit UI is unclear.
3. `src/RCDragManagerProd/Domain/Car.cs` has both `Id` and alias `CarID`; mixed identity naming could affect external mapping conventions.
4. `src/RCDragManagerProd/Domain/RaceSession.cs` and `src/RCDragManagerProd/ViewModels/MatchResultSave.cs` both define `MatchResultSave` classes in different namespaces; intended long-term boundary between domain and UI save models is not explicit.
5. `src/RCDragManagerProd/Controllers/RaceController.Persistence.cs` infers `RaceType` from engine/round labels when blank; expected persisted race-type source of truth is not explicit.
6. `src/RCDragManagerProd/RoundRobinMode/RoundRobinMatch.cs` is declared in global namespace (no namespace block), unlike most project files; whether intentional or accidental is unclear.
7. `src/RCDragManagerProd/Program.cs` includes comment `//claude agent test`; uncertain whether this marks unfinished cleanup or deliberate sentinel.
8. `src/RCDragManagerProd/Integration/LiveApiClient.cs` provides outbound update transport, but there is no corresponding inbound ingestion contract for external timing systems; intended integration seam is not codified.
9. `src/RCDragManagerProd/Logging/Logger.cs`, `Program.cs`, and `AppSettings.cs` contain swallowed exception paths (`catch {}`), which makes failure visibility behavior policy unclear.
10. No `.db` file import/watcher pipeline exists in current code; expected orchestration point for third-party timing file reconciliation must be defined externally.
