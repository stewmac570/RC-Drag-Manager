# Live Integration Step 2 Controller Review

## Exact Files Inspected
- `src/RCDragManagerProd/Controllers/RaceController.cs`
- `src/RCDragManagerProd/Controllers/RaceController.EngineCalls.cs`
- `src/RCDragManagerProd/Controllers/RaceController.Logging.cs`
- `src/RCDragManagerProd/Controllers/RaceController.Persistence.cs`
- `src/RCDragManagerProd/Controllers/RaceController.Results.cs`
- `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs`
- `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Finals.cs`
- `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Losers.cs`
- `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.View.cs`
- `src/RCDragManagerProd/Controllers/RaceController.Session.cs`

## Exact Methods Found
- `GenerateBracket(string raceType, List<Driver> drivers)`
  - File: `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs`
- `GenerateBracket(string raceType)`
  - File: `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs`
- `SubmitWinner(int matchId, bool firstOption)`
  - File: `src/RCDragManagerProd/Controllers/RaceController.Results.cs`
- `AdvanceRound()`
  - File: `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs`
- `BuildCurrentBracketRows()`
  - File: `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.View.cs`
- `PushNextMatch()`
  - File: `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs`
- `GetActiveRoundLabel()`
  - File: `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.View.cs`

## RaceController Partial Classes Found
- `RaceController.cs` (sealed partial base)
- `RaceController.EngineCalls.cs`
- `RaceController.Logging.cs`
- `RaceController.Persistence.cs`
- `RaceController.Results.cs`
- `RaceController.RoundFlow.Core.cs`
- `RaceController.RoundFlow.Finals.cs`
- `RaceController.RoundFlow.Losers.cs`
- `RaceController.RoundFlow.View.cs`
- `RaceController.Session.cs`

## Recommended Source Fields For Live DTO
- `eventName`:
  - Source: `_session?.EventName`
  - Fallback: `"Quick Session"` if null/blank
- `eventDate`:
  - Source: `_session?.EventDate`
  - Format: `yyyy-MM-dd`
  - Validity check: skip live send if `EventDate` is unset/default (`DateTime.MinValue`)
- `currentRound`:
  - Source: `GetActiveRoundLabel()`
  - Fallback: first revealed round from `_revealedRounds` if needed
- `nextUp`:
  - Source: controller engine truth (same criteria as `PushNextMatch()`), not UI text
  - Query: first unresolved match in revealed rounds from `EngineGetMatches(_engine)`
  - Format: lane-adjusted names via `GetLaneAdjustedNames(...)`, then `"Driver1 vs Driver2"`
- `matches`:
  - Source: `BuildCurrentBracketRows()`
  - Use only non-header rows (`IsHeader == false`) as visible bracket matches

## Exact Place To Insert New Methods
- Add new controller partial file:
  - `src/RCDragManagerProd/Controllers/RaceController.LiveUpdate.cs`
- Put both methods in that file:
  - `private LiveRaceUpdateDto BuildLiveRaceUpdateDto()`
  - `private void QueueLiveUpdate(string reason)`
- Add private field in same file:
  - `private readonly LiveRaceUpdateClient _liveRaceUpdateClient = new LiveRaceUpdateClient();`

## Exact First Hook Point To Use
- Hook only this path now:
  - End of successful `SubmitWinner(int matchId, bool firstOption)`
  - File: `src/RCDragManagerProd/Controllers/RaceController.Results.cs`
  - Placement: after existing state updates and RR per-round logging, immediately before method return/end
  - Call: `QueueLiveUpdate("SubmitWinner")`

## Blockers / Uncertainties
- `RaceController` is split across partial files and currently has no live-update partial; adding one is safe but requires project file include because this is a non-SDK `.csproj` with explicit `<Compile Include=...>` entries.
- `LiveRaceUpdateClient` and DTO files were previously added under `Integration/`, but are not currently listed in `RCDragManagerProd.csproj`; controller usage will not compile unless those files (and the new controller partial) are included.
- `eventDate` may be default/unset in some sessions; this should be treated as invalid for send and logged with `[LIVE][SKIP]`.
