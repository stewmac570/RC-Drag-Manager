# Live Integration Step 3 Hooks And Testing

## Exact Methods Inspected
- `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs`
  - `GenerateBracket(string raceType, List<Driver> drivers)`
  - `GenerateBracket(string raceType)` (wrapper)
  - `AdvanceRound()`
  - `PushNextMatch()` (reviewed for state readiness sequence)
- `src/RCDragManagerProd/Controllers/RaceController.Session.cs`
  - `Reset()`
- `src/RCDragManagerProd/Controllers/RaceController.LiveUpdate.cs`
  - `BuildLiveRaceUpdateDto()`
  - `QueueLiveUpdate(string reason)`

## Exact Hook Locations Chosen
- `GenerateBracket(string raceType, List<Driver> drivers)`
  - Hook location: immediately after `_winners.Clear(); PushFullRefresh();`
  - Call: `QueueLiveUpdate("GenerateBracket")`
- `AdvanceRound()`
  - Hook location: at end of successful path, after `PushNextMatch(); PushAdvanceState();`
  - Call: `QueueLiveUpdate("AdvanceRound")`

## Why These Locations Are Safe
- Both hook points occur only after controller state mutation + refresh calls are complete.
- DTO build uses controller/session/engine state only; no UI dependency.
- Existing early-return validation paths remain unchanged, so no send occurs for invalid/incomplete transitions.
- Queue path is fire-and-forget and guarded by try/catch, preserving race flow and offline safety.

## Reset Decision
- `Reset()` should remain unchanged in this step.
- Reason:
  - After reset, `_engine = null` and visible state is explicitly cleared.
  - Current live DTO builder requires valid engine/session race state and intentionally skips invalid state (`[LIVE][SKIP]`).
  - There is no explicit confirmed server contract for a "clear" payload in this task; sending guessed empty values risks bad/partial semantics.

## Recommended Smoke Test Steps
1. Start app with valid session/event date and `LiveUpdateEnabled=true`.
2. Generate a bracket with >=2 drivers.
3. Confirm log sequence includes:
   - `[LIVE][BUILD] reason=GenerateBracket`
   - `[LIVE][SEND]`
   - `[LIVE][OK]` or `[LIVE][FAIL]`
4. Submit one winner.
5. Confirm existing step-2 hook still logs:
   - `[LIVE][BUILD] reason=SubmitWinner`
6. Advance round when `Generate Next Round` is enabled.
7. Confirm log sequence includes:
   - `[LIVE][BUILD] reason=AdvanceRound`
   - `[LIVE][SEND]`
8. Disable network (or set unreachable URL) and repeat actions.
9. Confirm no race-flow interruption and `[LIVE][FAIL]` appears without exceptions surfacing to UI flow.
10. Press Reset and confirm no new live send attempt is required for this step; controller reset behavior remains unchanged.

## Risks / Unknowns
- Event date must be set (`_session.EventDate != default`) for live sends; otherwise updates skip with `[LIVE][SKIP]`.
- Round transitions that do not reach the successful end path (for example no more rounds) correctly produce no send in this step.
- Reset clear-state broadcast is intentionally deferred until a dedicated server-side clear contract is confirmed.
