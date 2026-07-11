# Live Integration Review

## 1. Current Architecture Summary
- Race state flow is `Form1` (UI actions/events) -> `RaceController` (state + orchestration) -> `IRaceEngine` adapters (`RoundRobinEngineAdapter`, `ProLadderEngineAdapter`, `RandomEngineAdapter`) for bracket/match truth.
- UI button handlers in `UI/Forms/Main/Form1.Events.cs` call controller methods such as:
  - `GenerateBracket(...)`
  - `SubmitWinner(...)` (via `HandleWinnerClick`)
  - `AdvanceRound()`
  - `StartLosersBracket()` / `SetBuybackDrivers(...)`
  - `EditWinnerInActiveRound(...)`
  - `Reset()`
- Controller owns live race state primitives:
  - Session/event metadata: `_session` (`Domain/RaceSession.cs`)
  - Active engine(s): `_engine`, `_losersEngine`
  - Visible progression: `_revealedRounds`
  - Current/next match selection logic: `PushNextMatch()`, `PeekUpcomingMatches(...)`
  - UI-visible bracket rows: `BuildCurrentBracketRows()`
- Engine adapters provide normalized match/round data through `IRaceEngine.GetMatches()` and `GetRoundOrder()`; controller wraps those in `EngineGetMatches(...)` and `EngineGetRoundOrder(...)` with consistent logging.

## 2. Best Place to Build Live DTO
- Best class: `Controllers/RaceController` (new partial file recommended, not UI).
- Best method location: a new private builder inside `RaceController`, called from controller state-transition points.
- Recommended new method signature:
  - `private LiveRaceUpdateDto BuildLiveRaceUpdateDto()`
- Why this location is safest:
  - All authoritative state is already in controller fields/methods.
  - Avoids scraping UI labels/buttons.
  - Keeps integration at orchestration layer (minimal architectural impact).
- Safest field sources for DTO:
  - `eventName`: `_session?.EventName` (fallback to current behavior label like `Quick Session` if null/blank).
  - `eventDate`: `_session?.EventDate` formatted `yyyy-MM-dd`.
  - `currentRound`: `GetActiveRoundLabel()` (normalized if needed via `RoundLabels.Normalize`).
  - `nextUp`: derive from controller match truth, not UI text. Use the same query as `PushNextMatch()` (`first unresolved match in revealed rounds`) and lane-adjust through `GetLaneAdjustedNames(...)`.
  - `matches`: derive from `BuildCurrentBracketRows()` and include only non-header rows (`IsHeader == false`) to represent the currently visible bracket state.

## 3. Best Trigger Points for Updates
- Use controller-level triggers after state is fully updated and UI events have been pushed.
- Exact existing methods to hook for POST (end of method):
  - `RaceController.GenerateBracket(string raceType, List<Driver> drivers)`
  - `RaceController.SubmitWinner(int matchId, bool firstOption)`
  - `RaceController.AdvanceRound()`
  - `RaceController.EditWinnerInActiveRound(int matchId, bool firstOption)`
  - `RaceController.StartLosersBracket()`
  - `RaceController.InjectFinal4Bracket()`
  - `RaceController.StartFinalsTop3NoBuyback()`
  - `RaceController.InjectFinalsAllAdvance(List<Driver> rankedDrivers)`
  - `RaceController.Reset()` (optional but recommended if remote should clear live state)
- Do not trigger from UI click handlers in `Form1`; those are presentation/event wiring and can miss non-UI controller transitions.

## 4. Safe HTTP Integration Design
- Proposed service class (no code yet):
  - `src/RCDragManagerProd/Integration/LiveRaceUpdateClient.cs`
- Proposed DTO classes:
  - `src/RCDragManagerProd/Integration/LiveRaceUpdateDto.cs`
  - Contains `EventName`, `EventDate`, `CurrentRound`, `NextUp`, `List<LiveMatchDto> Matches`
  - `LiveMatchDto` contains `Driver1`, `Driver2`
- Controller wiring approach (minimal):
  - Inject/create one service instance inside `RaceController`.
  - Add one private non-blocking publish method in controller, e.g. `QueueLiveUpdate(string reason)`.
  - Each trigger method calls `QueueLiveUpdate(...)` once at end of successful state transition.
- Contract details to enforce in service:
  - `POST https://stewmacrc.com/api/update`
  - Header `X-API-KEY: <your-api-key>`
  - Send full-state payload every time (replacement semantics).

## 5. Failure Handling Strategy
- Non-blocking requirement:
  - HTTP send must be fire-and-forget from race flow perspective; never block UI or controller state transitions.
- Offline-safe behavior:
  - Catch and log all HTTP/network/serialization exceptions in integration service.
  - Do not rethrow into race flow.
- Bad/partial state avoidance:
  - Build DTO only after controller has completed state mutation (`PushNextMatch`, `PushAdvanceState`, etc. already called).
  - If required fields are invalid (for example no event date and no acceptable fallback), skip send and log a clear validation marker.
- Concurrency safety:
  - Serialize outbound sends (single in-flight worker or lock) so updates do not race and overwrite with older snapshots.

## 6. Logging Plan
- Use existing `Logging/Logger.Log(...)` only (already central and resilient).
- Add clear markers around publish lifecycle:
  - `[LIVE][BUILD]` DTO built (include round, nextUp, matchCount)
  - `[LIVE][SKIP]` send skipped due to invalid/incomplete state
  - `[LIVE][SEND]` POST start (reason + endpoint)
  - `[LIVE][OK]` success (status code + latency)
  - `[LIVE][FAIL]` HTTP/network failure (status/exception)
- Best insertion points:
  - In the new controller publish method (`QueueLiveUpdate` / builder call)
  - In `LiveRaceUpdateClient` before/after POST attempt

## 7. Config Plan
- Safest location: `src/RCDragManagerProd/App.config` under existing `<appSettings>` (consistent with current logging config pattern).
- Proposed keys:
  - `LiveUpdateEnabled`
  - `LiveUpdateUrl` (value: `https://stewmacrc.com/api/update`)
  - `LiveUpdateApiKey` (value: provided key)
  - Optional: `LiveUpdateTimeoutMs`
- Reading strategy:
  - Follow existing config pattern used by logging (`Logger` uses `RCDragManagerProd.Config.AppSettings`).
  - If `AppSettings` wrapper already supports extension, add keys there; otherwise read via `ConfigurationManager.AppSettings` in the new integration service.

## 8. Risks / Unknowns
- `UI/Forms/Form1.cs` in this repo is partial; most runtime behavior is in `Form1.Events.cs`, `Form1.Display.cs`, etc. Recommendation above is based on those partials because they are part of `Form1` behavior.
- `eventDate` semantics need confirmation:
  - `RaceSession.EventDate` type is `DateTime`; confirm expected timezone/date source and whether empty/default dates should be sent or skipped.
- `nextUp` exact format needs confirmation:
  - Server example shows a single matchup string (`"Driver A vs Driver B"`), while UI label sometimes shows `On Deck / In The Hole` lines.
  - Recommended for API: send only the current upcoming race pair.
- Round label normalization:
  - Internal labels vary (`R1`, `LB-R1`, `SF`, etc.); confirm if server expects raw internal label or a user-friendly display label.
- HTTP stack location decision needs confirmation:
  - `RCDragManagerProd.Config.AppSettings` implementation was not in requested inspection list, so exact key-access path is inferred from `Logger.cs` usage.
